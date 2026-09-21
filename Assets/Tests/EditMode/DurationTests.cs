using System;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Tests
{
    /// <summary>F. 지속 시간 — 컴플렉스/특성/아이템 효과가 지정된 턴 수만큼만 유지되고, 만료가 정확히 한 번 발생하는지.</summary>
    public class DurationTests
    {
        [Test]
        public void Complex_ExpiresAfterExactlyItsDuration_AndFiresExpiredOnce()
        {
            var board = new ComplexBoard();
            var def = PrototypeContent.AntiPast(new DefaultEmotionPolarityTable());
            Assert.AreEqual(3, def.DefaultDuration, "기획서: 반 과거 컴플렉스 지속 시간 3턴.");
            var instance = new ComplexInstance(def, priority: 0);
            board.TryAttach(instance);

            var expiredCount = 0;
            board.Expired += _ => expiredCount++;

            for (var turn = 1; turn <= def.DefaultDuration; turn++)
            {
                CollectionAssert.Contains(board.Slots, instance,
                    $"턴 {turn} 해석 시점에는 아직 컴플렉스가 붙어 있어야 한다.");
                board.TickDurations();
            }

            CollectionAssert.DoesNotContain(board.Slots, instance, "지속 시간이 지나면 컴플렉스는 사라져야 한다.");
            Assert.AreEqual(1, expiredCount, "만료 이벤트는 정확히 한 번만 발생해야 한다.");
        }

        [Test]
        public void Trait_ActiveMultiplierLastsExactlyDurationTurns_ThenExpiresOnce()
        {
            var traits = new TraitBoard();
            traits.Grant(TraitType.Sensitive, duration: 3);

            var expiredCount = 0;
            traits.Expired += _ => expiredCount++;

            for (var turn = 1; turn <= 3; turn++)
            {
                Assert.AreEqual(3, traits.SensitivityMultiplier, $"턴 {turn} 해석 시점에는 배율이 적용되어야 한다.");
                traits.TickDurations();
            }

            Assert.AreEqual(1, traits.SensitivityMultiplier, "만료 후에는 배율이 1로 돌아와야 한다.");
            Assert.IsFalse(traits.Has(TraitType.Sensitive));
            Assert.AreEqual(1, expiredCount);
        }

        [Test]
        public void ActiveItem_ExpiresAfterExactlyItsDuration_AndFiresExpiredOnce()
        {
            var activeItems = new ActiveItemBoard();
            var def = new ItemDefinition("item_test", "테스트 아이템", "", duration: 2, new Sedative());

            var expiredCount = 0;
            activeItems.Expired += _ => expiredCount++;

            // ItemInventory.Use 가 하는 일을 그대로 재현한다: 발동 + (duration>0 이면) 등록.
            def.Behaviour.OnActivate(new ItemActivationContext(
                new ClueHand(new CluePool(Array.Empty<ClueDefinition>(), new SystemRandomSource(0))),
                new TraitBoard(),
                activeItems));
            activeItems.Register(def);

            for (var turn = 1; turn <= 2; turn++)
            {
                Assert.AreEqual(1, activeItems.Active.Count, $"턴 {turn} 시점에는 아이템 효과가 살아있어야 한다.");
                activeItems.TickDurations();
            }

            Assert.AreEqual(0, activeItems.Active.Count, "2턴 경과 후 아이템 효과는 사라져야 한다.");
            Assert.AreEqual(1, expiredCount);
        }

        [Test]
        public void ActiveItemBoard_ClearsFiltersAndModifiers_WhenLastActiveItemExpires()
        {
            var activeItems = new ActiveItemBoard();
            var tagger = new IdListDepressionTagger("complex_anti_past");
            var persuasion = new EmotionalPersuasion(tagger);
            var def = new ItemDefinition("item_persuasion", "감정적 설득", "", duration: 1, persuasion);

            def.Behaviour.OnActivate(new ItemActivationContext(
                new ClueHand(new CluePool(Array.Empty<ClueDefinition>(), new SystemRandomSource(0))),
                new TraitBoard(),
                activeItems));
            activeItems.Register(def);

            var targetComplex = new ComplexInstance(
                PrototypeContent.AntiPast(new DefaultEmotionPolarityTable()), priority: 0);

            Assert.IsTrue(activeItems.ShouldIgnore(targetComplex), "지속 중에는 반 과거를 무시해야 한다.");

            activeItems.TickDurations(); // duration=1 이므로 이 한 번으로 만료된다.

            Assert.AreEqual(0, activeItems.Active.Count);
            Assert.IsFalse(activeItems.ShouldIgnore(targetComplex), "만료 후에는 필터가 사라져 더 이상 무시하지 않아야 한다.");
        }
    }
}
