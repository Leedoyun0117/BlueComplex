using BlueComplex.Core.Turn;
using BlueComplex.UI.Bootstrap;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 기억 공간의 드롭 판정. 이 컴포넌트가 붙은 RectTransform 자체가 히트테스트 영역이므로,
    /// 프리팹에서 시각적 말풍선보다 사방 32px 더 크게 잡아야 한다 — 흥분 최대치의 _Shake
    /// 지터(±11.5px/±4.5px)와 요청된 20px 여유를 합친 것보다 넉넉하게.
    /// 히트테스트 자체(왜곡 보정 포함)는 Canvas의 DistortionCorrectedGraphicRaycaster가 이미
    /// 처리하므로 여기서 좌표 보정을 신경 쓸 필요는 없다.
    /// </summary>
    public sealed class MemorySpaceDropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField] private StageBootstrapper _bootstrapper;

        public void OnDrop(PointerEventData eventData)
        {
            if (_bootstrapper == null || _bootstrapper.Session == null) return;
            if (_bootstrapper.Session.Runner.Outcome != StageOutcome.InProgress) return;

            // 연출(CinematicTurnResultPresenter)이 아직 재생 중이면 새 단서를 못 낸다 — 안 그러면
            // 컴플렉스 발광/태그 연출이 겹치거나 순서가 뒤섞인다. 어느 Presenter가 붙어 있는지
            // 몰라도 되게 인터페이스로만 찾는다(즉시 반영형은 IsPresenting이 항상 false).
            var presenter = transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (presenter != null && presenter.IsPresenting) return;

            var dragged = eventData.pointerDrag;
            if (dragged == null) return;

            var handler = dragged.GetComponent<ClueCardDragHandler>();
            var view = dragged.GetComponent<ClueCardView>();
            if (handler == null || view == null || view.IsEmpty) return;

            handler.MarkHandled();
            _bootstrapper.Session.Runner.PlayClue(view.Card);
        }
    }
}
