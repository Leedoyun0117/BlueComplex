using System;
using System.Collections;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 단서 정보 책(파일) UI(UI 가이드 "추가: 단서 정보 인터페이스"). 단서를 클릭하면 전체화면을 덮는 파일철 한 장이 펼쳐진다 —
    /// 왼쪽 페이지엔 그 단서의 사진과 시간/인물/감정 태그 쪽지가 흩어져 붙고(탭과 무관하게 항상 그대로), 오른쪽 페이지는
    /// "단서" 탭(번호·이름·스토리)과 "메뉴얼" 탭(감정·시간대·인물 분류표, 고정 내용) 사이를 오간다.
    /// 배경이나 "돌아가기"를 누르면 닫힌다. 사진은 클릭을 받지 않는 장식이다 — 확대는 기획에서 빠졌다.
    ///
    /// 모양은 Resources/UI/ClueBookPanel.prefab에 있다 — MainHud.prefab은 이미 손으로 다듬어진 상태라 그 안에 넣지 않고,
    /// 처음 필요할 때 이 프리팹을 루트 캔버스 아래에 인스턴스화한다(GetOrCreate). 클릭은 프리팹 안 Button의 onClick에
    /// 이 클래스의 Hide/ShowClueTab/ShowManualTab이 연결돼 있다. 프리팹은 ClueBookPanelPrefabTool로 처음 구웠다.
    ///
    /// public 메서드를 지우거나 이름을 바꾸면 프리팹의 onClick 배선이 조용히 끊긴다(유니티가 없어진 메서드를
    /// 에러 없이 무시한다) — 버튼은 남아 눌리는데 아무 일도 안 일어난다. 반드시 프리팹의 Button도 같이 고칠 것.
    /// </summary>
    public sealed class ClueBookPanel : MonoBehaviour
    {
        private const string ResourcePath = "UI/ClueBookPanel";

        [Header("Left page")]
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _timeCardText;
        [SerializeField] private TMP_Text _personCardText;
        [SerializeField] private TMP_Text _emotionCardText;

        [Header("Right page")]
        [SerializeField] private GameObject _clueTab;
        [SerializeField] private GameObject _manualTab;
        [SerializeField] private TMP_Text _clueNumberText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _storyText;

        [Header("Tab buttons")]
        [SerializeField] private Image _clueTabButton;
        [SerializeField] private Image _manualTabButton;
        [SerializeField] private Color _tabIdle = new Color32(150, 150, 154, 255);
        [SerializeField] private Color _tabActive = new Color32(116, 116, 120, 255);

        [Header("Page turn")]
        [Tooltip("탭을 바꿀 때 오른쪽 페이지가 책등을 축으로 넘어가는 시간(초).")]
        [SerializeField] private float _pageTurnDuration = 0.5f;

        private Tween _pageTurn;
        private Coroutine _turnRoutine;
        private LSO_PageTurnEffect _pageTurnEffect;

        // 넘김에 필요한 계층 — EnsurePageTurnEffect가 처음 한 번 찾아 둔다(프리팹에 참조를 심지 않는다).
        private RectTransform _rightPage;
        private Canvas _canvas;

        /// <summary>책이 열렸을 때(단서를 눌러 <see cref="Show"/>가 불렸을 때). 튜토리얼 가이드가 "단서를 눌러 보게" 단계를 끝내는 데 쓴다.</summary>
        public event Action Opened;

        /// <summary>"메뉴얼" 탭 버튼을 눌렀을 때.</summary>
        public event Action ManualTabShown;

        /// <summary>책이 닫혔을 때("돌아가기"나 배경을 눌렀을 때).</summary>
        public event Action Closed;

        /// <summary>"메뉴얼" 탭 버튼의 사각형 — 가이드가 이 버튼을 강조한다.</summary>
        public RectTransform ManualTabButtonRect => _manualTabButton.rectTransform;

        public static ClueBookPanel GetOrCreate(Transform canvasRoot)
        {
            var existing = canvasRoot.GetComponentInChildren<ClueBookPanel>(true);
            if (existing != null) return existing;

            var prefab = Resources.Load<ClueBookPanel>(ResourcePath);
            if (prefab == null)
                throw new System.InvalidOperationException($"단서 책 프리팹을 찾을 수 없다: Resources/{ResourcePath}.prefab");

            var panel = Instantiate(prefab, canvasRoot, false);
            panel.name = prefab.name;
            SetLayerRecursively(panel.gameObject, canvasRoot.gameObject.layer);
            panel.transform.SetAsLastSibling();
            return panel;
        }

        /// <param name="clueNumber">손패 안에서 이 카드의 자리(1부터) — 목업의 "단서 1" 번호로 쓴다. 고유 식별자라기보다
        /// "지금 손에 든 몇 번째 단서인지" 표시다.</param>
        public void Show(ClueCardViewModel vm, Sprite icon, int clueNumber)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;

            _clueNumberText.text = $"단서 {clueNumber}";
            _titleText.text = vm.Title;

            _timeCardText.text = $"시간\n{vm.TimeText}";
            _personCardText.text = $"인물\n{string.Join(", ", vm.PersonTexts)}";
            _emotionCardText.text = $"감정\n{string.Join(", ", vm.EmotionTexts)}";

            _storyText.text = vm.StoryText;

            // 책이 막 열리는 참이라 넘김 연출 없이 "단서" 탭인 상태로 시작한다(아직 꺼져 있어 트윈도 못 돈다).
            SwitchTab(showClue: true, animate: false);
            gameObject.SetActive(true);
            Opened?.Invoke();
        }

        public void Hide()
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            KillPageTurn();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>프리팹의 "단서" 탭 Button.onClick이 부른다 — 이름을 바꾸면 배선이 조용히 끊긴다.</summary>
        public void ShowClueTab() => SwitchTab(showClue: true, animate: true);

        /// <summary>프리팹의 "메뉴얼" 탭 Button.onClick이 부른다 — 이름을 바꾸면 배선이 조용히 끊긴다.</summary>
        public void ShowManualTab()
        {
            SwitchTab(showClue: false, animate: true);
            ManualTabShown?.Invoke();
        }

        /// <summary>
        /// 오른쪽 페이지를 갈아 끼운다. <paramref name="animate"/>면 페이지가 통째로 책등을 축으로 넘어간다 —
        /// 넘어가기 직전 모습을 그림 한 장으로 떠서(<see cref="LSO_PageTurnEffect"/>) 그 장만 셰이더로 접는다.
        ///
        /// 내용은 연출이 시작되자마자 바꾼다. 넘어가는 장이 아직 제자리를 덮고 있어 안 보이고,
        /// 장이 젖혀지면서 그 밑의 새 페이지가 드러난다 — 실제 책이 넘어가는 순서와 같다.
        /// </summary>
        private void SwitchTab(bool showClue, bool animate)
        {
            var incoming = showClue ? _clueTab : _manualTab;

            // 이미 펼쳐진 탭을 다시 누르면 아무것도 하지 않는다(연출 중 연타 포함).
            if (animate && incoming.activeSelf && _turnRoutine == null) return;

            _clueTabButton.color = showClue ? _tabActive : _tabIdle;
            _manualTabButton.color = showClue ? _tabIdle : _tabActive;
            BringTabToFront(showClue ? _clueTabButton : _manualTabButton,
                showClue ? _manualTabButton : _clueTabButton);

            KillPageTurn();

            if (!animate || !isActiveAndEnabled)
            {
                SetTabContent(showClue);
                return;
            }

            _turnRoutine = StartCoroutine(PlayPageTurn(showClue));
        }

        /// <summary>페이지를 뜨려면 이번 프레임에 그게 다 그려져 있어야 해서 프레임 끝까지 기다린다.
        /// 못 뜨면(UI 카메라 배선이 없는 등) 연출을 건너뛰고 바로 갈아 끼운다 — 조용히 멈추지 않는다.</summary>
        private IEnumerator PlayPageTurn(bool showClue)
        {
            yield return new WaitForEndOfFrame();

            var effect = EnsurePageTurnEffect();
            if (effect == null || !effect.TryCapture(_rightPage, _canvas))
            {
                SetTabContent(showClue);
                _turnRoutine = null;
                yield break;
            }

            UiSoundHooks.Play(UiSoundCue.Paper);

            // 뜬 그림이 제자리를 덮고 있는 동안 밑을 갈아 끼운다.
            SetTabContent(showClue);

            // 게임이 멈춰 있어도 도는 연출이라 SetUpdate(true)(언스케일드) — 책은 전체 화면을 덮는 모달이다.
            _pageTurn = DOVirtual.Float(0f, 1f, _pageTurnDuration, effect.SetProgress)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);

            yield return _pageTurn.WaitForCompletion();

            effect.Hide();
            _pageTurn = null;
            _turnRoutine = null;
        }

        private void SetTabContent(bool showClue)
        {
            _clueTab.SetActive(showClue);
            _manualTab.SetActive(!showClue);
        }

        /// <summary>넘김을 멈추고 떠 둔 장을 치운다 — 중간에 끊긴 채로 화면에 남지 않게.</summary>
        private void KillPageTurn()
        {
            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }

            _pageTurn?.Kill();
            _pageTurn = null;
            if (_pageTurnEffect != null) _pageTurnEffect.Hide();
        }

        /// <summary>
        /// 넘김에 필요한 오른쪽 페이지 사각형과 캔버스를 한 번 찾아 둔다.
        ///
        /// 프리팹에 참조를 따로 심지 않고 계층에서 끌어온다 — 참조를 심으려면 프리팹을 고쳐야 하는데,
        /// 지금 구조에서 "단서 탭의 부모 = 오른쪽 페이지"가 확정이라 그걸로 충분하다.
        /// 계층이 바뀌면 여기서 경고를 남기고 연출만 꺼진다(책 자체는 그대로 동작한다).
        /// </summary>
        private LSO_PageTurnEffect EnsurePageTurnEffect()
        {
            if (_pageTurnEffect != null) return _pageTurnEffect;

            _rightPage = _clueTab.transform.parent as RectTransform;
            _canvas = GetComponentInParent<Canvas>();

            if (_rightPage == null || _canvas == null)
            {
                Debug.LogWarning("[ClueBookPanel] 페이지 넘김에 필요한 계층(단서 탭 → 오른쪽 페이지)을 찾지 못했다. " +
                    "연출 없이 탭만 바뀐다.", this);
                return null;
            }

            _pageTurnEffect = LSO_PageTurnEffect.Create(_rightPage);
            return _pageTurnEffect;
        }

        /// <summary>고른 탭이 페이지를 덮고, 나머지는 페이지 뒤로 들어간다(기획서 — 탭은 색만 바뀌는 게 아니라 순서도 바뀐다).
        /// 탭 둘과 페이지(Spread)가 모두 Backdrop의 형제라, 형제 순서가 곧 그리는 순서다 — 맨 앞으로 보내면 페이지를 덮고
        /// 맨 뒤로 보내면 페이지에 가려진다. 탭 오른쪽이 페이지 왼쪽 가장자리를 파고들어 있어(겹치는 띠가 있어) 차이가 보인다.
        /// 겹치는 띠가 없어지면 이 순서 교체는 아무 효과도 내지 않으니, 탭이나 페이지의 가로 앵커를 바꿀 땐 겹침을 유지할 것.</summary>
        private static void BringTabToFront(Image front, Image back)
        {
            front.transform.SetAsLastSibling();
            back.transform.SetAsFirstSibling();
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
