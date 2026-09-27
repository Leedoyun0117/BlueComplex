using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 배경 레이어 quad에 가로 스프라이트 시트를 프레임 재생으로 얹는다. Renderer가 붙은 오브젝트에 붙인다.
    ///
    /// ─── 어떻게 그리나 ────────────────────────────────────────────────────
    /// 메시는 그대로 두고 머티리얼의 tiling/offset(<c>_BaseMap_ST</c>)만 옮긴다.
    /// 배경 셰이더가 <c>TRANSFORM_TEX(uv, _BaseMap)</c>을 쓰므로 셰이더를 고칠 필요가 없다.
    /// 프레임은 왼쪽부터 오른쪽으로 <see cref="frameCount"/>장, 한 줄이어야 한다.
    ///
    /// ─── 머티리얼은 인스턴스를 만든다 ─────────────────────────────────────
    /// 여러 레이어가 같은 .mat 에셋을 공유하면 다 같이 같은 프레임으로 움직인다.
    /// (실제로 BothPeople은 Room2_LeftPeople.mat을 쓰고 있다.)
    /// 그래서 깨어날 때 사본을 만들어 그것만 건드린다 — <b>에셋 파일은 바뀌지 않는다.</b>
    ///
    /// ─── MaterialPropertyBlock을 안 쓰는 이유 ─────────────────────────────
    /// <c>_BaseMap_ST</c>는 UnityPerMaterial CBUFFER 안에 있어서 MPB로 덮으면 SRP Batcher가 깨진다.
    /// 매 프레임 바뀌는 시트 애니메이션은 그 손해가 크다. 머티리얼 사본은 배칭을 유지한다.
    ///
    /// ─── 한계 ─────────────────────────────────────────────────────────────
    /// 프레임 길이가 전부 같다(등간격). 아세프라이트처럼 프레임마다 길이가 다른 아트를 그대로 살리려면
    /// StageIdleImportTool이 뽑는 JSON을 읽는 쪽으로 가야 한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class LSO_LayerSheetAnimation : MonoBehaviour
    {
        [Header("시트")]
        [Tooltip("가로로 이어 붙인 프레임 수. 시트가 한 줄이어야 한다.")]
        [SerializeField, Min(1)] private int frameCount = 6;

        [Header("재생")]
        [Tooltip("초당 프레임. 0이면 첫 프레임에서 멈춘다.")]
        [SerializeField, Min(0f)] private float framesPerSecond = 12f;

        [SerializeField] private bool loop = true;

        [Tooltip("시작 프레임을 무작위로 고른다. 같은 아트를 쓰는 레이어들이 한 몸처럼 움직이는 걸 막는다.")]
        [SerializeField] private bool randomStartFrame;

        [Tooltip("게임 시간이 멈춰도(설정창 등) 계속 돌린다.")]
        [SerializeField] private bool ignoreTimeScale;

        /// <summary>
        /// 프레임 경계에서 이웃 프레임을 집지 않도록 UV를 안쪽으로 조이는 양(텍셀).
        /// Point 필터라 눈에는 보이지 않는다. StageIdleBackground와 같은 값.
        /// </summary>
        private const float InsetTexels = 0.02f;

        private Renderer _renderer;
        private Material _material;
        private int _frame;
        private float _elapsed;

        /// <summary>지금 보이는 프레임(0부터).</summary>
        public int Frame => _frame;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();

            var source = _renderer.sharedMaterial;
            if (source == null)
            {
                Debug.LogWarning($"[{nameof(LSO_LayerSheetAnimation)}] 머티리얼이 없다 — 재생할 수 없다.", this);
                enabled = false;
                return;
            }

            // 에셋을 공유하는 다른 레이어까지 같이 움직이지 않게 사본으로 바꾼다.
            _material = new Material(source) { name = $"{source.name} (sheet)" };
            _renderer.sharedMaterial = _material;

            if (_material.mainTexture == null)
                Debug.LogWarning($"[{nameof(LSO_LayerSheetAnimation)}] 머티리얼에 텍스처가 없다 — 프레임을 잘라낼 수 없다.", this);

            if (randomStartFrame) _frame = Random.Range(0, frameCount);
            ApplyFrame();
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            if (framesPerSecond <= 0f) return; // 0이면 정지 — 첫 프레임만 보여 준다.

            var secondsPerFrame = 1f / framesPerSecond;
            _elapsed += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

            // 한 프레임에 여러 장이 넘어갈 만큼 느려졌을 때도 밀리지 않게 while로 따라잡는다.
            while (_elapsed >= secondsPerFrame)
            {
                _elapsed -= secondsPerFrame;
                if (!Advance()) return;
            }
        }

        /// <summary>다음 프레임으로. 루프가 아니고 끝에 닿으면 스스로 멈춘다(false를 준다).</summary>
        private bool Advance()
        {
            var next = _frame + 1;
            if (next >= frameCount)
            {
                if (!loop)
                {
                    enabled = false;
                    return false;
                }

                next = 0;
            }

            SetFrame(next);
            return true;
        }

        /// <summary>프레임을 직접 지정한다. 범위를 벗어나면 잘라 낸다.</summary>
        public void SetFrame(int frame)
        {
            _frame = Mathf.Clamp(frame, 0, frameCount - 1);
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (_material == null) return;

            var texture = _material.mainTexture;
            var sheetWidth = texture != null ? texture.width : 1;
            var inset = InsetTexels / sheetWidth;
            var frameScale = 1f / frameCount;

            _material.mainTextureScale = new Vector2(frameScale - 2f * inset, 1f);
            _material.mainTextureOffset = new Vector2(_frame * frameScale + inset, 0f);
        }

        /// <summary>인스펙터에서 프레임 수를 고치면 바로 눈으로 확인할 수 있게 다시 잘라 준다.</summary>
        private void OnValidate()
        {
            if (frameCount < 1) frameCount = 1;
            if (_frame >= frameCount) _frame = frameCount - 1;
            if (Application.isPlaying) ApplyFrame();
        }
    }
}
