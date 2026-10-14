using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>한 번 움직일 때마다 최대 rise만큼 오르고 최대 drop만큼 내린다고 보는 단순 모델. 매 턴 최선의 카드가 있다고 가정하므로 후한 편이다.</summary>
    public sealed class PerTurnReachModel : IReachModel
    {
        private readonly int _maxRisePerMove;
        private readonly int _maxDropPerMove;

        public PerTurnReachModel(int maxRisePerMove, int maxDropPerMove)
        {
            _maxRisePerMove = maxRisePerMove;
            _maxDropPerMove = maxDropPerMove;
        }

        public (int Low, int High) Range(int startPosition, int moves)
        {
            var count = Math.Max(0, moves);
            return (startPosition - count * _maxDropPerMove, startPosition + count * _maxRisePerMove);
        }
    }
}
