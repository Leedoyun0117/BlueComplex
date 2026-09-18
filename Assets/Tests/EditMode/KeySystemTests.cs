using System.Collections.Generic;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>D. 키 — 등장 턴, 구역 범위, 획득/실패 판정, 클리어 조건, 스테이지 시작 시 전체 확정.</summary>
    public class KeySystemTests
    {
        [Test]
        public void IsKeyTurn_OnlyTrueOn3579()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3, 5, 7, 9 });

            for (var turn = 1; turn <= 10; turn++)
            {
                var expected = turn == 3 || turn == 5 || turn == 7 || turn == 9;
                Assert.AreEqual(expected, keys.IsKeyTurn(turn), $"턴 {turn}");
            }
        }

        [Test]
        public void RandomKeyZonePlacer_AlwaysLandsInLeftOrRightEdge_AcrossManySeeds()
        {
            var layout = new KeyZoneLayout(min: 0, max: 9, edgeWidth: 3, keyWidth: 2);

            for (var seed = 0; seed < 200; seed++)
            {
                var random = new SystemRandomSource(seed);
                var placer = new RandomKeyZonePlacer(random, layout);

                for (var i = 0; i < 20; i++)
                {
                    var zone = placer.Place(startPosition: 5, turnsUntilKey: 10); // 값은 RandomKeyZonePlacer가 쓰지 않는다.
                    var lastSlot = zone.StartSlot + zone.Width - 1;

                    var withinLeft = zone.StartSlot >= 0 && lastSlot <= 2;
                    var withinRight = zone.StartSlot >= 7 && lastSlot <= 9;

                    Assert.IsTrue(withinLeft || withinRight,
                        $"seed={seed} 반복={i}: 구역 [{zone.StartSlot}, {lastSlot}] 이 좌측 0~2 또는 우측 7~9 범위를 벗어났다.");
                }
            }
        }

        [Test]
        public void Judge_IndicatorInsideZone_CollectsKey()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3 });
            var collectedCounts = new List<int>();
            keys.KeyCollected += c => collectedCounts.Add(c);
            var missed = false;
            keys.ZoneMissed += () => missed = true;

            keys.PrepareZones(new Dictionary<int, KeyZone> { { 3, new KeyZone(0, 2) } }); // 0,1 구역
            keys.OpenZone(3);
            keys.Judge(indicatorPosition: 1);

            Assert.AreEqual(1, keys.Collected);
            CollectionAssert.AreEqual(new[] { 1 }, collectedCounts);
            Assert.IsFalse(missed);
            Assert.IsNull(keys.ActiveZone, "판정 후 구역은 닫혀야 한다.");
        }

        [Test]
        public void Judge_IndicatorOutsideZone_Misses()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3 });
            var missed = false;
            keys.ZoneMissed += () => missed = true;

            keys.PrepareZones(new Dictionary<int, KeyZone> { { 3, new KeyZone(0, 2) } }); // 0,1 구역
            keys.OpenZone(3);
            keys.Judge(indicatorPosition: 5);

            Assert.AreEqual(0, keys.Collected);
            Assert.IsTrue(missed);
        }

        [Test]
        public void Judge_WithNoActiveZone_IsNoOp()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3 });
            var missed = false;
            keys.ZoneMissed += () => missed = true;
            var collected = false;
            keys.KeyCollected += _ => collected = true;

            keys.Judge(indicatorPosition: 5); // 열린 구역이 없음

            Assert.IsFalse(missed);
            Assert.IsFalse(collected);
        }

        [Test]
        public void TwoKeysCollected_MarksComplete()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3, 5 });
            keys.PrepareZones(new Dictionary<int, KeyZone>
            {
                { 3, new KeyZone(0, 2) },
                { 5, new KeyZone(7, 2) }
            });

            keys.OpenZone(3);
            keys.Judge(0);
            Assert.IsFalse(keys.IsComplete, "키 1개로는 아직 완료되지 않아야 한다.");

            keys.OpenZone(5);
            keys.Judge(7);
            Assert.IsTrue(keys.IsComplete, "키 2개를 모으면 완료되어야 한다.");
        }

        [Test]
        public void StageStart_PreparesAllKeyZones_ForEveryKeyTurn()
        {
            var random = new SystemRandomSource(1);
            var polarityTable = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarityTable);
            var session = StageFactory.Create(config, random, new ClueKnowledgeLedger(), polarityTable);

            session.Runner.StartStage();

            Assert.AreEqual(config.KeyTurns.Count, session.Keys.Zones.Count,
                "키 턴 개수만큼 구역이 스테이지 시작 시 미리 결정되어 있어야 한다.");
            foreach (var turn in config.KeyTurns)
                Assert.IsTrue(session.Keys.Zones.ContainsKey(turn), $"턴 {turn} 구역이 미리 결정되어 있어야 한다.");
        }

        [Test]
        public void KeyZones_DoNotChange_OnceStageHasStarted()
        {
            var random = new SystemRandomSource(2);
            var polarityTable = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarityTable);
            var session = StageFactory.Create(config, random, new ClueKnowledgeLedger(), polarityTable);

            session.Runner.StartStage();
            var snapshot = new Dictionary<int, KeyZone>(session.Keys.Zones);

            for (var i = 0; i < 5 && session.Runner.Outcome == StageOutcome.InProgress; i++)
                session.Runner.PlayClue(session.Hand.Cards[0]);

            CollectionAssert.AreEquivalent(snapshot.Keys, session.Keys.Zones.Keys);
            foreach (var turn in snapshot.Keys)
                Assert.AreEqual(snapshot[turn], session.Keys.Zones[turn], $"턴 {turn} 구역이 진행 중 바뀌면 안 된다.");
        }

        [Test]
        public void StageStart_KeyZones_AlwaysWithinSurvivableRange_AcrossManySeeds()
        {
            var heartbeatZone = new HeartbeatZone();
            var polarityTable = new DefaultEmotionPolarityTable();

            for (var seed = 0; seed < 100; seed++)
            {
                var random = new SystemRandomSource(seed);
                var config = PrototypeContent.PrototypeStage(polarityTable);
                var session = StageFactory.Create(config, random, new ClueKnowledgeLedger(), polarityTable);

                session.Runner.StartStage();

                foreach (var pair in session.Keys.Zones)
                {
                    var turn = pair.Key;
                    var zone = pair.Value;
                    var lastSlot = zone.StartSlot + zone.Width - 1;

                    Assert.GreaterOrEqual(zone.StartSlot, heartbeatZone.SurvivableMin,
                        $"seed={seed} 턴={turn}: 구역 시작 {zone.StartSlot} 이 생존 구간({heartbeatZone.SurvivableMin}) 아래다.");
                    Assert.LessOrEqual(lastSlot, heartbeatZone.SurvivableMax,
                        $"seed={seed} 턴={turn}: 구역 끝 {lastSlot} 이 생존 구간({heartbeatZone.SurvivableMax})을 넘었다.");

                    for (var pos = zone.StartSlot; pos <= lastSlot; pos++)
                        Assert.IsFalse(heartbeatZone.IsFatal(pos),
                            $"seed={seed} 턴={turn}: 구역 [{zone.StartSlot},{lastSlot}] 이 Fatal 위치 {pos} 를 포함한다.");
                }
            }
        }
    }
}
