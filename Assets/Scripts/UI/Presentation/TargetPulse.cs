using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 아이템 대상 선택 모드에서 "이걸 고를 수 있다"를 알리는 깜박이는 금빛 테두리. 대상이 될 수 있는 뷰(컴플렉스 행, 단서 카드)가 자기 자식으로 한 장 만들어 쓴다 —
    /// 프리팹에 미리 붙어 있지 않고 처음 강조될 때 런타임에 생기므로 프리팹 배선(스크립트 GUID)을 건드리지 않는다.
    /// 클릭은 받지 않는다(원래 뷰가 받아서 <see cref="ItemTargetSelector"/>로 넘긴다). 깜박이는 시간은 UiMotionSettings.targetPulse.
    /// </summary>
    public sealed class TargetPulse : MonoBehaviour
    {
        private const string ChildName = "Target Pulse";

        /// <summary>기본 강조색(종이 카드·어두운 판넬 위에서 잘 보인다).</summary>
        public static readonly Color Gold = new Color32(255, 200, 60, 255);

        /// <summary>노란 포스트잇 위에서는 금색이 묻히므로 쓰는 잉크색 강조.</summary>
        public static readonly Color InkBlue = new Color32(30, 84, 214, 255);

        private Color _tint = Gold;

        private Image _fill;
        private Outline _edge;
        private Tween _tween;

        /// <summary>host 아래의 강조 테두리를 켜거나 끈다. 끌 때 아직 만든 적이 없으면 아무것도 하지 않는다.</summary>
        /// <param name="tint">강조색. 배경과 겹쳐 안 보이면 다른 색을 넘긴다(기본 금색).</param>
        public static void Set(Component host, bool on, Color? tint = null)
        {
            var existing = host.transform.Find(ChildName);
            if (existing == null && !on) return;

            var pulse = existing != null ? existing.GetComponent<TargetPulse>() : Create(host.transform);
            pulse._tint = tint ?? Gold;
            pulse.gameObject.SetActive(on);
        }

        private static TargetPulse Create(Transform parent)
        {
            var rect = RuntimeUi.CreateStretched(parent, ChildName);
            rect.SetAsLastSibling();
            // 꺼 둔 채로 컴포넌트를 붙인다 — 필드를 채우기 전에 OnEnable이 돌면 깜박임이 시작되지 않는다. 호출자(Set)가 켠다.
            rect.gameObject.SetActive(false);

            // 부모가 레이아웃 그룹이어도(손패 트레이, 목록) 자리를 차지하지 않고 부모 전체를 덮는다.
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var pulse = rect.gameObject.AddComponent<TargetPulse>();
            pulse._fill = rect.gameObject.AddComponent<Image>();
            pulse._fill.sprite = RuntimeUi.RoundedRect;
            pulse._fill.type = Image.Type.Sliced;
            pulse._fill.pixelsPerUnitMultiplier = 3f;
            pulse._fill.raycastTarget = false;
            pulse._fill.color = Color.clear;

            pulse._edge = rect.gameObject.AddComponent<Outline>();
            pulse._edge.effectDistance = new Vector2(3f, -3f);
            pulse._edge.effectColor = Color.clear;
            return pulse;
        }

        private void OnEnable()
        {
            if (_fill == null) return;

            var half = Mathf.Max(0.05f, UiMotion.Settings.targetPulse * 0.5f);
            SetPhase(0f);
            _tween = DOVirtual.Float(0f, 1f, half, SetPhase)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;
        }

        private void SetPhase(float t)
        {
            _fill.color = new Color(_tint.r, _tint.g, _tint.b, Mathf.Lerp(0.10f, 0.32f, t));
            _edge.effectColor = new Color(_tint.r, _tint.g, _tint.b, Mathf.Lerp(0.55f, 1f, t));
        }
    }
}
