using BlueComplex.Core.Stability;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// Heartbeat의 변화를 CRT 포스트프로세스 머티리얼 파라미터에 반영한다.
    /// 게임 규칙 판정은 하지 않으며, 코어가 발행한 이벤트를 읽어 셰이더 값만 쓴다.
    /// </summary>
    public sealed class CrtEffectDriver : MonoBehaviour
    {
        [SerializeField] private Material _crtMaterial;
        [SerializeField] private CrtEffectPreset _preset;
        [SerializeField] private float _tweenDuration = 0.4f;
        [SerializeField] private Ease _tweenEase = Ease.OutQuad;

        [Header("심박수 정규화 기준")]
        [Tooltip("정규화 t=0에 대응하는 심박수. 기본값은 시작값 80.")]
        [SerializeField] private int _normalizationCenter = 80;
        [Tooltip("정규화 t=-1에 대응하는 심박수 하한. 기본값은 매우 침체 구간의 시작인 10.")]
        [SerializeField] private int _normalizationLower = 10;
        [Tooltip("정규화 t=+1에 대응하는 심박수 상한. 기본값은 매우 흥분 구간의 끝인 190.")]
        [SerializeField] private int _normalizationUpper = 190;

        private static readonly int ScanIntensityId = Shader.PropertyToID("_ScanIntensity");
        private static readonly int ScanCountId = Shader.PropertyToID("_ScanCount");
        private static readonly int ScanThicknessId = Shader.PropertyToID("_ScanThickness");
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

        private Heartbeat _heartbeat;
        private CrtParams _current;
        private Tween _tween;

        private void Awake()
        {
            _current = _preset != null ? _preset.Neutral : CrtParams.Default;
            ApplyToMaterial(_current);
        }

        /// <summary>스테이지 세션이 만들어진 뒤, 구독할 심박수를 외부에서 넘겨준다.</summary>
        public void Bind(Heartbeat heartbeat)
        {
            if (_heartbeat != null) _heartbeat.Changed -= OnHeartbeatChanged;
            _heartbeat = heartbeat;
            if (_heartbeat != null)
            {
                _heartbeat.Changed += OnHeartbeatChanged;
                OnHeartbeatChanged(_heartbeat.Value, _heartbeat.Value);
            }
        }

        private void OnDisable()
        {
            if (_heartbeat != null) _heartbeat.Changed -= OnHeartbeatChanged;
            _tween?.Kill();
        }

        private void OnHeartbeatChanged(int from, int to)
        {
            if (_preset == null || _crtMaterial == null || _heartbeat == null) return;

            var target = _preset.Evaluate(NormalizedT(to));
            TweenTo(target);
        }

        /// <summary>
        /// 80을 중앙(0)으로 두고 하한 10 / 상한 190을 각각 -1 / +1로 매핑한다.
        /// 안정 구간이 중앙 기준 비대칭(-9/+20)이므로 양방향을 따로 계산한다.
        /// </summary>
        private float NormalizedT(int value)
        {
            if (value == _normalizationCenter) return 0f;

            return value < _normalizationCenter
                ? (value - _normalizationCenter) / (float)(_normalizationCenter - _normalizationLower)
                : (value - _normalizationCenter) / (float)(_normalizationUpper - _normalizationCenter);
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
            _crtMaterial.SetFloat(ScanThicknessId, p.ScanThickness);
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
