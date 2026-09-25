using System.Collections;
using BlueComplex.Core.Stability;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 천장 램프의 Point Light 강도를 만든다: 아주 작은 플리커 × 심박수 연동 배율 × 방 분위기 시퀀스.
    ///
    /// 심박수 연동은 <see cref="CrtEffectDriver"/>와 같은 방식이다 — StageBootstrapper가 Bind로 Heartbeat를 넘기고,
    /// 여기서 Changed를 구독해 배율만 트윈한다. 게임 규칙 판정은 하지 않는다.
    /// CRT도 같은 이벤트로 _Brightness를 내리므로 두 효과가 겹쳐 화면이 죽지 않게 배율 폭을 아주 작게 잡았다.
    ///
    /// 방 분위기 시퀀스(<see cref="_atmosphereEnabled"/>)는 게임 세션과 무관하게 항상 돈다: 느린 호흡(밝기가
    /// 커졌다 작아졌다) 위에 7~14초마다 임의 시점에 두 번 깜빡이는 것을 얹고, 그 주기가 3번 지나면 잠깐
    /// 정전됐다가 천천히 자연스럽게 되돌아온 뒤 처음부터 반복한다. 정전 순간에는 <see cref="_smoke"/>가 있으면
    /// 짧게 puff를 뿜어 합선 느낌을 더한다.
    /// </summary>
    public sealed class LampLightDriver : MonoBehaviour
    {
        [SerializeField] private Light _light;

        [Header("플리커(미세)")]
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

        [Header("방 분위기: 호흡 + 이중 플리커 + 정전")]
        [SerializeField] private bool _atmosphereEnabled = true;
        [Tooltip("빛이 세졌다 작아졌다 하는 폭. 0.12 = 기준값의 ±12%.")]
        [SerializeField, Range(0f, 0.3f)] private float _breathAmplitude = 0.12f;
        [Tooltip("호흡 한 번(밝아졌다 다시 밝아지기까지)에 걸리는 시간(초).")]
        [SerializeField] private float _breathPeriod = 4.5f;
        [Tooltip("이 구간(초) 안 임의 시점에 두 번 깜빡인다.")]
        [SerializeField] private float _flickerCycleMin = 7f;
        [SerializeField] private float _flickerCycleMax = 14f;
        [Tooltip("깜빡일 때 떨어지는 강도 배율(0에 가까울수록 어둡게 깜빡인다).")]
        [SerializeField, Range(0f, 1f)] private float _flickerDipIntensity = 0.08f;
        [SerializeField] private float _flickerDipDuration = 0.05f;
        [SerializeField] private float _flickerPairGapMin = 0.08f;
        [SerializeField] private float _flickerPairGapMax = 0.22f;
        [Tooltip("이중 플리커 주기가 이 횟수만큼 반복되면 정전이 온다.")]
        [SerializeField] private int _cyclesBeforeBlackout = 3;
        [SerializeField] private float _blackoutTripDuration = 0.1f;
        [SerializeField] private float _blackoutHoldMin = 0.6f;
        [SerializeField] private float _blackoutHoldMax = 1.4f;
        [Tooltip("정전 뒤 빛이 완전히 돌아오기까지 걸리는 시간(초). 천천히, 자연스럽게.")]
        [SerializeField] private float _restoreDuration = 3.2f;
        [Tooltip("정전 시작 순간 짧게 puff를 뿜을 연기 이펙트(선택).")]
        [SerializeField] private LampSmokeEmitter _smoke;

        [Header("전구 그림 연동")]
        [Tooltip("램프 레이어(Lamp.png)의 Quad Renderer. 이 레이어는 스스로 빛나는 아트라 조명(LightGain=0)을 안 받아서, 이 연동이 없으면 " +
                 "호흡·깜빡임·정전이 주변 벽에만 아주 약하게 비치고 전구 자체는 항상 그대로 보인다. 비우면 리그에서 Lamp/Quad를 찾는다.")]
        [SerializeField] private Renderer _bulbRenderer;
        [Tooltip("조명 밝기 변화를 전구 그림 밝기에 반영하는 정도. 호흡 최고점에서 아트 원래 밝기이고, 그보다 어두워지는 만큼에 이 배율을 곱한다. " +
                 "0이면 연동 끔(전구는 항상 그대로).")]
        [SerializeField, Range(0f, 4f)] private float _bulbSwing = 1.5f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Heartbeat _heartbeat;
        private Material _bulbMaterial;
        private Tween _tween;
        private Tween _gateTween;
        private Tween _dipTween;
        private Coroutine _atmosphereRoutine;
        private float _baseIntensity;
        private float _heartbeatMultiplier = 1f;
        private float _noiseSeed;
        private float _breathPhase;
        private float _atmosphereGate = 1f;
        private float _flickerDip = 1f;

        private void Awake()
        {
            if (_light == null) _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
            _noiseSeed = Random.value * 100f;
            _breathPhase = Random.value * 100f;

            if (_bulbRenderer == null) _bulbRenderer = FindBulbRenderer();
            // 공유 머티리얼 에셋(BG_Lamp.mat)을 건드리지 않도록 런타임 인스턴스로 바꿔서 색만 조절한다.
            if (_bulbRenderer != null) _bulbMaterial = _bulbRenderer.material;
        }

        private Renderer FindBulbRenderer()
        {
            var rig = GetComponentInParent<BackgroundLayerRig>();
            if (rig == null) return null;
            var quad = rig.transform.Find("Layers/Lamp/Quad") ?? rig.transform.Find("Lamp/Quad");
            return quad != null ? quad.GetComponent<Renderer>() : null;
        }

        private void OnDestroy()
        {
            if (_bulbMaterial != null) Destroy(_bulbMaterial);
        }

        private void OnEnable()
        {
            if (_atmosphereEnabled) _atmosphereRoutine = StartCoroutine(AtmosphereLoop());
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
            _gateTween?.Kill();
            _dipTween?.Kill();
            if (_atmosphereRoutine != null)
            {
                StopCoroutine(_atmosphereRoutine);
                _atmosphereRoutine = null;
            }
            _atmosphereGate = 1f;
            _flickerDip = 1f;
            if (_bulbMaterial != null) _bulbMaterial.SetColor(BaseColorId, Color.white); // 꺼진 채로 남지 않게.
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
            var microFlicker = 1f + noise * _flickerAmplitude;

            var breath = _atmosphereEnabled
                ? 1f + Mathf.Sin((Time.time + _breathPhase) * (Mathf.PI * 2f / Mathf.Max(0.01f, _breathPeriod))) * _breathAmplitude
                : 1f;

            _light.intensity = _baseIntensity * _heartbeatMultiplier * microFlicker * breath * _atmosphereGate * _flickerDip;
            ApplyBulbBrightness();
        }

        /// <summary>조명 배율을 전구 그림 색에도 곱한다. 배율이 호흡 최고점(1 + 진폭) 이상이면 아트 원래 밝기, 그보다 낮아질수록 어두워지고
        /// 이중 플리커의 깜빡임 순간·정전에는 (거의) 꺼진다. 아트 밝기를 넘겨 밝히지는 않는다.</summary>
        private void ApplyBulbBrightness()
        {
            if (_bulbMaterial == null || _baseIntensity <= 0f) return;

            var ratio = _light.intensity / _baseIntensity;
            var peak = 1f + (_atmosphereEnabled ? _breathAmplitude : 0f);
            var brightness = Mathf.Clamp01(1f - (peak - ratio) * _bulbSwing);
            _bulbMaterial.SetColor(BaseColorId, new Color(brightness, brightness, brightness, 1f));
        }

        // ── 방 분위기 시퀀스 ──────────────────────────────────────────────────────

        private IEnumerator AtmosphereLoop()
        {
            while (true)
            {
                for (var cycle = 0; cycle < _cyclesBeforeBlackout; cycle++)
                {
                    var cycleDuration = Random.Range(_flickerCycleMin, _flickerCycleMax);
                    var flickerAt = Random.Range(cycleDuration * 0.25f, cycleDuration * 0.75f);

                    yield return WaitSeconds(flickerAt);
                    yield return FlickerPair();
                    yield return WaitSeconds(cycleDuration - flickerAt);
                }

                yield return Blackout();
                yield return SlowRestore();
            }
        }

        private static WaitForSeconds WaitSeconds(float seconds) => new WaitForSeconds(Mathf.Max(0f, seconds));

        private IEnumerator FlickerPair()
        {
            yield return FlickerOnce();
            yield return WaitSeconds(Random.Range(_flickerPairGapMin, _flickerPairGapMax));
            yield return FlickerOnce();
        }

        private IEnumerator FlickerOnce()
        {
            _dipTween?.Kill();
            var half = _flickerDipDuration * 0.5f;
            var seq = DOTween.Sequence()
                .Append(DOTween.To(() => _flickerDip, x => _flickerDip = x, _flickerDipIntensity, half).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => _flickerDip, x => _flickerDip = x, 1f, half).SetEase(Ease.InQuad))
                .SetLink(gameObject);
            _dipTween = seq;
            yield return seq.WaitForCompletion();
        }

        private IEnumerator Blackout()
        {
            _smoke?.Burst();

            _gateTween?.Kill();
            var trip = DOTween.To(() => _atmosphereGate, x => _atmosphereGate = x, 0f, _blackoutTripDuration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject);
            _gateTween = trip;
            yield return trip.WaitForCompletion();

            yield return WaitSeconds(Random.Range(_blackoutHoldMin, _blackoutHoldMax));
        }

        private IEnumerator SlowRestore()
        {
            _gateTween?.Kill();
            var restore = DOTween.To(() => _atmosphereGate, x => _atmosphereGate = x, 1f, _restoreDuration)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
            _gateTween = restore;
            yield return restore.WaitForCompletion();

            // 완전히 안정되기 전에 살짝 한 번 더 흔들려서 "돌아오는 중" 느낌을 자연스럽게 남긴다.
            yield return FlickerOnce();
        }
    }
}
