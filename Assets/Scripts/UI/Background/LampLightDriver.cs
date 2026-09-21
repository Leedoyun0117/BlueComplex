using BlueComplex.Core.Stability;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 천장 램프의 Point Light 강도를 만든다: 아주 작은 플리커 × 심박수 연동 배율.
    ///
    /// 심박수 연동은 <see cref="CrtEffectDriver"/>와 같은 방식이다 — StageBootstrapper가 Bind로 Heartbeat를 넘기고,
    /// 여기서 Changed를 구독해 배율만 트윈한다. 게임 규칙 판정은 하지 않는다.
    /// CRT도 같은 이벤트로 _Brightness를 내리므로 두 효과가 겹쳐 화면이 죽지 않게 배율 폭을 아주 작게 잡았다.
    /// 이번 단계는 구조만 잡은 것이고, 값은 씬에서 보며 조정한다.
    /// </summary>
    public sealed class LampLightDriver : MonoBehaviour
    {
        [SerializeField] private Light _light;

        [Header("플리커")]
        [Tooltip("강도가 기준값 대비 ±이 비율만큼 흔들린다. 0.03 = ±3%. 눈에 거슬리면 안 된다.")]
        [SerializeField, Range(0f, 0.15f)] private float _flickerAmplitude = 0.03f;
        [Tooltip("초당 흔들림 속도(Perlin 노이즈 진행 속도). 낮을수록 느긋하게 일렁인다.")]
        [SerializeField] private float _flickerSpeed = 1.6f;

        [Header("심박수 연동 (강도 배율)")]
        [Tooltip("침체 끝(정규화 -1)에서의 배율. CRT의 _Brightness 감소와 겹치므로 작게.")]
        [SerializeField, Range(0.5f, 1f)] private float _depressedMultiplier = 0.85f;
        [Tooltip("흥분 끝(정규화 +1)에서의 배율.")]
        [SerializeField, Range(1f, 1.5f)] private float _excitedMultiplier = 1.06f;
        [SerializeField] private float _tweenDuration = 0.4f;
        [SerializeField] private Ease _tweenEase = Ease.OutQuad;

        [Header("심박수 정규화 기준 (CrtEffectDriver와 같은 값)")]
        [SerializeField] private int _normalizationCenter = 80;
        [SerializeField] private int _normalizationLower = 10;
        [SerializeField] private int _normalizationUpper = 190;

        private Heartbeat _heartbeat;
        private Tween _tween;
        private float _baseIntensity;
        private float _heartbeatMultiplier = 1f;
        private float _noiseSeed;

        private void Awake()
        {
            if (_light == null) _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
            _noiseSeed = Random.value * 100f;
        }

        /// <summary>스테이지 세션이 만들어진 뒤, 구독할 심박수를 외부에서 넘겨준다.</summary>
        public void Bind(Heartbeat heartbeat)
        {
            if (_heartbeat != null) _heartbeat.Changed -= OnHeartbeatChanged;
            _heartbeat = heartbeat;
            if (_heartbeat == null) return;

            _heartbeat.Changed += OnHeartbeatChanged;
            // 세션 시작/재시작 시점의 값으로 즉시 맞춘다(트윈 없이).
            _tween?.Kill();
            _heartbeatMultiplier = MultiplierFor(_heartbeat.Value);
        }

        private void OnDisable()
        {
            if (_heartbeat != null) _heartbeat.Changed -= OnHeartbeatChanged;
            _tween?.Kill();
        }

        private void OnHeartbeatChanged(int from, int to)
        {
            _tween?.Kill();
            _tween = DOTween.To(() => _heartbeatMultiplier, x => _heartbeatMultiplier = x, MultiplierFor(to), _tweenDuration)
                .SetEase(_tweenEase)
                .SetLink(gameObject);
        }

        private float MultiplierFor(int heartbeat)
        {
            var t = HeartbeatNormalization.Normalize(heartbeat, _normalizationCenter, _normalizationLower, _normalizationUpper);
            t = Mathf.Clamp(t, -1f, 1f);
            return t < 0f
                ? Mathf.Lerp(1f, _depressedMultiplier, -t)
                : Mathf.Lerp(1f, _excitedMultiplier, t);
        }

        private void Update()
        {
            if (_light == null) return;

            // 두 옥타브 Perlin(0~1)을 -1~1로 옮겨 진폭을 곱한다.
            var time = Time.time * _flickerSpeed;
            var noise = (Mathf.PerlinNoise(_noiseSeed, time) * 0.7f + Mathf.PerlinNoise(_noiseSeed + 17f, time * 2.7f) * 0.3f) * 2f - 1f;
            var flicker = 1f + noise * _flickerAmplitude;

            _light.intensity = _baseIntensity * _heartbeatMultiplier * flicker;
        }
    }
}
