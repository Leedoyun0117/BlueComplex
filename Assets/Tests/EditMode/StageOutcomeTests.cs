using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>G. 스테이지 종료 — 턴 소진 시 Failed, 키 완성 시 즉시 Cleared.</summary>
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

        private static ClueDefinition NeutralClue(string id) =>
            new(id, id, "", TimeTag.None, Array.Empty<PersonTag>(), Array.Empty<EmotionTag>());

        // TurnRunner는 내부 ClueHand를 외부에 노출하지 않으므로(StageSession을 통해서만 노출된다),
        // 수동 조립 테스트에서는 hand를 별도로 들고 있어야 카드를 낼 수 있다.
        private static (TurnRunner Runner, ClueHand Hand) BuildRunner(int totalTurns, KeyProgress keys, IKeyZonePlacer placer, int clueCount = 40)
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
                heartbeat, zone, evaluator, items, activeItems, traits, keys, placer, ledger, totalTurns);
            return (runner, hand);
        }

        [Test]
        public void TenTurnsUsedWithoutEnoughKeys_ResultsInFailed()
        {
            // keyTurns 를 비워 두면 구역이 한 번도 열리지 않으므로 키를 모을 수 없다.
            var keys = new KeyProgress(required: 2, keyTurns: Array.Empty<int>());
            var (runner, hand) = BuildRunner(totalTurns: 10, keys, new AlwaysHitPlacer(201));

            runner.StartStage();

            for (var i = 0; i < 10 && runner.Outcome == StageOutcome.InProgress; i++)
                runner.PlayClue(hand.Cards[0]);

            Assert.AreEqual(StageOutcome.Failed, runner.Outcome);
            Assert.AreEqual(10, runner.CurrentTurn);
            Assert.AreEqual(0, keys.Collected);
        }

        [Test]
        public void TwoKeysCollectedBeforeLastTurn_ResultsInImmediateCleared()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 1, 2 });
            var (runner, hand) = BuildRunner(totalTurns: 10, keys, new AlwaysHitPlacer(201));

            runner.StartStage();

            var turns = 0;
            while (runner.Outcome == StageOutcome.InProgress && turns < 10)
            {
                runner.PlayClue(hand.Cards[0]);
                turns++;
            }

            Assert.AreEqual(StageOutcome.Cleared, runner.Outcome);
            Assert.AreEqual(2, runner.CurrentTurn, "키 2개를 다 모으면 그 즉시 멈추고 남은 턴을 소진하지 않아야 한다.");
            Assert.IsTrue(keys.IsComplete);
        }

        [Test]
        public void StageEndedEvent_FiresExactlyOnce_WithFinalOutcome()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 1, 2 });
            var (runner, hand) = BuildRunner(totalTurns: 10, keys, new AlwaysHitPlacer(201));

            var endedOutcomes = new List<StageOutcome>();
            runner.StageEnded += o => endedOutcomes.Add(o);

            runner.StartStage();
            for (var i = 0; i < 10 && runner.Outcome == StageOutcome.InProgress; i++)
                runner.PlayClue(hand.Cards[0]);

            Assert.AreEqual(1, endedOutcomes.Count);
            Assert.AreEqual(StageOutcome.Cleared, endedOutcomes[0]);
        }
    }
}
