using System;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 단서 카드를 기억 공간으로 드래그하는 입력만 담당한다(판정은 MemorySpaceDropZone→TurnRunner가 함).
    /// 드래그로 이어지지 않은 짧은 클릭은 단서 정보 책 UI(ClueBookPanel)를 열고, 누른 채 끌면 손에 든 카드(<see cref="ClueCardMotion"/>)가
    /// 커서를 따라다닌다 — 놓은 자리가 기억 공간이면 제출(빨려 들어감), 아니면 원래 자리로 돌아간다.
    ///
    /// 손에 든 카드를 "그리는" 좌표는 eventData.position을 그대로 쓰지 않는다 — CRT 배럴
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
        private ClueCardMotion _motion;
        private RectTransform _canvasRect;
        private DistortionCorrectedGraphicRaycaster _raycaster;
        private MemorySpaceBubble _bubble;
        private bool _accepted;

        public bool IsDragging { get; private set; }

        /// <summary>실제로 드래그가 시작될 때(빈 슬롯이 아닐 때)만 쏜다 — 엑스레이 판넬이 이걸 구독해서 펼친다.</summary>
        public event Action DragStarted;

        /// <summary>드래그가 끝났을 때(드롭이 처리됐든 허공에 놓았든) 쏜다. 드롭 처리는 OnEndDrag보다 먼저 일어나므로
        /// 구독자는 이 시점에 턴이 이미 시작됐는지(ITurnResultPresenter.IsPresenting) 볼 수 있다.</summary>
        public event Action DragEnded;

        private void Awake()
        {
            _view = GetComponent<ClueCardView>();

            // 프리팹에 없어도 붙는다 — 이미 구워진 카드 프리팹을 다시 굽지 않아도 되게.
            _motion = GetComponent<ClueCardMotion>();
            if (_motion == null) _motion = gameObject.AddComponent<ClueCardMotion>();

            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _canvasRect = (RectTransform)canvas.transform;
            _raycaster = canvas.GetComponent<DistortionCorrectedGraphicRaycaster>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 아이템 대상 선택 중에는 카드를 끌 수 없다 — 카드 클릭이 대상 선택이다.
            if (_view.IsEmpty || _motion.IsBusy || ItemTargetSelector.Active != null || !TryGetCanvasPoint(eventData, out var point))
            {
                eventData.pointerDrag = null;
                return;
            }

            IsDragging = true;
            _accepted = false;
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;

            _motion.BeginHold(point, _canvasRect);
            DragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging || !TryGetCanvasPoint(eventData, out var point)) return;
            _motion.Follow(point);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
            // 드롭으로 낸 카드는 이미 손패에서 빠져 슬롯이 비어 있다 — 안 보이는 슬롯이 입력을 가로채지 않게 한다.
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = !_view.IsEmpty;

            _motion.EndHold(_accepted, BubbleCenterWorld());
            _accepted = false;

            DragEnded?.Invoke();
        }

        /// <summary>
        /// 드롭이 받아들여졌음을 알린다(OnDrop → OnEndDrag 순서). 이 표시가 있으면 손에 든 카드가 기억 풍선으로 빨려 들어가고,
        /// 없으면(허공·연출 중이라 거절됨) 원래 자리로 돌아간다.
        /// </summary>
        public void MarkHandled() => _accepted = true;

        /// <summary>Unity가 드래그 임계값을 안 넘은 press+release만 클릭으로 판정해준다 — 드래그와
        /// 따로 가드할 필요가 없다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_view.IsEmpty) return;

            // 아이템 대상 선택 중이면 클릭은 대상 선택이다(단서 정보 책을 열지 않는다).
            if (ItemTargetSelector.TryPick(_view.Card)) return;

            var font = _canvasRect.GetComponentInChildren<TMP_Text>(true)?.font;
            var book = ClueBookPanel.GetOrCreate(_canvasRect, font);
            book.Show(_view.ViewModel);
        }

        private Vector3? BubbleCenterWorld()
        {
            if (_bubble == null) _bubble = _canvasRect.GetComponentInChildren<MemorySpaceBubble>(true);
            if (_bubble == null) return null;

            var root = _bubble.Root;
            return root.TransformPoint(root.rect.center);
        }

        private bool TryGetCanvasPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            var curvature = _raycaster != null ? _raycaster.CurrentCurvature : 0f;
            var screenPos = curvature == 0f
                ? eventData.position
                : DistortionMath.ApplyBarrel(eventData.position, curvature, Screen.width, Screen.height);

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, eventData.pressEventCamera, out localPoint);
        }
    }
}
