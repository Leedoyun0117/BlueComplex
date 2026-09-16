using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 키 시스템 보정 — 도달 가능 범위 안에서만 키 구역을 고르는 ReachabilityKeyZonePlacer 검증.
    /// 기본 레이아웃(Slots=10, EdgeWidth=3, KeyWidth=2) 기준 후보는 좌측 [0,1]/[1,2], 우측 [7,8]/[8,9] 네 개다.
    /// </summary>
    public class ReachabilityKeyZonePlacerTests
    {
        [Test]
        public void Place_OnlyChoosesReachableCandidates_WhenSomeCandidatesAreOutOfRange()
        {
            // startPosition=1, turnsUntilKey=0 → 도달 범위 [1,1]. 좌측 두 후보만 겹치고 우측 두 후보는 벗어난다.
            for (var seed = 0; seed < 50; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 1, turnsUntilKey: 0);
                var lastSlot = zone.StartSlot + zone.Width - 1;

                Assert.LessOrEqual(lastSlot, 2,
                    $"seed={seed}: 도달 범위 밖(우측)의 후보 [{zone.StartSlot},{lastSlot}] 가 선택되었다.");
            }
        }

        [Test]
        public void Place_WhenNoCandidateOverlapsRange_PicksNearestCandidate()
        {
            // startPosition=4, turnsUntilKey=0 → 도달 범위 [4,4]. 네 후보([0,1],[1,2],[7,8],[8,9]) 중 어느 것도 겹치지 않는다.
            // 가장 가까운 후보는 [1,2] (거리 2) — [7,8](거리 3) 보다 가깝다.
            for (var seed = 0; seed < 20; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 4, turnsUntilKey: 0);

                Assert.AreEqual(new KeyZone(1, 2), zone,
                    $"seed={seed}: 겹치는 후보가 없으면 가장 가까운 후보를 결정적으로 골라야 한다.");
            }
        }

        [Test]
        public void Place_WithGenerousReach_NeverPicksZoneOutsideBoard()
        {
            // 스펙 예시: 중앙 5, 최대 이동폭 3, 3턴 키 → turnsUntilKey=2 → 도달 범위 5±6 → 사실상 0~9 전체.
            for (var seed = 0; seed < 30; seed++)
            {
                var placer = new ReachabilityKeyZonePlacer(new SystemRandomSource(seed));

                var zone = placer.Place(startPosition: 5, turnsUntilKey: 2);
                var lastSlot = zone.StartSlot + zone.Width - 1;

                Assert.GreaterOrEqual(zone.StartSlot, 0);
                Assert.LessOrEqual(lastSlot, 9);
            }
        }
    }
}
