using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// "카메라가 확대된다"의 실체. 배경(3D 씬)과 UI는 서로 다른 카메라가 그려 CRT 후처리 한 패스에서 합쳐지므로(UiCompositorRig 참고),
    /// 어느 카메라도 움직이지 않고 합쳐진 화면을 읽는 창을 좁혀서 둘을 한꺼번에 확대한다 — CrtEffect.shader의 <c>_Zoom</c>/<c>_ZoomShift</c>.
    /// 어디를 확대할지(<paramref name="focus"/>)와 얼마나 오래 머물지는 이 클래스가 모른다 — 확대 창을 트윈으로 옮기기만 한다.
    ///
    /// 창이 화면 밖을 읽지 않도록 확대 창의 중심을 화면 안쪽으로 눌러 둔다(가장자리의 표시기를 확대해도 검은 띠가 안 생긴다).
    /// 머티리얼은 CrtEffectDriver가 심박수로 흔드는 그 공유 머티리얼이다 — 이 클래스는 <c>_Zoom</c>/<c>_ZoomShift</c>만 만진다.
    /// </summary>
    public sealed class CrtZoom
    {
        private static readonly int ZoomId = Shader.PropertyToID("_Zoom");
        private static readonly int ShiftId = Shader.PropertyToID("_ZoomShift");

        private readonly Material _material;
        private Tween _tween;
        private float _amount; // 0 = 원래 화면, 1 = 완전히 확대
        private float _zoom = 1f;
        private Vector2 _shift;

        /// <summary>머티리얼이 없거나 확대 속성이 없는 셰이더면(구버전 셰이더, CRT 없는 씬) 확대 없이 조용히 지나간다.</summary>
        public CrtZoom(Material material)
        {
            _material = material != null && material.HasProperty(ZoomId) ? material : null;
        }

        public bool IsAvailable => _material != null;

        /// <summary>지금 화면이 (조금이라도) 확대돼 있는가.</summary>
        public bool IsZoomed => _amount > 0f;

        /// <summary>
        /// <paramref name="focus"/>(0~1 화면 좌표)가 있는 곳으로 <paramref name="zoom"/>배 확대한다.
        /// 확대 창이 화면 밖으로 나가지 않도록 중심을 눌러 두므로, 표시기가 가장자리에 있으면 화면 중앙까지 오지는 않는다.
        /// </summary>
        public Tween ZoomIn(Vector2 focus, float zoom, float duration, Ease ease = Ease.InOutCubic)
        {
            zoom = Mathf.Max(1f, zoom);
            var half = 0.5f / zoom;
            var center = new Vector2(Mathf.Clamp(focus.x, half, 1f - half), Mathf.Clamp(focus.y, half, 1f - half));
            var targetShift = center - new Vector2(0.5f, 0.5f);

            return Drive(1f, zoom, targetShift, duration, ease);
        }

        /// <summary>원래 화면으로 돌아온다. 확대할 때의 목표를 기억해 그대로 되감는다.</summary>
        public Tween ZoomOut(float duration, Ease ease = Ease.InOutCubic) => Drive(0f, _zoom, _shift, duration, ease);

        /// <summary>진행 중인 확대를 멈추고 바로 원래 화면으로 되돌린다(재시작). 머티리얼 에셋에 확대가 남지 않게 한다.</summary>
        public void ResetNow()
        {
            _tween?.Kill();
            _tween = null;
            _amount = 0f;
            Apply();
        }

        private Tween Drive(float target, float zoom, Vector2 shift, float duration, Ease ease)
        {
            _tween?.Kill();

            if (_material == null) return DOTween.Sequence().SetUpdate(true); // 확대가 없어도 호출자가 같은 방식으로 기다릴 수 있게 빈 트윈을 준다.

            // 확대는 목표(zoom, shift)를 기억해 두고 진행도(_amount)만 0↔1로 움직인다 — 되감을 때 같은 경로를 되돌아온다.
            _zoom = zoom;
            _shift = shift;

            _tween = DOTween.To(() => _amount, value =>
                {
                    _amount = value;
                    Apply();
                }, target, Mathf.Max(0.01f, duration))
                .SetEase(ease).SetUpdate(true).SetTarget(this);
            return _tween;
        }

        private void Apply()
        {
            if (_material == null) return;

            _material.SetFloat(ZoomId, Mathf.Lerp(1f, _zoom, _amount));
            _material.SetVector(ShiftId, _shift * _amount);
        }
    }
}
