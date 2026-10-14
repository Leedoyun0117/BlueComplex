using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 키 획득 진행 상황. 쿼터마다 목표 구역이 하나 있고, 그 쿼터의 마지막 턴이 끝난 시점에만 판정한다
    /// (쿼터 중간에 구역을 스쳐 지나가는 것은 의미 없다). 목표 개수를 채우면 스테이지 클리어.
    /// 구역은 판정 턴(쿼터 마지막 턴)을 키로 보관한다 — UI 탭 라벨이 그 턴 번호("4턴·8턴·12턴")를 그대로 읽는다.
    /// </summary>
    public sealed class KeyProgress
    {
        private readonly Dictionary<int, KeyZone> _zonesByTurn = new();
        private readonly bool?[] _results;
        private int _activeQuarter;

        public int Required { get; }
        public int Collected { get; private set; }
        public QuarterSchedule Schedule { get; }

        /// <summary>진행 중인 쿼터의 목표 구역. 쿼터가 시작될 때 열리고, 판정 후 닫힌다.</summary>
        public KeyZone? ActiveZone { get; private set; }

        /// <summary>판정이 일어나는 턴 목록(각 쿼터의 마지막 턴). 스테이지 시작 시 이 턴 전부의 구역을 한꺼번에 확정한다.</summary>
        public IReadOnlyCollection<int> KeyTurns { get; }

        /// <summary>판정 턴별로 확정된 키 구역. UI가 스테이지 시작부터 전체 쿼터의 목표를 보여주는 데 쓴다.</summary>
        public IReadOnlyDictionary<int, KeyZone> Zones => _zonesByTurn;

        /// <summary>쿼터별 판정 결과(인덱스 0 = 1쿼터). null=아직 판정 전, true=키 획득, false=실패.</summary>
        public IReadOnlyList<bool?> Results => _results;

        public bool IsComplete => Collected >= Required;

        /// <summary>쿼터가 시작되어 그 쿼터의 목표 구역이 열릴 때.</summary>
        public event Action<KeyZone> ZoneOpened;
        public event Action<int> KeyCollected;
        public event Action ZoneMissed;

        public KeyProgress(int required, QuarterSchedule schedule)
        {
            Required = required;
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            KeyTurns = schedule.QuarterEndTurns().ToList();
            _results = new bool?[schedule.QuarterCount];
        }

        /// <summary>이 턴이 끝나면 키를 판정하는가(쿼터의 마지막 턴).</summary>
        public bool IsKeyTurn(int turn) => Schedule.IsQuarterEnd(turn);

        /// <summary>스테이지 시작 시 모든 쿼터의 구역을 한꺼번에 확정한다(판정 턴이 키). 이후에는 바뀌지 않는다.</summary>
        public void PrepareZones(IReadOnlyDictionary<int, KeyZone> zones)
        {
            foreach (var pair in zones) _zonesByTurn[pair.Key] = pair.Value;
        }

        /// <summary>미리 확정된 구역을 그 쿼터의 활성 구역으로 연다.</summary>
        public void OpenQuarter(int quarter)
        {
            var zone = _zonesByTurn[Schedule.LastTurnOf(quarter)];
            _activeQuarter = quarter;
            ActiveZone = zone;
            ZoneOpened?.Invoke(zone);
        }

        /// <summary><see cref="Judge"/>와 같은 판정이지만 아무 상태도 바꾸지 않는다(결과 기록·구역 닫기·획득 수·이벤트 없음). 열린 구역이 없으면 null.</summary>
        public KeyJudgement? PreviewJudge(int indicatorPosition)
        {
            if (ActiveZone == null) return null;

            var zone = ActiveZone.Value;
            return new KeyJudgement(_activeQuarter, Schedule.LastTurnOf(_activeQuarter), zone, indicatorPosition, zone.Contains(indicatorPosition));
        }

        /// <summary>쿼터 마지막 턴 종료 시점의 인디케이터 위치로 획득 여부를 판정한다. 열린 구역이 없으면 null.</summary>
        public KeyJudgement? Judge(int indicatorPosition)
        {
            if (ActiveZone == null) return null;

            var zone = ActiveZone.Value;
            var success = zone.Contains(indicatorPosition);
            var judgement = new KeyJudgement(_activeQuarter, Schedule.LastTurnOf(_activeQuarter), zone, indicatorPosition, success);

            _results[_activeQuarter - 1] = success;
            ActiveZone = null;

            if (success)
            {
                Collected++;
                KeyCollected?.Invoke(Collected);
            }
            else
            {
                ZoneMissed?.Invoke();
            }

            return judgement;
        }
    }
}
