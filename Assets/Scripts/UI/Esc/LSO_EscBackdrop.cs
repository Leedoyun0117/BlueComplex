using UnityEngine;
using UnityEngine.UI;

namespace UI.Esc
{
    /// <summary>
    /// 설정창 뒤를 덮는 전체화면 막. 창을 쥔 뿌리(<see cref="LSO_EscPanel"/>이 붙은 오브젝트)에 붙인다.
    ///
    /// ─── 이 프로젝트에서 "덮는다"가 뜻하는 것 ─────────────────────────────
    /// UI는 화면에 직접 그려지지 않는다. UI 카메라가 RT_UI에 그리고, CRT 셰이더가
    /// <c>lerp(scene, ui.rgb, ui.a)</c>로 합성한다 — 즉 <b>RT_UI의 알파가 "씬을 얼마나 가릴지"</b>다.
    /// 앞에 놓는 것만으로는 안 가려진다. 그 자리의 알파가 1이어야 한다.
    ///
    /// ─── 반투명이면 알파가 제곱된다 ───────────────────────────────────────
    /// 유니티 기본 UI 셰이더는 Blend SrcAlpha OneMinusSrcAlpha 라, 알파 채널에도 같은 식이 적용돼
    /// 빈 배경에 그릴 때 <c>dstA = srcA²</c>가 된다. 작성한 알파 0.5는 실제로 0.25로 남아
    /// 씬이 75% 비친다. 페이드 중(알파 0.18)이면 0.03, 사실상 안 가린 것이나 같다.
    ///
    /// 그래서 이 막은 <b>절대 페이드하지 않는다</b> — 알파는 항상 1이고, 보임은 Image.enabled로만 켠다.
    /// 창의 CanvasGroup 알파가 곱해지지 않도록 자체 CanvasGroup에 ignoreParentGroups를 켠다.
    ///
    /// 한계: 닫기 시작하는 순간 막이 꺼져서, 창이 올라가는 동안(약 0.35초)은 게임 화면이 보인다.
    /// 거슬리면 "창이 화면 밖으로 나갈 때까지 유지"로 바꿔야 하는데, 그러려면 슬라이드 완료 시점을
    /// 알아야 해서 구조가 늘어난다. 지금은 단순함을 택했다.
    ///
    /// 프리팹을 건드리지 않고 런타임에 짓는다 — 에디터가 열린 채 프리팹 파일을 손대다 오브젝트가
    /// 날아간 적이 있어 더 안전한 쪽을 택했다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_EscBackdrop : MonoBehaviour
    {
        [Tooltip("막 색. 알파는 1로 둘 것 — 낮추면 제곱돼서 의도보다 훨씬 많이 비친다.")]
        [SerializeField] private Color color = new Color(0f, 0f, 0f, 1f);

        [Tooltip("만들어질 오브젝트 이름. 같은 이름의 자식이 이미 있으면 그걸 쓴다.")]
        [SerializeField] private string objectName = "Backdrop";

        private const string SealShader = "BlueComplex/LSO/UiAlphaSeal";

        private Image _backdrop;
        private Image _seal;
        private Material _sealMaterial;
        private CanvasGroup _group;
        private LSO_EscPanel _panel;

        private void Awake()
        {
            _backdrop = FindExisting() ?? Create(objectName);
            _backdrop.color = color;
            _backdrop.raycastTarget = true;
            _backdrop.rectTransform.SetAsFirstSibling(); // 맨 뒤에 깔린다(형제 순서가 곧 그리는 순서).
            _backdrop.enabled = false;

            CreateSeal();

            // 창의 페이드가 막에 곱해지지 않게 끊는다. 알파는 항상 1.
            _group = _backdrop.GetComponent<CanvasGroup>();
            if (_group == null) _group = _backdrop.gameObject.AddComponent<CanvasGroup>();
            _group.ignoreParentGroups = true;
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _panel = GetComponent<LSO_EscPanel>();
            if (_panel == null)
                Debug.LogWarning("[LSO_EscBackdrop] 같은 오브젝트에 LSO_EscPanel이 없다. 막이 켜지지 않는다.", this);
        }

        private void LateUpdate()
        {
            if (_panel == null) return;

            _backdrop.enabled = _panel.IsOpen;
            _group.blocksRaycasts = _panel.IsOpen;

            // 봉인 판은 항상 창 내용보다 뒤(형제 순서상 마지막)에 있어야 한다 —
            // 창 안에서 무언가 런타임에 추가되면 순서가 밀리므로 매 프레임 확인한다.
            if (_seal != null)
            {
                _seal.enabled = _panel.IsOpen;
                var last = transform.childCount - 1;
                if (_seal.rectTransform.GetSiblingIndex() != last) _seal.rectTransform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// UI를 다 그린 뒤 알파 채널만 1로 봉하는 판. 색은 안 건드린다.
        ///
        /// 막이 알파 1을 깔아도 그 위 반투명 요소(글자 안티에일리어싱 등)가 알파를 도로 깎는다
        /// (유니티 UI 블렌드가 알파를 제곱해서 쌓기 때문 — 자세한 건 셰이더 주석 참고).
        /// 실측으로 RT alpha 최솟값이 0.357까지 떨어져 씬이 64% 비쳤다. 이 판이 그걸 막는다.
        /// </summary>
        private void CreateSeal()
        {
            var shader = Shader.Find(SealShader);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning($"[LSO_EscBackdrop] 셰이더 '{SealShader}'를 쓸 수 없다. " +
                    "막 위의 반투명 UI가 알파를 깎아 뒤 화면이 비칠 수 있다. " +
                    "빌드에서는 Always Included Shaders에 넣을 것.", this);
                return;
            }

            _seal = Create("Alpha Seal");
            _seal.raycastTarget = false;
            _sealMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _seal.material = _sealMaterial;
            _seal.enabled = false;
            _seal.rectTransform.SetAsLastSibling();
        }

        private void OnDestroy()
        {
            if (_sealMaterial != null) Destroy(_sealMaterial);
        }

        /// <summary>손으로 이미 넣어 둔 막이 있으면 그걸 쓴다 — 두 겹으로 깔리지 않게.</summary>
        private Image FindExisting()
        {
            var existing = transform.Find(objectName);
            return existing != null ? existing.GetComponent<Image>() : null;
        }

        private Image Create(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image))
            {
                // UI 카메라가 UI 레이어만 그린다 — 기본 레이어로 만들면 화면에 안 나온다.
                layer = gameObject.layer
            };

            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go.GetComponent<Image>();
        }
    }
}
