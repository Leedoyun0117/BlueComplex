using System.Linq;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>보유 아이템. 클릭하면 사용한다.</summary>
    internal sealed class DebugItemView : DebugSessionView
    {
        protected override void BuildUI()
        {
            DebugUIFactory.AddHorizontalLayout(gameObject, spacing: 12, expandWidth: false);
        }

        protected override void SubscribeSession(StageSession session)
        {
            session.Items.Gained += OnItemsChanged;
            session.Items.Used += OnItemsChanged;
            session.Runner.TurnResolved += OnTurnResolved;
        }

        protected override void UnsubscribeSession(StageSession session)
        {
            session.Items.Gained -= OnItemsChanged;
            session.Items.Used -= OnItemsChanged;
            session.Runner.TurnResolved -= OnTurnResolved;
        }

        private void OnItemsChanged(ItemDefinition _) => Render();
        private void OnTurnResolved(TurnReport report) => Render();

        protected override void Render()
        {
            if (Session == null) return;

            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            var inProgress = Session.Runner.Outcome == StageOutcome.InProgress;

            if (Session.Items.Held.Count == 0)
            {
                var empty = DebugUIFactory.CreateText(transform, "Empty", 14);
                empty.text = "(보유 아이템 없음)";
                return;
            }

            foreach (var item in Session.Items.Held)
            {
                var button = DebugUIFactory.CreateButton(transform, $"Item_{item.Id}", out var label,
                    new UnityEngine.Color(0.22f, 0.3f, 0.22f));
                DebugUIFactory.AddLayoutElement(button.gameObject, minWidth: 160, minHeight: 60);
                label.text = $"{item.DisplayName}\n{item.Description}";
                button.interactable = inProgress && Session.Runner.CanUseItem(item);
                // 디버그 뷰에는 대상 선택 UI가 없다 — 대상이 필요한 아이템은 고를 수 있는 첫 대상에 쓴다.
                button.onClick.AddListener(() =>
                {
                    var runner = Bootstrapper.Session.Runner;
                    var targets = runner.GetItemTargets(item);
                    runner.UseItem(item, item.TargetKind == ItemTargetKind.None ? null : targets.FirstOrDefault());
                });
            }
        }
    }
}
