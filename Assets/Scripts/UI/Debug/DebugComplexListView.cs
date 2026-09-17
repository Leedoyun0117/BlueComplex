using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>현재 붙어 있는 컴플렉스 이름/남은 턴/설명을 나열한다.</summary>
    internal sealed class DebugComplexListView : DebugSessionView
    {
        private Transform _listRoot;
        private Text _titleText;

        protected override void BuildUI()
        {
            var go = gameObject;
            DebugUIFactory.AddVerticalLayout(go, spacing: 4);

            _titleText = DebugUIFactory.CreateText(transform, "Title", 16, color: new Color(0.8f, 0.85f, 1f));
            _titleText.text = "컴플렉스";

            var listGo = DebugUIFactory.CreateEmpty(transform, "List");
            DebugUIFactory.AddVerticalLayout(listGo.gameObject, spacing: 6);
            _listRoot = listGo;
        }

        protected override void SubscribeSession(StageSession session)
        {
            session.Complexes.Attached += OnBoardChanged;
            session.Complexes.Expired += OnBoardChanged;
            session.Runner.TurnResolved += OnTurnResolved;
        }

        protected override void UnsubscribeSession(StageSession session)
        {
            session.Complexes.Attached -= OnBoardChanged;
            session.Complexes.Expired -= OnBoardChanged;
            session.Runner.TurnResolved -= OnTurnResolved;
        }

        private void OnBoardChanged(BlueComplex.Core.Complexes.ComplexInstance _) => Render();
        private void OnTurnResolved(TurnReport report) => Render();

        protected override void Render()
        {
            if (Session == null) return;

            for (var i = _listRoot.childCount - 1; i >= 0; i--)
                Destroy(_listRoot.GetChild(i).gameObject);

            if (Session.Complexes.Slots.Count == 0)
            {
                var empty = DebugUIFactory.CreateText(_listRoot, "Empty", 14, color: new Color(0.6f, 0.6f, 0.6f));
                empty.text = "(없음)";
                return;
            }

            foreach (var complex in Session.Complexes.InPriorityOrder())
            {
                var entry = DebugUIFactory.CreateText(_listRoot, complex.Definition.Id, 14);
                entry.text = $"{complex.Definition.DisplayName} (남은 {complex.RemainingTurns}턴)\n{complex.Definition.Description}";
            }
        }
    }
}
