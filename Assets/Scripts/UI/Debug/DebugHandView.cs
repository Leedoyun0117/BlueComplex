using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>손패 4장. 해금/검열 규칙을 반영해 표시하고, 클릭하면 그 카드를 낸다.</summary>
    internal sealed class DebugHandView : DebugSessionView
    {
        protected override void BuildUI()
        {
            DebugUIFactory.AddHorizontalLayout(gameObject, spacing: 12, expandWidth: true, expandHeight: true);
        }

        protected override void SubscribeSession(StageSession session)
        {
            session.Runner.TurnBegan += OnTurnBegan;
            session.Runner.TurnResolved += OnTurnResolved;
            session.Censorship.LevelChanged += OnCensorshipChanged;
        }

        protected override void UnsubscribeSession(StageSession session)
        {
            session.Runner.TurnBegan -= OnTurnBegan;
            session.Runner.TurnResolved -= OnTurnResolved;
            session.Censorship.LevelChanged -= OnCensorshipChanged;
        }

        // StartStage()가 손패를 채운 뒤 TurnBegan(1)을 곧바로 쏘는데, 이걸 안 들으면 첫 턴에는
        // 아무 카드도 안 낸 상태라 TurnResolved가 없어 손패가 빈 채로 남는다.
        private void OnTurnBegan(int turn) => Render();
        private void OnTurnResolved(TurnReport report) => Render();
        private void OnCensorshipChanged(CensorshipLevel level) => Render();

        protected override void Render()
        {
            if (Session == null) return;

            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            var inProgress = Session.Runner.Outcome == StageOutcome.InProgress;
            var censorship = Session.Censorship.Level;

            for (var i = 0; i < Session.Hand.Cards.Count; i++)
            {
                var card = Session.Hand.Cards[i];
                var index = i;

                var button = DebugUIFactory.CreateButton(transform, $"Card_{card.Definition.Id}", out var label);
                DebugUIFactory.AddLayoutElement(button.gameObject, minWidth: 180, flexibleWidth: 1, flexibleHeight: 1);
                label.text = ClueCardFormatter.Format(card, Session.Ledger, censorship);
                button.interactable = inProgress;
                button.onClick.AddListener(() => Bootstrapper.PlayCardAtIndex(index));
            }
        }
    }
}
