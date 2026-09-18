using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

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
            Assert.AreEqual(30, placer.MaxMovePerTurn, "감정 조합 최대 개수(3) × 태그 영향력(10) = 30");
        }
    }
}
