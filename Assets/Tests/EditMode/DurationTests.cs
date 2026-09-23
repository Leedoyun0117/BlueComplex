using System;
using System.Collections.Generic;
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
            var traits = new TraitBoard(PrototypeContent.Traits());
            traits.Grant(PrototypeContent.TraitSensitive, duration: 3);

            var expiredCount = 0;
            traits.Expired += _ => expiredCount++;

            for (var turn = 1; turn <= 3; turn++)
            {
                Assert.AreEqual(3.0, traits.HeartbeatMultiplier, 1e-9, $"턴 {turn} 해석 시점에는 배율이 적용되어야 한다.");
                traits.TickDurations();
            }

            Assert.AreEqual(1.0, traits.HeartbeatMultiplier, 1e-9, "만료 후에는 배율이 1로 돌아와야 한다.");
            Assert.IsFalse(traits.Has(PrototypeContent.TraitSensitive));
            Assert.AreEqual(1, expiredCount);
        }

        private static ItemActivationContext Context(ActiveItemBoard activeItems, IReadOnlyDictionary<string, IReadOnlyList<string>> parameters = null) =>
            new(new ClueHand(new CluePool(Array.Empty<ClueDefinition>(), new SystemRandomSource(0))),
                new TraitBoard(), activeItems, stageParameters: parameters);

        private static ItemInventory HoldOnly(ItemDefinition definition)
        {
            var inventory = new ItemInventory(new[] { definition }, new SystemRandomSource(0), capacity: 1);
            inventory.Refill();
            return inventory;
        }

        [Test]
        public void ActiveItem_ExpiresAfterExactlyItsDuration_AndFiresExpiredOnce()
        {
            var activeItems = new ActiveItemBoard();
            var def = new ItemDefinition("item_test", "테스트 아이템", "", duration: 2, new CollapseDuplicateEmotions());

            var expiredCount = 0;
            activeItems.Expired += _ => expiredCount++;

            HoldOnly(def).Use(def, Context(activeItems)); // 발동 + (duration>0 이면) 등록

            for (var turn = 1; turn <= 2; turn++)
            {
                Assert.AreEqual(1, activeItems.Active.Count, $"턴 {turn} 시점에는 아이템 효과가 살아있어야 한다.");
                activeItems.TickDurations();
            }

            Assert.AreEqual(0, activeItems.Active.Count, "2턴 경과 후 아이템 효과는 사라져야 한다.");
            Assert.AreEqual(1, expiredCount);
        }

        [Test]
        public void ActiveItemBoard_RemovesOnlyTheExpiredItemsEffects_WhenItemsOverlap()
        {
            var activeItems = new ActiveItemBoard();
            var parameters = new Dictionary<string, IReadOnlyList<string>>
            {
                [PrototypeContent.PersuasionTargetsKey] = new[] { "complex_anti_past" }
            };
            var persuasion = new ItemDefinition("item_persuasion", "감정적 설득", "", duration: 1,
                new IgnoreStageComplexes(PrototypeContent.PersuasionTargetsKey));
            var logic = new ItemDefinition("item_logic", "논리적 설득", "", duration: 2, new CollapseDuplicateEmotions());

            HoldOnly(persuasion).Use(persuasion, Context(activeItems, parameters));
            HoldOnly(logic).Use(logic, Context(activeItems, parameters));

            var targetComplex = new ComplexInstance(
                PrototypeContent.AntiPast(new DefaultEmotionPolarityTable()), priority: 0);
            var doubled = new TagSet(emotions: new[] { EmotionTag.Sadness, EmotionTag.Sadness });

            Assert.IsTrue(activeItems.ShouldIgnore(targetComplex), "지속 중에는 지정한 컴플렉스를 무시해야 한다.");

            activeItems.TickDurations(); // 감정적 설득(1턴)만 만료된다.

            Assert.AreEqual(1, activeItems.Active.Count);
            Assert.IsFalse(activeItems.ShouldIgnore(targetComplex), "만료 후에는 필터가 사라져 더 이상 무시하지 않아야 한다.");

            activeItems.Modify(doubled);
            Assert.AreEqual(1, doubled.CountOf(EmotionTag.Sadness), "아직 살아 있는 논리적 설득의 보정은 그대로 남아야 한다.");
        }
    }
}
