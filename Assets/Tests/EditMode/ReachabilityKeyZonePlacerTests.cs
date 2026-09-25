using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 키 구역 배치 정책 — 좌/우를 도달 가능 여부와 무관하게 균등 확률로 먼저 정하고, 그 쪽 구간 안에서
    /// (도달 가능한 후보가 있으면 그 중에서) 위치를 고르는 ReachabilityKeyZonePlacer 검증.
    /// 기본 레이아웃(HeartbeatZone 기본 경계 기준 Min=10, Max=190, EdgeWidth=60, KeyWidth=36) 기준
    /// 좌측 후보는 offset 0~24 → [10,69] 구간을, 우측 후보는 [131,190] 구간을 덮는다.
    /// 양 끝(0~9, 191~200)은 Fatal 구간이므로 키 구역이 그 안에 놓이면 도달 자체가 불가능해진다.
    /// </summary>
    public class ReachabilityKeyZonePlacerTests
    {
        [Test]
        public void Place_ChoosesReachablePositionWithinTheChosenSide()
        {
            // startPosition=20, turnsUntilKey=0 → 도달 범위 [20,20]. 좌측에선 20을 덮는 후보만,
            // 우측([131,190])엔 도달 가능한 후보가 없으므로 구간 전체에서 뽑는다.
            var sawLeft = false;
            var sawRight = false;
            for (var seed = 0; seed < 200; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 20, turnsUntilKey: 0);

                if (IsLeft(zone))
                {
                    sawLeft = true;
                    Assert.IsTrue(zone.Contains(20),
                        $"seed={seed}: 좌측 구역 [{zone.StartSlot},{zone.StartSlot + zone.Width - 1}] 이 도달 범위(20)를 덮지 않는다.");
                }
                else
                {
                    sawRight = true;
                    Assert.GreaterOrEqual(zone.StartSlot, 131, $"seed={seed}: 우측 구간 밖.");
                    Assert.LessOrEqual(zone.StartSlot + zone.Width - 1, 190, $"seed={seed}: 우측 구간 밖.");
                }
            }

            Assert.IsTrue(sawLeft && sawRight, "도달 가능 여부와 무관하게 양쪽이 다 나와야 한다.");
        }

        [Test]
        public void Place_WhenSideHasNoReachableCandidate_SpreadsOverTheWholeSideInsteadOfPinningOne()
        {
            // startPosition=80, turnsUntilKey=0 → 도달 범위 [80,80]. 양쪽 다 도달 후보가 없다 — 예전엔 가장 가까운 후보 하나로 고정됐다.
            var leftStarts = new HashSet<int>();
            var rightStarts = new HashSet<int>();
            for (var seed = 0; seed < 400; seed++)
            {
                var zone = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed)).Place(startPosition: 80, turnsUntilKey: 0);
                (IsLeft(zone) ? leftStarts : rightStarts).Add(zone.StartSlot);
            }

            Assert.AreEqual(25, leftStarts.Count, "좌측 후보 25개(offset 0~24)가 모두 나와야 한다.");
            Assert.AreEqual(25, rightStarts.Count, "우측 후보 25개가 모두 나와야 한다.");
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

        [TestCase(3)]
        [TestCase(5)]
        public void PlaceAll_PicksEachSideWithEqualProbability_IndependentOfReach(int zoneCount)
        {
            // 시작 100, 키 턴 4번 이후 → 도달 범위 100±90 이상이라 모든 후보가 도달 가능하다.
            var turns = Enumerable.Range(0, zoneCount).Select(i => 4 + i * 2).ToArray();
            const int seeds = 2000;
            var rightByTurn = new int[zoneCount];

            for (var seed = 0; seed < seeds; seed++)
            {
                var zones = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed)).PlaceAll(100, turns);

                Assert.AreEqual(zoneCount, zones.Count, $"seed={seed}: 모든 키 턴에 구역이 있어야 한다.");
                for (var i = 0; i < zoneCount; i++)
                    if (!IsLeft(zones[turns[i]])) rightByTurn[i]++;
            }

            for (var i = 0; i < zoneCount; i++)
                Assert.That(rightByTurn[i] / (double)seeds, Is.InRange(0.45, 0.55), $"턴 {turns[i]}의 우측 비율이 균등하지 않다.");
        }

        [Test]
        public void PlaceAll_DoesNotForceBothSides_SoAllZonesMayLandOnOneSide()
        {
            // 좌우 분산 보장은 없다 — 구역마다 독립인 동전이므로 3개가 전부 한쪽에 몰리는 시드(이론상 25%)가 있어야 한다.
            var turns = new[] { 4, 8, 12 };
            var oneSided = Enumerable.Range(0, 400).Count(seed =>
            {
                var zones = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed)).PlaceAll(100, turns);
                return zones.Values.All(IsLeft) || zones.Values.All(z => !IsLeft(z));
            });

            Assert.That(oneSided / 400.0, Is.InRange(0.18, 0.32), "3구역이 한쪽에 몰릴 확률은 25%여야 한다.");
        }

        [Test]
        public void PlaceAll_SideChoiceIgnoresReach_EvenWhenOneSideIsUnreachable()
        {
            // 도달 범위 [10,100] → 우측 구역(131~)은 전부 도달 불가지만 우측 배정 비율은 여전히 절반이다.
            var right = 0;
            var total = 0;
            for (var seed = 0; seed < 1000; seed++)
            {
                var zones = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 100))
                    .PlaceAll(80, new[] { 4, 8, 12 });
                total += zones.Count;
                right += zones.Values.Count(z => !IsLeft(z));
            }

            Assert.That(right / (double)total, Is.InRange(0.47, 0.53));
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

        [Test]
        public void PlaceAll_WhenZonesShareOneSide_SpreadsTheirPositions()
        {
            // 한쪽 구간(오프셋 0~24)에 세 개가 몰린 경우 — 어느 쪽이든 서로 다른 위치로 벌려 놓아야 한다.
            var checkedGroups = 0;
            for (var seed = 0; seed < 600; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 100));
                var zones = placer.PlaceAll(80, new[] { 4, 8, 12 }).Values.ToList();

                foreach (var side in new[] { true, false })
                {
                    var group = zones.Where(z => IsLeft(z) == side).ToList();
                    if (group.Count != 3) continue;

                    checkedGroups++;
                    var starts = group.Select(z => z.StartSlot).OrderBy(x => x).ToList();
                    Assert.GreaterOrEqual(starts[1] - starts[0], 6, $"seed={seed}: 시작 위치 {string.Join(",", starts)} 가 너무 붙어 있다.");
                    Assert.GreaterOrEqual(starts[2] - starts[1], 6, $"seed={seed}: 시작 위치 {string.Join(",", starts)} 가 너무 붙어 있다.");
                    Assert.GreaterOrEqual(starts[2] - starts[0], 18, $"seed={seed}: 구간 전체에 퍼지지 않았다.");
                }
            }

            Assert.Greater(checkedGroups, 50, "한쪽에 3개가 몰리는 경우가 충분히 검사되어야 한다.");
        }

        [Test]
        public void MinOverlapFraction_NarrowsPositionsWithinTheSideToReachableOnes()
        {
            // 도달 범위 [10,160]. 우측 구역(시작 131+offset, 폭 36)이 절반(18칸) 이상 걸치려면 시작 ≤ 143(offset ≤ 12).
            // 엄격 모드에선 우측이 뽑혀도 그 범위 안에서만, 기본(한 칸만 겹쳐도 가능)에선 더 바깥 위치까지 나온다.
            var strictMaxStart = 0;
            var looseMaxStart = 0;
            for (var seed = 0; seed < 400; seed++)
            {
                var strict = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 160), minOverlapFraction: 0.5)
                    .PlaceAll(80, new[] { 4, 8, 12 });
                foreach (var zone in strict.Values.Where(z => !IsLeft(z)))
                    strictMaxStart = System.Math.Max(strictMaxStart, zone.StartSlot);

                var loose = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed), reach: new FixedReach(10, 160))
                    .PlaceAll(80, new[] { 4, 8, 12 });
                foreach (var zone in loose.Values.Where(z => !IsLeft(z)))
                    looseMaxStart = System.Math.Max(looseMaxStart, zone.StartSlot);
            }

            Assert.LessOrEqual(strictMaxStart, 143, "절반 미만만 걸치는 우측 위치가 도달 가능으로 취급되었다.");
            Assert.Greater(looseMaxStart, 143, "minOverlapFraction=0(기본)이면 한 칸만 겹쳐도 도달 가능이라 더 바깥 위치도 나올 수 있어야 한다.");
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
        public void StageFactoryPlacer_PlacesBothSidesEvenly_ForPrototypeStage()
        {
            // 지금 카드 풀(상승 카드가 모자람)로는 우측 구역이 실질적으로 도달 불가지만, 배치는 그와 무관하게 절반씩이다.
            // 그 밸런스 영향은 KeyReachabilityBotComparisonTest 로그의 좌/우별 성공률에서 본다.
            var table = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(table);
            var zone = new HeartbeatZone();

            var right = 0;
            var total = 0;
            for (var seed = 0; seed < 1000; seed++)
            {
                var placer = StageFactory.CreateKeyPlacer(config, new SystemRandomSource(seed), zone, table);

                var zones = placer.PlaceAll(Heartbeat.DefaultStartValue, config.Quarters.QuarterEndTurns());

                total += zones.Count;
                right += zones.Values.Count(z => !IsLeft(z));
            }

            Assert.That(right / (double)total, Is.InRange(0.47, 0.53), "우측 배치 비율");
        }

        [Test]
        public void StageFactoryPlacer_IsReproducibleForSameSeed()
        {
            var table = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(table);

            for (var seed = 0; seed < 50; seed++)
            {
                var a = StageFactory.CreateKeyPlacer(config, new SystemRandomSource(seed), new HeartbeatZone(), table)
                    .PlaceAll(Heartbeat.DefaultStartValue, config.Quarters.QuarterEndTurns());
                var b = StageFactory.CreateKeyPlacer(config, new SystemRandomSource(seed), new HeartbeatZone(), table)
                    .PlaceAll(Heartbeat.DefaultStartValue, config.Quarters.QuarterEndTurns());

                foreach (var turn in a.Keys)
                    Assert.AreEqual(a[turn], b[turn], $"seed={seed} 턴 {turn}");
            }
        }
    }
}
