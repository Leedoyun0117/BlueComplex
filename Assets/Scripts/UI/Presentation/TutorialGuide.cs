using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 튜토리얼 가이드 오버레이 — 청장이 말풍선으로 안내하고, 지금 눌러야 할 화면 요소를 강조하고, 그 밖의 곳은 눌리지 않게 막는다.
    ///
    /// 어떤 단계에서 무엇을 가리키고 언제 다음으로 넘어가는지는 코어(<see cref="TutorialGuideFlow"/>·<see cref="TutorialGuideContent"/>)가 정한다 — 이 클래스는 그 상태를 화면에 그리고,
    /// 화면에서 일어난 일(책 열림·메뉴얼 탭·책 닫힘·엑스레이 열림·아이템 사용·턴 결산)을 코어에 알리기만 한다.
    ///
    /// · 말풍선: 청장 대사를 한 글자씩 타이핑한다. 행동을 기다리는 단계에서는 클릭을 받지 않는다(비차단) — 읽기 단계에서만 누르면 다음으로 넘어간다.
    /// · 강조: 기존 <see cref="TargetPulse"/>(아이템 대상 선택에 쓰는 깜박이는 금빛 테두리)를 그대로 쓴다.
    /// · 클릭 막: 화면 전체를 덮는 투명한 막이 강조 요소(와 그 단계에서 꼭 쓰는 요소) 안을 뺀 모든 클릭을 삼킨다.
    /// · 턴 결산은 그 턴의 결과 연출(<see cref="ITurnResultPresenter"/>)이 끝난 뒤에 알린다 — 연출이 도는 동안 다음 대사가 끼어들지 않게(연출의 타이밍은 Presenter가 쥔다).
    /// </summary>
    public sealed class TutorialGuide : MonoBehaviour
    {
        private static readonly Color BubbleInk = new Color32(30, 28, 26, 255);
        private static readonly Color NameTagColor = new Color32(104, 96, 86, 255);
        private static readonly Color NameTagInk = new Color32(238, 242, 238, 255);

        private const float BubbleWidth = 820f;
        private const float BubbleMargin = 22f;
        private const float BubbleGap = 26f;
        private const float ScreenMargin = 24f;

        /// <summary>읽기 단계의 최소/최대 머무는 시간(초) — 타이핑이 끝난 뒤 글자 수에 비례해 머물다가 저절로 넘어간다.</summary>
        private const float ReadMinSeconds = 2.2f;
        private const float ReadMaxSeconds = 7f;
        private const float ReadSecondsPerChar = 0.045f;

        /// <summary>엑스레이를 못 열고 막혀도 헤어나올 수 있게, 이만큼 지나면 말풍선을 눌러 넘길 수 있다.</summary>
        private const float StuckSkipSeconds = 20f;

        private Transform _canvasRoot;
        private RectTransform _canvasRect;
        private RectTransform _bubble;
        private Image _bubbleImage;
        private TMP_Text _bubbleText;
        private CanvasGroup _nextIndicator;
        private CanvasGroup _group;
        private GuideClickMask _mask;

        private StageSession _session;
        private TutorialGuideFlow _flow;
        private ClueBookPanel _book;
        private ITurnResultPresenter _presenter;
        private readonly Queue<int> _pendingTurns = new();

        private Tween _typing;
        private Coroutine _readRoutine;
        private bool _typingDone;
        private string _currentLine = string.Empty;
        private float _stepStartedAt;
        private readonly List<Component> _pulsed = new();

        /// <summary>넘겨받은 트랜스폼이 속한 최상위 캔버스. 대화 오버레이처럼 자기 캔버스(중첩)를 가진 UI 아래에서 넘어와도 가이드는 항상 루트 캔버스 밑에 생긴다.</summary>
        private static Transform RootCanvasOf(Transform any)
        {
            if (any == null) return null;

            var canvas = any.GetComponentInParent<Canvas>(true);
            return canvas != null ? canvas.rootCanvas.transform : any;
        }

        public static TutorialGuide GetOrCreate(Transform canvasRoot)
        {
            canvasRoot = RootCanvasOf(canvasRoot);
            if (canvasRoot == null) return null;

            var existing = canvasRoot.GetComponentInChildren<TutorialGuide>(true);
            if (existing != null) return existing;

            var go = new GameObject("Tutorial Guide", typeof(RectTransform), typeof(CanvasGroup)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var guide = go.AddComponent<TutorialGuide>();
            guide._canvasRoot = canvasRoot;
            guide._canvasRect = (RectTransform)canvasRoot.GetComponentInParent<Canvas>().rootCanvas.transform;
            guide._group = go.GetComponent<CanvasGroup>();
            guide.Build();
            return guide;
        }

        /// <summary>이미 만들어진 가이드(없으면 null). 튜토리얼이 아닌 세션을 시작할 때 남은 것을 치우는 데 쓴다.</summary>
        public static TutorialGuide Find(Transform canvasRoot)
        {
            canvasRoot = RootCanvasOf(canvasRoot);
            return canvasRoot != null ? canvasRoot.GetComponentInChildren<TutorialGuide>(true) : null;
        }

        /// <summary>지금 안내가 진행 중인가(시작했고 아직 안 끝났다).</summary>
        public bool IsActive => _flow != null && _flow.IsStarted && !_flow.IsFinished;

        public TutorialGuideStep CurrentStep => _flow?.Current;

        // ── 시작 / 정리 ──────────────────────────────────────────────────────────

        /// <summary>새 튜토리얼 세션에 붙는다. 첫 턴이 시작될 때(<c>Runner.StartStage()</c> 뒤) 안내가 시작된다 — 시작 대화(청장과의 대화)가 끝난 뒤다.</summary>
        public void Begin(StageSession session)
        {
            ResetNow();

            _session = session;
            _flow = TutorialGuideContent.CreateFlow();
            _flow.StepChanged += OnStepChanged;
            _flow.Finished += OnFinished;
            _presenter = _canvasRoot.GetComponentInChildren<ITurnResultPresenter>(true);

            // GetOrCreate는 프리팹을 켠 채로 만든다 — 책은 단서를 눌러 Show가 불릴 때만 보여야 하므로, 미리 만들어 두는 여기서 끈다.
            _book = ClueBookPanel.GetOrCreate(_canvasRoot);
            if (_book.gameObject.activeSelf) _book.gameObject.SetActive(false);
            _book.Opened += OnBookOpened;
            _book.ManualTabShown += OnManualTabShown;
            _book.Closed += OnBookClosed;

            session.Runner.TurnBegan += OnTurnBegan;
            session.Runner.TurnResolved += OnTurnResolved;
            session.Items.Used += OnItemUsed;

            gameObject.SetActive(true);
            _group.alpha = 1f;
            _bubble.gameObject.SetActive(false);
            _mask.gameObject.SetActive(false);
        }

        /// <summary>안내를 멈추고 말풍선·강조·막을 치운다(재시작, 다른 스테이지 시작, 종료).</summary>
        public void ResetNow()
        {
            if (_session != null)
            {
                _session.Runner.TurnBegan -= OnTurnBegan;
                _session.Runner.TurnResolved -= OnTurnResolved;
                _session.Items.Used -= OnItemUsed;
                _session = null;
            }

            if (_book != null)
            {
                _book.Opened -= OnBookOpened;
                _book.ManualTabShown -= OnManualTabShown;
                _book.Closed -= OnBookClosed;
                _book = null;
            }

            if (_flow != null)
            {
                _flow.StepChanged -= OnStepChanged;
                _flow.Finished -= OnFinished;
                _flow = null;
            }

            _pendingTurns.Clear();
            StopTypingAndRead();
            ClearPulses();
            if (_bubble != null) _bubble.gameObject.SetActive(false);
            if (_mask != null) _mask.gameObject.SetActive(false);
        }

        // ── 코어 사건 → 흐름 ─────────────────────────────────────────────────────

        private void OnTurnBegan(int turn)
        {
            if (turn == 1) _flow?.Begin();
        }

        /// <summary>턴 결산은 곧바로 알리지 않고 쌓아 둔다 — 결과 연출이 끝난 뒤 <see cref="Update"/>가 하나씩 알린다.</summary>
        private void OnTurnResolved(TurnReport report) => _pendingTurns.Enqueue(report.Turn);

        private void OnItemUsed(BlueComplex.Core.Items.ItemDefinition item) => _flow?.Notify(GuideAdvance.ItemUsed);
        private void OnBookOpened() => _flow?.Notify(GuideAdvance.BookOpened);
        private void OnManualTabShown() => _flow?.Notify(GuideAdvance.ManualTabShown);
        private void OnBookClosed() => _flow?.Notify(GuideAdvance.BookClosed);

        private void Update()
        {
            if (_flow == null) return;

            if (_pendingTurns.Count > 0 && IsPresentationIdle())
                _flow.Notify(GuideAdvance.TurnResolved, _pendingTurns.Dequeue());

            var step = _flow.Current;
            if (step != null && step.Advance == GuideAdvance.XrayOpened)
            {
                var xray = UnityEngine.Object.FindFirstObjectByType<ComplexXrayPanel>(FindObjectsInactive.Include);
                if (xray != null && xray.IsOpen) _flow.Notify(GuideAdvance.XrayOpened);
            }
        }

        private bool IsPresentationIdle()
        {
            if (_presenter != null && _presenter.IsPresenting) return false;

            var overlay = UnityEngine.Object.FindFirstObjectByType<StageDialogueOverlay>(FindObjectsInactive.Exclude);
            return overlay == null || !overlay.gameObject.activeInHierarchy;
        }

        // ── 단계 그리기 ──────────────────────────────────────────────────────────

        private void OnStepChanged(TutorialGuideStep step)
        {
            StopTypingAndRead();
            ClearPulses();
            _stepStartedAt = Time.unscaledTime;

            transform.SetAsLastSibling(); // 그 사이에 생긴 다른 패널(책 등) 위로.
            ApplyTarget(step);

            _mask.gameObject.SetActive(true);
            _mask.Allowed = () => AllowedRects(step);

            if (step.Effect == GuideEffect.HypnosisConnect) UiSoundHooks.Play(UiSoundCue.HypnosisConnect); // 원문 "(효과음)"

            ShowLine(step.Line, ResolveTargetRect(step.Target), step.Advance == GuideAdvance.Read);

            if (step.Advance == GuideAdvance.Read) _readRoutine = StartCoroutine(AutoAdvanceRead(step));
        }

        private void OnFinished()
        {
            StopTypingAndRead();
            ClearPulses();
            _mask.gameObject.SetActive(false);
            _bubble.gameObject.SetActive(false);
        }

        /// <summary>읽기 단계: 타이핑이 끝난 뒤 글자 수에 비례해 잠깐 머물다가 저절로 넘어간다(말풍선을 누르면 바로 넘어간다).</summary>
        private IEnumerator AutoAdvanceRead(TutorialGuideStep step)
        {
            while (!_typingDone) yield return null;

            var hold = Mathf.Clamp(step.Line.Length * ReadSecondsPerChar, ReadMinSeconds, ReadMaxSeconds);
            yield return new WaitForSecondsRealtime(hold);

            _readRoutine = null;
            _flow?.Notify(GuideAdvance.Read);
        }

        // ── 강조 / 클릭 막 ───────────────────────────────────────────────────────

        private void ApplyTarget(TutorialGuideStep step)
        {
            switch (step.Target)
            {
                case GuideTarget.Cards:
                    var tray = UnityEngine.Object.FindFirstObjectByType<ClueCardTray>(FindObjectsInactive.Include);
                    if (tray == null) break;
                    for (var i = 0; i < tray.CardCount; i++)
                    {
                        var card = tray.GetCard(i);
                        if (card == null || card.IsEmpty) continue;
                        Pulse(card);
                    }
                    break;

                case GuideTarget.ManualTab:
                    if (_book != null) Pulse(_book.ManualTabButtonRect);
                    break;

                case GuideTarget.Xray:
                    // 강조는 접힌 상태에서 보이는 태블릿과 이름표만 — Root는 열렸을 때의 넓은 판 전체라 그대로 강조하면 화면 반을 덮는다.
                    var xray = UnityEngine.Object.FindFirstObjectByType<ComplexXrayPanel>(FindObjectsInactive.Include);
                    if (xray == null) break;
                    if (xray.TabletRect != null) Pulse(xray.TabletRect);
                    if (xray.HandleTagRect != null) Pulse(xray.HandleTagRect);
                    break;

                case GuideTarget.Items:
                    var items = UnityEngine.Object.FindFirstObjectByType<ItemDisplayPanel>(FindObjectsInactive.Include);
                    if (items == null) break;
                    for (var i = 0; i < items.SlotCount; i++)
                    {
                        var slot = items.GetSlot(i);
                        if (slot != null && slot.Item != null) Pulse(slot);
                    }
                    break;
            }
        }

        private void Pulse(Component host)
        {
            TargetPulse.Set(host, true);
            _pulsed.Add(host);
        }

        private void ClearPulses()
        {
            foreach (var host in _pulsed)
                if (host != null) TargetPulse.Set(host, false);
            _pulsed.Clear();
        }

        /// <summary>말풍선을 붙일 대상의 사각형(없으면 null — 기본 자리).</summary>
        private RectTransform ResolveTargetRect(GuideTarget target)
        {
            switch (target)
            {
                case GuideTarget.Cards:
                    return UnityEngine.Object.FindFirstObjectByType<ClueCardTray>(FindObjectsInactive.Include)?.Root;
                case GuideTarget.ManualTab:
                    return _book != null ? _book.ManualTabButtonRect : null;
                case GuideTarget.Xray:
                    return UnityEngine.Object.FindFirstObjectByType<ComplexXrayPanel>(FindObjectsInactive.Include)?.TabletRect;
                case GuideTarget.Items:
                    return UnityEngine.Object.FindFirstObjectByType<ItemDisplayPanel>(FindObjectsInactive.Include)?.Root;
                default:
                    return null;
            }
        }

        /// <summary>이 단계에서 눌려도 되는 사각형들. 밖은 막이 삼킨다. 읽기 단계는 아무 데도 없다(말풍선만 눌린다).</summary>
        private IEnumerable<RectTransform> AllowedRects(TutorialGuideStep step)
        {
            switch (step.Advance)
            {
                case GuideAdvance.Read:
                    yield break;

                case GuideAdvance.BookOpened:
                    foreach (var rect in TrayRects()) yield return rect;
                    yield break;

                case GuideAdvance.ManualTabShown:
                    if (_book != null) yield return _book.ManualTabButtonRect;
                    yield break;

                case GuideAdvance.BookClosed:
                    if (_book != null) yield return (RectTransform)_book.transform;
                    yield break;

                case GuideAdvance.XrayOpened:
                    foreach (var rect in XrayRects()) yield return rect;
                    yield break;

                default: // 단서를 내는 단계(턴 결산·아이템 사용): 카드, 기억 공간, 열린 책, 엑스레이, 아이템.
                    foreach (var rect in TrayRects()) yield return rect;
                    var drop = UnityEngine.Object.FindFirstObjectByType<MemorySpaceDropZone>(FindObjectsInactive.Include);
                    if (drop != null) yield return (RectTransform)drop.transform;
                    if (_book != null && _book.gameObject.activeInHierarchy) yield return (RectTransform)_book.transform;
                    foreach (var rect in XrayRects()) yield return rect;
                    var items = UnityEngine.Object.FindFirstObjectByType<ItemDisplayPanel>(FindObjectsInactive.Include);
                    if (items != null) yield return items.Root;
                    yield break;
            }
        }

        private static IEnumerable<RectTransform> TrayRects()
        {
            var tray = UnityEngine.Object.FindFirstObjectByType<ClueCardTray>(FindObjectsInactive.Include);
            if (tray != null) yield return tray.Root;
        }

        private static IEnumerable<RectTransform> XrayRects()
        {
            var xray = UnityEngine.Object.FindFirstObjectByType<ComplexXrayPanel>(FindObjectsInactive.Include);
            if (xray != null) yield return xray.Root;
        }

        // ── 게이트 안내 ──────────────────────────────────────────────────────────

        /// <summary>카드가 게이트에 걸렸을 때의 청장 대사를 말풍선에 띄운다(단계·강조·막은 그대로). 안내가 진행 중이 아니면 false — 호출자가 다른 곳에 띄운다.</summary>
        public bool TryShowFeedback(PlayVerdict verdict, int turn)
        {
            if (!IsActive) return false;

            var line = TutorialGuideContent.RejectionLine(verdict, turn, KoreanLabels.Emotion);
            if (line == null) return false;

            StopTypingAndRead();
            ShowLine(line, ResolveTargetRect(_flow.Current.Target), clickToAdvance: false);
            return true;
        }

        // ── 말풍선 ───────────────────────────────────────────────────────────────

        private void ShowLine(string line, RectTransform target, bool clickToAdvance)
        {
            _currentLine = line;
            _bubble.gameObject.SetActive(true);
            _bubbleText.text = line;
            _nextIndicator.alpha = 0f;

            // 전체 문장 기준으로 크기를 잡는다 — 타이핑 중에 말풍선이 커지지 않게.
            var textWidth = BubbleWidth - BubbleMargin * 2f;
            var preferred = _bubbleText.GetPreferredValues(line, textWidth, 0f);
            var height = Mathf.Ceil(preferred.y) + BubbleMargin * 2f + 14f;
            _bubble.sizeDelta = new Vector2(BubbleWidth, height);
            PlaceBubble(target, new Vector2(BubbleWidth, height));

            // 행동을 기다리는 단계에서는 말풍선이 클릭을 받지 않는다(밑의 요소를 가리지 않게).
            _bubbleImage.raycastTarget = clickToAdvance;

            _bubbleText.text = string.Empty;
            StartTyping(line);
        }

        private void StartTyping(string line)
        {
            _typing?.Kill();
            _typingDone = false;

            var motion = UiMotion.Settings;
            var count = line.Length;
            var shown = 0;
            var sinceSound = 0;

            void Reveal(int visible)
            {
                if (visible == shown) return;
                shown = visible;
                _bubbleText.text = line.Substring(0, shown);
                if (!char.IsWhiteSpace(line[shown - 1]) && sinceSound++ % Mathf.Max(1, motion.dialogueSoundEvery) == 0)
                    UiSoundHooks.Play(UiSoundCue.Type);
            }

            if (count == 0)
            {
                FinishTyping(line);
                return;
            }

            Reveal(1);
            _typing = DOTween.To(() => 0f, v => Reveal(Mathf.Min(count, Mathf.FloorToInt(v) + 1)),
                    count, count * motion.dialogueSecondsPerChar)
                .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this)
                .OnComplete(() => FinishTyping(line));
        }

        private void FinishTyping(string line)
        {
            _typing?.Kill();
            _typing = null;
            _bubbleText.text = line;
            _typingDone = true;
            _nextIndicator.alpha = _bubbleImage.raycastTarget ? 1f : 0f;
        }

        /// <summary>말풍선을 눌렀을 때 — 타이핑 중이면 그 줄을 즉시 완성하고, 다 나온 읽기 단계면 다음으로 넘어간다.</summary>
        private void OnBubbleClicked()
        {
            var step = _flow?.Current;
            if (step == null) return;

            if (!_typingDone)
            {
                FinishTyping(_currentLine);
                return;
            }

            var canSkipStuck = step.Advance == GuideAdvance.XrayOpened && Time.unscaledTime - _stepStartedAt > StuckSkipSeconds;
            if (step.Advance == GuideAdvance.Read) _flow.Notify(GuideAdvance.Read);
            else if (canSkipStuck) _flow.Notify(GuideAdvance.XrayOpened);
        }

        private void StopTypingAndRead()
        {
            _typing?.Kill();
            _typing = null;
            if (_readRoutine != null)
            {
                StopCoroutine(_readRoutine);
                _readRoutine = null;
            }
        }

        /// <summary>대상 옆(위 → 아래 → 왼쪽 → 오른쪽 중 화면 안에 들어오는 첫 자리)에 붙이고, 대상이 없으면 위쪽 가운데의 기본 자리에 둔다.</summary>
        private void PlaceBubble(RectTransform target, Vector2 size)
        {
            var bounds = _canvasRect.rect;
            var half = size * 0.5f;

            Vector2 center;
            if (target == null)
            {
                center = new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, 0.34f), Mathf.Lerp(bounds.yMin, bounds.yMax, 0.8f));
            }
            else
            {
                var corners = new Vector3[4];
                target.GetWorldCorners(corners);
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                foreach (var corner in corners)
                {
                    var local = (Vector2)_canvasRect.InverseTransformPoint(corner);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }

                var middle = (min + max) * 0.5f;
                var candidates = new[]
                {
                    new Vector2(middle.x, max.y + BubbleGap + half.y),
                    new Vector2(middle.x, min.y - BubbleGap - half.y),
                    new Vector2(min.x - BubbleGap - half.x, middle.y),
                    new Vector2(max.x + BubbleGap + half.x, middle.y)
                };

                center = candidates.FirstOrDefault(c => Fits(c, half, bounds));
                if (center == default) center = candidates[0];
            }

            center.x = Mathf.Clamp(center.x, bounds.xMin + ScreenMargin + half.x, bounds.xMax - ScreenMargin - half.x);
            center.y = Mathf.Clamp(center.y, bounds.yMin + ScreenMargin + half.y, bounds.yMax - ScreenMargin - half.y);
            _bubble.position = _canvasRect.TransformPoint(center);
        }

        private static bool Fits(Vector2 center, Vector2 half, Rect bounds) =>
            center.x - half.x >= bounds.xMin + ScreenMargin && center.x + half.x <= bounds.xMax - ScreenMargin
            && center.y - half.y >= bounds.yMin + ScreenMargin && center.y + half.y <= bounds.yMax - ScreenMargin;

        // ── 조립 ─────────────────────────────────────────────────────────────────

        private void Build()
        {
            var font = RuntimeUi.FindFont(_canvasRoot);

            // 클릭 막: 화면 전체를 덮는 투명한 그래픽. 강조/허용 사각형 안의 클릭만 뚫어 준다.
            var maskRect = RuntimeUi.CreateStretched(transform, "Click Mask");
            var maskImage = maskRect.gameObject.AddComponent<Image>();
            maskImage.color = new Color(0f, 0f, 0f, 0f);
            maskImage.raycastTarget = true;
            _mask = maskRect.gameObject.AddComponent<GuideClickMask>();

            // 말풍선: 종이 판 + 이름표 + 글 + 다음 표시.
            var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(Button)) { layer = gameObject.layer };
            bubbleGo.transform.SetParent(transform, false);
            _bubble = (RectTransform)bubbleGo.transform;
            _bubble.anchorMin = _bubble.anchorMax = _bubble.pivot = new Vector2(0.5f, 0.5f);
            _bubble.sizeDelta = new Vector2(BubbleWidth, 150f);

            _bubbleImage = bubbleGo.GetComponent<Image>();
            _bubbleImage.sprite = RuntimeUi.RoundedRect;
            _bubbleImage.type = Image.Type.Sliced;
            _bubbleImage.color = MockupStyle.Paper;
            var outline = bubbleGo.AddComponent<Outline>();
            outline.effectColor = MockupStyle.Border;
            outline.effectDistance = new Vector2(3f, -3f);
            MockupStyle.AddShadow(bubbleGo);

            var button = bubbleGo.GetComponent<Button>();
            button.targetGraphic = _bubbleImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnBubbleClicked);

            _bubbleText = RuntimeUi.CreateText(_bubble, "Text", string.Empty, font, 30f, BubbleInk,
                TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one);
            var textRect = (RectTransform)_bubbleText.transform;
            textRect.offsetMin = new Vector2(BubbleMargin, BubbleMargin);
            textRect.offsetMax = new Vector2(-BubbleMargin, -BubbleMargin - 4f);
            _bubbleText.textWrappingMode = TextWrappingModes.Normal;
            _bubbleText.raycastTarget = false;

            // 이름표("청장") — 말풍선 왼쪽 위 모서리에 걸친다.
            var tag = RuntimeUi.CreateImage(_bubble, "Name Tag", NameTagColor, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -18f), new Vector2(112f, 14f), RuntimeUi.RoundedRect);
            tag.type = Image.Type.Sliced;
            var tagText = RuntimeUi.CreateText(tag.transform, "Label", TutorialGuideContent.GuideName, font, 22f, NameTagInk,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            tagText.raycastTarget = false;

            var indicator = RuntimeUi.CreateImage(_bubble, "Next Indicator", BubbleInk, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-46f, 14f), new Vector2(-16f, 38f), RuntimeUi.TriangleDown);
            _nextIndicator = indicator.gameObject.AddComponent<CanvasGroup>();
            _nextIndicator.alpha = 0f;
            _nextIndicator.blocksRaycasts = false;
            _nextIndicator.DOFade(0.25f, UiMotion.Settings.dialogueNextBlink).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);

            _bubble.gameObject.SetActive(false);
            _mask.gameObject.SetActive(false);
        }

        private void OnDestroy() => DOTween.Kill(this);
    }

    /// <summary>
    /// 화면 전체를 덮는 투명 막에 붙는 클릭 필터. 허용 사각형 안의 클릭은 "이 막은 없는 것"으로 통과시키고(밑의 요소가 받는다), 그 밖은 막이 받아 삼킨다.
    /// 좌표는 CRT 배럴 보정을 이미 거친 화면 좌표로 온다(DistortionCorrectedGraphicRaycaster).
    /// </summary>
    public sealed class GuideClickMask : MonoBehaviour, ICanvasRaycastFilter
    {
        /// <summary>지금 뚫어 줄 사각형들. null이면 아무 데도 뚫지 않는다(전부 막는다).</summary>
        public Func<IEnumerable<RectTransform>> Allowed { get; set; }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var rects = Allowed?.Invoke();
            if (rects == null) return true;

            foreach (var rect in rects)
            {
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, eventCamera)) return false;
            }

            return true;
        }
    }
}
