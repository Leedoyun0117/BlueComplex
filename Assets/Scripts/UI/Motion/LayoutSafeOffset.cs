using UnityEngine;

namespace BlueComplex.UI.Motion
{
    /// <summary>
    /// 레이아웃 그룹 자식의 위치를 트윈으로 움직일 때 쓰는 오프셋. 레이아웃이 다시 계산되면 자식 위치는 기본 자리로 덮어써지므로
    /// 절대 좌표를 트윈하지 않고 "지금 적용된 오프셋과의 차이"만 더한다 — 레이아웃이 중간에 위치를 되돌려도 오차가 쌓이지 않는다.
    /// </summary>
    public sealed class LayoutSafeOffset
    {
        private readonly RectTransform _rect;
        private Vector2 _applied;

        public LayoutSafeOffset(RectTransform rect) => _rect = rect;

        public Vector2 Value => _applied;

        public void Set(Vector2 value)
        {
            _rect.anchoredPosition += value - _applied;
            _applied = value;
        }

        public void SetY(float y) => Set(new Vector2(_applied.x, y));

        public void SetX(float x) => Set(new Vector2(x, _applied.y));

        /// <summary>오프셋을 적용된 채로 잊는다 — 레이아웃이 이미 기본 자리로 되돌렸다고 알고 있을 때.</summary>
        public void Forget() => _applied = Vector2.zero;
    }
}
