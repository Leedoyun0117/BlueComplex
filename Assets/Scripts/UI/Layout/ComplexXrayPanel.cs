using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using BlueComplex.UI.Rendering;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 컴플렉스 인터페이스의 엑스레이 판넬 — 화면 왼쪽 위 구석에 관절 팔에 매달려 작게 <b>접혀</b> 있다가, 작동하면 팔이 펴지며 유키의 실제 화면
    /// 위치(머리 쪽)까지 뻗어 손으로 그린 원(<see cref="HandDrawnStrokeGraphic"/>, 기억 풍선과 같은 클래스)이 커지며 뇌(<see cref="BrainView"/>)를 드러낸다.
    /// 원 자체는 접힌 동안에도 계속 그려져 있다(작게) — 커지고 작아지는 느낌은 원의 그리기 진행이 아니라 판넬 스케일 트윈이 낸다.
    /// 열 때마다 기억 풍선처럼 모양을 새로 뽑아 매번 살짝 다르다. 컴플렉스 반응이 끝나면 다시 접혀 들어간다.
    ///
    /// 작동 조건 두 가지(UI 디자인 가이드):
    /// 1. 판넬을 마우스로 끌어 유키 위에 놓기 — 이 컴포넌트가 IBeginDrag/IDrag/IEndDrag를 직접 구현한다(판넬·손잡이를 잡으면 이벤트가 이 루트까지 올라온다).
    ///    끄는 동안 관절 팔이 판넬을 따라 늘어나고, 실제 Open() 트리거는 PortraitView.OnDrop("Yuki Portrait" 인스턴스만)이 쥔다. 놓은 곳이 유키가 아니면 제자리(접힘)로 돌아간다.
    /// 2. 단서를 집어 드래그 시작 — ClueCardTray의 각 카드 ClueCardDragHandler.DragStarted를 구독한다.
    ///
    /// 반응이 끝나면 Close() — 턴 연출이 재생되는 동안은 CinematicTurnResultPresenter가 접는 시점을 쥔다.
    /// 다만 단서를 집었다가 기억 공간에 안 놓고 놓아버린 경우나 연출이 없는 Presenter(Immediate)에서는 아무도 Close()를 부르지 않으므로,
    /// 드래그가 끝났는데 연출이 재생 중이 아니면 이 컴포넌트가 스스로 접는다. 유키에게 직접 끌어다 놓아 연 판넬은 판넬의 "접기" 버튼으로 접는다.
    ///
    /// 루트는 접힌 위치부터 펼친 위치까지를 덮는 투명한 컨테이너다(그래픽 없음 — 빈 곳은 클릭을 막지 않는다). 모든 위치는 컨테이너 기준
    /// <see cref="ReferenceSize"/> 픽셀로 설계하고 실제 크기 비율로 환산한다. 시간 값은 UiMotionSettings(인스펙터)에서 온다.
    /// </summary>
    public sealed class ComplexXrayPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>컨테이너를 1080p 기준으로 환산한 크기(참조 픽셀). 위치·길이 상수는 전부 이 좌표계(왼쪽 위 원점, y 아래로 +)다.
        /// 컨테이너 자체(Anchors.Xray, UiLayoutCleanupTool.cs)는 화면 왼쪽을 덮는 투명 영역이라 이 값과 항상 같이 맞춘다.</summary>
        private static readonly Vector2 ReferenceSize = new(960f, 918f);

        /// <summary>펼친 원의 지름(=내용물이 들어갈 정사각 판의 한 변). 사용자가 에디터에서 Tablet RectTransform을 직접 맞춘 값
        /// (Pos 462,-437, Size 500.6) — 그 Pos는 Tablet의 중심을 이 클래스의 참조 좌표(왼쪽 위 원점, y 아래로 +)로 바로 옮긴 값이다.</summary>
        private static readonly Vector2 TabletSize = new(500.6f, 500.6f);
        /// <summary>접힌 위치는 왼쪽 위 "STAGE 01" 표기보다 확실히 아래에 둔다(렌더로 확인).</summary>
        private static readonly Vector2 ShoulderPoint = new(118f, 156f);
        private const float ArmLength = 260f;

        /// <summary>접힌 손목(어깨 바로 옆 — 두 선분이 포개진다)과 펼친 손목(원 왼쪽 가장자리 중앙). 펼친 손목은 사용자가 지정한 원 중심
        /// (462, 437)에서 역산했다: center = wrist + (TabletSize.x*0.5 + HingeGap, 0) → wrist = (462 - 250.3 - 6, 437).</summary>
        private static readonly Vector2 FoldedWrist = new(88f, 160f);
        private static readonly Vector2 OpenWrist = new(205.7f, 437f);

        private const float FoldedScale = 0.26f;
        private const float OpenScale = 1f;
        private const float DragScale = 0.7f;
        private static readonly Vector2 HandleTagSize = new(184f, 32f);

        /// <summary>손목과 판넬 왼쪽 가장자리 사이의 경첩 간격.</summary>
        private const float HingeGap = 6f;

        [SerializeField] private RectTransform _tablet;
        [SerializeField] private RectTransform _basePlate;
        [SerializeField] private RectTransform _handleTag;
        [SerializeField] private HandDrawnStrokeGraphic _frame;
        [SerializeField] private Image _glass;
        [SerializeField] private XrayArm _arm;
        [SerializeField] private CanvasGroup _contentGroup;
        [SerializeField] private CanvasGroup _handleGroup;
        [SerializeField] private Button _foldButton;
        [SerializeField] private BrainView _brain;
        [SerializeField] private PortraitXrayView _portrait;
        [SerializeField] private ClueCardTray _clueTray;

        private RectTransform _rect;
        private CanvasGroup _tabletGroup;
        private DistortionCorrectedGraphicRaycaster _raycaster;

        private float _fold;
        private float _dragBlend;
        private Vector2 _dragCenter;
        private Vector2 _dragGrabOffset;
        private bool _dragging;

        private Tween _foldTween;
        private Tween _dragTween;
        private ITurnResultPresenter _presenter;

        public RectTransform Root => _rect;
        public bool IsOpen { get; private set; }

        /// <summary>0 = 접힘, 1 = 펼침(트윈 중에는 그 사이, 펼칠 때 살짝 넘칠 수 있다).</summary>
        public float Fold => _fold;

        public bool IsDragging => _dragging;

        private void Awake()
        {
            _rect = (RectTransform)transform;

            if (_tablet == null) _tablet = (RectTransform)transform.Find("Tablet");
            if (_arm == null) _arm = GetComponentInChildren<XrayArm>(true);
            if (_brain == null) _brain = GetComponentInChildren<BrainView>(true);
            // 초상화는 순수 장식이라 없어도 판넬은 정상 동작한다 — 못 찾아도 경고하지 않는다.
            if (_portrait == null) _portrait = GetComponentInChildren<PortraitXrayView>(true);

            // 참조가 끊겼으면(프리팹의 스크립트 GUID가 어긋난 경우 등) 판넬이 프리팹 기본 자세(크게 펼쳐진 뇌)로 화면을 덮는다 — 그 전에 큰 소리로 알리고 뇌 내용만이라도 숨긴다.
            if (_contentGroup != null) _contentGroup.alpha = 0f;
            if (_tablet == null || _arm == null || _brain == null)
                Debug.LogError("[ComplexXrayPanel] 프리팹 참조가 끊겼다(Tablet/XrayArm/BrainView) — ComplexXrayPanel.prefab의 스크립트 GUID를 확인하고 " +
                               "BlueComplex/UI/Apply Layout Cleanup을 다시 돌려라.", this);

            _tabletGroup = _tablet.GetComponent<CanvasGroup>();
            if (_tabletGroup == null) _tabletGroup = _tablet.gameObject.AddComponent<CanvasGroup>();

            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _raycaster = canvas.GetComponent<DistortionCorrectedGraphicRaycaster>();

            if (_foldButton != null) _foldButton.onClick.AddListener(OnFoldButton);

            // 원은 접혔을 때도(작게) 계속 보인다 — 팔이 쥔 게 빈 손이 아니라 접힌 원이라는 걸 알 수 있게. 자라나는 느낌은
            // 원 자체의 그리기 진행이 아니라 _tablet의 스케일 트윈(FoldedScale→OpenScale)이 담당한다.
            _frame?.Draw(0f);

            // _clueTray가 인스펙터에 안 물려 있을 수 있다 — 먼저 같은 루트 아래에서 찾고, 계층이 다르면 씬 전체에서 한 번 더 찾는다.
            if (_clueTray == null) _clueTray = transform.root.GetComponentInChildren<ClueCardTray>(true);
            if (_clueTray == null) _clueTray = FindFirstObjectByType<ClueCardTray>(FindObjectsInactive.Include);
            if (_clueTray == null)
                Debug.LogWarning("[ComplexXrayPanel] ClueCardTray를 못 찾았다 — 단서를 집어도 판넬이 안 열린다.", this);

            SubscribeToClueDrag();
            ApplyPose();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            UnsubscribeFromClueDrag();
            if (_foldButton != null) _foldButton.onClick.RemoveListener(OnFoldButton);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_rect != null && _tablet != null && _arm != null) ApplyPose();
        }

        // -----------------------------------------------------------------
        // 접힘 / 펼침
        // -----------------------------------------------------------------

        /// <summary>펼침을 시작하고 그 트윈을 돌려준다 — 이미 열려 있으면 아무것도 안 하고 null.
        /// 호출자(CinematicTurnResultPresenter)가 <c>yield return Open().WaitForCompletion(true)</c>로 펼침이 끝날 때까지 기다릴 수 있다.</summary>
        public Tween Open()
        {
            if (IsOpen) return null;
            IsOpen = true;

            // 초상화는 열릴 때마다 무표정으로 되돌린다 — 지난 반응 표정이 이번 판에 남아 있지 않게(연출 유무와 무관).
            _portrait?.ResetToNeutral();

            // 턴 연출 밖에서 그냥 열릴 때만 코어 상태로 뇌를 채운다 — 연출 중에는 Presenter가 자기 타이밍에 Refresh한다(결과를 앞질러 보여 주지 않게).
            if (!IsTurnPresenting && _brain != null) _brain.SyncFromSession();

            // 열 때마다 모양을 새로 뽑는다(기억 풍선처럼 매번 살짝 다르게) — 이미 다 그려진 상태라 모양만 바뀌고,
            // 커지는 느낌은 아래 _tablet 스케일 트윈이 담당한다.
            _frame?.Regenerate(0);

            _foldTween?.Kill();
            _foldTween = DOTween.To(() => _fold, v =>
                {
                    _fold = v;
                    ApplyPose();
                }, 1f, UiMotion.Settings.xrayUnfold)
                .SetEase(Ease.OutBack, 1.1f).SetUpdate(true).SetTarget(this);
            return _foldTween;
        }

        /// <summary>접힘을 시작하고 그 트윈을 돌려준다. 이미 접혀 있으면 null.</summary>
        public Tween Close()
        {
            if (!IsOpen) return null;
            IsOpen = false;

            _foldTween?.Kill();
            _foldTween = DOTween.To(() => _fold, v =>
                {
                    _fold = v;
                    ApplyPose();
                }, 0f, UiMotion.Settings.xrayFold)
                .SetEase(Ease.InOutCubic).SetUpdate(true).SetTarget(this);
            return _foldTween;
        }

        private void OnFoldButton()
        {
            if (!IsTurnPresenting) Close();
        }

        /// <summary>팔과 판넬의 자세를 <see cref="_fold"/>(와 끄는 중이면 드래그 위치)로 계산해 놓는다.</summary>
        private void ApplyPose()
        {
            if (_tablet == null || _arm == null) return;

            var unit = _rect.rect.width / ReferenceSize.x;
            if (unit <= 0f) return;

            var wrist = Vector2.LerpUnclamped(FoldedWrist, OpenWrist, _fold);
            var scale = Mathf.LerpUnclamped(FoldedScale, OpenScale, _fold);

            if (_dragBlend > 0f)
            {
                var dragScale = Mathf.Lerp(scale, DragScale, _dragBlend);
                var dragWrist = _dragCenter - new Vector2(TabletSize.x * 0.5f * DragScale + HingeGap, 0f);
                wrist = Vector2.Lerp(wrist, dragWrist, _dragBlend);
                scale = dragScale;
            }

            // 손목이 닿는 거리를 넘으면 팔이 뻗을 수 있는 데까지만 간다 — 판넬은 손목에 매달려 있으니 같이 멈춘다.
            wrist = _arm.Solve(ShoulderPoint, wrist, ArmLength, unit);

            // 어깨 받침대와 손잡이 안내표는 접힌 위치에 고정이다(컨테이너 크기에 맞춰 환산만 한다).
            if (_basePlate != null)
            {
                _basePlate.anchoredPosition = new Vector2(ShoulderPoint.x, -ShoulderPoint.y) * unit;
                _basePlate.sizeDelta = new Vector2(70f, 56f) * unit;
            }

            if (_handleTag != null)
            {
                // 안내표는 화면 왼쪽 가장자리에 잘리지 않게 폭의 절반 + 여백 이상으로 놓는다.
                var folded = FoldedWrist + new Vector2(TabletSize.x * 0.5f * FoldedScale + HingeGap, TabletSize.y * 0.5f * FoldedScale + 24f);
                folded.x = Mathf.Max(folded.x, HandleTagSize.x * 0.5f + 8f);
                _handleTag.anchoredPosition = new Vector2(folded.x, -folded.y) * unit;
                _handleTag.sizeDelta = HandleTagSize * unit;
            }

            var center = wrist + new Vector2(TabletSize.x * 0.5f * scale + HingeGap, 0f);
            _tablet.sizeDelta = TabletSize * unit;
            _tablet.anchoredPosition = new Vector2(center.x, -center.y) * unit;
            _tablet.localScale = Vector3.one * scale;

            // 뇌 내용은 펼쳐질수록 또렷하고(접힌 작은 판넬 위에선 읽을 수 없다), 손잡이 안내는 접혔을 때만 보인다.
            var openness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.9f, Mathf.Max(_fold, _dragBlend)));
            if (_contentGroup != null)
            {
                _contentGroup.alpha = openness;
                // 접힌 동안 뇌는 완전히 숨고 입력도 안 받는다 — 접힌 판넬에서는 손잡이(판넬 틀·안내표)만 드래그를 받는다.
                _contentGroup.blocksRaycasts = openness > 0.5f;
            }

            if (_handleGroup != null) _handleGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.45f, Mathf.Max(_fold, _dragBlend)));
        }

        // -----------------------------------------------------------------
        // 조건 2: 단서를 집어 드래그 시작
        // -----------------------------------------------------------------

        private void SubscribeToClueDrag() => ForEachCardDragHandler(h =>
        {
            h.DragStarted += OnClueDragStarted;
            h.DragEnded += OnClueDragEnded;
        });

        private void UnsubscribeFromClueDrag() => ForEachCardDragHandler(h =>
        {
            h.DragStarted -= OnClueDragStarted;
            h.DragEnded -= OnClueDragEnded;
        });

        // DragStarted는 Action(반환값 없음)인데 Open()은 Tween을 돌려주므로 메서드 그룹을 바로 못 물린다 — 반환값을 버리는 얇은 래퍼.
        // 연출이 재생 중일 때 집은 단서는 어차피 못 낸다(MemorySpaceDropZone) — 그때 펼치면 접어 줄 시점이 없다.
        private void OnClueDragStarted()
        {
            if (!IsTurnPresenting) Open();
        }

        /// <summary>드롭은 OnEndDrag보다 먼저 처리되므로 단서를 냈다면 이 시점에 이미 연출이 시작돼 있다.
        /// 연출 중이면 접는 시점을 Presenter에게 맡기고, 아니면(허공에 놓았거나 즉시 반영형 Presenter) 지금 접는다.</summary>
        private void OnClueDragEnded()
        {
            if (!IsTurnPresenting) Close();
        }

        private bool IsTurnPresenting
        {
            get
            {
                _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
                return _presenter != null && _presenter.IsPresenting;
            }
        }

        private void ForEachCardDragHandler(System.Action<ClueCardDragHandler> apply)
        {
            if (_clueTray == null) return;

            var found = 0;
            for (var i = 0; i < _clueTray.CardCount; i++)
            {
                var drag = _clueTray.GetCard(i).GetComponent<ClueCardDragHandler>();
                if (drag == null) continue;
                found++;
                apply(drag);
            }

            if (found == 0)
                Debug.LogWarning("[ComplexXrayPanel] ClueCardTray는 찾았지만 카드에 ClueCardDragHandler가 하나도 없다.", this);
        }

        // -----------------------------------------------------------------
        // 조건 1: 판넬을 끌어 유키 위에 놓기 (ClueCardDragHandler와 같은 좌표 보정 패턴)
        // -----------------------------------------------------------------

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!TryGetContainerPoint(eventData, out var point))
            {
                eventData.pointerDrag = null;
                return;
            }

            _dragging = true;
            _dragTween?.Kill();

            // 잡은 자리를 유지한 채 끈다 — 판넬이 커서 밑으로 튀지 않는다.
            var unit = _rect.rect.width / ReferenceSize.x;
            var current = new Vector2(_tablet.anchoredPosition.x, -_tablet.anchoredPosition.y) / unit;
            _dragGrabOffset = current - point;
            _dragCenter = ClampToScreen(current);

            // 끌고 있는 판넬이 커서 밑을 가리면 유키(PortraitView)가 드롭을 못 받는다 — 놓을 때까지 히트테스트에서 뺀다.
            _tabletGroup.blocksRaycasts = false;

            _dragTween = DOTween.To(() => _dragBlend, v =>
                {
                    _dragBlend = v;
                    ApplyPose();
                }, 1f, UiMotion.Settings.xrayReturn * 0.6f)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);

            UiSoundHooks.Play(UiSoundCue.Paper);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || !TryGetContainerPoint(eventData, out var point)) return;

            _dragCenter = ClampToScreen(point + _dragGrabOffset);
            ApplyPose();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 드롭이 유키 위에서 성공했으면 PortraitView.OnDrop이 이미 Open()을 불렀다 — 펼친 자리로, 아니면 접힌 자리로 돌아간다(팔이 다시 접힌다).
            _dragging = false;
            _tabletGroup.blocksRaycasts = true;

            _dragTween?.Kill();
            _dragTween = DOTween.To(() => _dragBlend, v =>
                {
                    _dragBlend = v;
                    ApplyPose();
                }, 0f, UiMotion.Settings.xrayReturn)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(this);
        }

        /// <summary>끄는 판넬이 화면 왼쪽/위 밖으로 나가지 않게 가둔다(컨테이너가 화면 왼쪽 가장자리에 붙어 있어 왼쪽 여유가 없다).</summary>
        private static Vector2 ClampToScreen(Vector2 center) => new(
            Mathf.Max(center.x, TabletSize.x * 0.5f * DragScale + HingeGap + 10f),
            Mathf.Max(center.y, TabletSize.y * 0.5f * DragScale - 60f));

        /// <summary>포인터 위치를 컨테이너 참조 픽셀 좌표(왼쪽 위 원점, y 아래로 +)로 바꾼다. CRT 배럴 왜곡 보정을 적용한다.</summary>
        private bool TryGetContainerPoint(PointerEventData eventData, out Vector2 point)
        {
            var curvature = _raycaster != null ? _raycaster.CurrentCurvature : 0f;
            var screenPos = curvature == 0f
                ? eventData.position
                : DistortionMath.ApplyBarrel(eventData.position, curvature, Screen.width, Screen.height);

            point = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPos, eventData.pressEventCamera, out var local))
                return false;

            var rect = _rect.rect;
            var unit = rect.width / ReferenceSize.x;
            point = new Vector2(local.x - rect.xMin, rect.yMax - local.y) / unit;
            return true;
        }
    }
}
