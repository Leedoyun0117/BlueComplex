using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 인디케이터가 극단에 닿으면 진입하고, 정확히 해제 위치로 돌아와야만 풀리는 상태.
    /// 진입/해제 조건이 비대칭이므로(그 사이 값에서는 직전 상태를 유지) 상태를 별도로 기억해야 한다.
    /// 코어는 이 bool 상태만 관리한다 — 무엇을 가릴지는 UI(표시 계층)의 책임이다.
    /// </summary>
    public sealed class CensorshipState
    {
        private readonly HashSet<int> _triggerPositions;
        private readonly int _releasePosition;

        public bool IsCensored { get; private set; }

        public event Action<bool> CensorshipChanged;

        public CensorshipState(StabilityIndicator indicator,
                               IEnumerable<int> triggerPositions,
                               int releasePosition)
        {
            if (indicator == null) throw new ArgumentNullException(nameof(indicator));
            _triggerPositions = triggerPositions.ToHashSet();
            _releasePosition = releasePosition;

            indicator.Moved += OnIndicatorMoved;
        }

        private void OnIndicatorMoved(int from, int to)
        {
            if (_triggerPositions.Contains(to)) SetCensored(true);
            else if (to == _releasePosition) SetCensored(false);
            // 그 외 위치에서는 직전 상태를 그대로 유지한다.
        }

        private void SetCensored(bool value)
        {
            if (IsCensored == value) return;
            IsCensored = value;
            CensorshipChanged?.Invoke(value);
        }
    }
}
