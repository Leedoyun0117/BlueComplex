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
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>G. 스테이지 종료와 쿼터 경계 — 키는 쿼터 마지막 턴에만 판정, 쿼터 시작 때만 손패 보충, 마지막 턴 소진 시 Failed, 키 완성 시 즉시 Cleared.</summary>
    public class StageOutcomeTests
    {
        private sealed class NeverSpawnPolicy : IComplexSpawnPolicy
        {
            public bool ShouldSpawn(int heartbeatValue) => false;
        }

        /// <summary>구역 폭이 전체 범위를 덮어, 심박수 값과 무관하게 항상 키를 획득시키는 테스트용 배치자.</summary>
        private sealed class AlwaysHitPlacer : IKeyZonePlacer
        {
            private readonly int _width;
            public AlwaysHitPlacer(int width) => _width = width;
            public KeyZone Place(int startPosition, int turnsUntilKey) => new(0, _width);
        }

        /// <summary>시작 심박수(80)에서 멀리 떨어진 좁은 구역 — 중립 단서만 내면 절대 키를 못 얻는다.</summary>
        private sealed class NeverHitPlacer : IKeyZonePlacer
        {
            public KeyZone Place(int startPosition, int turnsUntilKey) => new(150, 10);
        }

        private static ClueDefinition NeutralClue(string id) =>
            new(id, id, "", TimeTag.None, Array.Empty<PersonTag>(), Array.Empty<EmotionTag>());

        private static QuarterSchedule Prototype => new(quarterCount: 3, turnsPerQuarter: 4);

        // TurnRunner는 내부 ClueHand를 외부에 노출하지 않으므로(StageSession을 통해서만 노출된다),
        // 수동 조립 테스트에서는 hand를 별도로 들고 있어야 카드를 낼 수 있다.
        private static (TurnRunner Runner, ClueHand Hand, ComplexBoard Board) BuildRunner(KeyProgress keys, IKeyZonePlacer placer, int clueCount = 40)
        {
            var random = new SystemRandomSource(123);
            var clueDefs = Enumerable.Range(0, clueCount).Select(i => NeutralClue($"c{i}")).ToList();
            var pool = new CluePool(clueDefs, random);
            var hand = new ClueHand(pool);

            var complexBoard = new ComplexBoard();
            var resolver = new ComplexResolver(complexBoard);
            var spawner = new ComplexSpawner(Array.Empty<ComplexDefinition>(), random);

            var heartbeat = new Heartbeat();
            var zone = new HeartbeatZone();
            var evaluator = new EmotionEvaluator(new DefaultEmotionPolarityTable());

            var items = new ItemInventory(Array.Empty<ItemDefinition>(), random);
            var activeItems = new ActiveItemBoard();
            var traits = new TraitBoard();

            var ledger = new ClueKnowledgeLedger();

            var runner = new TurnRunner(hand, complexBoard, resolver, spawner, new NeverSpawnPolicy(),
                heartbeat, zone, evaluator, items, activeItems, traits, keys, placer, ledger);
            return (runner, hand, complexBoard);
        }

        private static void PlayUntilEnd(TurnRunner runner, ClueHand hand, int guard = 100)
        {
            while (runner.Outcome == StageOutcome.InProgress && guard-- > 0)
                runner.PlayClue(hand.Cards[0]);
        }

        [Test]
        public void AllTurnsUsedWithoutEnoughKeys_ResultsInFailed()
        {
            var keys = new KeyProgress(required: 2, Prototype);
            var (runner, hand, _) = BuildRunner(keys, new NeverHitPlacer());

            runner.StartStage();
            PlayUntilEnd(runner, hand);

            Assert.AreEqual(StageOutcome.Failed, runner.Outcome);
            Assert.AreEqual(12, runner.CurrentTurn);
            Assert.AreEqual(0, keys.Collected);
            CollectionAssert.AreEqual(new bool?[] { false, false, false }, keys.Results);
        }

        [Test]
        public void TwoKeysCollected_ResultsInImmediateCleared_AtTheQuarterEndThatCompletesIt()
        {
            var keys = new KeyProgress(required: 2, Prototype);
            var (runner, hand, _) = BuildRunner(keys, new AlwaysHitPlacer(201));

            runner.StartStage();
            PlayUntilEnd(runner, hand);

            Assert.AreEqual(StageOutcome.Cleared, runner.Outcome);
            Assert.AreEqual(8, runner.CurrentTurn, "2쿼터 종료 시점에 키 2개가 차면 그 즉시 멈추고 3쿼터를 진행하지 않아야 한다.");
            Assert.IsTrue(keys.IsComplete);
        }

        [Test]
        public void StageEndedEvent_FiresExactlyOnce_WithFinalOutcome()
        {
            var keys = new KeyProgress(required: 2, Prototype);
            var (runner, hand, _) = BuildRunner(keys, new AlwaysHitPlacer(201));

            var endedOutcomes = new List<StageOutcome>();
            runner.StageEnded += o => endedOutcomes.Add(o);

            runner.StartStage();
            PlayUntilEnd(runner, hand);

            Assert.AreEqual(1, endedOutcomes.Count);
            Assert.AreEqual(StageOutcome.Cleared, endedOutcomes[0]);
        }

        [Test]
        public void Keys_AreJudgedOnlyOnTheLastTurnOfAQuarter()
        {
            // 구역이 심박수 전체를 덮으므로 "그 자리에 있기만 하면" 언제든 키가 나온다 — 중간 턴에서는 판정하지 않아야 한다.
            var keys = new KeyProgress(required: 3, Prototype);
            var (runner, hand, _) = BuildRunner(keys, new AlwaysHitPlacer(201));
            var judgedOnTurns = new List<int>();
            runner.TurnResolved += report =>
            {
                if (report.KeyResult != null) judgedOnTurns.Add(report.Turn);
            };

            runner.StartStage();
            var collectedAfterTurn = new List<int>();
            for (var i = 0; i < 12 && runner.Outcome == StageOutcome.InProgress; i++)
            {
                runner.PlayClue(hand.Cards[0]);
                collectedAfterTurn.Add(keys.Collected);
            }

            CollectionAssert.AreEqual(new[] { 4, 8, 12 }, judgedOnTurns);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3 }, collectedAfterTurn);
        }

        [Test]
        public void Hand_IsRefilledOnlyAtQuarterStart_AndShrinksWithinTheQuarter()
        {
            var keys = new KeyProgress(required: 3, Prototype);
            var (runner, hand, _) = BuildRunner(keys, new NeverHitPlacer());
            var handSizeAtTurnBegin = new List<int>();
            runner.TurnBegan += _ => handSizeAtTurnBegin.Add(hand.Cards.Count);

            runner.StartStage();
            PlayUntilEnd(runner, hand);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1, 4, 3, 2, 1, 4, 3, 2, 1 }, handSizeAtTurnBegin,
                "쿼터 시작에만 4장으로 채워지고, 쿼터 중에는 낸 만큼 줄어들어야 한다.");
        }

        [Test]
        public void QuarterStructure_ComesFromTheScheduleNotFromConstants()
        {
            var schedule = new QuarterSchedule(quarterCount: 2, turnsPerQuarter: 3);
            var keys = new KeyProgress(required: 2, schedule);
            var (runner, hand, _) = BuildRunner(keys, new AlwaysHitPlacer(201));
            var judgedOnTurns = new List<int>();
            var handSizeAtQuarterStart = new List<int>();
            runner.TurnResolved += report =>
            {
                if (report.KeyResult != null) judgedOnTurns.Add(report.Turn);
            };
            runner.TurnBegan += turn =>
            {
                if (schedule.IsQuarterStart(turn)) handSizeAtQuarterStart.Add(hand.Cards.Count);
            };

            runner.StartStage();
            PlayUntilEnd(runner, hand);

            CollectionAssert.AreEqual(new[] { 3, 6 }, judgedOnTurns);
            CollectionAssert.AreEqual(new[] { 4, 4 }, handSizeAtQuarterStart);
            Assert.AreEqual(6, runner.TotalTurns);
            Assert.AreEqual(StageOutcome.Cleared, runner.Outcome);
        }

        [Test]
        public void ComplexDuration_AccumulatesAcrossQuarterBoundaries_WithoutReset()
        {
            var keys = new KeyProgress(required: 3, Prototype);
            var (runner, hand, board) = BuildRunner(keys, new NeverHitPlacer());
            var complex = new ComplexInstance(PrototypeContent.Stockholm(), priority: 0, duration: 6);
            board.TryAttach(complex);

            runner.StartStage();
            for (var i = 0; i < 4; i++) runner.PlayClue(hand.Cards[0]); // 1쿼터 종료

            Assert.AreEqual(2, complex.RemainingTurns, "쿼터가 끝나도 남은 지속 시간은 리셋되지 않고 누적 감소한다.");
            CollectionAssert.Contains(board.Slots, complex);

            runner.PlayClue(hand.Cards[0]);
            runner.PlayClue(hand.Cards[0]);
            CollectionAssert.DoesNotContain(board.Slots, complex, "6턴이 지나면 쿼터 경계와 무관하게 만료된다.");
        }

        [Test]
        public void ExhaustedPool_PassesCluelessTurnEveryQuarter_AndStillJudgesEachQuarter()
        {
            // 단서 정의가 손패 크기(4)보다 적은 3개뿐 — 쿼터 시작마다 ClueHand.RefillForNewQuarter가 지난 쿼터에
            // 낸 단서를 전부 되돌려도 3장만 채워지므로, 매 쿼터의 마지막 턴(4/8/12)은 손패가 비어 자동으로
            // 넘어간다. 그 턴에도 쿼터 판정은 그대로 이뤄져야 한다.
            var keys = new KeyProgress(required: 3, Prototype);
            var (runner, hand, _) = BuildRunner(keys, new AlwaysHitPlacer(201), clueCount: 3);
            var reports = new List<TurnReport>();
            runner.TurnResolved += reports.Add;

            runner.StartStage();
            for (var quarter = 0; quarter < 3; quarter++)
                for (var i = 0; i < 3; i++) runner.PlayClue(hand.Cards[0]); // 쿼터당 3장 — 4번째 턴은 자동으로 넘어간다.

            Assert.AreEqual(12, reports.Count, "쿼터마다 3장을 낸 뒤 4번째 턴은 자동으로 넘어가 보고서가 나온다.");

            var passes = reports.Where(r => r.IsPass).ToList();
            Assert.AreEqual(3, passes.Count, "매 쿼터의 마지막 턴이 넘어간다.");
            foreach (var pass in passes)
            {
                Assert.IsNull(pass.Clue);
                Assert.AreEqual(0, pass.HeartbeatDelta);
                Assert.IsNotNull(pass.KeyResult, "넘어간 턴이 쿼터의 마지막 턴이면 그 쿼터의 키를 판정한다.");
            }

            var last = reports[^1];
            Assert.IsTrue(last.IsPass);
            Assert.AreEqual(3, last.KeyResult.Value.Quarter);
            Assert.AreEqual(StageOutcome.Cleared, runner.Outcome);
            Assert.AreEqual(3, keys.Collected);
        }

        [Test]
        public void ExhaustedPool_WithoutEnoughKeys_ResultsInFailedAfterPassedTurns()
        {
            var keys = new KeyProgress(required: 2, Prototype);
            var (runner, hand, _) = BuildRunner(keys, new NeverHitPlacer(), clueCount: 3);
            var endedOutcomes = new List<StageOutcome>();
            runner.StageEnded += endedOutcomes.Add;

            runner.StartStage();
            for (var quarter = 0; quarter < 3; quarter++)
                for (var i = 0; i < 3; i++) runner.PlayClue(hand.Cards[0]);

            Assert.AreEqual(StageOutcome.Failed, runner.Outcome);
            Assert.AreEqual(12, runner.CurrentTurn);
            Assert.AreEqual(1, endedOutcomes.Count);
        }

        [Test]
        public void StageConfig_RejectsMoreRequiredKeysThanQuarters()
        {
            Assert.Throws<ArgumentException>(() => new StageConfig("s", "s", quarterCount: 3, turnsPerQuarter: 4,
                requiredKeys: 4, complexWeight: 1.0, clues: Array.Empty<ClueDefinition>(),
                complexPool: Array.Empty<ComplexDefinition>(), startingComplex: null, itemPool: Array.Empty<ItemDefinition>()));
        }

        [TestCase(1, 1, 1)]
        [TestCase(4, 1, 4)]
        [TestCase(5, 2, 1)]
        [TestCase(8, 2, 4)]
        [TestCase(9, 3, 1)]
        [TestCase(12, 3, 4)]
        public void QuarterSchedule_MapsTurnsToQuarterAndPosition(int turn, int quarter, int turnInQuarter)
        {
            var schedule = Prototype;

            Assert.AreEqual(quarter, schedule.QuarterOf(turn));
            Assert.AreEqual(turnInQuarter, schedule.TurnInQuarter(turn));
            Assert.AreEqual(turnInQuarter == 1, schedule.IsQuarterStart(turn));
            Assert.AreEqual(turnInQuarter == 4, schedule.IsQuarterEnd(turn));
        }
    }
}
