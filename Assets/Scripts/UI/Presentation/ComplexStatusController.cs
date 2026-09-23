using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 컴플렉스 상시 표시(UI 가이드 8번 "컴플렉스 지속 시간 표시") — 지금 붙어 있는 컴플렉스의 이름과 남은 지속 턴을 항상 보여준다.
    /// 목업의 왼쪽 노란 포스트잇(컴플렉스 포스트잇)이고, 종이·압정·떼어졌다 붙는 모션은 같은 오브젝트에 붙는 <see cref="Postit"/>이 맡는다.
    /// 엑스레이 판넬 안의 목록(6번, 발광 연출용)과는 별개의 목록이다.
    ///
    /// 갱신 시점은 두 갈래다.
    /// 1. 턴 밖의 변화(스테이지 시작, 재시작)는 이 컨트롤러가 직접 그린다.
    /// 2. 턴 안의 변화 — 컴플렉스가 붙거나 만료되고 남은 턴이 줄어드는 것 — 는 ITurnResultPresenter가 포스트잇이 떼어져 있는 사이에 <see cref="RefreshForTurn"/>을 부른다.
    ///    그래서 결과 연출이 도는 동안엔 이전 상태가 그대로 보이고, 새로 붙는 포스트잇에 갱신된 상태가 적혀 있다.
    ///    남은 턴이 줄어드는 것(Tick)은 코어 이벤트가 없어 어차피 Presenter가 알려줘야 한다.
    ///
    /// Attached/Expired도 구독하지만 곧바로 그리지 않는다. 둘 다 TurnResolved보다 먼저(=Presenter가 연출을 시작하기 전에) 터지므로,
    /// 바로 그리면 연출을 앞질러 결과가 미리 보인다. 그래서 표시만 해 두고 다음 프레임 끝(LateUpdate)에 연출 중이 아닐 때만 그린다 —
    /// 연출 중이면 Presenter의 Refresh가 대신 그린다. 아이템처럼 턴 밖에서 컴플렉스가 바뀌는 경우는 이 경로가 받는다.
    ///
    /// 새로 발현된 컴플렉스에는 행에 빨간 펜 "NEW"가 붙는다(기획서에 없는 구분 표시 — 제안). 표시는 다음 턴 갱신 때 걷힌다.
    /// </summary>
    public sealed class ComplexStatusController : SessionBoundView
    {
        [SerializeField] private ComplexListView _list;

        private ITurnResultPresenter _presenter;
        private Postit _postit;
        private bool _dirty;
        private bool _brainDirty;

        // 지난번에 그릴 때 보이던 컴플렉스와, 그중 이번에 새로 나타난 것.
        private readonly HashSet<ComplexInstance> _known = new();
        private readonly HashSet<ComplexInstance> _fresh = new();

        /// <summary>이 목록이 얹힌 포스트잇. 없으면 이 시점에 붙인다(프리팹에는 넣어 두지 않았다 — 붙이면 프리팹의 자식이 Content로 옮겨진다).</summary>
        public Postit Postit => _postit != null ? _postit : _postit = Postit.Attach(gameObject);

        protected override void Awake()
        {
            // 행이 그려지기 전에 자식을 종이 안으로 옮겨 둔다.
            _postit = Postit.Attach(gameObject);
            base.Awake();
        }

        protected override void Subscribe(StageSession session)
        {
            session.Complexes.Attached += OnBoardChanged;
            session.Complexes.Expired += OnBoardChanged;
            session.Complexes.DurationChanged += OnDurationChanged;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Complexes.Attached -= OnBoardChanged;
            session.Complexes.Expired -= OnBoardChanged;
            session.Complexes.DurationChanged -= OnDurationChanged;
        }

        protected override void Render()
        {
            // 새 세션: 시작 시점의 컴플렉스는 "새로" 나타난 게 아니다.
            _known.Clear();
            _fresh.Clear();
            Postit.SnapAttached();
            Draw(turnBoundary: false, markNew: false);
        }

        /// <summary>턴 밖의 변화(아이템 등)를 바로 반영한다. 새로 붙은 컴플렉스의 "NEW"도 바로 찍힌다.</summary>
        public void Refresh()
        {
            Draw(turnBoundary: false, markNew: true);
            _list.RevealNewMarks();
        }

        /// <summary>
        /// 턴이 넘어가며 포스트잇이 떼어져 있는 사이에 부른다: 붙어 있는 컴플렉스를 우선순위 순서로 다시 적는다.
        /// 남은 턴이 갱신되고, 만료된 컴플렉스는 사라지고, 새로 발현된 컴플렉스에는 "NEW" 표시가 준비된다(<see cref="RevealNewMarks"/>가 드러낸다).
        /// </summary>
        public void RefreshForTurn() => Draw(turnBoundary: true, markNew: true);

        /// <summary>준비된 "NEW" 표시를 드러낸다 — 갱신된 포스트잇이 다 붙은 뒤에 부른다.</summary>
        public void RevealNewMarks() => _list.RevealNewMarks();

        private void Draw(bool turnBoundary, bool markNew)
        {
            _dirty = false;
            if (Session == null) return;

            var current = Session.Complexes.InPriorityOrder().ToList();

            if (turnBoundary) _fresh.Clear();
            if (markNew)
                foreach (var complex in current)
                    if (!_known.Contains(complex))
                        _fresh.Add(complex);

            _fresh.IntersectWith(current); // 이미 만료된 컴플렉스의 표시는 버린다.
            _known.Clear();
            _known.UnionWith(current);

            _list.Refresh(current, _fresh);
        }

        private void OnBoardChanged(ComplexInstance complex) => _dirty = true;

        /// <summary>아이템(극복)이 남은 턴을 바꿨다 — 턴 해석 밖의 변화라 이 경로가 받는다. 뇌 영역의 "N턴" 글자도 같이 맞춘다.</summary>
        private void OnDurationChanged(ComplexInstance complex)
        {
            _dirty = true;
            _brainDirty = true;
        }

        private void LateUpdate()
        {
            if (!_dirty) return;

            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            Refresh();

            if (!_brainDirty) return;
            _brainDirty = false;
            transform.root.GetComponentInChildren<BrainView>(true)?.SyncFromSession();
        }
    }
}
