using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 손패 트레이를 코어에 연결한다. 손패는 "턴 밖에서도 변하는" 요소라 코어 이벤트를
    /// 직접 구독한다(Presenter를 거치지 않음) — 다만 ImmediateTurnResultPresenter도 턴 결과 후
    /// 같은 RefreshAll을 호출해, 낸 카드가 손에 남았을 때 새로 해금된 속성을 반영한다.
    /// </summary>
    public sealed class ClueHandController : SessionBoundView
    {
        [SerializeField] private ClueCardTray _tray;

        protected override void Subscribe(StageSession session)
        {
            session.Hand.CardAdded += OnHandChanged;
            session.Hand.CardDestroyed += OnHandChanged;
            session.Censorship.LevelChanged += OnCensorshipChanged;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Hand.CardAdded -= OnHandChanged;
            session.Hand.CardDestroyed -= OnHandChanged;
            session.Censorship.LevelChanged -= OnCensorshipChanged;
        }

        protected override void Render()
        {
            _tray.RefreshAll(Session.Hand.Cards, Session.Ledger, Session.Censorship.Level);
        }

        private void OnHandChanged(ClueInstance card) => Render();
        private void OnCensorshipChanged(CensorshipLevel level) => Render();
    }
}
