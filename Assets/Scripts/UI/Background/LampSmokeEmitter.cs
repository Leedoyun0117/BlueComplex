using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 낡은 천장 램프 소켓에서 새어 나오는 아주 옅은 연기. 항상 은은하게 위로 흐르고,
    /// 정전 순간(<see cref="Burst"/>)에는 잠깐 더 진하게 뿜는다 — <see cref="LampLightDriver"/>의
    /// 조명 시퀀스와는 느슨하게만 엮인, 스스로 완결된 이펙트다.
    ///
    /// 파티클 모듈과 소프트 원형 텍스처를 전부 Awake에서 코드로 만든다 — 씬 파일에 파티클 커브를
    /// 손으로 심거나 별도 PNG를 아트로 받을 필요가 없다(에디터에서는 정지 화면으로 보이고,
    /// 실제 모습은 플레이 모드에서 확인한다).
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class LampSmokeEmitter : MonoBehaviour
    {
        [Header("은은한 상시 연기")]
        [Tooltip("초당 발생량. 아주 적어도 된다 — '조금이라도 자연스럽게' 나오면 충분하다.")]
        [SerializeField, Range(0f, 5f)] private float _ambientRatePerSecond = 1.2f;
        [SerializeField] private float _lifetime = 3.5f;
        [SerializeField] private float _riseSpeed = 0.35f;
        [SerializeField] private float _startSize = 0.35f;
        [SerializeField] private float _endSizeMultiplier = 3.2f;
        [Tooltip("연기가 가장 진할 때의 알파.")]
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.16f;
        [SerializeField] private Color _smokeColor = new Color(0.55f, 0.6f, 0.62f);

        [Header("정전 순간 puff")]
        [SerializeField] private int _burstCount = 10;

        [Header("정렬")]
        [Tooltip("배경 레이어(BaseRenderQueue+i, Lamp=3005, Things=3006) 사이에 끼워 넣는 큐. " +
                 "램프 아트보다 앞, 테이블 위 사물/인물보다는 뒤에 그려지게 Things와 같은 값을 쓴다.")]
        [SerializeField] private int _renderQueue = 3006;

        private static Material _cachedMaterial;
        private static Texture2D _cachedTexture;

        private ParticleSystem _ps;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            // AddComponent<ParticleSystem>()는 기본 playOnAwake=true라 이 시점에 이미 재생 중일 수 있다 —
            // 재생 중에는 duration 등 main 모듈 값을 못 바꾸므로 먼저 완전히 멈추고 구성한 뒤 다시 재생한다.
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Configure();
            _ps.Play();
        }

        /// <summary>정전이 시작되는 순간 호출: 짧게 더 진한 연기를 뿜는다(전기 합선 느낌).</summary>
        public void Burst()
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            if (_ps == null) return;
            _ps.Emit(_burstCount);
        }

        private void Configure()
        {
            var main = _ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 5f;
            main.startLifetime = _lifetime;
            main.startSpeed = 0f; // 속도는 velocityOverLifetime이 전담한다.
            main.startSize = _startSize;
            main.startColor = new Color(_smokeColor.r, _smokeColor.g, _smokeColor.b, _maxAlpha);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = _ps.emission;
            emission.enabled = true;
            emission.rateOverTime = _ambientRatePerSecond;

            var shape = _ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.05f;

            var velocityOverLifetime = _ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            // x/y/z는 전부 같은 커브 모드(TwoConstants)여야 한다 — 하나만 남겨두면 "curves must all be in the same mode" 에러가 난다.
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(_riseSpeed * 0.7f, _riseSpeed * 1.3f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            var noise = _ps.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;

            var sizeOverLifetime = _ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, _endSizeMultiplier);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            // RGB는 흰색으로 고정하고 startColor의 색을 그대로 곱해서 쓴다(여기서 회색을 또 곱하면 이중 감쇠로 어두워진다).
            // 알파만 부드럽게 나타났다 사라지도록 담당한다.
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(0.6f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var rotationOverLifetime = _ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-15f, 15f);

            var psRenderer = GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sortMode = ParticleSystemSortMode.Distance;
            psRenderer.material = GetSmokeMaterial(_renderQueue);
            psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;
        }

        private static Material GetSmokeMaterial(int renderQueue)
        {
            if (_cachedMaterial != null)
            {
                _cachedMaterial.renderQueue = renderQueue;
                return _cachedMaterial;
            }

            var shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = "Generated Smoke (Sprites/Default)" };
            material.mainTexture = GetSmokeTexture();
            material.renderQueue = renderQueue;
            _cachedMaterial = material;
            return material;
        }

        /// <summary>부드러운 원형 알파 폴오프 텍스처를 코드로 굽는다 — 별도 연기 아트 없이 자연스러운 뭉치 모양을 낸다.</summary>
        private static Texture2D GetSmokeTexture()
        {
            if (_cachedTexture != null) return _cachedTexture;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true) { name = "GeneratedSmokePuff" };
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var maxDist = center.magnitude;
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    var alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0f, 1f, dist));
                    alpha = Mathf.Pow(alpha, 1.6f); // 가우시안에 가깝게: 중심은 진하고 가장자리는 빠르게 옅어진다.
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            _cachedTexture = tex;
            return tex;
        }
    }
}
