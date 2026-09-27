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
    /// ─── 재생 중에는 머티리얼 사본 ────────────────────────────────────────
    /// 여러 레이어가 같은 .mat 에셋을 공유하면 다 같이 같은 프레임으로 움직인다.
    /// 그래서 깨어날 때 사본을 만들어 그것만 건드린다 — <b>에셋 파일은 바뀌지 않는다.</b>
    /// MaterialPropertyBlock을 안 쓰는 이유는 <c>_BaseMap_ST</c>가 UnityPerMaterial CBUFFER 안에 있어서
    /// MPB로 덮으면 SRP Batcher가 깨지기 때문이다. 매 프레임 바뀌는 시트 애니메이션은 그 손해가 크다.
    ///
    /// ─── 에디터 미리보기는 반대로 MPB ─────────────────────────────────────
    /// 편집 중에는 사본을 만들 수 없다 — 씬에 정체 모를 머티리얼이 남고 .mat 에셋을 고치면 그걸 같이 쓰는
    /// 다른 씬까지 바뀐다. 그래서 미리보기는 MaterialPropertyBlock으로 덮는다.
    /// MPB는 씬에 저장되지 않으므로 파일을 더럽히지 않고, 편집 중에는 배칭 손해도 상관없다.
    ///
    /// ─── 한계 ─────────────────────────────────────────────────────────────
    /// 프레임 길이가 전부 같다(등간격). 아세프라이트처럼 프레임마다 길이가 다른 아트를 그대로 살리려면
    /// StageIdleImportTool이 뽑는 JSON을 읽는 쪽으로 가야 한다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class LSO_LayerSheetAnimation : MonoBehaviour
    {
        [Header("시트")]
        [Tooltip("가로로 이어 붙인 프레임 수. 시트가 한 줄이어야 한다.")]
        [SerializeField, Min(1)] private int frameCount = 6;

        [Header("재생")]
        [Tooltip("초당 프레임. 0이면 첫 프레임에서 멈춘다.")]
        [SerializeField, Min(0f)] private float framesPerSecond = 6f;

        [SerializeField] private bool loop = true;

        [Tooltip("시작 프레임을 무작위로 고른다. 같은 아트를 쓰는 레이어들이 한 몸처럼 움직이는 걸 막는다.")]
        [SerializeField] private bool randomStartFrame;

        [Tooltip("게임 시간이 멈춰도(설정창 등) 계속 돌린다.")]
        [SerializeField] private bool ignoreTimeScale;

        [Header("에디터")]
        [Tooltip("Play를 누르지 않아도 Scene 뷰에서 돌린다. 편집이 번거로우면 끈다.")]
        [SerializeField] private bool previewInEditor = true;

        /// <summary>
        /// 프레임 경계에서 이웃 프레임을 집지 않도록 UV를 안쪽으로 조이는 양(텍셀).
        /// Point 필터라 눈에는 보이지 않는다. StageIdleBackground와 같은 값.
        /// </summary>
        private const float InsetTexels = 0.02f;

        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        private Renderer _renderer;
        private Material _instance;
        private MaterialPropertyBlock _block;
        private int _frame;
        private float _elapsed;
        private double _editorTime;

        /// <summary>지금 보이는 프레임(0부터).</summary>
        public int Frame => _frame;

        /// <summary>재생 중에는 사본, 편집 중에는 에셋. 프레임을 잘라낼 때 텍스처 크기를 여기서 읽는다.</summary>
        private Material Source => _instance != null ? _instance : _renderer != null ? _renderer.sharedMaterial : null;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();

            // 편집 중에는 사본을 만들지 않는다 — 씬에 정체 모를 머티리얼이 남는다.
            if (!Application.isPlaying) return;

            var source = _renderer.sharedMaterial;
            if (source == null)
            {
                Debug.LogWarning($"[{nameof(LSO_LayerSheetAnimation)}] 머티리얼이 없다 — 재생할 수 없다.", this);
                enabled = false;
                return;
            }

            _instance = new Material(source) { name = $"{source.name} (sheet)" };
            _renderer.sharedMaterial = _instance;

            if (_instance.mainTexture == null)
                Debug.LogWarning($"[{nameof(LSO_LayerSheetAnimation)}] 머티리얼에 텍스처가 없다 — 프레임을 잘라낼 수 없다.", this);

            if (randomStartFrame) _frame = Random.Range(0, frameCount);
        }

        private void OnEnable()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            ApplyFrame();

