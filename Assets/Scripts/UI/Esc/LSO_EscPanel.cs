using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [Header("설정")]
        [Tooltip("위에서 내려오는 창. 이 컴포넌트가 붙은 오브젝트가 아니라 그 안의 창을 물린다.")]
        [SerializeField] private RectTransform content;
        [SerializeField] private Key key = Key.Escape;
        [Tooltip("열려 있는 동안 게임 시간을 멈춘다. 켜면 아래 ignoreTimeScale도 같이 켜야 연출이 돈다.")]
        [SerializeField] private bool stopTime = true;
        [Tooltip("연출을 게임 시간과 무관하게 돌린다(Time.timeScale = 0 이어도 움직인다).")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Header("연출")]
        [SerializeField] private LSO_PanelSlide slide = new LSO_PanelSlide();

        /// <summary>창이 열리기 시작할 때 쏜다 — 설정 값을 화면에 채울 시점.</summary>
        public event Action Opened;

        /// <summary>창이 닫히기 시작할 때 쏜다 — 설정 값을 저장할 시점.</summary>
        public event Action Closed;

        public bool IsOpen => _opened;

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

        /// <summary>
        /// 열릴 때마다 형제 중 맨 뒤(= 맨 앞에 그려짐)로 보낸다.
        ///
        /// uGUI는 형제 순서가 곧 그리는 순서인데, 이 프로젝트의 여러 패널이 런타임에 캔버스 아래에 생기면서
        /// 스스로를 SetAsLastSibling()으로 앞에 세운다(ClueBookPanel·QuarterHud·StageDialoguePlayer 등).
        /// 그래서 씬에 미리 놓아둔 ESC 창은 플레이가 진행될수록 뒤로 밀려 가려진다.
        /// 이 창은 캔버스 루트의 직속 자식이라(그 패널들과 같은 형제 레벨) 여기서 한 번 밀어 주면 앞에 선다.
        ///
        /// 부모가 캔버스 루트가 아니게 되면 이 한 줄로는 부족해진다 — 형제 순서는 같은 부모 안에서만
        /// 의미가 있어서, 더 깊이 들어가면 루트 수준의 앞뒤는 조상 가지가 정한다. 그때는 창 전용 Canvas에
        /// overrideSorting을 켜는 쪽으로 바꿔야 하는데, 그 경우 이 프로젝트에서는 GraphicRaycaster를
        /// 일반 것이 아니라 DistortionCorrectedGraphicRaycaster로 붙여야 클릭 좌표가 어긋나지 않는다.
        /// </summary>
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
