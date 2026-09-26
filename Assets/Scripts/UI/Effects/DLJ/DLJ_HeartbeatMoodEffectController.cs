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
    public sealed class DLJ_HeartbeatMoodEffectController : MonoBehaviour
    {
        public enum ScatterMode { Lamps, WindowEdges }
        public enum WindowLightStyle { FineRays, SoftBands, RoomShafts, CenterShafts, WindowGlow }

        [System.Serializable]
        public sealed class WindowOutline
        {
            public string Name = "창문";
            [Tooltip("창문 테두리를 순서대로 연결한 Hierarchy 지점들. 마지막 지점은 첫 지점으로 이어져. 창살은 별도 창문 항목으로 연결할 수 있어.")]
            public List<Transform> Corners = new();
            public bool Closed = true;
            // 이전 씬의 직렬화 연결만 보존해. 산란 방향은 창문 윤곽의 바깥 법선으로 계산해.
            [HideInInspector]
            public Transform InteriorTarget;
            [HideInInspector] public float Direction = -35f;
            [Tooltip("화면 높이 기준 빛줄기 길이")]
            [Range(0.005f, 1f)] public float Length = 0.28f;
            [Range(0f, 2f)] public float Strength = 0.75f;
            [Range(0.001f, 0.05f)] public float EdgeWidth = 0.012f;
        }

        [System.Serializable]
        public sealed class MoodSettings
        {
            [Tooltip("전등 중심에서 발광부를 포함하는 반경. 화면 높이 기준이며 빛이 퍼지는 폭과는 별개다.")]
            [Range(0.005f, 0.1f)] public float WaterSourceRadius = 0.035f;

            [Header("전환")]
            [Min(0f)] public float TransitionSeconds = 0.65f;
            [Range(0f, 1f)] public float NormalStateStrength = 0.75f;

            [Header("침체: 파란 수중 화면")]
            [ColorUsage(false)] public Color DepressedTint = new Color(0.04f, 0.36f, 1f);
            [Range(0f, 1f)] public float BlueTintStrength = 0.9f;
            [Range(1f, 1.8f)] public float DepressedContrast = 1.3f;
            [Tooltip("배경 윤곽만 선명하게 한다. UI 글자는 샤프닝에서 제외한다.")]
            [Range(0f, 1.5f)] public float DepressedSharpness = 0.85f;
            [Range(0f, 2f)] public float WaterLightStrength = 0.95f;
            [Tooltip("빛줄기 길이. 방향을 회전해도 화면 높이 기준 길이를 유지한다.")]
            [Range(0.005f, 0.6f)] public float WaterLightSpread = 0.28f;
            [Tooltip("값을 올릴수록 각각의 빛줄기가 가늘고 날카로워진다.")]
            [Range(2f, 32f)] public float WaterLightSharpness = 12f;
            [Tooltip("전등 하나당 기본 빛줄기 수. 넓은 각도에서는 중앙 보강용 광선을 자동 추가한다(최대 97개).")]
            [Range(8, 48)] public int WaterBeamCount = 24;
            [Tooltip("각 빛줄기 한가운데의 밝은 심 굵기. 1은 기본, 0.5는 절반, 2는 두 배. 넓어진 심 전체에 중심 밝기를 유지하며 주변 산란광과 테두리 흐림 폭은 바꾸지 않는다.")]
            [Range(0.1f, 5f)] public float WaterBeamWidth = 1f;
            [Tooltip("가느다란 줄과 넓은 빛 띠의 굵기 차이. 0이면 기본 굵기가 같아진다.")]
            [Range(0f, 1f)] public float WaterWidthVariation = 1f;
            [Tooltip("빛줄기의 Y축 회전 배율과 밝기 일렁임 강도. 전구 위치와 아래를 향하는 축은 유지하며 0이면 회전과 일렁임을 끈다.")]
            [Range(0f, 2f)] public float WaterWaveStrength = 1f;
            [Tooltip("빛줄기별 Y축 회전과 밝기 일렁임의 전체 속도. 각 줄기는 고정된 0.55~1.45배 속도로 회전한다. 강도 1, 속도 0.8이면 한 바퀴에 약 15~41초. 0이면 시작 모양과 밝기로 고정한다.")]
            [Range(0f, 3f)] public float WaterWaveSpeed = 0.8f;
            [Tooltip("빛줄기 주변의 넓은 수중 산란광 강도. 끝부분 잔광은 Water Afterglow Strength로 별도 조절한다.")]
            [Range(0f, 2f)] public float WaterScatterStrength = 1.15f;
            [Tooltip("전구 아래의 넓은 원뿔과 빛줄기 끝에 남는 청록 잔광 강도. 0이면 잔광을 끈다. 밝은 심의 굵기와는 독립적이다.")]
            [Range(0f, 2f)] public float WaterAfterglowStrength = 0.65f;
            [Tooltip("넓은 잔광의 전체 벌어짐 각도. 기본 60도는 전구 아래에서 양옆으로 퍼지는 형태다. 빛줄기의 회전 범위는 바꾸지 않는다.")]
            [Range(0f, 140f)] public float WaterAfterglowAngle = 60f;
            [Tooltip("빛줄기 길이에 대한 넓은 잔광의 길이 배율. 기본 1.25이며 끝으로 갈수록 투명해진다.")]
            [Range(0.5f, 2f)] public float WaterAfterglowLength = 1.25f;
            [Tooltip("빛과 잔광이 투명해지기 시작하는 위치. 각각의 전체 길이 기준으로 0은 전구부터, 0.5는 절반부터, 0.9는 끝 10%부터다. 시간(초)이 아닌 거리 비율이며 옆면의 부드러운 경계는 유지한다.")]
            [Range(0f, 0.95f)] public float WaterFadeStart = 0.45f;
            [Tooltip("Water Fade Start 이후 빛과 잔광이 옅어지는 강도. 높일수록 빨리 투명해지며, 0이어도 끝에서는 부드럽게 사라진다.")]
            [Range(0f, 4f)] public float WaterDistanceFade = 1.6f;
            [Tooltip("빛줄기 묶음의 고정 방향. 기본 0은 아래를 향하는 Y축 회전이며, 90=오른쪽, -90=왼쪽, 180=위.")]
            [Range(-180f, 180f)] public float WaterLightDirection = 0f;
            [Tooltip("빛줄기 묶음의 전체 벌어짐 각도. 0=평행, 기본 22=참고 이미지처럼 좁게, 120=넓게.")]
            [Range(0f, 160f)] public float WaterScatterAngle = 22f;

            [Header("창문 광선 스타일: 이 방에만 적용")]
            [InspectorName("광선 방식")]
            public WindowLightStyle WindowStyle = WindowLightStyle.FineRays;
            [Tooltip("Soft Bands, Room Shafts, Center Shafts의 빛줄기 폭. 화면 높이 기준이며 기존 가는 광선에는 영향이 없어.")]
            [InspectorName("빛줄기 폭")]
            [Range(0.003f, 0.05f)] public float WindowBandWidth = 0.016f;
            [Tooltip("높일수록 빛줄기의 가장자리가 부드러워져.")]
            [InspectorName("가장자리 흐림")]
            [Range(0f, 1f)] public float WindowBandSoftness = 0.8f;
            [Tooltip("창문 광선의 전체 표시량. 0=완전히 투명, 1=현재 밝기 그대로. 빛기둥·주변 번짐·잔광이 함께 옅어져.")]
            [InspectorName("빛 불투명도")]
            [Range(0f, 1f)] public float WindowBandOpacity = 1f;
            [Tooltip("창문 광선의 가운데 밝은 부분을 강조해. 1=기본, 2=중심 밝기 두 배. 전체 투명도는 계속 적용돼.")]
            [InspectorName("중심 밝기")]
            [Range(1f, 5f)] public float WindowBandCoreBrightness = 2f;
            [Tooltip("창문 광선과 주변 확산광의 색. 선택한 방의 창문 스타일에 적용돼.")]
            [InspectorName("빛 색상")]
            [ColorUsage(false)] public Color WindowBandTint = new Color(1f, 0.97f, 0.88f);
            [Tooltip("Room Shafts와 Center Shafts의 광선을 몇 줄기씩 묶는 정도. 0=고른 간격, 1=묶음 사이 간격이 큼.")]
            [InspectorName("줄기 뭉침")]
            [Range(0f, 1f)] public float WindowShaftGrouping = 0.75f;
            [Tooltip("광선 출발점을 조금 올리고 아래쪽 광선을 약간 더 밝게 해. 위·아래 길이는 아래 두 필드에서 따로 조절해.")]
            [InspectorName("출발점 높이")]
            [Range(0f, 0.6f)] public float WindowShaftVerticalBias = 0.52f;
            [Tooltip("창문 위쪽으로 뻗는 광선의 길이. 화면 높이 기준이며 다른 방향과 독립적이야.")]
            [InspectorName("위쪽 길이")]
            [Range(0.05f, 1.5f)] public float WindowShaftTopLength = 0.18f;
            [Tooltip("창문 아래쪽으로 뻗는 광선의 길이. 화면 높이 기준이며 다른 방향과 독립적이야.")]
            [InspectorName("아래쪽 길이")]
            [Range(0.05f, 1.5f)] public float WindowShaftBottomLength = 0.85f;
            [Tooltip("창문 주변 확산광. Window Glow에서는 이 빛만 표시해. 0이면 끄고, 광선 밝기와 별도로 조절해.")]
            [InspectorName("주변 확산광")]
            [Range(0f, 0.5f)] public float WindowShaftAmbientStrength = 0.2f;

            [Header("흥분: 진입 색수차 후 파스텔 핑크")]
            [Tooltip("감쇠를 포함한 진입 색수차의 전체 시간. 끝나면 핑크 단색 톤만 유지한다.")]
            [Min(0f)] public float ChromaticSeconds = 3.5f;
            [Tooltip("색수차 시간의 마지막 구간에 잔상과 일렁임을 줄이며 핑크 톤으로 전환한다.")]
            [Min(0f)] public float ChromaticFadeSeconds = 1f;
            [Range(0f, 0.025f)] public float PastelSeparation = 0.012f;
            [Range(0f, 1f)] public float PastelStrength = 0.82f;
            [Tooltip("노션 첫 참고 이미지 기준: 원색 대신 우유빛 분홍/민트/라벤더 실루엣을 겹친다.")]
            [ColorUsage(false)] public Color PastelPink = new Color(0.96f, 0.70f, 0.86f);
            [ColorUsage(false)] public Color PastelMint = new Color(0.67f, 0.94f, 0.87f);
            [ColorUsage(false)] public Color PastelLavender = new Color(0.78f, 0.75f, 0.97f);
            [Range(0f, 1f)] public float PastelSaturation = 0.58f;
            [Range(0.5f, 1f)] public float PastelContrast = 0.84f;
            [Range(0f, 1f)] public float PastelGradeStrength = 0.8f;
            [Min(0f)] public float GlitchSeconds = 2f;
            [Range(0f, 0.035f)] public float GlitchDisplacement = 0.012f;

            public MoodSettings Clone() => (MoodSettings)MemberwiseClone();
        }

        [System.Serializable]
        public sealed class RoomBinding
        {
            public string Name = "방";
            [Tooltip("선택 사항. 비활성인 방에서는 광원과 산란을 적용하지 않아.")]
            public GameObject Root;
            public Camera SceneCamera;
            [Tooltip("침체 중 흰색으로 바꿀 광원. 비어 있으면 아무 광원도 변경하지 않아.")]
            public List<Light> Lights = new();
            public ScatterMode Mode;
            [Tooltip("전구 산란 출발점. 방 목록에서는 비어 있으면 산란을 끄고 다른 방 광원을 찾지 않아.")]
            public List<Light> ScatterLights = new();
            [Tooltip("창문 테두리 목록. 한 방에서 최대 8개 변을 렌더링해.")]
            public List<WindowOutline> Windows = new();
            [InspectorName("연출 설정")] public MoodSettings Settings = new();
            [HideInInspector] public bool SettingsInitialized;
        }

        [Header("방별 연결: Hierarchy 오브젝트를 끌어 넣어")]
        [SerializeField] private List<RoomBinding> _rooms = new();
        [Tooltip("0=방 1, 1=방 2, 2=방 3. 방 전환 코드에서는 SelectRoom(index)를 호출해.")]
        [SerializeField, Min(0)] private int _activeRoomIndex;

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

        private MoodSettings _legacySettings;

        public bool NeedsRoomSettingsInitialization => _rooms != null && _rooms.Exists(room => room != null && (!room.SettingsInitialized || room.Settings == null));

        // 이전 씬의 조정값을 최초 한 번만 복사해. 방마다 별개 인스턴스를 소유해.
        public bool InitializeRoomSettings()
        {
            if (!NeedsRoomSettingsInitialization) return false;
            var legacy = GetLegacySettings();
            foreach (var room in _rooms)
            {
                if (room == null || (room.SettingsInitialized && room.Settings != null)) continue;
                room.Settings = legacy.Clone();
                room.SettingsInitialized = true;
            }
            return true;
        }

        private MoodSettings GetSettings()
        {
            InitializeRoomSettings();
            return CurrentRoom?.Settings ?? GetLegacySettings();
        }

        private MoodSettings GetLegacySettings()
        {
            _legacySettings ??= new MoodSettings();
            _legacySettings.WaterSourceRadius = _waterSourceRadius;
            _legacySettings.TransitionSeconds = _transitionSeconds;
            _legacySettings.NormalStateStrength = _normalStateStrength;
            _legacySettings.DepressedTint = _depressedTint;
            _legacySettings.BlueTintStrength = _blueTintStrength;
            _legacySettings.DepressedContrast = _depressedContrast;
            _legacySettings.DepressedSharpness = _depressedSharpness;
            _legacySettings.WaterLightStrength = _waterLightStrength;
            _legacySettings.WaterLightSpread = _waterLightSpread;
            _legacySettings.WaterLightSharpness = _waterLightSharpness;
            _legacySettings.WaterBeamCount = _waterBeamCount;
            _legacySettings.WaterBeamWidth = _waterBeamWidth;
            _legacySettings.WaterWidthVariation = _waterWidthVariation;
            _legacySettings.WaterWaveStrength = _waterWaveStrength;
            _legacySettings.WaterWaveSpeed = _waterWaveSpeed;
            _legacySettings.WaterScatterStrength = _waterScatterStrength;
            _legacySettings.WaterAfterglowStrength = _waterAfterglowStrength;
            _legacySettings.WaterAfterglowAngle = _waterAfterglowAngle;
            _legacySettings.WaterAfterglowLength = _waterAfterglowLength;
            _legacySettings.WaterFadeStart = _waterFadeStart;
            _legacySettings.WaterDistanceFade = _waterDistanceFade;
            _legacySettings.WaterLightDirection = _waterLightDirection;
            _legacySettings.WaterScatterAngle = _waterScatterAngle;
            _legacySettings.ChromaticSeconds = _chromaticSeconds;
            _legacySettings.ChromaticFadeSeconds = _chromaticFadeSeconds;
            _legacySettings.PastelSeparation = _pastelSeparation;
            _legacySettings.PastelStrength = _pastelStrength;
            _legacySettings.PastelPink = _pastelPink;
            _legacySettings.PastelMint = _pastelMint;
            _legacySettings.PastelLavender = _pastelLavender;
            _legacySettings.PastelSaturation = _pastelSaturation;
            _legacySettings.PastelContrast = _pastelContrast;
            _legacySettings.PastelGradeStrength = _pastelGradeStrength;
            _legacySettings.GlitchSeconds = _glitchSeconds;
            _legacySettings.GlitchDisplacement = _glitchDisplacement;
            return _legacySettings;
        }

        private void ConfigureState(MoodSettings settings)
        {
            _state.TransitionSeconds = settings.TransitionSeconds;
            _state.GlitchSeconds = settings.GlitchSeconds;
            _state.ChromaticSeconds = settings.ChromaticSeconds;
            _state.ChromaticFadeSeconds = settings.ChromaticFadeSeconds;
            if (!Mathf.Approximately(_state.NormalStateStrength, settings.NormalStateStrength))
            {
                _state.NormalStateStrength = settings.NormalStateStrength;
                // 강도 목표만 다시 계산해. 같은 감정 상태의 진입 타이머는 재시작하지 않아.
                _state.Show(_state.DisplayedHeartbeat, _session?.Zone ?? _previewZone, false);
            }
        }

        private struct LightState
        {
            public Light Light;
            public Color Color;
            public bool UseTemperature;
        }

        private readonly List<LightState> _lightStates = new();
        private const int MaxWaterSources = 8;
        private readonly Vector4[] _waterSources = new Vector4[MaxWaterSources];
        public const int MaxWindowEdges = 8;
        private readonly Vector4[] _windowEdges = new Vector4[MaxWindowEdges];
        private readonly Vector4[] _windowDirections = new Vector4[MaxWindowEdges];
        private readonly Vector4[] _windowStyles = new Vector4[MaxWindowEdges];
        private RoomBinding _boundRoom;
        private bool _boundRoomEnabled;
        private bool _boundUsesRooms;
        private readonly HeartbeatZone _previewZone = new();
        private readonly DLJ_HeartbeatMoodEffectState _state = new();
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
        public int ActiveRoomIndex => _activeRoomIndex;
        public IReadOnlyList<RoomBinding> Rooms => _rooms;
        private bool UsesRooms => _rooms != null && _rooms.Count > 0;
        private RoomBinding CurrentRoom => UsesRooms && _activeRoomIndex >= 0 && _activeRoomIndex < _rooms.Count
            ? _rooms[_activeRoomIndex] : null;
        private static bool RoomEnabled(RoomBinding room) => room != null && (room.Root == null || room.Root.activeInHierarchy);
        private Camera SourceCamera => UsesRooms ? CurrentRoom?.SceneCamera : _sceneCamera;

        /// <summary>연출 연결만 교체해. 배경 활성화와 게임 상태 전환은 호출자가 처리해.</summary>
        public void SelectRoom(int index)
        {
            if (_rooms == null || index < 0 || index >= _rooms.Count) return;
            _activeRoomIndex = index;
            CaptureLights();
            ApplyVisuals();
        }

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
                Debug.LogWarning("[DLJ Mood] CRT Feature, 머티리얼 또는 Effect Shader가 없어 화면 셰이더 연출은 건너뛰고 방 광원 연출만 적용해.", this);
                return true;
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
            RestoreLights();
            _boundRoom = CurrentRoom;
            _boundUsesRooms = UsesRooms;
            _boundRoomEnabled = RoomEnabled(_boundRoom);
            if (UsesRooms && !_boundRoomEnabled) return;
            IEnumerable<Light> candidates = UsesRooms ? _boundRoom.Lights : _lights != null && _lights.Length > 0
                ? _lights : FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (candidates == null) return;
            var seen = new HashSet<Light>();
            foreach (var light in candidates)
            {
                if (light == null || light.gameObject.scene != gameObject.scene || !seen.Add(light)) continue;
                if (!UsesRooms && (_lights == null || _lights.Length == 0) && light.type != LightType.Point && light.type != LightType.Spot) continue;
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
            var settings = GetSettings();
            _state.TransitionSeconds = settings.TransitionSeconds;
            _state.NormalStateStrength = settings.NormalStateStrength;
            _state.GlitchSeconds = settings.GlitchSeconds;
            _state.ChromaticSeconds = settings.ChromaticSeconds;
            _state.ChromaticFadeSeconds = settings.ChromaticFadeSeconds;
            _state.Show(value, _session?.Zone ?? _previewZone, snap);
            ApplyVisuals();
        }

        private void LateUpdate()
        {
            if (_boundUsesRooms != UsesRooms || _boundRoom != CurrentRoom || _boundRoomEnabled != RoomEnabled(CurrentRoom))
                CaptureLights();
            ConfigureState(GetSettings());
            var dt = Time.unscaledDeltaTime;
            _clock += dt;
            _state.Advance(dt);
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            var settings = GetSettings();
            ConfigureState(settings);
            if (_runtimeMaterial != null)
            {
                // 기존 클릭 보정기가 원본 머티리얼에서 읽는 곡률을 그대로 따라간다.
                if (_originalMaterial != null)
                    _runtimeMaterial.SetFloat("_Curvature", _originalMaterial.GetFloat("_Curvature"));
                _runtimeMaterial.SetFloat("_MoodTime", _clock);
                _runtimeMaterial.SetFloat("_DepressedAmount", _state.Depressed);
                _runtimeMaterial.SetColor("_DepressedTint", settings.DepressedTint);
                _runtimeMaterial.SetFloat("_BlueTintStrength", settings.BlueTintStrength);
                _runtimeMaterial.SetFloat("_DepressedContrast", settings.DepressedContrast);
                _runtimeMaterial.SetFloat("_DepressedSharpness", settings.DepressedSharpness);
                _runtimeMaterial.SetFloat("_WaterLightStrength", settings.WaterLightStrength);
                _runtimeMaterial.SetFloat("_WaterLightSpread", settings.WaterLightSpread);
                _runtimeMaterial.SetFloat("_WaterLightSharpness", settings.WaterLightSharpness);
                _runtimeMaterial.SetInt("_WaterBeamCount", Mathf.Clamp(settings.WaterBeamCount, 8, 48));
                _runtimeMaterial.SetFloat("_WaterBeamWidth", Mathf.Clamp(settings.WaterBeamWidth, 0.1f, 5f));
                _runtimeMaterial.SetFloat("_WaterWidthVariation", settings.WaterWidthVariation);
                _runtimeMaterial.SetFloat("_WaterWaveStrength", settings.WaterWaveStrength);
                _runtimeMaterial.SetFloat("_WaterWaveSpeed", settings.WaterWaveSpeed);
                _runtimeMaterial.SetFloat("_WaterScatterStrength", settings.WaterScatterStrength);
                _runtimeMaterial.SetFloat("_WaterAfterglowStrength", Mathf.Clamp(settings.WaterAfterglowStrength, 0f, 2f));
                _runtimeMaterial.SetFloat("_WaterAfterglowAngle", Mathf.Clamp(settings.WaterAfterglowAngle, 0f, 140f));
                _runtimeMaterial.SetFloat("_WaterAfterglowLength", Mathf.Clamp(settings.WaterAfterglowLength, 0.5f, 2f));
                _runtimeMaterial.SetFloat("_WaterFadeStart", Mathf.Clamp(settings.WaterFadeStart, 0f, 0.95f));
                _runtimeMaterial.SetFloat("_WaterDistanceFade", Mathf.Clamp(settings.WaterDistanceFade, 0f, 4f));
                _runtimeMaterial.SetFloat("_WaterScatterAngle", settings.WaterScatterAngle);
                _runtimeMaterial.SetFloat("_WaterLightDirection", settings.WaterLightDirection);
                // 광선 스타일은 현재 방의 창문 모드에만 적용해. 다른 방으로 이동하면 매번 해제해.
                var windowStyle = CurrentRoom != null && CurrentRoom.Mode == ScatterMode.WindowEdges
                    ? settings.WindowStyle : WindowLightStyle.FineRays;
                _runtimeMaterial.SetInt("_WindowLightStyle", (int)windowStyle);
                _runtimeMaterial.SetFloat("_WindowBandWidth", Mathf.Clamp(settings.WindowBandWidth, 0.003f, 0.05f));
                _runtimeMaterial.SetFloat("_WindowBandSoftness", Mathf.Clamp01(settings.WindowBandSoftness));
                _runtimeMaterial.SetFloat("_WindowBandOpacity", Mathf.Clamp01(settings.WindowBandOpacity));
                _runtimeMaterial.SetFloat("_WindowBandCoreBrightness", Mathf.Clamp(settings.WindowBandCoreBrightness, 1f, 5f));
                _runtimeMaterial.SetVector("_WindowBandTint", settings.WindowBandTint);
                _runtimeMaterial.SetFloat("_WindowShaftGrouping", Mathf.Clamp01(settings.WindowShaftGrouping));
                _runtimeMaterial.SetFloat("_WindowShaftVerticalBias", Mathf.Clamp(settings.WindowShaftVerticalBias, 0f, 0.6f));
                _runtimeMaterial.SetFloat("_WindowShaftTopLength", Mathf.Clamp(settings.WindowShaftTopLength, 0.05f, 1.5f));
                _runtimeMaterial.SetFloat("_WindowShaftBottomLength", Mathf.Clamp(settings.WindowShaftBottomLength, 0.05f, 1.5f));
                _runtimeMaterial.SetFloat("_WindowShaftAmbientStrength", Mathf.Clamp(settings.WindowShaftAmbientStrength, 0f, 0.5f));
                UpdateWaterSources();
                _runtimeMaterial.SetFloat("_ExcitedAmount", _state.Excited);
                _runtimeMaterial.SetFloat("_ExcitedBlend", Mathf.Clamp01(_state.Excited / Mathf.Max(0.001f, settings.NormalStateStrength)));
                _runtimeMaterial.SetFloat("_ChromaticBurst", _state.ChromaticBurst);
                _runtimeMaterial.SetFloat("_PastelSeparation", settings.PastelSeparation);
                _runtimeMaterial.SetFloat("_PastelStrength", settings.PastelStrength);
                // sRGB 팔레트를 셰이더에 그대로 전달한다.
                _runtimeMaterial.SetVector("_PastelPink", settings.PastelPink);
                _runtimeMaterial.SetVector("_PastelMint", settings.PastelMint);
                _runtimeMaterial.SetVector("_PastelLavender", settings.PastelLavender);
                _runtimeMaterial.SetFloat("_PastelSaturation", settings.PastelSaturation);
                _runtimeMaterial.SetFloat("_PastelContrast", settings.PastelContrast);
                _runtimeMaterial.SetFloat("_PastelGradeStrength", settings.PastelGradeStrength);
                var glitch = Mathf.Clamp01(_state.GlitchRemaining / 0.35f);
                _runtimeMaterial.SetFloat("_GlitchAmount", glitch);
                _runtimeMaterial.SetFloat("_GlitchDisplacement", settings.GlitchDisplacement);
            }
            foreach (var saved in _lightStates)
            {
                if (saved.Light == null) continue;
                // 매우 침체가 아니어도 광원은 완전한 흰색까지 전환한다.
                var white = Mathf.Clamp01(_state.Depressed / Mathf.Max(0.001f, settings.NormalStateStrength));
                saved.Light.color = Color.Lerp(saved.Color, Color.white, white);
                saved.Light.useColorTemperature = white <= 0f && saved.UseTemperature;
            }
        }

        private void UpdateWaterSources()
        {
            var count = 0;
            var edgeCount = 0;
            var camera = SourceCamera;
            if (camera != null && camera.isActiveAndEnabled && camera.gameObject.scene == gameObject.scene
                && (!UsesRooms || RoomEnabled(CurrentRoom)))
            {
                if (UsesRooms)
                {
                    var room = CurrentRoom;
                    if (room.Mode == ScatterMode.WindowEdges)
                    {
                        if (room.Windows != null)
                            foreach (var window in room.Windows) AddWindowEdges(window, camera, ref edgeCount);
                    }
                    else if (room.ScatterLights != null)
                        foreach (var light in room.ScatterLights) AddWaterSource(light, ref count);
                }
                else if (_scatterLights != null && _scatterLights.Length > 0)
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
            _runtimeMaterial.SetInt("_WindowEdgeCount", edgeCount);
            _runtimeMaterial.SetVectorArray("_WindowEdges", _windowEdges);
            _runtimeMaterial.SetVectorArray("_WindowDirections", _windowDirections);
            _runtimeMaterial.SetVectorArray("_WindowStyles", _windowStyles);
        }

        private void AddWaterSource(Light light, ref int count)
        {
            if (count >= MaxWaterSources || light == null || !light.isActiveAndEnabled
                || light.intensity <= 0f || light.gameObject.scene != gameObject.scene) return;
            var camera = SourceCamera;
            var viewport = camera.WorldToViewportPoint(light.transform.position);
            if (viewport.z < camera.nearClipPlane || viewport.z > camera.farClipPlane
                || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return;
            // Blit.hlsl의 텍스처 UV 원점과 맞춘다. 배럴 왜곡 전 소스 UV이므로 곡률 보정은 불필요.
            var y = SystemInfo.graphicsUVStartsAtTop ? 1f - viewport.y : viewport.y;
            var radius = Mathf.Max(0.001f, GetSettings().WaterSourceRadius);
            _waterSources[count++] = new Vector4(viewport.x, y,
                radius / Mathf.Max(0.001f, camera.aspect), radius);
        }

        private void AddWindowEdges(WindowOutline window, Camera camera, ref int count)
        {
            if (window == null || window.Corners == null || window.Corners.Count < 2 || window.Strength <= 0f) return;
            // 화면 밖으로 잘라내기 전 전체 윤곽의 감김 방향을 사용해. 모서리 등록 순서가 반대여도 바깥은 같아.
            var signedArea = 0f;
            for (var i = 0; i < window.Corners.Count; i++)
            {
                var first = window.Corners[i];
                var next = window.Corners[(i + 1) % window.Corners.Count];
                if (first == null || next == null) continue;
                var a = camera.WorldToViewportPoint(first.position);
                var b = camera.WorldToViewportPoint(next.position);
                signedArea += a.x * b.y - b.x * a.y;
            }
            if (SystemInfo.graphicsUVStartsAtTop) signedArea = -signedArea;
            var segments = window.Closed && window.Corners.Count > 2 ? window.Corners.Count : window.Corners.Count - 1;
            for (var i = 0; i < segments && count < MaxWindowEdges; i++)
            {
                var start = window.Corners[i];
                var end = window.Corners[(i + 1) % window.Corners.Count];
                if (!ValidWindowPoint(start) || !ValidWindowPoint(end)) continue;
                var a = camera.WorldToViewportPoint(start.position);
                var b = camera.WorldToViewportPoint(end.position);
                if (!ClipWindowEdge(ref a, ref b, camera.nearClipPlane, camera.farClipPlane)) continue;
                var yA = SystemInfo.graphicsUVStartsAtTop ? 1f - a.y : a.y;
                var yB = SystemInfo.graphicsUVStartsAtTop ? 1f - b.y : b.y;
                var outward = WindowOutwardNormal(new Vector2(a.x * camera.aspect, yA),
                    new Vector2(b.x * camera.aspect, yB), signedArea);
                _windowEdges[count] = new Vector4(a.x, yA, b.x, yB);
                _windowDirections[count] = new Vector4(outward.x, outward.y, Mathf.Clamp(window.Length, 0.005f, 1f), Mathf.Clamp(window.Strength, 0f, 2f));
                _windowStyles[count++] = new Vector4(Mathf.Clamp(window.EdgeWidth, 0.001f, 0.05f), 0f, 0f, 0f);
            }
        }

        public static Vector2 WindowOutwardNormal(Vector2 a, Vector2 b, float signedArea)
        {
            var edge = b - a;
            var right = new Vector2(edge.y, -edge.x).normalized;
            return signedArea >= 0f ? right : -right;
        }

        private bool ValidWindowPoint(Transform point) => point != null && point.gameObject.activeInHierarchy
            && point.gameObject.scene == gameObject.scene;

        // 화면을 가로지르는 변은 양 끝이 화면 밖이어도 남겨. 카메라 뒤/클립 평면을 가로지르는 변은 제외해.
        public static bool ClipWindowEdge(ref Vector3 a, ref Vector3 b, float near, float far)
        {
            if (a.z < near || b.z < near || a.z > far || b.z > far) return false;
            var delta = b - a;
            if (new Vector2(delta.x, delta.y).sqrMagnitude < 0.00000001f) return false;
            var enter = 0f;
            var leave = 1f;
            if (!ClipAxis(a.x, delta.x, ref enter, ref leave) || !ClipAxis(a.y, delta.y, ref enter, ref leave)) return false;
            b = a + delta * leave;
            a += delta * enter;
            return leave - enter > 0.00001f;
        }

        private static bool ClipAxis(float origin, float delta, ref float enter, ref float leave)
        {
            if (Mathf.Abs(delta) < 0.000001f) return origin >= 0f && origin <= 1f;
            var first = -origin / delta;
            var last = (1f - origin) / delta;
            enter = Mathf.Max(enter, Mathf.Min(first, last));
            leave = Mathf.Min(leave, Mathf.Max(first, last));
            return enter <= leave;
        }

        /// <summary>플레이 중 표시만 테스트한다. 카드/심박수/게임 결과는 바꾸지 않는다.</summary>
        public void PreviewHeartbeat(int value)
        {
            if (!Application.isPlaying) return;
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
            RestoreLights();
            if (_crtFeature != null && _runtimeMaterial != null && _crtFeature.passMaterial == _runtimeMaterial)
                _crtFeature.passMaterial = _originalMaterial;
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
            _originalMaterial = null;
            _preview = false;
            _state.Reset();
        }

        private void RestoreLights()
        {
            foreach (var saved in _lightStates)
            {
                if (saved.Light == null) continue;
                saved.Light.color = saved.Color;
                saved.Light.useColorTemperature = saved.UseTemperature;
            }
            _lightStates.Clear();
        }
    }
}
