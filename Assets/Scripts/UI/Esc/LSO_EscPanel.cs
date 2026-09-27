using System;
using BlueComplex.UI.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.Esc
{
    /// <summary>
    /// ESC 메뉴 창. 키를 누르면 창이 위에서 내려오고, 다시 누르면 올라가 사라진다.
    /// 이 클래스는 "언제 열고 닫을지"만 정한다 — 연출은 <see cref="LSO_PanelSlide"/>,
    /// 시간·입력 잠금은 <see cref="LSO_GameplayLock"/>이 맡는다.
    ///
    /// 이 컴포넌트는 창 자체가 아니라 "창을 쥐고 있는 뿌리"에 붙인다. 창을 SetActive로 껐다 켜면
    /// 그동안 Update가 돌지 않아 ESC를 못 받으므로, 대신 뿌리의 CanvasGroup으로 보임/입력을 껐다 켠다.
    /// CanvasGroup이 없으면 붙여 준다.
    ///
    /// 나중에 들어올 소리·밝기 설정은 <see cref="content"/> 아래에 자식으로 붙이고,
    /// <see cref="Opened"/>에서 현재 값을 읽어 오고 <see cref="Closed"/>에서 저장하면 된다 —
    /// 그 컴포넌트들이 이 클래스를 고칠 필요가 없게 이벤트로 열어 두었다.
    /// </summary>
    public class LSO_EscPanel : MonoBehaviour
    {
        private const string ResourcePath = "UI/Esc";

        /// <summary>씬에 이미 있으면 그것을, 없으면 <see cref="ResourcePath"/> 프리팹을 <paramref name="canvasRoot"/> 아래에 지어서 돌려준다.
        /// 게임 화면 어디서든(메인 화면 포함) ESC로 열려야 하므로 세션 캔버스에 한 번만 만들어 두고 계속 쓴다.</summary>
        public static LSO_EscPanel GetOrCreate(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;

            var existing = canvasRoot.GetComponentInChildren<LSO_EscPanel>(true);
            if (existing != null)
            {
                existing.LiftAboveGameUi();
                return existing;
            }

            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[LSO_EscPanel] 설정창 프리팹을 찾을 수 없다: Resources/{ResourcePath}.prefab");
                return null;
            }

            var instance = Instantiate(prefab, canvasRoot, false);
            instance.name = prefab.name;
            instance.transform.SetAsLastSibling();
            var panel = instance.GetComponent<LSO_EscPanel>();
            if (panel != null) panel.LiftAboveGameUi();
            return panel;
        }

        /// <summary>창을 게임 UI와 따로 그린다 — 분기 대사 장면·스테이지 시작 대화는 최상위 캔버스의 CanvasGroup 알파를 0으로 내려 게임 UI를 통째로 숨기는데,
        /// 그 캔버스에 딸린 그래픽은 알파 0이면 아예 그려지지 않는다(ignoreParentGroups만으로는 안 된다). 그래서 창이 자기 중첩 Canvas를 갖고
        /// 부모 CanvasGroup을 무시하며, 정렬 순서를 덮어써 같은 캔버스 안의 대사 오버레이·가이드 등 무엇보다 위에 그린다.
        /// 중첩 캔버스·정렬 순서·레이캐스터는 <see cref="ConfigureCanvas"/>가 맡는다(Start에서도 불리지만, 창이 처음 열리기 전에 대화가 먼저 시작될 수 있어 여기서 미리 한다).</summary>
        private void LiftAboveGameUi()
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;

            ConfigureCanvas();
        }

        [Header("설정")]
        [Tooltip("위에서 내려오는 창. 이 컴포넌트가 붙은 오브젝트가 아니라 그 안의 창을 물린다.")]
        [SerializeField] private RectTransform content;
        [SerializeField] private Key key = Key.Escape;
        [Tooltip("열려 있는 동안 게임 시간을 멈춘다. 켜면 아래 ignoreTimeScale도 같이 켜야 연출이 돈다.")]
        [SerializeField] private bool stopTime = true;
        [Tooltip("연출을 게임 시간과 무관하게 돌린다(Time.timeScale = 0 이어도 움직인다).")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Tooltip("ESC 전용 Canvas 정렬 순서. 일반 UI와 인트로 Canvas(5000)보다 앞에 표시한다.")]
        [SerializeField] private int sortingOrder = 10000;

        [Header("연출")]
        [SerializeField] private LSO_PanelSlide slide = new LSO_PanelSlide();

        /// <summary>창이 열리기 시작할 때 쏜다 — 설정 값을 화면에 채울 시점.</summary>
        public event Action Opened;

        /// <summary>창이 닫히기 시작할 때 쏜다 — 설정 값을 저장할 시점.</summary>
        public event Action Closed;

        public bool IsOpen => _opened;

        /// <summary>위에서 내려오는 창. 화면에 고정돼야 하는 것(배경막 등)은 이 아래에 두면 같이 내려가니 주의.</summary>
        public RectTransform Content => content;

        private bool _opened;
        private CanvasGroup _group;
        private LSO_GameplayLock _lock;

        private void Start()
        {
            if (content == null)
            {
                Debug.LogWarning("LSO_EscPanel content is null!", this);
                enabled = false;
                return;
            }

            ConfigureCanvas();

            // 창은 CanvasGroup으로 숨기므로 오브젝트 자체는 켜져 있어야 한다(꺼져 있으면 아예 안 그려진다).
            // activeInHierarchy가 아니라 activeSelf를 본다 — 조상이 꺼져 있는 건 여기서 켤 수 없고,
            // 그 경우엔 이 컴포넌트의 Start조차 돌지 않는다.
            if (!content.gameObject.activeSelf) content.gameObject.SetActive(true);

            if (key == Key.None)
            {
                Debug.LogWarning("[LSO_EscPanel] key가 None이라 창을 열 수 없다. Escape로 대신한다.", this);
                key = Key.Escape;
            }

            if (stopTime && !ignoreTimeScale)
            {
                // timeScale이 0인데 연출이 게임 시간을 따르면 트윈이 진행되지 않아 창이 안 나타난다.
                Debug.LogWarning("[LSO_EscPanel] stopTime이 켜져 있으면 ignoreTimeScale도 켜야 한다. " +
                    "지금 설정으로는 창이 멈춘 채로 나타나지 않는다.", this);
            }

            if (!slide.HasDuration)
                Debug.LogWarning("[LSO_EscPanel] duration이 0이라 연출 없이 즉시 나타난다.", this);

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            _lock = new LSO_GameplayLock(stopTime);

            slide.Bind(content, _group, ignoreTimeScale);
            slide.SnapClosed();
            SetInteractable(false);
            LSO_EscSilhouette.Ensure(this);
        }

        private void OnDestroy()
        {
            slide.Kill();
            // 창이 열린 채로 씬이 바뀌면 시간과 입력이 잠긴 채로 남는다.
            _lock?.Release();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current[key].wasPressedThisFrame) return;

            if (_opened) ClosePanel();
            else OpenPanel();
        }

        private void OpenPanel()
        {
            _opened = true;
            Opened?.Invoke();

            BringToFront();
            SetInteractable(true);
            _lock.Acquire();
            slide.Show();
        }

        // 씬·프리팹 오버라이드를 바꾸지 않고 모든 ESC 인스턴스에 적용한다.
        private void ConfigureCanvas()
        {
            var parentCanvas = transform.parent != null
                ? transform.parent.GetComponentInParent<Canvas>(true)
                : null;
            if (parentCanvas == null)
            {
                Debug.LogWarning("[LSO_EscPanel] 부모 Canvas가 없어 ESC 정렬 순서를 설정할 수 없다.", this);
                return;
            }

            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.worldCamera = parentCanvas.rootCanvas.worldCamera;
            canvas.additionalShaderChannels = parentCanvas.additionalShaderChannels;
            canvas.sortingLayerID = parentCanvas.sortingLayerID;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            var source = transform.parent.GetComponentInParent<DistortionCorrectedGraphicRaycaster>(true);
            var raycaster = GetComponent<DistortionCorrectedGraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<DistortionCorrectedGraphicRaycaster>();
            if (source != null)
            {
                raycaster.SetCrtMaterial(source.CrtMaterial);
                raycaster.ignoreReversedGraphics = source.ignoreReversedGraphics;
                raycaster.blockingObjects = source.blockingObjects;
                raycaster.blockingMask = source.blockingMask;
            }

            // 일반 레이캐스터가 함께 있으면 보정 전후 좌표로 클릭이 중복 판정된다.
            foreach (var existing in GetComponents<GraphicRaycaster>())
                if (existing != raycaster) existing.enabled = false;
        }

        /// <summary>같은 정렬 순서 안의 형제 순서도 정리한다. 새 UI보다 앞에 유지하는 역할은 전용 Canvas가 맡는다.</summary>
        private void BringToFront() => transform.SetAsLastSibling();

        private void ClosePanel()
        {
            _opened = false;
            Closed?.Invoke();

            // 올라가는 중에는 누를 수 없지만 뒤쪽도 눌리면 안 된다 — 레이캐스트는 다 올라간 뒤에 놓는다.
            _group.interactable = false;
            slide.Hide(() =>
            {
                SetInteractable(false);
                _lock.Release();
            });
        }

        private void SetInteractable(bool on)
        {
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }
    }
}
