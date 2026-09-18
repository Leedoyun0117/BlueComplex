using BlueComplex.Core.Items;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>아이템 슬롯을 코어에 연결한다. 아이템은 턴 밖에서도 변하므로 직접 구독한다.</summary>
    public sealed class ItemController : SessionBoundView
    {
        [SerializeField] private ItemDisplayPanel _panel;

        protected override void Awake()
        {
            for (var i = 0; i < _panel.SlotCount; i++)
                _panel.GetSlot(i).Clicked += OnSlotClicked;

            base.Awake();
        }

        protected override void Subscribe(StageSession session)
        {
            session.Items.Gained += OnInventoryChanged;
            session.Items.Used += OnInventoryChanged;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Items.Gained -= OnInventoryChanged;
            session.Items.Used -= OnInventoryChanged;
        }

        protected override void Render()
        {
            for (var i = 0; i < _panel.SlotCount; i++)
            {
                if (i < Session.Items.Held.Count) _panel.GetSlot(i).Render(Session.Items.Held[i]);
                else _panel.GetSlot(i).SetEmpty();
            }
        }

        private void OnInventoryChanged(ItemDefinition item) => Render();

        private void OnSlotClicked(ItemDefinition item)
        {
            if (Session == null || Session.Runner.Outcome != StageOutcome.InProgress) return;
            Session.Runner.UseItem(item);
        }
    }
}
