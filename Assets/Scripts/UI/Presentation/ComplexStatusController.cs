using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stage;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 컴플렉스 상시 표시(UI 가이드 8번 "컴플렉스 지속 시간 표시") — 지금 붙어 있는 컴플렉스의 이름과 남은 지속 턴을 항상 보여준다.
    /// 엑스레이 판넬 안의 목록(6번, 발광 연출용)과는 별개의 목록이다.
    ///
    /// 갱신 시점은 두 갈래다.
    /// 1. 턴 밖의 변화(스테이지 시작, 재시작)는 이 컨트롤러가 직접 그린다.
    /// 2. 턴 안의 변화 — 컴플렉스가 붙거나 만료되고 남은 턴이 줄어드는 것 — 는 ITurnResultPresenter가 연출 순서에 맞춰 <see cref="Refresh"/>를 부른다.
    ///    남은 턴이 줄어드는 것(Tick)은 코어 이벤트가 없어 어차피 Presenter가 알려줘야 한다.
    ///
    /// Attached/Expired도 구독하지만 곧바로 그리지 않는다. 둘 다 TurnResolved보다 먼저(=Presenter가 연출을 시작하기 전에) 터지므로,
    /// 바로 그리면 연출을 앞질러 결과가 미리 보인다. 그래서 표시만 해 두고 다음 프레임 끝(LateUpdate)에 연출 중이 아닐 때만 그린다 —
    /// 연출 중이면 Presenter의 Refresh가 대신 그린다. 아이템처럼 턴 밖에서 컴플렉스가 바뀌는 경우는 이 경로가 받는다.
    /// </summary>
    public sealed class ComplexStatusController : SessionBoundView
    {
        [SerializeField] private ComplexListView _list;

        private ITurnResultPresenter _presenter;
        private bool _dirty;

        protected override void Subscribe(StageSession session)
        {
            session.Complexes.Attached += OnBoardChanged;
            session.Complexes.Expired += OnBoardChanged;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Complexes.Attached -= OnBoardChanged;
            session.Complexes.Expired -= OnBoardChanged;
        }

        protected override void Render() => Refresh();

        /// <summary>붙어 있는 컴플렉스를 우선순위 순서로 다시 그린다. 남은 턴이 줄었으면 막대가 줄어든다(ComplexRowView).</summary>
        public void Refresh()
        {
            _dirty = false;
            if (Session == null) return;

            _list.Refresh(Session.Complexes.InPriorityOrder().ToList());
        }

        private void OnBoardChanged(ComplexInstance complex) => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty) return;

            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            Refresh();
        }
    }
}
