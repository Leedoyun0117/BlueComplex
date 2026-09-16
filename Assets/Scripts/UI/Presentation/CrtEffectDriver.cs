using BlueComplex.Core.Stability;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// StabilityIndicator의 움직임을 CRT 포스트프로세스 머티리얼 파라미터에 반영한다.
    /// 게임 규칙 판정은 하지 않으며, 코어가 발행한 이벤트를 읽어 셰이더 값만 쓴다.
    /// </summary>
    public sealed class CrtEffectDriver : MonoBehaviour
    {
        [SerializeField] private Material _crtMaterial;
        [SerializeField] private CrtEffectPreset _preset;
        [SerializeField] private float _tweenDuration = 0.4f;
        [SerializeField] private Ease _tweenEase = Ease.OutQuad;

        private static readonly int ScanIntensityId = Shader.PropertyToID("_ScanIntensity");
        private static readonly int ScanCountId = Shader.PropertyToID("_ScanCount");
        private static readonly int ScanSpeedId = Shader.PropertyToID("_ScanSpeed");
        private static readonly int CurvatureId = Shader.PropertyToID("_Curvature");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int BloomId = Shader.PropertyToID("_Bloom");
        private static readonly int AberrationId = Shader.PropertyToID("_Aberration");
        private static readonly int NoiseId = Shader.PropertyToID("_Noise");
        private static readonly int FlickerId = Shader.PropertyToID("_Flicker");
        private static readonly int ShakeId = Shader.PropertyToID("_Shake");
        private static readonly int TintRId = Shader.PropertyToID("_TintR");
        private static readonly int PastelId = Shader.PropertyToID("_Pastel");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

        private StabilityIndicator _indicator;
        private CrtParams _current;
        private Tween _tween;

        private void Awake()
        {
            _current = _preset != null ? _preset.Neutral : CrtParams.Default;
            ApplyToMaterial(_current);
        }

        /// <summary>스테이지 세션이 만들어진 뒤, 구독할 인디케이터를 외부에서 넘겨준다.</summary>
        public void Bind(StabilityIndicator indicator)
        {
            if (_indicator != null) _indicator.Moved -= OnIndicatorMoved;
            _indicator = indicator;
            if (_indicator != null)
            {
                _indicator.Moved += OnIndicatorMoved;
                OnIndicatorMoved(_indicator.Position, _indicator.Position);
            }
        }

        private void OnDisable()
        {
            if (_indicator != null) _indicator.Moved -= OnIndicatorMoved;
            _tween?.Kill();
        }

        private void OnIndicatorMoved(int from, int to)
        {
            if (_preset == null || _crtMaterial == null || _indicator == null) return;

            var target = _preset.Evaluate(NormalizedT(to));
            TweenTo(target);
        }

        private float NormalizedT(int position)
        {
            var center = _indicator.Center;
            if (position == center) return 0f;

            return position < center
                ? (position - center) / (float)center
                : (position - center) / (float)(_indicator.Slots - 1 - center);
        }

        private void TweenTo(CrtParams target)
        {
            _tween?.Kill();
            var start = _current;
            _tween = DOTween.To(() => 0f, x =>
            {
                _current = CrtParams.Lerp(start, target, x);
                ApplyToMaterial(_current);
            }, 1f, _tweenDuration).SetEase(_tweenEase);
        }

        private void ApplyToMaterial(CrtParams p)
        {
            if (_crtMaterial == null) return;

            _crtMaterial.SetFloat(ScanIntensityId, p.ScanIntensity);
            _crtMaterial.SetFloat(ScanCountId, p.ScanCount);
            _crtMaterial.SetFloat(ScanSpeedId, p.ScanSpeed);
            _crtMaterial.SetFloat(CurvatureId, p.Curvature);
            _crtMaterial.SetFloat(VignetteId, p.Vignette);
            _crtMaterial.SetFloat(BloomId, p.Bloom);
            _crtMaterial.SetFloat(AberrationId, p.Aberration);
            _crtMaterial.SetFloat(NoiseId, p.Noise);
            _crtMaterial.SetFloat(FlickerId, p.Flicker);
            _crtMaterial.SetFloat(ShakeId, p.Shake);
            _crtMaterial.SetFloat(TintRId, p.TintR);
            _crtMaterial.SetFloat(PastelId, p.Pastel);
            _crtMaterial.SetFloat(BrightnessId, p.Brightness);
        }
    }
}
