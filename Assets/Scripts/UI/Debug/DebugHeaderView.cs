using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>상단 HUD: 심박수/상태, 검열 수준, 턴 진행도, 키 진행도.</summary>
    internal sealed class DebugHeaderView : DebugSessionView
    {
        private Text _heartbeatText;
        private Text _censorshipText;
        private Text _turnText;
        private Text _keyText;

        protected override void BuildUI()
        {
            var go = gameObject;
            DebugUIFactory.AddHorizontalLayout(go, spacing: 24, expandWidth: false);

            _heartbeatText = DebugUIFactory.CreateText(transform, "Heartbeat", 18);
            _censorshipText = DebugUIFactory.CreateText(transform, "Censorship", 18);
            _turnText = DebugUIFactory.CreateText(transform, "Turn", 18);
            _keyText = DebugUIFactory.CreateText(transform, "Keys", 18);
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

        private void OnTurnBegan(int turn) => Render();
        private void OnTurnResolved(TurnReport report) => Render();
        private void OnCensorshipChanged(CensorshipLevel level) => Render();

        protected override void Render()
        {
            if (Session == null) return;

            var value = Session.Heartbeat.Value;
            var state = Session.Zone.StateOf(value);
            _heartbeatText.text = $"{value} / {DebugKoreanLabels.State(state)}";

            _censorshipText.text = $"검열: {DebugKoreanLabels.Censorship(Session.Censorship.Level)}";

            _turnText.text = $"턴: {Session.Runner.CurrentTurn} / {Session.Runner.TotalTurns}";

            _keyText.text = $"키: {Session.Keys.Collected} / {Session.Keys.Required}";
        }
    }
}
