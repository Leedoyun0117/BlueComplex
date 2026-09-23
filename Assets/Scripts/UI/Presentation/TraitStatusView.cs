using System.Linq;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Traits;
using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 특성 표시(목업: 기억 풍선 아래 작은 글자 "특성 없음"). 붙어 있는 특성의 이름과 남은 턴을 보여 준다 — 예: "예민 3턴".
    ///
    /// 특성은 턴 안에서(아이템 사용, 지속 턴 감소) 바뀌므로 갱신 시점은 컴플렉스 상시 표시와 같은 규칙이다:
    /// 세션 시작은 직접 그리고, 턴 안의 변화는 ITurnResultPresenter가 연출 순서에 맞춰 <see cref="Refresh"/>를 부른다.
    /// Granted/Expired는 표시만 해 두고 연출 중이 아닐 때만 그린다(아이템처럼 턴 밖에서 특성이 바뀌는 경우는 이 경로가 받는다).
    /// </summary>
    public sealed class TraitStatusView : SessionBoundView
    {
        private const string NoTraitText = "특성 없음";

        [SerializeField] private TMP_Text _label;

        private ITurnResultPresenter _presenter;
        private bool _dirty;

        protected override void Subscribe(StageSession session)
        {
            session.Traits.Granted += OnBoardChanged;
            session.Traits.Expired += OnBoardChanged;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Traits.Granted -= OnBoardChanged;
            session.Traits.Expired -= OnBoardChanged;
        }

        protected override void Render() => Refresh();

        public void Refresh()
        {
            _dirty = false;
            if (_label == null) return;
            if (Session == null)
            {
                _label.text = NoTraitText;
                return;
            }

            var traits = Session.Traits.Traits;
            _label.text = traits.Count == 0
                ? NoTraitText
                : string.Join(", ", traits.Select(KoreanLabels.Trait));
        }

        private void OnBoardChanged(TraitInstance trait) => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty) return;

            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            Refresh();
        }
    }
}
