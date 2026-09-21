using System;
using System.Collections.Generic;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 스테이지의 쿼터 구조. 쿼터 수와 쿼터당 턴 수만 알고, 턴 번호와 쿼터 번호 사이의 환산을 맡는다.
    /// 턴과 쿼터는 모두 1부터 센다. 쿼터의 마지막 턴이 끝난 시점에 그 쿼터의 키를 판정한다.
    /// </summary>
    public sealed class QuarterSchedule
    {
        public int QuarterCount { get; }
        public int TurnsPerQuarter { get; }
        public int TotalTurns => QuarterCount * TurnsPerQuarter;

        public QuarterSchedule(int quarterCount, int turnsPerQuarter)
        {
            if (quarterCount < 1)
                throw new ArgumentOutOfRangeException(nameof(quarterCount), "쿼터는 1개 이상이어야 합니다.");
            if (turnsPerQuarter < 1)
                throw new ArgumentOutOfRangeException(nameof(turnsPerQuarter), "쿼터당 턴은 1 이상이어야 합니다.");

            QuarterCount = quarterCount;
            TurnsPerQuarter = turnsPerQuarter;
        }

        /// <summary>turn(1~TotalTurns)이 속한 쿼터 번호(1~QuarterCount).</summary>
        public int QuarterOf(int turn)
        {
            RequireValidTurn(turn);
            return (turn - 1) / TurnsPerQuarter + 1;
        }

        /// <summary>turn이 자기 쿼터 안에서 몇 번째 턴인지(1~TurnsPerQuarter).</summary>
        public int TurnInQuarter(int turn)
        {
            RequireValidTurn(turn);
            return (turn - 1) % TurnsPerQuarter + 1;
        }

        public bool IsQuarterStart(int turn) => turn >= 1 && turn <= TotalTurns && (turn - 1) % TurnsPerQuarter == 0;

        /// <summary>쿼터의 마지막 턴인가 — 키를 판정하는 턴이다.</summary>
        public bool IsQuarterEnd(int turn) => turn >= 1 && turn <= TotalTurns && turn % TurnsPerQuarter == 0;

        public int FirstTurnOf(int quarter)
        {
            RequireValidQuarter(quarter);
            return (quarter - 1) * TurnsPerQuarter + 1;
        }

        public int LastTurnOf(int quarter)
        {
            RequireValidQuarter(quarter);
            return quarter * TurnsPerQuarter;
        }

        /// <summary>각 쿼터의 마지막 턴, 쿼터 순서대로.</summary>
        public IEnumerable<int> QuarterEndTurns()
        {
            for (var quarter = 1; quarter <= QuarterCount; quarter++)
                yield return LastTurnOf(quarter);
        }

        private void RequireValidTurn(int turn)
        {
            if (turn < 1 || turn > TotalTurns)
                throw new ArgumentOutOfRangeException(nameof(turn), $"턴 {turn}은(는) 1~{TotalTurns} 범위를 벗어났습니다.");
        }

        private void RequireValidQuarter(int quarter)
        {
            if (quarter < 1 || quarter > QuarterCount)
                throw new ArgumentOutOfRangeException(nameof(quarter), $"쿼터 {quarter}은(는) 1~{QuarterCount} 범위를 벗어났습니다.");
        }
    }
}
