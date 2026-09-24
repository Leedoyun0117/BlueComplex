using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>D. 키 — 판정 턴(쿼터 마지막 턴), 구역 범위, 획득/실패 판정, 클리어 조건, 스테이지 시작 시 전체 확정.</summary>
    public class KeySystemTests
    {
        [Test]
        public void IsKeyTurn_OnlyTrueOnQuarterEnds()
        {
            var keys = new KeyProgress(required: 2, new QuarterSchedule(quarterCount: 3, turnsPerQuarter: 4));

            for (var turn = 1; turn <= 12; turn++)
            {
                var expected = turn == 4 || turn == 8 || turn == 12;
                Assert.AreEqual(expected, keys.IsKeyTurn(turn), $"턴 {turn}");
            }

            CollectionAssert.AreEqual(new[] { 4, 8, 12 }, keys.KeyTurns, "판정 턴 목록은 각 쿼터의 마지막 턴이어야 한다.");
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

        private static KeyProgress SingleQuarterKeys(KeyZone zone)
        {
            var keys = new KeyProgress(required: 1, new QuarterSchedule(quarterCount: 1, turnsPerQuarter: 4));
            keys.PrepareZones(new Dictionary<int, KeyZone> { { 4, zone } });
            return keys;
        }

        [Test]
        public void Judge_IndicatorInsideZone_CollectsKey()
        {
            var keys = SingleQuarterKeys(new KeyZone(0, 2)); // 0,1 구역
            var collectedCounts = new List<int>();
            keys.KeyCollected += c => collectedCounts.Add(c);
            var missed = false;
            keys.ZoneMissed += () => missed = true;

            keys.OpenQuarter(1);
            var judgement = keys.Judge(indicatorPosition: 1);

            Assert.AreEqual(1, keys.Collected);
            CollectionAssert.AreEqual(new[] { 1 }, collectedCounts);
            Assert.IsFalse(missed);
            Assert.IsNull(keys.ActiveZone, "판정 후 구역은 닫혀야 한다.");

            Assert.IsNotNull(judgement);
            Assert.IsTrue(judgement.Value.Success);
            Assert.AreEqual(1, judgement.Value.Quarter);
            Assert.AreEqual(4, judgement.Value.Turn, "판정 턴은 쿼터의 마지막 턴이다.");
            Assert.AreEqual(1, judgement.Value.Position);
        }

        [Test]
        public void Judge_IndicatorOutsideZone_Misses()
        {
            var keys = SingleQuarterKeys(new KeyZone(0, 2)); // 0,1 구역
            var missed = false;
            keys.ZoneMissed += () => missed = true;

            keys.OpenQuarter(1);
            var judgement = keys.Judge(indicatorPosition: 5);

            Assert.AreEqual(0, keys.Collected);
            Assert.IsTrue(missed);
            Assert.IsFalse(judgement.Value.Success);
        }

        [Test]
        public void Judge_WithNoActiveZone_IsNoOp()
        {
            var keys = SingleQuarterKeys(new KeyZone(0, 2));
            var missed = false;
            keys.ZoneMissed += () => missed = true;
            var collected = false;
            keys.KeyCollected += _ => collected = true;

            var judgement = keys.Judge(indicatorPosition: 5); // 열린 구역이 없음

            Assert.IsNull(judgement);
            Assert.IsFalse(missed);
            Assert.IsFalse(collected);
        }

        [Test]
        public void TwoKeysCollected_MarksComplete_AndRecordsResultPerQuarter()
        {
            var keys = new KeyProgress(required: 2, new QuarterSchedule(quarterCount: 3, turnsPerQuarter: 4));
            keys.PrepareZones(new Dictionary<int, KeyZone>
            {
                { 4, new KeyZone(0, 2) },
                { 8, new KeyZone(7, 2) },
                { 12, new KeyZone(50, 2) }
            });

            keys.OpenQuarter(1);
            keys.Judge(0);
            Assert.IsFalse(keys.IsComplete, "키 1개로는 아직 완료되지 않아야 한다.");

            keys.OpenQuarter(2);
            keys.Judge(3); // 실패
            Assert.IsFalse(keys.IsComplete);

            keys.OpenQuarter(3);
            keys.Judge(51);
            Assert.IsTrue(keys.IsComplete, "3번 중 2번 성공하면 완료되어야 한다.");

            CollectionAssert.AreEqual(new bool?[] { true, false, true }, keys.Results);
        }

        [Test]
        public void OpenQuarter_OpensThatQuartersZone_FromTheStartOfTheQuarter()
        {
            var keys = new KeyProgress(required: 2, new QuarterSchedule(quarterCount: 3, turnsPerQuarter: 4));
            keys.PrepareZones(new Dictionary<int, KeyZone>
            {
                { 4, new KeyZone(0, 2) },
                { 8, new KeyZone(7, 2) },
                { 12, new KeyZone(50, 2) }
            });
            var opened = new List<KeyZone>();
            keys.ZoneOpened += opened.Add;

            keys.OpenQuarter(2);

            Assert.AreEqual(new KeyZone(7, 2), keys.ActiveZone.Value);
            Assert.AreEqual(1, opened.Count);
            Assert.AreEqual(new KeyZone(7, 2), opened[0]);
        }

        [Test]
        public void StageStart_PreparesAllQuarterZones_KeyedByJudgingTurn()
        {
            var random = new SystemRandomSource(1);
            var polarityTable = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarityTable);
            var session = StageFactory.Create(config, random, new ClueKnowledgeLedger(), polarityTable);

            session.Runner.StartStage();

            Assert.AreEqual(config.Quarters.QuarterCount, session.Keys.Zones.Count,
                "쿼터 수만큼 구역이 스테이지 시작 시 미리 결정되어 있어야 한다.");
            foreach (var turn in config.Quarters.QuarterEndTurns())
                Assert.IsTrue(session.Keys.Zones.ContainsKey(turn), $"판정 턴 {turn} 구역이 미리 결정되어 있어야 한다.");
            CollectionAssert.AreEqual(new[] { 4, 8, 12 }, session.Keys.Zones.Keys.OrderBy(t => t).ToList(),
                "키 구역 탭이 읽는 턴 번호는 4·8·12가 되어야 한다.");
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

            for (var i = 0; i < 6 && session.Runner.Outcome == StageOutcome.InProgress; i++)
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

        [Test]
        public void StageStart_PrototypeZones_SpreadApartWithinEachSide_AndAreReproducible_AcrossManySeeds()
        {
            // 좌/우는 쿼터마다 독립 동전이라 한쪽에 몰릴 수 있다 — 같은 쪽에 놓인 구역끼리는 위치를 벌리고, 같은 시드는 같은 배치여야 한다.
            var polarityTable = new DefaultEmotionPolarityTable();

            for (var seed = 0; seed < 300; seed++)
            {
                var config = PrototypeContent.PrototypeStage(polarityTable);
                var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), polarityTable);
                var again = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), polarityTable);

                session.Runner.StartStage();
                again.Runner.StartStage();

                foreach (var pair in session.Keys.Zones)
                    Assert.AreEqual(pair.Value, again.Keys.Zones[pair.Key], $"seed={seed}: 같은 시드인데 배치가 다르다.");

                foreach (var isLeft in new[] { true, false })
                {
                    var starts = session.Keys.Zones.Values.Where(z => (z.StartSlot < 100) == isLeft).Select(z => z.StartSlot).OrderBy(x => x).ToList();
                    for (var i = 1; i < starts.Count; i++)
                        Assert.GreaterOrEqual(starts[i] - starts[i - 1], 6,
                            $"seed={seed}: {(isLeft ? "좌" : "우")}측 구역 시작 위치 {string.Join(",", starts)} 가 서로 붙어 있다.");
                }
            }
        }
    }
}