#if UNITY_EDITOR
            if (!Application.isPlaying) HookEditor();
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnhookEditor();

            // 덮어 둔 미리보기를 걷어 머티리얼 본래 값이 보이게 한다.
            if (!Application.isPlaying && _renderer != null) _renderer.SetPropertyBlock(null);
#endif
        }

        private void OnDestroy()
        {
            if (_instance != null) Destroy(_instance);
        }

        private void Update()
        {
            if (!Application.isPlaying) return; // 편집 중 재생은 EditorTick이 맡는다.

            Step(ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>흐른 시간만큼 프레임을 넘긴다. 실제로 넘어갔으면 true.</summary>
        private bool Step(float delta)
        {
            if (framesPerSecond <= 0f) return false; // 0이면 정지 — 첫 프레임만 보여 준다.

            var secondsPerFrame = 1f / framesPerSecond;
            _elapsed += delta;

            // 한 프레임에 여러 장이 넘어갈 만큼 느려졌을 때도 밀리지 않게 while로 따라잡는다.
            var moved = false;
            while (_elapsed >= secondsPerFrame)
            {
                _elapsed -= secondsPerFrame;
                if (!Advance()) break;
                moved = true;
            }

            return moved;
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
            var source = Source;
            if (source == null) return;

            var texture = source.mainTexture;
            var sheetWidth = texture != null ? texture.width : 1;
            var inset = InsetTexels / sheetWidth;
            var frameScale = 1f / frameCount;

            var scale = new Vector2(frameScale - 2f * inset, 1f);
            var offset = new Vector2(_frame * frameScale + inset, 0f);

            // 재생 중 — 사본을 직접 고친다(SRP Batcher 유지).
            if (_instance != null)
            {
                _instance.mainTextureScale = scale;
                _instance.mainTextureOffset = offset;
                return;
            }

            // 편집 중 — 에셋을 건드리지 않게 PropertyBlock으로 덮는다. 씬에 저장되지 않는다.
            if (_renderer == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_block);
            _block.SetVector(BaseMapSt, new Vector4(scale.x, scale.y, offset.x, offset.y));
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>인스펙터에서 값을 고치면 바로 눈으로 확인할 수 있게 다시 잘라 준다.</summary>
        private void OnValidate()
        {
            if (frameCount < 1) frameCount = 1;
            if (_frame >= frameCount) _frame = frameCount - 1;

#if UNITY_EDITOR
            // 인스펙터에서 previewInEditor를 껐다 켰을 수 있다. OnValidate는 직렬화 중이라 바로 못 부른다.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                ApplyFrame();
                if (Application.isPlaying) return;

                UnhookEditor();
                if (isActiveAndEnabled) HookEditor();
            };
#else
            ApplyFrame();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 편집 중에는 Update가 매 프레임 돌지 않는다(에디터가 다시 그릴 때만 돈다).
        /// 그래서 EditorApplication.update에 붙어 직접 시간을 재고, 프레임이 넘어갈 때만 Scene 뷰를 다시 그린다.
        /// </summary>
        private void HookEditor()
        {
            if (!previewInEditor) return;

            _editorTime = UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.update += EditorTick;
        }

        private void UnhookEditor()
        {
            UnityEditor.EditorApplication.update -= EditorTick;
        }

        private void EditorTick()
        {
            // 컴포넌트가 사라져도 델리게이트는 남는다 — 스스로 떼어낸다.
            if (this == null)
            {
                UnityEditor.EditorApplication.update -= EditorTick;
                return;
            }

            if (Application.isPlaying || !previewInEditor || !isActiveAndEnabled) return;

            var now = UnityEditor.EditorApplication.timeSinceStartup;
            var delta = (float)(now - _editorTime);
            _editorTime = now;

            // 에디터가 한참 멈춰 있었으면(컴파일·다른 창) 밀린 시간을 다 소화하지 않고 넘긴다.
            if (delta > 0.5f) return;

            if (Step(delta)) UnityEditor.SceneView.RepaintAll();
        }
#endif
    }
}
