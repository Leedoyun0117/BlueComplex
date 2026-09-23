using System;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>컴플렉스 최대 중첩(뇌 인터페이스가 3분할이라 4 → 3) — 보드의 상한, 스테이지 설정에서 읽기, 가득 찬 보드에서 새 컴플렉스가 나타날 차례가 왔을 때의 "넘침" 보고.</summary>
    public class ComplexStackLimitTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private sealed class AlwaysSpawnPolicy : IComplexSpawnPolicy
        {
            public bool ShouldSpawn(int heartbeatValue) => true;
        }

        private sealed class NeverHitPlacer : IKeyZonePlacer
        {
            public KeyZone Place(int startPosition, int turnsUntilKey) => new(150, 10);
        }

        private static ComplexInstance Attached(ComplexDefinition definition, int priority) =>
            new(definition, priority, duration: 99);

        private static (TurnRunner Runner, ClueHand Hand, ComplexBoard Board) BuildRunner(ComplexBoard board, ComplexDefinition spawnable)
        {
            var random = new SystemRandomSource(1);
            var clues = Enumerable.Range(0, 40)
                .Select(i => new ClueDefinition($"c{i}", $"c{i}", "", TimeTag.None, Array.Empty<PersonTag>(), Array.Empty<EmotionTag>()))
                .ToList();
            var hand = new ClueHand(new CluePool(clues, random));
            var schedule = new QuarterSchedule(quarterCount: 3, turnsPerQuarter: 4);

            var runner = new TurnRunner(hand, board, new ComplexResolver(board),
                new ComplexSpawner(new[] { spawnable }, random), new AlwaysSpawnPolicy(),
                new Heartbeat(), new HeartbeatZone(), new EmotionEvaluator(Polarity),
                new ItemInventory(Array.Empty<ItemDefinition>(), random), new ActiveItemBoard(), new TraitBoard(),
                new KeyProgress(required: 2, schedule), new NeverHitPlacer(), new ClueKnowledgeLedger());
            return (runner, hand, board);
        }

        [Test]
        public void Board_DefaultsToThreeSlots_AndRejectsTheFourth()
        {
            var board = new ComplexBoard();

            Assert.AreEqual(3, ComplexBoard.DefaultMaxSlots);
            Assert.AreEqual(3, board.MaxSlots);

            Assert.IsTrue(board.TryAttach(Attached(PrototypeContent.GoodChild(Polarity), 0)));
            Assert.IsTrue(board.TryAttach(Attached(PrototypeContent.AntiPast(Polarity), 1)));
            Assert.IsFalse(board.IsFull);
            Assert.IsTrue(board.TryAttach(Attached(PrototypeContent.ChildhoodFriend(), 2)));

            Assert.IsTrue(board.IsFull);
            Assert.IsFalse(board.TryAttach(Attached(PrototypeContent.Optimism(), 3)), "세 개가 붙은 보드는 네 번째를 받지 않는다.");
            Assert.AreEqual(3, board.Slots.Count);
        }

        [Test]
        public void Board_WithoutAtLeastOneSlot_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ComplexBoard(0));
        }

        [Test]
        public void StageConfig_DefaultsToThree_AndTheSessionBoardFollowsIt()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            Assert.AreEqual(3, config.MaxComplexSlots);

            var session = StageFactory.Create(config, new SystemRandomSource(1), new ClueKnowledgeLedger(), Polarity);
            Assert.AreEqual(3, session.Complexes.MaxSlots);

            var wider = new StageConfig(config.Id, config.DisplayName, config.Quarters.QuarterCount, config.Quarters.TurnsPerQuarter,
                config.RequiredKeys, config.ComplexWeight, config.Clues, config.ComplexPool, config.StartingComplex, config.ItemPool,
                config.KeyWidth, maxComplexSlots: 4);
            Assert.AreEqual(4, StageFactory.Create(wider, new SystemRandomSource(1), new ClueKnowledgeLedger(), Polarity).Complexes.MaxSlots);
        }

        [Test]
        public void SpawnRollOnAFullBoard_IsReportedAsOverflow_AndAttachesNothing()
        {
            var board = new ComplexBoard();
            board.TryAttach(Attached(PrototypeContent.GoodChild(Polarity), 0));
            board.TryAttach(Attached(PrototypeContent.AntiPast(Polarity), 1));
            board.TryAttach(Attached(PrototypeContent.ChildhoodFriend(), 2));
            var (runner, hand, _) = BuildRunner(board, PrototypeContent.Optimism());

            runner.StartStage();
            var report = runner.PlayClue(hand.Cards[0]);

            Assert.IsTrue(report.ComplexOverflowed, "최대 중첩(3)에 가득 찬 채로 새 컴플렉스가 나타날 차례가 오면 넘침이다.");
            Assert.IsNull(report.SpawnedComplex);
            Assert.AreEqual(3, board.Slots.Count);
        }

        [Test]
        public void SpawnRollWithARoomySlot_SpawnsInsteadOfOverflowing()
        {
            var board = new ComplexBoard();
            board.TryAttach(Attached(PrototypeContent.GoodChild(Polarity), 0));
            board.TryAttach(Attached(PrototypeContent.AntiPast(Polarity), 1));
            var (runner, hand, _) = BuildRunner(board, PrototypeContent.Optimism());

            runner.StartStage();
            var report = runner.PlayClue(hand.Cards[0]);

            Assert.IsFalse(report.ComplexOverflowed);
            Assert.IsNotNull(report.SpawnedComplex);
            Assert.AreEqual(3, board.Slots.Count);
        }
    }
}
