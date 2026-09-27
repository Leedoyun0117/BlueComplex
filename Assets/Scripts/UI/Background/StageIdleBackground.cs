using System;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Presentation;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 스테이지별 배경 Idle 애니메이션(스테이지 1 파란 방 / 2 시계탑 / 3 초록 방)을 카메라 앞 한 장의 quad에 프레임 재생으로 보여 준다.
    /// 프레임은 StageIdleImportTool이 .aseprite에서 뽑아 Resources/StageIdle/에 둔 가로 스프라이트 시트 한 장이고,
    /// 재생은 텍스처 UV 오프셋만 옮긴다(Point 필터, 아트에 저장된 프레임별 길이 그대로, 무한 루프).
    ///
    /// quad는 <see cref="BackgroundLayerRig"/>와 같은 "카메라 정지 자세" 위에 놓인다 — 그래서 CRT·시차 등 기존 카메라 처리는 그대로 받는다.
    /// 아트 가로세로비가 화면(16:9)보다 넓으면(시계탑 2:1) 세로를 화면에 맞추고 좌우를 잘라낸다(cover).
    ///
    /// 켜 둔 동안(<see cref="_replaceLegacyRig"/>) 기존 다층 배경 리그(Background Rig)는 비활성화한다 — 새 아트에는 시계·램프·김이 이미 그려져 있다.
    /// 이 컴포넌트를 끄거나 없애면 리그가 원래대로 돌아온다.
    /// 씬에 배치하지 않아도 Play가 시작되면 스스로 만들어진다(<see cref="EnsureExists"/>).
    /// </summary>
    public sealed class StageIdleBackground : SessionBoundView
    {
        public const string ResourceFolder = "StageIdle";
        private const string ShaderName = "BlueComplex/Background/Layer";
        private const string ObjectName = "Stage Idle Background";
        private const float FrameAspect = 16f / 9f;
        private const float RigCanvasHeightUnits = 18f;
        private const int MinFrameMs = 20;
        private const int LastStage = 3;

        /// <summary>시트 옆의 JSON. 프레임은 왼쪽부터 오른쪽으로 frameCount장.</summary>
        [Serializable]
        public sealed class SheetData
        {
            public int frameWidth;
            public int frameHeight;
            public int frameCount;
            public int[] durationsMs;
        }

        public static string SheetName(int stage) => $"Stage{stage}Idle";

        [Tooltip("카메라 정지 자세에서 배경 quad까지의 거리(유닛). 리그의 BG 레이어(15)와 같은 자리다 — 스케일은 거리에 맞춰 자동 계산된다.")]
        [SerializeField] private float _distance = 15f;

        [Tooltip("재생 속도 배율(1 = 아트에 저장된 프레임 길이 그대로).")]
        [SerializeField, Min(0.05f)] private float _playbackSpeed = 1f;

        [Tooltip("켜면 기존 Background Rig(정적 다층 배경·조명·시계·컵 김)를 비활성화하고 이 애니메이션이 대신한다.")]
        [SerializeField] private bool _replaceLegacyRig = true;

        private BackgroundLayerRig _rig;
        private Renderer _renderer;
        private Material _material;
        private Transform _quad;

        private int _shownStage;
        private SheetData _data;
        private int _frame;
        private float _frameElapsedMs;

        // 스테이지별로 한 번만 읽는다(시트 텍스처는 Resources 에셋이라 재시작마다 다시 로드해도 같은 인스턴스지만 JSON 파싱은 아낀다).
        private readonly Texture2D[] _sheets = new Texture2D[LastStage + 1];
        private readonly SheetData[] _sheetData = new SheetData[LastStage + 1];

        /// <summary>씬에 없으면 만든다. 시트가 하나도 없으면(임포트 전) 만들지 않는다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindFirstObjectByType<StageIdleBackground>(FindObjectsInactive.Include) != null) return;
            if (Resources.Load<Texture2D>($"{ResourceFolder}/{SheetName(1)}") == null) return;

            new GameObject(ObjectName).AddComponent<StageIdleBackground>();
        }

        protected override void Awake()
        {
            _rig = FindFirstObjectByType<BackgroundLayerRig>(FindObjectsInactive.Include);
            BuildQuad();

            if (_replaceLegacyRig && _rig != null) _rig.gameObject.SetActive(false);

            // 세션이 열리기 전(오프닝 등)에도 배경이 비어 있지 않게 기본 스테이지를 먼저 보여 준다. 이후 세션 시작(Render)이 스테이지를 다시 고른다.
            ShowStage(1);
            base.Awake();
        }

        private void OnDestroy()
        {
            if (_replaceLegacyRig && _rig != null) _rig.gameObject.SetActive(true);
            if (_material != null) Destroy(_material);
        }

        protected override void Subscribe(StageSession session) { }
        protected override void Unsubscribe(StageSession session) { }

        /// <summary>새 세션(스테이지 시작/재시작) — 그 스테이지의 배경으로 바꾼다. 같은 스테이지 재시작이면 재생 위치를 유지한다.</summary>
        protected override void Render()
        {
            if (Bootstrapper != null) ShowStage(Bootstrapper.StageNumber);
        }

        private void Update()
        {
            if (_data == null || _data.frameCount <= 1) return;

            _frameElapsedMs += Time.deltaTime * 1000f * _playbackSpeed;
            var advanced = false;
            // 프레임 길이보다 긴 dt(정지 후 복귀 등)도 한 번에 따라잡되, 한 바퀴 이상은 돌지 않는다.
            for (var guard = 0; guard < _data.frameCount && _frameElapsedMs >= FrameMs(_frame); guard++)
            {
                _frameElapsedMs -= FrameMs(_frame);
                _frame = (_frame + 1) % _data.frameCount;
                advanced = true;
            }

            if (advanced) ApplyFrame();
        }

        private int FrameMs(int frame)
        {
            var durations = _data.durationsMs;
            return durations != null && frame < durations.Length ? Mathf.Max(MinFrameMs, durations[frame]) : 100;
        }

        private void ShowStage(int stage)
        {
            stage = Mathf.Clamp(stage, 1, LastStage);
            if (stage == _shownStage) return;

            if (!TryLoad(stage, out var sheet, out var data))
            {
                Debug.LogWarning($"스테이지 {stage} Idle 배경 시트를 찾지 못했습니다(Resources/{ResourceFolder}/{SheetName(stage)}). " +
                                 "메뉴 BlueComplex/Background/Import Stage Idle (Aseprite)를 실행하세요.");
                return;
            }

            _shownStage = stage;
            _data = data;
            _frame = 0;
            _frameElapsedMs = 0f;
            _material.mainTexture = sheet;
            Layout(data);
            ApplyFrame();
        }

        private bool TryLoad(int stage, out Texture2D sheet, out SheetData data)
        {
            sheet = _sheets[stage];
            data = _sheetData[stage];
            if (sheet != null && data != null) return true;

            sheet = Resources.Load<Texture2D>($"{ResourceFolder}/{SheetName(stage)}");
            var json = Resources.Load<TextAsset>($"{ResourceFolder}/{SheetName(stage)}");
            if (sheet == null || json == null) return false;

            data = JsonUtility.FromJson<SheetData>(json.text);
            if (data == null || data.frameCount < 1 || data.frameWidth < 1 || data.frameHeight < 1) return false;

            _sheets[stage] = sheet;
            _sheetData[stage] = data;
            return true;
        }

        private void BuildQuad()
        {
            var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = "Quad";
            var collider = quadObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            _quad = quadObject.transform;
            _quad.SetParent(transform, false);

            var shader = Shader.Find(ShaderName);
            if (shader == null) Debug.LogError($"셰이더 '{ShaderName}'를 찾지 못했습니다 — 스테이지 Idle 배경이 그려지지 않습니다.");

            // 조명·그림자에 영향받지 않는 아트 그대로의 색(LightGain 0, 알파는 불투명이라 보정 불필요).
            _material = new Material(shader) { name = "StageIdleBackground (runtime)" };
            _material.SetFloat("_LightGain", 0f);
            _material.SetFloat("_AlphaPower", 1f);
            _material.SetFloat("_SurfaceGlow", 0f);

            _renderer = quadObject.GetComponent<Renderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        /// <summary>카메라 정지 자세(리그 없으면 메인 카메라) 앞 거리 <see cref="_distance"/>에 놓고, 화면(16:9)을 꽉 채우도록(cover) 크기를 정한다.</summary>
        private void Layout(SheetData data)
        {
            var rigTransform = _rig != null ? _rig.transform : null;
            var camera = Camera.main;
            var pose = rigTransform != null ? rigTransform : camera != null ? camera.transform : transform;

            // 화면 높이가 그 거리에서 차지하는 월드 높이. 리그가 있으면 리그 레이어와 똑같은 계산(거리/풀프레임 거리 × 캔버스 높이).
            float frameHeight;
            if (_rig != null) frameHeight = RigCanvasHeightUnits * _rig.ScaleAt(_distance);
            else if (camera != null) frameHeight = 2f * _distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            else frameHeight = RigCanvasHeightUnits;

            var artAspect = (float)data.frameWidth / data.frameHeight;
            // 화면보다 넓은 아트는 세로 맞춤(좌우 잘림), 좁거나 같은 아트는 가로 맞춤.
            var height = artAspect >= FrameAspect ? frameHeight : frameHeight * FrameAspect / artAspect;
            var width = height * artAspect;

            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _quad.localPosition = new Vector3(0f, 0f, _distance);
            _quad.localRotation = Quaternion.identity;
            _quad.localScale = new Vector3(width, height, 1f);
        }

        private void ApplyFrame()
        {
            // 프레임 경계에서 이웃 프레임을 집지 않도록 UV를 텍셀 1/50만큼 안쪽으로 조인다(Point 필터라 눈에는 안 보인다).
            var sheetWidth = _data.frameWidth * _data.frameCount;
            var inset = 0.02f / sheetWidth;
            var frameScale = 1f / _data.frameCount;
            _material.mainTextureScale = new Vector2(frameScale - 2f * inset, 1f);
            _material.mainTextureOffset = new Vector2(_frame * frameScale + inset, 0f);
        }
    }
}
