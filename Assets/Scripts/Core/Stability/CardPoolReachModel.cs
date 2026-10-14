using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 단서는 한 번 쓰면 사라진다 — 그래서 moves번 움직여 닿는 최대 상승은 "가진 카드 중 이동량이 큰 moves장의 합"이고
    /// 최대 하강은 작은 moves장의 합이다(카드가 모자라면 남은 턴은 넘어가 0). 매 턴 최선의 카드가 나온다고 보는
    /// <see cref="PerTurnReachModel"/> 과 달리 카드 풀의 실제 구성이 상한을 정한다.
    /// 카드마다 두 값을 받는다: 상승 쪽은 컴플렉스가 없을 때의 이동량(Best), 하강 쪽은 컴플렉스가 최악으로 겹칠 때의
    /// 이동량(Worst). 컴플렉스는 대체로 방해 요소라 상승 여력에서는 뺐고, 하강은 그대로 열어 둔다 —
    /// 안 그러면 카드를 다 쓴 뒤의 위치가 한 점으로 고정돼 실제로는 닿는 아래쪽 구역이 도달 불가로 분류된다.
    /// 한 장의 이동량은 한 턴 최대 이동폭으로 자른다.
    /// </summary>
    public sealed class CardPoolReachModel : IReachModel
    {
        private readonly int[] _bestDescending;
        private readonly int[] _worstAscending;

        public CardPoolReachModel(IEnumerable<(int Best, int Worst)> cards, int maxRisePerMove, int maxDropPerMove)
        {
            var list = cards.ToList();
            _bestDescending = list.Select(c => Math.Clamp(c.Best, -maxDropPerMove, maxRisePerMove))
                .OrderByDescending(d => d).ToArray();
            _worstAscending = list.Select(c => Math.Clamp(c.Worst, -maxDropPerMove, maxRisePerMove))
                .OrderBy(d => d).ToArray();
        }

        public (int Low, int High) Range(int startPosition, int moves)
        {
            var count = Math.Min(Math.Max(0, moves), _bestDescending.Length);
            return (startPosition + _worstAscending.Take(count).Sum(), startPosition + _bestDescending.Take(count).Sum());
        }
    }
}
