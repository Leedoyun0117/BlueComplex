using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 키 시스템 보정 — 도달 가능 범위 안에서만 키 구역을 고르는 ReachabilityKeyZonePlacer 검증.
    /// 기본 레이아웃(HeartbeatZone 기본 경계 기준 Min=10, Max=190, EdgeWidth=60, KeyWidth=36) 기준
    /// 좌측 후보는 offset 0~24 → [10,69] 구간을, 우측 후보는 [131,190] 구간을 덮는다.
    /// 양 끝(0~9, 191~200)은 Fatal 구간이므로 키 구역이 그 안에 놓이면 도달 자체가 불가능해진다.
    /// </summary>
    public class ReachabilityKeyZonePlacerTests
    {
        [Test]
        public void Place_OnlyChoosesReachableCandidates_WhenSomeCandidatesAreOutOfRange()
        {
            // startPosition=20, turnsUntilKey=0 → 도달 범위 [20,20]. 좌측 후보([10,69] 범위)만 겹치고
            // 우측 후보([131,190])는 모두 벗어난다.
            for (var seed = 0; seed < 50; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 20, turnsUntilKey: 0);
                var lastSlot = zone.StartSlot + zone.Width - 1;

                Assert.LessOrEqual(lastSlot, 69,
                    $"seed={seed}: 도달 범위 밖(우측)의 후보 [{zone.StartSlot},{lastSlot}] 가 선택되었다.");
            }
        }

        [Test]
        public void Place_WhenNoCandidateOverlapsRange_PicksNearestCandidate()
        {
            // startPosition=80, turnsUntilKey=0 → 도달 범위 [80,80]. 좌측 최댓값([34,69], 거리 11)이
            // 우측 최솟값([131,166], 거리 51)보다 가까우므로 좌측 offset24([34,36])가 결정적으로 선택되어야 한다.
            for (var seed = 0; seed < 20; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 80, turnsUntilKey: 0);

                Assert.AreEqual(new KeyZone(34, 36), zone,
                    $"seed={seed}: 겹치는 후보가 없으면 가장 가까운 후보를 결정적으로 골라야 한다.");
            }
        }

        [Test]
        public void Place_WithGenerousReach_NeverPicksZoneOutsideSurvivableRange()
        {
            // 중앙 100, 한 턴 최대 이동폭 30, 2턴 여유 → 도달 범위 100±60 → 사실상 생존 구간 전체.
            var heartbeatZone = new HeartbeatZone();

            for (var seed = 0; seed < 30; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 100, turnsUntilKey: 2);
                var lastSlot = zone.StartSlot + zone.Width - 1;

                Assert.GreaterOrEqual(zone.StartSlot, heartbeatZone.SurvivableMin,
                    $"seed={seed}: 구역 시작이 생존 구간({heartbeatZone.SurvivableMin}) 아래로 내려갔다.");
                Assert.LessOrEqual(lastSlot, heartbeatZone.SurvivableMax,
                    $"seed={seed}: 구역 끝이 생존 구간({heartbeatZone.SurvivableMax})을 넘어섰다.");
            }
        }

        [Test]
        public void Place_NeverOverlapsFatalZone_AcrossManySeedsAndPositions()
        {
            var heartbeatZone = new HeartbeatZone();
            var startPositions = new[] { 0, 5, 15, 50, 100, 150, 195, 200 };

            for (var seed = 0; seed < 40; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                foreach (var start in startPositions)
                {
                    for (var turnsUntilKey = 0; turnsUntilKey <= 3; turnsUntilKey++)
                    {
                        var zone = placer.Place(start, turnsUntilKey);
                        var lastSlot = zone.StartSlot + zone.Width - 1;

                        for (var pos = zone.StartSlot; pos <= lastSlot; pos++)
                        {
                            Assert.IsFalse(heartbeatZone.IsFatal(pos),
                                $"seed={seed} start={start} turnsUntilKey={turnsUntilKey}: " +
                                $"구역 [{zone.StartSlot},{lastSlot}] 이 Fatal 위치 {pos} 를 포함한다.");
                        }
                    }
                }
            }
        }

        private static bool IsLeft(KeyZone zone) => zone.StartSlot < 100;

        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(5, 2)]
        public void PlaceAll_SpreadsZonesAcrossBothSides_WhenAllCandidatesAreReachable(int zoneCount, int minPerSide)
        {
            // 시작 100, 키 턴 4번 이후 → 도달 범위 100±90 이상이라 모든 후보가 도달 가능하다.
            var turns = Enumerable.Range(0, zoneCount).Select(i => 4 + i * 2).ToArray();

            for (var seed = 0; seed < 500; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zones = placer.PlaceAll(100, turns);

                Assert.AreEqual(zoneCount, zones.Count, $"seed={seed}: 모든 키 턴에 구역이 있어야 한다.");
                var left = zones.Values.Count(IsLeft);
                var right = zoneCount - left;
                Assert.GreaterOrEqual(left, minPerSide, $"seed={seed}: 좌측 구역 {left}/{zoneCount}개 — 한쪽으로 쏠렸다.");
                Assert.GreaterOrEqual(right, minPerSide, $"seed={seed}: 우측 구역 {right}/{zoneCount}개 — 한쪽으로 쏠렸다.");
            }
        }

        [Test]
        public void PlaceAll_StillRespectsReach_WhenOneSideIsUnreachableForEarlyTurn()
        {
            // 시작 20, 키 턴 1 → 도달 범위 [20,20]: 우측은 도달 불가. 분산보다 도달 가능성이 우선이어야 하고,
            // 다른 턴(도달 범위 100±...)이 반대쪽을 맡아 분산은 유지되어야 한다.
            for (var seed = 0; seed < 200; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zones = placer.PlaceAll(20, new[] { 1, 6, 8 });

                Assert.IsTrue(IsLeft(zones[1]), $"seed={seed}: 턴 1의 구역은 도달 가능한 좌측이어야 한다.");
                Assert.IsTrue(zones.Values.Any(z => !IsLeft(z)), $"seed={seed}: 다른 턴이 우측을 맡아 분산되어야 한다.");
            }
        }

        [Test]
        public void PlaceAll_IsDeterministicForSameSeed_RegardlessOfKeyTurnOrder()
        {
            var a = new ReachabilityKeyZonePlacer(new SystemRandomSource(42)).PlaceAll(100, new[] { 3, 5, 7, 9 });
            var b = new ReachabilityKeyZonePlacer(new SystemRandomSource(42)).PlaceAll(100, new[] { 9, 3, 7, 5 });

            foreach (var turn in a.Keys)
                Assert.AreEqual(a[turn], b[turn], $"턴 {turn}");
        }

        [Test]
        public void DefaultLayoutAndReach_MatchHeartbeatScaleConversion()
        {
            var heartbeatZone = new HeartbeatZone();
            var layout = new KeyZoneLayout(heartbeatZone);

            Assert.AreEqual(heartbeatZone.SurvivableMin, layout.Min, "레이아웃 최솟값은 HeartbeatZone에서 가져와야 한다.");
            Assert.AreEqual(heartbeatZone.SurvivableMax, layout.Max, "레이아웃 최댓값은 HeartbeatZone에서 가져와야 한다.");
            Assert.AreEqual(181, layout.Slots, "생존 구간(10~190) 전체 칸 수.");
            Assert.AreEqual(60, layout.EdgeWidth, "3칸 × 20 = 60");
            Assert.AreEqual(36, layout.KeyWidth, "생존 구간 폭(181) 대비 원래 비율(20%)을 유지한 값.");

            var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(0));
            Assert.AreEqual(20, placer.MaxMovePerTurn, "단서 하나의 감정 최대 2개 × 태그 영향력(10) = 20 (실측 최대 상승)");
            Assert.AreEqual(30, ReachabilityKeyZonePlacer.DefaultMaxDropPerTurn, "침체 감정 2개 + 컴플렉스가 더하는 감정 1개 = 30");
        }

        /// <summary>이동 횟수와 무관하게 고정된 도달 범위를 돌려주는 테스트용 모델.</summary>
        private sealed class FixedReach : IReachModel
        {
            private readonly int _low;
            private readonly int _high;
            public FixedReach(int low, int high) { _low = low; _high = high; }
            public (int Low, int High) Range(int startPosition, int moves) => (_low, _high);
        }

        /// <summary>이동 횟수가 적은 판정 턴은 좁은 범위(좌측만), 많으면 전체를 돌려주는 테스트용 모델.</summary>
        private sealed class WidensWithMoves : IReachModel
        {
            public (int Low, int High) Range(int startPosition, int moves) => moves <= 4 ? (10, 100) : (0, 200);
        }

        [Test]
        public void PlaceAll_WhenRightSideIsUnreachable_PutsEveryZoneOnTheReachableSide()
        {
            // 도달 범위 [10,100] → 우측 구역(131~)은 전부 도달 불가. 분산을 강제하지 않고 전부 좌측에 둔다.
            for (var seed = 0; seed < 200; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 100));

                var zones = placer.PlaceAll(80, new[] { 4, 8, 12 });

                Assert.AreEqual(3, zones.Count);
                Assert.IsTrue(zones.Values.All(IsLeft), $"seed={seed}: 우측이 도달 불가인데 우측 구역이 배정되었다.");
            }
        }

        [Test]
        public void PlaceAll_WhenAllZonesShareOneSide_SpreadsTheirPositions()
        {
            // 같은 좌측 구간(오프셋 0~24)에 세 개가 놓이므로 서로 다른 위치로 벌려 놓아야 한다.
            for (var seed = 0; seed < 300; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 100));

                var starts = placer.PlaceAll(80, new[] { 4, 8, 12 }).Values.Select(z => z.StartSlot).OrderBy(x => x).ToList();

                Assert.GreaterOrEqual(starts[1] - starts[0], 6, $"seed={seed}: 시작 위치 {string.Join(",", starts)} 가 너무 붙어 있다.");
                Assert.GreaterOrEqual(starts[2] - starts[1], 6, $"seed={seed}: 시작 위치 {string.Join(",", starts)} 가 너무 붙어 있다.");
                Assert.GreaterOrEqual(starts[2] - starts[0], 18, $"seed={seed}: 구간 전체에 퍼지지 않았다.");
            }
        }

        [Test]
        public void PlaceAll_PutsTurnsWithOnlyOneReachableSideOnThatSide_AndSpreadsTheRest()
        {
            // 판정 턴 4는 좌측만 도달 가능, 8·12는 양쪽 다 가능 → 4는 좌측 고정, 8·12 중 하나는 반대쪽(우측)이어야 한다.
            for (var seed = 0; seed < 200; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new WidensWithMoves());

                var zones = placer.PlaceAll(80, new[] { 4, 8, 12 });

                Assert.IsTrue(IsLeft(zones[4]), $"seed={seed}: 좌측만 도달 가능한 턴 4가 우측에 배정되었다.");
                Assert.IsTrue(!IsLeft(zones[8]) || !IsLeft(zones[12]), $"seed={seed}: 양쪽 다 도달 가능한 턴에서 분산되지 않았다.");
            }
        }

        [Test]
        public void MinOverlapFraction_TreatsZonesThatOnlyGrazeTheReachRangeAsUnreachable()
        {
            // 도달 범위 상한이 140이면 우측 구역([131~]) 중 가장 안쪽도 10칸밖에 안 걸친다(폭 36의 절반=18 미만).
            for (var seed = 0; seed < 100; seed++)
            {
                var strict = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 140), minOverlapFraction: 0.5);
                Assert.IsTrue(strict.PlaceAll(80, new[] { 4, 8, 12 }).Values.All(IsLeft), $"seed={seed}: 끝자락만 스치는 우측 구역이 도달 가능으로 취급되었다.");
            }

            var anyOverlap = Enumerable.Range(0, 100).Any(seed =>
                new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 140))
                    .PlaceAll(80, new[] { 4, 8, 12 }).Values.Any(z => !IsLeft(z)));
            Assert.IsTrue(anyOverlap, "minOverlapFraction=0(기본)이면 한 칸만 겹쳐도 도달 가능이라 우측이 배정될 수 있어야 한다.");
        }

        [Test]
        public void CardPoolReachModel_LimitsReachByTheBestSingleUseCards()
        {
            // (Best, Worst): 컴플렉스가 없을 때 이동량과 최악의 이동량.
            var model = new CardPoolReachModel(new[] { (20, -20), (10, -10), (10, -30), (0, -30), (-10, -10), (-20, -20) },
                maxRisePerMove: 20, maxDropPerMove: 30);

            Assert.AreEqual((80, 80), model.Range(80, 0));
            Assert.AreEqual((20, 110), model.Range(80, 2), "두 번 움직이면 최대 상승은 Best 큰 두 장(20+10), 최대 하강은 Worst 작은 두 장(-30-30).");
            Assert.AreEqual((-40, 90), model.Range(80, 6), "카드를 다 쓰면 Best 합(10)이 상한, Worst 합(-120)이 하한.");
            Assert.AreEqual((-40, 90), model.Range(80, 12), "카드가 모자라 넘어간 턴은 이동량 0.");
        }

        [Test]
        public void CardPoolReachModel_ClampsEachCardToPerTurnLimits()
        {
            var model = new CardPoolReachModel(new[] { (50, 20), (-50, -50) }, maxRisePerMove: 20, maxDropPerMove: 30);

            Assert.AreEqual((50, 100), model.Range(80, 1), "Best 50은 상승 한도 20으로, Worst -50은 하강 한도 -30으로 잘린다.");
        }

        [Test]
        public void PrototypeReach_MakesRightSideUnreachable_ForEveryQuarter()
        {
            // 스테이지 1 카드 풀(상승 카드 5장, 각 +10)로는 시작 80에서 4번 움직여 120, 8번 이상 움직여도 130까지가 상한이라
            // 우측 구역(131~)은 절반(18칸=148 이상) 이상 걸치지 못한다. 태그 영향력이 ±20이면 이 전제가 깨진다.
            var table = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(table);
            var zone = new HeartbeatZone();

            for (var seed = 0; seed < 200; seed++)
            {
                var placer = StageFactory.CreateKeyPlacer(config, new SystemRandomSource(seed), zone, table);

                var zones = placer.PlaceAll(Heartbeat.DefaultStartValue, config.Quarters.QuarterEndTurns());

                Assert.IsTrue(zones.Values.All(IsLeft), $"seed={seed}: 도달 불가인 우측 구역이 배정되었다.");
            }
        }
    }
}
