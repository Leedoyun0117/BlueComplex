using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>쿼터 하나의 키 판정 결과. 쿼터 마지막 턴이 끝난 시점의 심박수로 정해진다.</summary>
    public readonly struct KeyJudgement
    {
        /// <summary>쿼터 번호(1부터).</summary>
        public int Quarter { get; }

        /// <summary>판정이 일어난 턴(그 쿼터의 마지막 턴).</summary>
        public int Turn { get; }

        public KeyZone Zone { get; }

        /// <summary>판정에 쓰인 심박수.</summary>
        public int Position { get; }

        public bool Success { get; }

        public KeyJudgement(int quarter, int turn, KeyZone zone, int position, bool success)
        {
            Quarter = quarter;
            Turn = turn;
            Zone = zone;
            Position = position;
            Success = success;
        }
    }
}
