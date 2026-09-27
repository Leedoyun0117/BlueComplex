using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 미리 정해 둔 구역을 판정 턴 순서대로 내주는 배치기(튜토리얼). 난수를 쓰지 않는다 — 같은 입력이면 언제나 같은 구역이다.
    /// 구역 목록은 판정 턴이 이른 것부터 차례로 대응한다(첫 구역 = 첫 쿼터의 키).
    /// </summary>
    public sealed class ScriptedKeyPlacer : IKeyZonePlacer
    {
        private readonly IReadOnlyList<KeyZone> _zonesInTurnOrder;

        public ScriptedKeyPlacer(IReadOnlyList<KeyZone> zonesInTurnOrder)
        {
            if (zonesInTurnOrder == null || zonesInTurnOrder.Count == 0)
                throw new ArgumentException("구역이 하나 이상 있어야 합니다.", nameof(zonesInTurnOrder));

            _zonesInTurnOrder = zonesInTurnOrder;
        }

        /// <summary>정확한 심박수 범위(양 끝 포함)를 쿼터 순서대로 준다. 좌/우 끝 구역 규격(<see cref="KeyZoneLayout"/>)과 무관하게 그 값 그대로가 목표다 — 예: (90, 100), (70, 80).</summary>
        public static ScriptedKeyPlacer FromRanges(params (int Min, int Max)[] ranges)
        {
            if (ranges == null || ranges.Length == 0)
                throw new ArgumentException("범위가 하나 이상 있어야 합니다.", nameof(ranges));

            foreach (var (min, max) in ranges)
                if (max < min) throw new ArgumentException($"범위 {min}~{max}: 최댓값이 최솟값보다 작습니다.", nameof(ranges));

            return new ScriptedKeyPlacer(ranges.Select(r => new KeyZone(r.Min, r.Max - r.Min + 1)).ToList());
        }

        /// <summary>한 구역만 필요한 호출(<see cref="IKeyZonePlacer.Place"/>)은 첫 구역을 준다. 스테이지 시작에서는 <see cref="PlaceAll"/>이 쓰인다.</summary>
        public KeyZone Place(int startPosition, int turnsUntilKey) => _zonesInTurnOrder[0];

        public IReadOnlyDictionary<int, KeyZone> PlaceAll(int startPosition, IEnumerable<int> keyTurns)
        {
            var turns = keyTurns.Distinct().OrderBy(t => t).ToList();
            if (turns.Count != _zonesInTurnOrder.Count)
                throw new InvalidOperationException($"판정 턴 {turns.Count}개에 미리 정해 둔 구역이 {_zonesInTurnOrder.Count}개입니다.");

            var zones = new Dictionary<int, KeyZone>();
            for (var i = 0; i < turns.Count; i++) zones[turns[i]] = _zonesInTurnOrder[i];
            return zones;
        }
    }
}
