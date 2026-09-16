using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 키 시스템 보정 — 도달 가능 범위 안에서만 키 구역을 고르는 ReachabilityKeyZonePlacer 검증.
    /// 기본 레이아웃(Slots=201, EdgeWidth=60, KeyWidth=40) 기준 좌측 후보는 offset 0~20 → [0,59] 구간을,
    /// 우측 후보는 [141,200] 구간을 덮는다.
    /// </summary>
    public class ReachabilityKeyZonePlacerTests
    {
        [Test]
        public void Place_OnlyChoosesReachableCandidates_WhenSomeCandidatesAreOutOfRange()
        {
            // startPosition=20, turnsUntilKey=0 → 도달 범위 [20,20]. 좌측 후보([0,59] 범위)만 겹치고
            // 우측 후보([141,200])는 모두 벗어난다.
            for (var seed = 0; seed < 50; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 20, turnsUntilKey: 0);
                var lastSlot = zone.StartSlot + zone.Width - 1;

                Assert.LessOrEqual(lastSlot, 59,
                    $"seed={seed}: 도달 범위 밖(우측)의 후보 [{zone.StartSlot},{lastSlot}] 가 선택되었다.");
            }
        }

        [Test]
        public void Place_WhenNoCandidateOverlapsRange_PicksNearestCandidate()
        {
            // startPosition=80, turnsUntilKey=0 → 도달 범위 [80,80]. 좌측 최댓값([20,59], 거리 21)이
            // 우측 최솟값([141,180], 거리 61)보다 가까우므로 좌측 offset20([20,59])이 결정적으로 선택되어야 한다.
            for (var seed = 0; seed < 20; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 80, turnsUntilKey: 0);

                Assert.AreEqual(new KeyZone(20, 40), zone,
                    $"seed={seed}: 겹치는 후보가 없으면 가장 가까운 후보를 결정적으로 골라야 한다.");
            }
        }

        [Test]
        public void Place_WithGenerousReach_NeverPicksZoneOutsideBoard()
        {
            // 중앙 100, 최대 이동폭 60, 3턴 키 → turnsUntilKey=2 → 도달 범위 100±120 → 사실상 0~200 전체.
            for (var seed = 0; seed < 30; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 100, turnsUntilKey: 2);
                var lastSlot = zone.StartSlot + zone.Width - 1;

                Assert.GreaterOrEqual(zone.StartSlot, 0);
                Assert.LessOrEqual(lastSlot, 200);
            }
        }

        [Test]
        public void DefaultLayoutAndReach_MatchHeartbeatScaleConversion()
        {
            // 10칸 기준 양끝 3칸·키 2칸을 200 스케일로 그대로 환산한 값이 기본값이어야 한다.
            var layout = new KeyZoneLayout();
            Assert.AreEqual(201, layout.Slots);
            Assert.AreEqual(60, layout.EdgeWidth, "3칸 × 20 = 60");
            Assert.AreEqual(40, layout.KeyWidth, "2칸 × 20 = 40");

            var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(0));
            Assert.AreEqual(30, placer.MaxMovePerTurn, "감정 조합 최대 개수(3) × 태그 영향력(10) = 30");
        }
    }
}
