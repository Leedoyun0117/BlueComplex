using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Tests
{
    /// <summary>기획서 "아이템과 특성": 논리적 설득 = 중복 감정 정리 + 랜덤 특성 하나 제거(보유 중인 특성 중에서, 특수 특성 포함).</summary>
    public class TraitRemovalTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private sealed class AnyZonePlacer : IKeyZonePlacer
        {
            public KeyZone Place(int startPosition, int turnsUntilKey) => new(150, 10);
        }

        private static ClueDefinition Clue(string id, params EmotionTag[] emotions) =>
            new(id, id, "", TimeTag.None, Array.Empty<PersonTag>(), emotions);

        private static ItemDefinition Item(string id) => PrototypeContent.Items().First(i => i.Id == id);

        private static StageSession Start(IReadOnlyList<ItemDefinition> items, ComplexDefinition starting = null, int seed = 1)
        {
            var config = new StageConfig("test", "test", quarterCount: 3, turnsPerQuarter: 4, requiredKeys: 2, complexWeight: 0.0,
                clues: Enumerable.Range(0, 12).Select(i => Clue($"c{i}", EmotionTag.Happiness, EmotionTag.Happiness)).ToList(),
                complexPool: Array.Empty<ComplexDefinition>(),
                startingComplex: starting,
                itemPool: items,
                keyWidth: KeyZoneLayout.DefaultKeyWidth,
                maxComplexSlots: 3,
                traits: PrototypeContent.Traits(),
                itemSlots: items.Count,
                itemParameters: PrototypeContent.PrototypeStage(Polarity).ItemParameters);
            var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), Polarity,
                heartbeatStartValue: Heartbeat.DefaultStartValue, keyPlacer: new AnyZonePlacer());
            session.Runner.StartStage();
            return session;
        }

        // ------------------------------------------------------------------
        // TraitBoard.RemoveRandom
        // ------------------------------------------------------------------

        [Test]
        public void RemoveRandom_RemovesExactlyOneHeldTrait_AndRaisesRemovedAndExpired()
        {
            var board = new TraitBoard(PrototypeContent.Traits(), new SystemRandomSource(3));
            board.Grant(PrototypeContent.TraitSensitive);
            board.Grant(PrototypeContent.TraitLethargy);
            var removedEvents = new List<string>();
            var expiredEvents = new List<string>();
            board.Removed += t => removedEvents.Add(t.Definition.Id);
            board.Expired += t => expiredEvents.Add(t.Definition.Id);

            var removed = board.RemoveRandom();

            Assert.IsNotNull(removed);
            Assert.AreEqual(1, board.Traits.Count);
            Assert.IsFalse(board.Has(removed.Definition));
            CollectionAssert.AreEqual(new[] { removed.Definition.Id }, removedEvents);
            CollectionAssert.AreEqual(new[] { removed.Definition.Id }, expiredEvents);
        }

        [Test]
        public void RemoveRandom_WithNoTraits_DoesNothing_EvenWithoutARandomSource()
        {
            var board = new TraitBoard(PrototypeContent.Traits());

            Assert.IsNull(board.RemoveRandom());
        }

        [Test]
        public void RemoveRandom_PicksEveryHeldTraitEventually_IncludingSpecialOnes()
        {
            var picked = new HashSet<string>();
            for (var seed = 0; seed < 60; seed++)
            {
                var board = new TraitBoard(PrototypeContent.Traits(), new SystemRandomSource(seed));
                board.Grant(PrototypeContent.TraitSensitive);
                board.Grant(PrototypeContent.TraitHighFunctioningDepression);
                picked.Add(board.RemoveRandom().Definition.Id);
            }

            CollectionAssert.AreEquivalent(new[] { PrototypeContent.TraitSensitive, PrototypeContent.TraitHighFunctioningDepression }, picked);
        }

        // ------------------------------------------------------------------
        // 논리적 설득 아이템
        // ------------------------------------------------------------------

        [Test]
        public void Logic_RemovesOneHeldTrait_AndStillCollapsesDuplicates()
        {
            var logic = Item("item_logic");
            var session = Start(new[] { logic });
            session.Traits.Grant(PrototypeContent.TraitSensitive);
            session.Traits.Grant(PrototypeContent.TraitLethargy);

            session.Runner.UseItem(logic);

            Assert.AreEqual(1, session.Traits.Traits.Count, "붙어 있던 특성 둘 중 하나가 지워진다.");
            var report = session.Runner.PlayClue(session.Hand.Cards[0]); // 행복 ×2 단서 — 중복이 접혀 행복 1개
            Assert.AreEqual(1, report.FinalTags.CountOf(EmotionTag.Happiness), "기존 효과(중복 감정 정리)는 그대로다.");
        }

        [Test]
        public void Logic_WithNoTraits_IsStillUsable_AndGrantsNothing()
        {
            var logic = Item("item_logic");
            var session = Start(new[] { logic });

            Assert.IsTrue(session.Runner.CanUseItem(logic));
            session.Runner.UseItem(logic);

            Assert.AreEqual(0, session.Traits.Traits.Count);
        }

        [Test]
        public void Logic_CanRemoveASpecialTrait()
        {
            var logic = Item("item_logic");
            var session = Start(new[] { logic });
            session.Traits.Grant(PrototypeContent.TraitHyperexcitement);

            session.Runner.UseItem(logic);

            Assert.IsFalse(session.Traits.HasSpecial);
        }

        [Test]
        public void TraitRemovedByAnItem_BeforeTheResult_IsNotListedAsManifested()
        {
            var overcome = Item("item_overcome");
            var logic = Item("item_logic");
            var session = Start(new[] { overcome, logic }, starting: PrototypeContent.Optimism());

            session.Runner.UseItem(overcome, ItemTarget.Of(session.Complexes.Slots[0])); // 예민 발현
            session.Runner.UseItem(logic);                                                // 유일한 특성이라 예민이 지워진다
            var report = session.Runner.PlayClue(session.Hand.Cards[0]);

            Assert.IsFalse(session.Traits.Has(PrototypeContent.TraitSensitive));
            Assert.IsEmpty(report.TraitsManifested, "결과에 한 번도 걸리지 않은 특성은 결과 화면에 뜨지 않는다.");
        }
    }
}
