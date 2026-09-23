using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 테이블 위 컵에서 계속 피어오르는 김. Things.png에는 김이 정적 그림으로만 그려져 있어서(움직이는 파티클/시트는
    /// 원래 없었다) 그 위에 아주 옅은 소프트 퍼프를 천천히 올려 보내 "살아 있는" 느낌만 더한다.
    ///
    /// <see cref="LampSmokeEmitter"/>처럼 파티클 모듈을 Awake에서 코드로 구성하고, 퍼프 텍스처도 그쪽 것을 재사용한다.
    /// 위치는 BackgroundSetupTool이 컵 입구 픽셀을 월드 좌표로 바꿔 배치한다(Things 레이어 바로 앞).
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class CupSteamEmitter : MonoBehaviour
    {
        public const string ObjectName = "Cup Steam";

        /// <summary>컵 입구(Things.png의 머그 윗면 중앙, 캔버스 픽셀·좌상단 원점) — 김이 피어오르기 시작하는 자리.</summary>
        public static readonly Vector2 CupMouthPixel = new Vector2(1430f, 1170f);

        /// <summary>Things 레이어(11)보다 살짝 카메라 쪽: 그 위에 얹히되 인물 레이어(10)보다는 뒤에 있다.</summary>
        public const float DefaultDistance = 10.95f;

        [Tooltip("초당 발생량. 김은 끊기지 않고 이어져 보일 만큼만.")]
        [SerializeField, Range(0f, 12f)] private float _ratePerSecond = 3.5f;
        [SerializeField] private float _lifetime = 3.4f;
        [Tooltip("올라가는 속도(월드 유닛/초). Things 레이어 기준 컵 위로 그려진 김 높이(약 1.8유닛)를 수명 안에 오르도록.")]
        [SerializeField] private float _riseSpeed = 0.5f;
        [SerializeField] private float _startSize = 0.28f;
        [SerializeField] private float _endSizeMultiplier = 2.6f;
        [Tooltip("김이 가장 진할 때의 알파. 아트에 그려진 정적 김(알파 최대 0.18)과 겹쳐도 뭉치지 않게 옅게.")]
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.22f;
        [SerializeField] private Color _steamColor = new Color(0.78f, 0.86f, 0.84f);
        [Tooltip("옆으로 살랑이는 정도(노이즈 세기).")]
        [SerializeField, Range(0f, 1f)] private float _sway = 0.28f;

        [Tooltip("Things(3006)와 같은 큐 — 같은 큐 안에선 카메라에 가까운 쪽이 나중에 그려지므로 Things 바로 앞에 놓으면 그 위에 얹힌다. " +
                 "인물(3007~)보다는 뒤에 그려진다.")]
        [SerializeField] private int _renderQueue = 3006;

        private ParticleSystem _ps;
        private Material _material;

        /// <summary>안전망: BackgroundSetupTool(Setup Background)을 다시 안 돌린 씬에서도 Play 하면 김이 나오도록,
        /// 씬에 이 컴포넌트가 없고 배경 리그가 있으면 컵 입구에 자동으로 하나 만든다. Setup Background를 돌리면
        /// 같은 자리에 씬 오브젝트로 저장되므로(위치를 손으로 옮겨도 유지) 그땐 이 경로를 안 탄다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInScene()
        {
            if (FindFirstObjectByType<CupSteamEmitter>(FindObjectsInactive.Include) != null) return;

            var rig = FindFirstObjectByType<BackgroundLayerRig>();
            if (rig == null) return;

            var go = new GameObject(ObjectName);
            go.transform.SetParent(rig.transform, false);
            go.transform.position = rig.CanvasPixelToWorld(CupMouthPixel, DefaultDistance);
            go.AddComponent<CupSteamEmitter>();
        }

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            // AddComponent<ParticleSystem>()는 playOnAwake=true라 이미 재생 중일 수 있고, 재생 중엔 main 모듈 일부를 못 바꾼다.
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Configure();
            _ps.Play();
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Configure()
        {
            var main = _ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(_lifetime * 0.8f, _lifetime * 1.2f);
            main.startSpeed = 0f; // 속도는 velocityOverLifetime이 전담한다.
            main.startSize = new ParticleSystem.MinMaxCurve(_startSize * 0.8f, _startSize * 1.2f);
            main.startColor = new Color(_steamColor.r, _steamColor.g, _steamColor.b, _maxAlpha);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            // 켜자마자 컵 위에 김이 이미 올라와 있도록 한 수명 치를 미리 돌려 둔다.
            main.prewarm = true;

            var emission = _ps.emission;
            emission.enabled = true;
            emission.rateOverTime = _ratePerSecond;

            var shape = _ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            var velocity = _ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // x/y/z 모두 같은 커브 모드(TwoConstants)여야 한다.
            velocity.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            velocity.y = new ParticleSystem.MinMaxCurve(_riseSpeed * 0.75f, _riseSpeed * 1.25f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var noise = _ps.noise;
            noise.enabled = true;
            noise.strength = _sway;
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.35f;
            noise.damping = true;

            var size = _ps.sizeOverLifetime;
            size.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, _endSizeMultiplier);
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var color = _ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0.55f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            var rotation = _ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-12f, 12f);

            var psRenderer = GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sortMode = ParticleSystemSortMode.Distance;
            psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;

            // 램프 연기와 재질을 공유하지 않는다 — 렌더 큐가 다르게 조정돼도 서로 안 덮어쓴다.
            _material = new Material(Shader.Find("Sprites/Default")) { name = "Generated Cup Steam (Sprites/Default)" };
            _material.mainTexture = LampSmokeEmitter.GetSmokeTexture();
            _material.renderQueue = _renderQueue;
            psRenderer.material = _material;
        }
    }
}
