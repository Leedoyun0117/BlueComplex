using BlueComplex.UI.Presentation;
using BlueComplex.UI.Rendering;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 컴플렉스 인터페이스(엑스레이 판넬). 평소엔 구석에 접혀 보이지 않다가 펼쳐지며 안쪽의
    /// 컴플렉스 목록(ComplexListView, 4개 Row — 최대 4중첩과 맞춘 슬롯 수)을 드러낸다.
    /// 펼침은 0.3초 내외로 짧게 잡는다 —
    /// 단서를 집는 순간 발동하는데 느리면 드래그하는 동안 뇌가 아직 안 보이는 상태가 된다.
    ///
    /// 작동 조건 두 가지(UI 디자인 가이드):
    /// 1. 판넬 자체를 드래그해서 유키 위에 놓기 — 이 컴포넌트가 직접 IBeginDrag/IDrag/IEndDrag를
    ///    구현한다. 실제 Open() 트리거는 PortraitView.OnDrop("Yuki Portrait" 인스턴스만)이 쥔다.
    /// 2. 단서를 집어 드래그 시작 — ClueCardTray의 각 카드 ClueCardDragHandler.DragStarted를 구독한다.
    ///
    /// 반응이 끝나면 Close() — 턴 연출이 재생되는 동안은 CinematicTurnResultPresenter가 접는 시점을 쥔다.
    /// 다만 단서를 집었다가 기억 공간에 안 놓고 놓아버린 경우나 연출이 없는 Presenter(Immediate)에서는
    /// 아무도 Close()를 부르지 않으므로, 드래그가 끝났는데 연출이 재생 중이 아니면 이 컴포넌트가 스스로 접는다.
    /// </summary>
    public sealed class ComplexXrayPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float RevealDuration = 0.3f;

        /// <summary>접힌 판넬은 화면에 안 보인다 — 관절 팔 아트가 생기면 이 값을 올려 접힌 기기를 드러낸다.
        /// (그 전까지는 투명한 채로 드래그 손잡이 역할만 한다.)</summary>
        private const float FoldedAlpha = 0f;

        /// <summary>접힌 상태의 구석 박스(화면 비율 앵커). 목업에서 관절 팔이 접혀 있는 유키 왼쪽 위 구석이다.
        /// 펼친 상태는 Awake 시점의 원래 앵커를 그대로 기억해서 쓴다 — MainHud의 배치가 바뀌어도 여기 하드코딩할 필요가 없다.</summary>
        private static readonly Vector2 FoldedAnchorMin = new(0.03f, 0.62f);
        private static readonly Vector2 FoldedAnchorMax = new(0.09f, 0.72f);

        [SerializeField] private ClueCardTray _clueTray;

        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private RectTransform _canvasRect;
        private DistortionCorrectedGraphicRaycaster _raycaster;

        private Vector2 _openAnchorMin;
        private Vector2 _openAnchorMax;

        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Vector2 _originalAnchoredPosition;

        private Tween _revealTween;
        private ITurnResultPresenter _presenter;

        public RectTransform Root => _rect;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _openAnchorMin = _rect.anchorMin;
            _openAnchorMax = _rect.anchorMax;

            // BuildComplexXrayPanel(UiLayoutSetupTool.cs)은 CanvasGroup을 만들지 않고, Background
            // Image도 raycastTarget=false로 굽는다. 이미 디스크에 있는 ComplexXrayPanel.prefab은
            // EnsureElementPrefab의 short-circuit 때문에 그 메서드를 고쳐도 재생성되지 않으므로,
            // 여기서 런타임에 직접 확보/보정해야 프리팹을 다시 굽지 않아도 동작한다.
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // "Background"는 이 GameObject 자체가 아니라 자식이다(BuildComplexXrayPanel 참고) —
            // 자식의 raycastTarget이 true면 Unity가 히트 시 부모 체인에서 IBeginDragHandler를
            // 찾아 이 컴포넌트까지 이벤트를 올려준다.
            var background = transform.Find("Background")?.GetComponent<Image>();
            if (background != null) background.raycastTarget = true;

            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _canvasRect = (RectTransform)canvas.transform;
            _raycaster = canvas.GetComponent<DistortionCorrectedGraphicRaycaster>();

            SnapClosed();

            // _clueTray가 인스펙터에 안 물려 있을 수 있다(위와 같은 이유) — 먼저 같은 루트
            // 아래에서 찾고, 혹시 계층이 다르면(수동 편집된 씬 등) 씬 전체에서 한 번 더 찾는다.
            if (_clueTray == null) _clueTray = transform.root.GetComponentInChildren<ClueCardTray>(true);
            if (_clueTray == null) _clueTray = FindFirstObjectByType<ClueCardTray>(FindObjectsInactive.Include);
            if (_clueTray == null)
                Debug.LogWarning("[ComplexXrayPanel] ClueCardTray를 못 찾았다 — 단서를 집어도 판넬이 안 열린다.", this);

            SubscribeToClueDrag();
        }

        private void OnDestroy()
        {
            _revealTween?.Kill();
            UnsubscribeFromClueDrag();
        }

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

        // DragStarted는 Action(반환값 없음)인데 Open()은 이제 Tween을 돌려주므로 메서드 그룹을
        // 바로 못 물린다 — 반환값을 버리는 얇은 래퍼.
        // 연출이 재생 중일 때 집은 단서는 어차피 못 낸다(MemorySpaceDropZone) — 그때 펼치면 접어줄 시점이 없다.
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

        /// <summary>펼침을 시작하고 그 트윈을 돌려준다 — 이미 열려 있으면 아무것도 안 하고 null.
        /// 호출자(3단계 CinematicTurnResultPresenter)가 <c>yield return Open().WaitForCompletion(true)</c>
        /// 로 펼침이 끝날 때까지 기다릴 수 있다.</summary>
        public Tween Open()
        {
            if (IsOpen) return null;
            IsOpen = true;

            _revealTween?.Kill();
            _revealTween = DOTween.Sequence()
                .Append(_rect.DOAnchorMin(_openAnchorMin, RevealDuration).SetEase(Ease.OutBack))
                .Join(_rect.DOAnchorMax(_openAnchorMax, RevealDuration).SetEase(Ease.OutBack))
                .Join(_canvasGroup.DOFade(1f, RevealDuration));
            return _revealTween;
        }

        /// <summary>접힘을 시작하고 그 트윈을 돌려준다. 이미 접혀 있으면 null.</summary>
        public Tween Close()
        {
            if (!IsOpen) return null;
            IsOpen = false;

            _revealTween?.Kill();
            _revealTween = DOTween.Sequence()
                .Append(_rect.DOAnchorMin(FoldedAnchorMin, RevealDuration).SetEase(Ease.InQuad))
                .Join(_rect.DOAnchorMax(FoldedAnchorMax, RevealDuration).SetEase(Ease.InQuad))
                .Join(_canvasGroup.DOFade(FoldedAlpha, RevealDuration));
            return _revealTween;
        }

        private void SnapClosed()
        {
            // localScale만 줄이던 이전 버전에서 접힌 상태의 드래그/클릭 판정 영역이 원래 앵커
            // 크기(화면 상당 부분)로 남는다는 리포트가 있었다 — 앵커 자체(=RectTransform의 실제
            // 폭/높이)를 구석의 작은 박스로 바꾸면 시각 크기와 판정 영역이 항상 같은 값에서
            // 나오므로 둘이 어긋날 여지가 없다.
            _rect.anchorMin = FoldedAnchorMin;
            _rect.anchorMax = FoldedAnchorMax;
            _canvasGroup.alpha = FoldedAlpha;
            IsOpen = false;
        }

        // -----------------------------------------------------------------
        // 조건 1: 판넬 자체를 드래그해서 유키 위에 놓기 (ClueCardDragHandler와 같은 좌표 보정 패턴)
        // -----------------------------------------------------------------

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalAnchoredPosition = _rect.anchoredPosition;

            transform.SetParent(_canvasRect, worldPositionStays: true);
            transform.SetAsLastSibling();

            // 끌고 있는 판넬이 커서 밑을 가리면 유키(PortraitView)가 드롭을 못 받는다 — 놓을 때까지 히트테스트에서 뺀다.
            _canvasGroup.blocksRaycasts = false;

            MoveTo(eventData);
        }

        public void OnDrag(PointerEventData eventData) => MoveTo(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            // 드롭이 유키 위에서 성공했으면 PortraitView.OnDrop이 이미 Open()을 불렀다 — 위치만
            // 원래 자리(구석 폴드 위치)로 되돌린다. 실패해도 마찬가지로 제자리 복귀.
            _canvasGroup.blocksRaycasts = true;
            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rect.anchoredPosition = _originalAnchoredPosition;
        }

        private void MoveTo(PointerEventData eventData)
        {
            var curvature = _raycaster != null ? _raycaster.CurrentCurvature : 0f;
            var screenPos = curvature == 0f
                ? eventData.position
                : DistortionMath.ApplyBarrel(eventData.position, curvature, Screen.width, Screen.height);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenPos, eventData.pressEventCamera, out var localPoint))
            {
                _rect.localPosition = localPoint;
            }
        }
    }
}
