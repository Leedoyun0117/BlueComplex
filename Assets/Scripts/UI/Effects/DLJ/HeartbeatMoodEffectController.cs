using System.Collections.Generic;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlueComplex.UI.Effects.DLJ
{
    /// <summary>
    /// UI 연출 기획의 침체(파란 색감/흰 광원/수중 번짐), 흥분(진입 색수차 후 핑크 단색/2초 글리치).
    /// 기존 CRT 패스의 런타임 머티리얼만 교체한다. BGM과 게임 심박수는 변경하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("BlueComplex/DLJ/Heartbeat Mood Effects")]
    public sealed class HeartbeatMoodEffectController : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private FullScreenPassRendererFeature _crtFeature;
        [SerializeField] private Shader _effectShader;
        [SerializeField] private CrtEffectPreset _basePreset;
        [SerializeField] private StageBootstrapper _bootstrapper;
        [SerializeField] private HeartRateController _heartRate;
        [Tooltip("침체 중 흰색으로 바꿀 광원. 비워두면 같은 씬의 활성 Point/Spot Light를 사용한다.")]
        [SerializeField] private Light[] _lights;
        [Tooltip("배경을 렌더링하는 카메라. 비워두면 같은 씬의 Main Camera를 사용한다.")]
        [SerializeField] private Camera _sceneCamera;
        [Tooltip("수중 산란의 출발점으로 허용할 전등(최대 8개). 비워두면 위 광원 중 Point Light만 사용한다.")]
        [SerializeField] private Light[] _scatterLights;
        [Tooltip("전등 중심에서 발광부를 포함하는 반경. 화면 높이 기준이며 빛이 퍼지는 폭과는 별개다.")]
        [SerializeField, Range(0.005f, 0.1f)] private float _waterSourceRadius = 0.035f;

        [Header("전환")]
        [SerializeField, Min(0f)] private float _transitionSeconds = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _normalStateStrength = 0.75f;

        [Header("침체: 파란 수중 화면")]
        [SerializeField, ColorUsage(false)] private Color _depressedTint = new Color(0.04f, 0.36f, 1f);
        [SerializeField, Range(0f, 1f)] private float _blueTintStrength = 0.9f;
        [SerializeField, Range(1f, 1.8f)] private float _depressedContrast = 1.3f;
        [Tooltip("배경 윤곽만 선명하게 한다. UI 글자는 샤프닝에서 제외한다.")]
        [SerializeField, Range(0f, 1.5f)] private float _depressedSharpness = 0.85f;
        [SerializeField, Range(0f, 2f)] private float _waterLightStrength = 0.95f;
        [Tooltip("빛줄기 길이. 방향을 회전해도 화면 높이 기준 길이를 유지한다.")]
        [SerializeField, Range(0.005f, 0.6f)] private float _waterLightSpread = 0.28f;
        [Tooltip("값을 올릴수록 각각의 빛줄기가 가늘고 날카로워진다.")]
        [SerializeField, Range(2f, 32f)] private float _waterLightSharpness = 12f;
        [Tooltip("전등 하나당 기본 빛줄기 수. 넓은 각도에서는 중앙 보강용 광선을 자동 추가한다(최대 97개).")]
        [SerializeField, Range(8, 48)] private int _waterBeamCount = 24;
        [Tooltip("각 빛줄기 한가운데의 밝은 심 굵기. 1은 기본, 0.5는 절반, 2는 두 배. 넓어진 심 전체에 중심 밝기를 유지하며 주변 산란광과 테두리 흐림 폭은 바꾸지 않는다.")]
        [SerializeField, Range(0.1f, 5f)] private float _waterBeamWidth = 1f;
        [Tooltip("가느다란 줄과 넓은 빛 띠의 굵기 차이. 0이면 기본 굵기가 같아진다.")]
        [SerializeField, Range(0f, 1f)] private float _waterWidthVariation = 1f;
        [Tooltip("빛줄기의 Y축 회전 배율과 밝기 일렁임 강도. 전구 위치와 아래를 향하는 축은 유지하며 0이면 회전과 일렁임을 끈다.")]
        [SerializeField, Range(0f, 2f)] private float _waterWaveStrength = 1f;
        [Tooltip("빛줄기별 Y축 회전과 밝기 일렁임의 전체 속도. 각 줄기는 고정된 0.55~1.45배 속도로 회전한다. 강도 1, 속도 0.8이면 한 바퀴에 약 15~41초. 0이면 시작 모양과 밝기로 고정한다.")]
        [SerializeField, Range(0f, 3f)] private float _waterWaveSpeed = 0.8f;
        [Tooltip("빛줄기 주변의 넓은 수중 산란광 강도. 끝부분 잔광은 Water Afterglow Strength로 별도 조절한다.")]
        [SerializeField, Range(0f, 2f)] private float _waterScatterStrength = 1.15f;
        [Tooltip("전구 아래의 넓은 원뿔과 빛줄기 끝에 남는 청록 잔광 강도. 0이면 잔광을 끈다. 밝은 심의 굵기와는 독립적이다.")]
        [SerializeField, Range(0f, 2f)] private float _waterAfterglowStrength = 0.65f;
        [Tooltip("넓은 잔광의 전체 벌어짐 각도. 기본 60도는 전구 아래에서 양옆으로 퍼지는 형태다. 빛줄기의 회전 범위는 바꾸지 않는다.")]
        [SerializeField, Range(0f, 140f)] private float _waterAfterglowAngle = 60f;
        [Tooltip("빛줄기 길이에 대한 넓은 잔광의 길이 배율. 기본 1.25이며 끝으로 갈수록 투명해진다.")]
        [SerializeField, Range(0.5f, 2f)] private float _waterAfterglowLength = 1.25f;
        [Tooltip("빛과 잔광이 투명해지기 시작하는 위치. 각각의 전체 길이 기준으로 0은 전구부터, 0.5는 절반부터, 0.9는 끝 10%부터다. 시간(초)이 아닌 거리 비율이며 옆면의 부드러운 경계는 유지한다.")]
        [SerializeField, Range(0f, 0.95f)] private float _waterFadeStart = 0.45f;
        [Tooltip("Water Fade Start 이후 빛과 잔광이 옅어지는 강도. 높일수록 빨리 투명해지며, 0이어도 끝에서는 부드럽게 사라진다.")]
        [SerializeField, Range(0f, 4f)] private float _waterDistanceFade = 1.6f;
        [Tooltip("빛줄기 묶음의 고정 방향. 기본 0은 아래를 향하는 Y축 회전이며, 90=오른쪽, -90=왼쪽, 180=위.")]
        [SerializeField, Range(-180f, 180f)] private float _waterLightDirection = 0f;
        [Tooltip("빛줄기 묶음의 전체 벌어짐 각도. 0=평행, 기본 22=참고 이미지처럼 좁게, 120=넓게.")]
        [SerializeField, Range(0f, 160f)] private float _waterScatterAngle = 22f;

        [Header("흥분: 진입 색수차 후 파스텔 핑크")]
        [Tooltip("감쇠를 포함한 진입 색수차의 전체 시간. 끝나면 핑크 단색 톤만 유지한다.")]
        [SerializeField, Min(0f)] private float _chromaticSeconds = 3.5f;
        [Tooltip("색수차 시간의 마지막 구간에 잔상과 일렁임을 줄이며 핑크 톤으로 전환한다.")]
        [SerializeField, Min(0f)] private float _chromaticFadeSeconds = 1f;
        [SerializeField, Range(0f, 0.025f)] private float _pastelSeparation = 0.012f;
        [SerializeField, Range(0f, 1f)] private float _pastelStrength = 0.82f;
        [Tooltip("노션 첫 참고 이미지 기준: 원색 대신 우유빛 분홍/민트/라벤더 실루엣을 겹친다.")]
        [SerializeField, ColorUsage(false)] private Color _pastelPink = new Color(0.96f, 0.70f, 0.86f);
        [SerializeField, ColorUsage(false)] private Color _pastelMint = new Color(0.67f, 0.94f, 0.87f);
        [SerializeField, ColorUsage(false)] private Color _pastelLavender = new Color(0.78f, 0.75f, 0.97f);
        [SerializeField, Range(0f, 1f)] private float _pastelSaturation = 0.58f;
        [SerializeField, Range(0.5f, 1f)] private float _pastelContrast = 0.84f;
        [SerializeField, Range(0f, 1f)] private float _pastelGradeStrength = 0.8f;
        [SerializeField, Min(0f)] private float _glitchSeconds = 2f;
        [SerializeField, Range(0f, 0.035f)] private float _glitchDisplacement = 0.012f;

        private struct LightState
        {
            public Light Light;
            public Color Color;
            public bool UseTemperature;
        }

        private readonly List<LightState> _lightStates = new();
        private const int MaxWaterSources = 8;
        private readonly Vector4[] _waterSources = new Vector4[MaxWaterSources];
        private readonly HeartbeatZone _previewZone = new();
        private readonly HeartbeatMoodEffectState _state = new();
        private Material _originalMaterial;
        private Material _runtimeMaterial;
        private StageSession _session;
        private float _clock;
        private int _lastPresentedValue = Heartbeat.DefaultStartValue;
        private bool _preview;
        private bool _subscribedToHeartRate;

        public bool IsPreviewing => _preview;
        public int DisplayedHeartbeat => _state.DisplayedHeartbeat;
        public float GlitchRemaining => _state.GlitchRemaining;
        public float ChromaticRemaining => _state.ChromaticRemaining;

        private void OnEnable()
        {
            // 다른 씬의 UI/광원에 연결하지 않는다. 재활성화 시에도 이벤트를 다시 구독한다.
            if (_bootstrapper == null) _bootstrapper = FindInScene<StageBootstrapper>();
            if (_heartRate == null) _heartRate = FindInScene<HeartRateController>();
            if (!AcquireMaterial()) return;
            CaptureLights();
            if (_sceneCamera == null)
            {
                foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (camera.gameObject.scene == gameObject.scene && camera.CompareTag("MainCamera"))
                    {
                        _sceneCamera = camera;
                        break;
                    }
            }
            _subscribedToHeartRate = _heartRate != null && _heartRate.isActiveAndEnabled;
            if (_subscribedToHeartRate) _heartRate.HeartbeatPresented += OnPresented;
            if (_bootstrapper != null)
            {
                _bootstrapper.SessionStarted += OnSessionStarted;
                if (_bootstrapper.Session != null) OnSessionStarted(_bootstrapper.Session);
            }
            ApplyHeartbeat(_lastPresentedValue, true);
        }

        private T FindInScene<T>() where T : Component
        {
            foreach (var candidate in FindObjectsByType<T>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == gameObject.scene) return candidate;
            return null;
        }

        private bool AcquireMaterial()
        {
            if (_crtFeature == null || _crtFeature.passMaterial == null || _effectShader == null)
            {
                Debug.LogError("[DLJ Mood] CRT Feature와 Effect Shader를 연결해 줘. Tools/BlueComplex/DLJ 메뉴로 설정 가능해.", this);
                return false;
            }
            if (_crtFeature.passMaterial.shader == _effectShader)
            {
                Debug.LogError("[DLJ Mood] 이 CRT 패스를 이미 다른 Mood 컨트롤러가 사용 중이야. 하나만 활성화해 줘.", this);
                return false;
            }
            _originalMaterial = _crtFeature.passMaterial;
            _runtimeMaterial = new Material(_originalMaterial)
            {
                name = "DLJ Mood (Runtime)",
                hideFlags = HideFlags.HideAndDontSave,
                shader = _effectShader
            };
            // 기존 드라이버의 상태값이 저장된 머티리얼이어도 기본 CRT만 가져온다.
            // 곡률은 원본 값을 유지해 UI 포인터의 역왜곡 좌표와 일치시킨다.
            var p = _basePreset != null ? _basePreset.Neutral : CrtParams.Default;
            _runtimeMaterial.SetFloat("_ScanIntensity", p.ScanIntensity);
            _runtimeMaterial.SetFloat("_ScanCount", p.ScanCount);
            _runtimeMaterial.SetFloat("_ScanThickness", p.ScanThickness);
            _runtimeMaterial.SetFloat("_ScanSpeed", p.ScanSpeed);
            _runtimeMaterial.SetFloat("_Vignette", p.Vignette);
            _runtimeMaterial.SetFloat("_Bloom", p.Bloom);
            _runtimeMaterial.SetFloat("_Aberration", p.Aberration);
            _runtimeMaterial.SetFloat("_Noise", p.Noise);
            _runtimeMaterial.SetFloat("_Flicker", p.Flicker);
            _runtimeMaterial.SetFloat("_Shake", 0f);
            _runtimeMaterial.SetFloat("_TintR", 0f);
            _runtimeMaterial.SetFloat("_Pastel", 0f);
            _runtimeMaterial.SetFloat("_Brightness", p.Brightness);
            _crtFeature.passMaterial = _runtimeMaterial;
            return true;
        }

        private void CaptureLights()
        {
            _lightStates.Clear();
            var candidates = _lights != null && _lights.Length > 0
                ? _lights : FindObjectsByType<Light>(FindObjectsSortMode.None);
            var seen = new HashSet<Light>();
            foreach (var light in candidates)
            {
                if (light == null || light.gameObject.scene != gameObject.scene || !seen.Add(light)) continue;
                if ((_lights == null || _lights.Length == 0) && light.type != LightType.Point && light.type != LightType.Spot) continue;
                _lightStates.Add(new LightState { Light = light, Color = light.color, UseTemperature = light.useColorTemperature });
            }
        }

        private void OnSessionStarted(StageSession session)
        {
            if (_session != null) _session.Runner.TurnResolved -= OnTurnResolved;
            _session = session;
            // UI 없는 디버그 씬만 턴 종료에 직접 연결한다.
            if (!_subscribedToHeartRate) _session.Runner.TurnResolved += OnTurnResolved;
            _preview = false;
            _lastPresentedValue = session.Heartbeat.Value;
            ApplyHeartbeat(_lastPresentedValue, true);
        }

        private void OnTurnResolved(TurnReport report) => OnPresented(report.HeartbeatValue, false);

        private void OnPresented(int value, bool snap)
        {
            _lastPresentedValue = value;
            if (!_preview) ApplyHeartbeat(value, snap);
        }

        private void ApplyHeartbeat(int value, bool snap)
        {
            _state.TransitionSeconds = _transitionSeconds;
            _state.NormalStateStrength = _normalStateStrength;
            _state.GlitchSeconds = _glitchSeconds;
            _state.ChromaticSeconds = _chromaticSeconds;
            _state.ChromaticFadeSeconds = _chromaticFadeSeconds;
            _state.Show(value, _session?.Zone ?? _previewZone, snap);
            ApplyVisuals();
        }

        private void LateUpdate()
        {
            if (_runtimeMaterial == null) return;
            var dt = Time.unscaledDeltaTime;
            _clock += dt;
            _state.Advance(dt);
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (_runtimeMaterial == null) return;
            // 기존 클릭 보정기가 원본 머티리얼에서 읽는 곡률을 그대로 따라간다.
            if (_originalMaterial != null)
                _runtimeMaterial.SetFloat("_Curvature", _originalMaterial.GetFloat("_Curvature"));
            _runtimeMaterial.SetFloat("_MoodTime", _clock);
            _runtimeMaterial.SetFloat("_DepressedAmount", _state.Depressed);
            _runtimeMaterial.SetColor("_DepressedTint", _depressedTint);
            _runtimeMaterial.SetFloat("_BlueTintStrength", _blueTintStrength);
            _runtimeMaterial.SetFloat("_DepressedContrast", _depressedContrast);
            _runtimeMaterial.SetFloat("_DepressedSharpness", _depressedSharpness);
            _runtimeMaterial.SetFloat("_WaterLightStrength", _waterLightStrength);
            _runtimeMaterial.SetFloat("_WaterLightSpread", _waterLightSpread);
            _runtimeMaterial.SetFloat("_WaterLightSharpness", _waterLightSharpness);
            _runtimeMaterial.SetInt("_WaterBeamCount", Mathf.Clamp(_waterBeamCount, 8, 48));
            _runtimeMaterial.SetFloat("_WaterBeamWidth", Mathf.Clamp(_waterBeamWidth, 0.1f, 5f));
            _runtimeMaterial.SetFloat("_WaterWidthVariation", _waterWidthVariation);
            _runtimeMaterial.SetFloat("_WaterWaveStrength", _waterWaveStrength);
            _runtimeMaterial.SetFloat("_WaterWaveSpeed", _waterWaveSpeed);
            _runtimeMaterial.SetFloat("_WaterScatterStrength", _waterScatterStrength);
            _runtimeMaterial.SetFloat("_WaterAfterglowStrength", Mathf.Clamp(_waterAfterglowStrength, 0f, 2f));
            _runtimeMaterial.SetFloat("_WaterAfterglowAngle", Mathf.Clamp(_waterAfterglowAngle, 0f, 140f));
            _runtimeMaterial.SetFloat("_WaterAfterglowLength", Mathf.Clamp(_waterAfterglowLength, 0.5f, 2f));
            _runtimeMaterial.SetFloat("_WaterFadeStart", Mathf.Clamp(_waterFadeStart, 0f, 0.95f));
            _runtimeMaterial.SetFloat("_WaterDistanceFade", Mathf.Clamp(_waterDistanceFade, 0f, 4f));
            _runtimeMaterial.SetFloat("_WaterScatterAngle", _waterScatterAngle);
            _runtimeMaterial.SetFloat("_WaterLightDirection", _waterLightDirection);
            UpdateWaterSources();
            _runtimeMaterial.SetFloat("_ExcitedAmount", _state.Excited);
            _runtimeMaterial.SetFloat("_ExcitedBlend", Mathf.Clamp01(_state.Excited / Mathf.Max(0.001f, _normalStateStrength)));
            _runtimeMaterial.SetFloat("_ChromaticBurst", _state.ChromaticBurst);
            _runtimeMaterial.SetFloat("_PastelSeparation", _pastelSeparation);
            _runtimeMaterial.SetFloat("_PastelStrength", _pastelStrength);
            // sRGB 팔레트 그대로 전달. 셰이더의 디스플레이 색 공간에서 혼합 후 선형으로 복원한다.
            _runtimeMaterial.SetVector("_PastelPink", _pastelPink);
            _runtimeMaterial.SetVector("_PastelMint", _pastelMint);
            _runtimeMaterial.SetVector("_PastelLavender", _pastelLavender);
            _runtimeMaterial.SetFloat("_PastelSaturation", _pastelSaturation);
            _runtimeMaterial.SetFloat("_PastelContrast", _pastelContrast);
            _runtimeMaterial.SetFloat("_PastelGradeStrength", _pastelGradeStrength);
            // 마지막 0.35초만 감쇠. 2초를 넘기면 정확히 0이 된다.
            var glitch = Mathf.Clamp01(_state.GlitchRemaining / 0.35f);
            _runtimeMaterial.SetFloat("_GlitchAmount", glitch);
            _runtimeMaterial.SetFloat("_GlitchDisplacement", _glitchDisplacement);
            foreach (var saved in _lightStates)
            {
                if (saved.Light == null) continue;
                // 매우 침체가 아니어도 광원은 완전한 흰색까지 전환한다.
                var white = Mathf.Clamp01(_state.Depressed / Mathf.Max(0.001f, _normalStateStrength));
                saved.Light.color = Color.Lerp(saved.Color, Color.white, white);
                saved.Light.useColorTemperature = white <= 0f && saved.UseTemperature;
            }
        }

        private void UpdateWaterSources()
        {
            var count = 0;
            if (_sceneCamera != null && _sceneCamera.isActiveAndEnabled)
            {
                if (_scatterLights != null && _scatterLights.Length > 0)
                {
                    foreach (var light in _scatterLights) AddWaterSource(light, ref count);
                }
                else
                {
                    foreach (var saved in _lightStates)
                        if (saved.Light != null && saved.Light.type == LightType.Point)
                            AddWaterSource(saved.Light, ref count);
                }
            }
            // 연결된 광원이 없을 때 전체 화면 밝기 추출로 되돌아가지 않는다.
            _runtimeMaterial.SetInt("_WaterSourceCount", count);
            _runtimeMaterial.SetVectorArray("_WaterSources", _waterSources);
        }

        private void AddWaterSource(Light light, ref int count)
        {
            if (count >= MaxWaterSources || light == null || !light.isActiveAndEnabled
                || light.intensity <= 0f || light.gameObject.scene != gameObject.scene) return;
            var viewport = _sceneCamera.WorldToViewportPoint(light.transform.position);
            if (viewport.z < _sceneCamera.nearClipPlane || viewport.z > _sceneCamera.farClipPlane
                || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return;
            // Blit.hlsl의 텍스처 UV 원점과 맞춘다. 배럴 왜곡 전 소스 UV이므로 곡률 보정은 불필요.
            var y = SystemInfo.graphicsUVStartsAtTop ? 1f - viewport.y : viewport.y;
            var radius = Mathf.Max(0.001f, _waterSourceRadius);
            _waterSources[count++] = new Vector4(viewport.x, y,
                radius / Mathf.Max(0.001f, _sceneCamera.aspect), radius);
        }

        /// <summary>플레이 중 표시만 테스트한다. 카드/심박수/게임 결과는 바꾸지 않는다.</summary>
        public void PreviewHeartbeat(int value)
        {
            if (!Application.isPlaying || _runtimeMaterial == null) return;
            _preview = true;
            ApplyHeartbeat(value, false);
        }

        public void ResumeLive()
        {
            _preview = false;
            ApplyHeartbeat(_lastPresentedValue, true);
        }

        private void OnDisable()
        {
            if (_bootstrapper != null) _bootstrapper.SessionStarted -= OnSessionStarted;
            if (_heartRate != null && _subscribedToHeartRate) _heartRate.HeartbeatPresented -= OnPresented;
            if (_session != null) _session.Runner.TurnResolved -= OnTurnResolved;
            _session = null;
            _subscribedToHeartRate = false;
            foreach (var saved in _lightStates)
            {
                if (saved.Light == null) continue;
                saved.Light.color = saved.Color;
                saved.Light.useColorTemperature = saved.UseTemperature;
            }
            _lightStates.Clear();
            if (_crtFeature != null && _runtimeMaterial != null && _crtFeature.passMaterial == _runtimeMaterial)
                _crtFeature.passMaterial = _originalMaterial;
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
            _originalMaterial = null;
            _preview = false;
            _state.Reset();
        }
    }
}
