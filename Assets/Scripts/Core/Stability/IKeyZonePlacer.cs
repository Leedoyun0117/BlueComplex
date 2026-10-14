using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 키 구역을 어디에 열지 결정하는 정책.
    /// startPosition은 인디케이터의 스테이지 시작 위치, turnsUntilKey는 그 구역이 판정되기까지 확보되는 이동 횟수다.
    /// (테스트/비교용으로 남겨둔 <see cref="RandomKeyZonePlacer"/>는 이 값을 쓰지 않는다.)
    /// </summary>
    public interface IKeyZonePlacer
    {
        KeyZone Place(int startPosition, int turnsUntilKey);

        /// <summary>
        /// 한 스테이지의 모든 판정 턴 구역을 한꺼번에 배치한다. 반환 키는 판정 턴 번호.
        /// 판정은 그 턴의 이동까지 끝난 뒤에 이뤄지므로 이동 횟수는 판정 턴 번호 그대로다.
        /// 기본 구현은 턴마다 <see cref="Place"/> 를 독립 호출한다 — 구역 사이의 관계(분산 등)를 다루려는 정책만 재정의한다.
        /// </summary>
        IReadOnlyDictionary<int, KeyZone> PlaceAll(int startPosition, IEnumerable<int> keyTurns)
        {
            var zones = new Dictionary<int, KeyZone>();
            foreach (var turn in keyTurns)
                zones[turn] = Place(startPosition, turn);
            return zones;
        }
    }
}
