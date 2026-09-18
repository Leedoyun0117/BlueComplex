using System;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 단서 카드를 기억 공간으로 드래그하는 입력만 담당한다(판정은 MemorySpaceDropZone→TurnRunner가 함).
    /// 드래그로 이어지지 않은 클릭은 단서 정보 책 UI(ClueBookPanel)를 연다.
    ///
    /// 드래그 중 카드를 "그리는" 좌표는 eventData.position을 그대로 쓰지 않는다 — CRT 배럴
    /// 왜곡 때문에 실제 화면에 보이는 커서 위치와 UI 캔버스(왜곡 전 좌표계)의 원시 좌표가
    /// 어긋난다. DistortionMath.ApplyBarrel로 같은 보정을 적용해야 카드가 커서를 따라간다.
    /// (레이캐스터의 히트테스트 보정과는 별개 — 그건 DistortionCorrectedGraphicRaycaster가 이미
    /// 알아서 한다.)
    /// </summary>
    [RequireComponent(typeof(ClueCardView))]
    public sealed class ClueCardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private ClueCardView _view;
        private RectTransform _rect;
        private RectTransform _canvasRect;
        private DistortionCorrectedGraphicRaycaster _raycaster;

        private Transform _originalParent;
        private int _originalSiblingIndex;

        public bool IsDragging { get; private set; }

        /// <summary>실제로 드래그가 시작될 때(빈 슬롯이 아닐 때)만 쏜다 — 엑스레이 판넬이 이걸 구독해서 펼친다.</summary>
        public event Action DragStarted;

        private void Awake()
        {
            _view = GetComponent<ClueCardView>();
            _rect = (RectTransform)transform;

            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _canvasRect = (RectTransform)canvas.transform;
            _raycaster = canvas.GetComponent<DistortionCorrectedGraphicRaycaster>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_view.IsEmpty)
            {
                eventData.pointerDrag = null;
                return;
            }

            IsDragging = true;
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            transform.SetParent(_canvasRect, worldPositionStays: true);
            transform.SetAsLastSibling();
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;

            MoveTo(eventData);
            DragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            MoveTo(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = true;

            // 드롭이 처리됐어도(_handled) 트레이 슬롯으로 복귀시켜야 한다 — 이 오브젝트 자체가
            // ClueCardTray._cards의 슬롯이므로, 여기서 부모/위치를 되돌려놔야 RefreshAll이
            // 그려주는 내용(소진되어 비워지든 다음 카드로 갱신되든)이 트레이 안에서 보인다.
            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 드롭이 처리됐음을 알린다. 현재는 OnEndDrag가 항상 원래 슬롯으로 복귀시키므로
        /// 동작에 영향은 없지만, MemorySpaceDropZone과의 계약(성공 판정 통보)을 유지하기 위해 남겨둔다.
        /// </summary>
        public void MarkHandled() { }

        /// <summary>Unity가 드래그 임계값을 안 넘은 press+release만 클릭으로 판정해준다 — 드래그와
        /// 따로 가드할 필요가 없다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_view.IsEmpty) return;

            var font = _canvasRect.GetComponentInChildren<TMP_Text>(true)?.font;
            var book = ClueBookPanel.GetOrCreate(_canvasRect, font);
            book.Show(_view.ViewModel);
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
