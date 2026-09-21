using BlueComplex.Core.Items;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 아이템 슬롯을 코어에 연결한다. 사용(Used)은 플레이어의 클릭이라 턴 해석 밖에서 일어나므로 바로 반영한다 —
    /// 카드가 구겨지며 사라지고, 끝나면 칸이 비워진다.
    /// 획득(Gained)은 TurnRunner.BeginTurn, 즉 턴 해석 직후에 일어난다. 연출이 재생 중이면 그 타이밍은 Presenter가 쥔다 —
    /// 새 카드가 끼워지는 움직임을 미뤄 두었다가 Presenter가 연출을 마친 뒤 <see cref="FlushPending"/>을 불러 재생한다.
    /// 슬롯은 코어의 보유 한도(ItemInventory.Capacity)만큼만 보이고, 카드가 없는 칸은 빈 테두리로 남는다.
    /// </summary>
    public sealed class ItemController : SessionBoundView
    {
        [SerializeField] private ItemDisplayPanel _panel;

        private ITurnResultPresenter _presenter;
        private int _shownCount;
        private int _crumpling;
        private bool _insertPending;

        protected override void Awake()
        {
            for (var i = 0; i < _panel.SlotCount; i++)
                _panel.GetSlot(i).Clicked += OnSlotClicked;

            base.Awake();
        }

        protected override void Subscribe(StageSession session)
        {
            session.Items.Gained += OnGained;
            session.Items.Used += OnUsed;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Items.Gained -= OnGained;
            session.Items.Used -= OnUsed;
        }

        /// <summary>세션 시작(재시작 포함): 애니메이션 없이 현재 보유 상태를 그대로 놓는다.</summary>
        protected override void Render()
        {
            _crumpling = 0;
            _insertPending = false;
            _shownCount = Session.Items.Held.Count;
            Refresh(animateNew: false);
        }

        /// <summary>연출이 끝난 뒤 Presenter가 부른다 — 미뤄 둔 새 카드를 끼운다.</summary>
        public void FlushPending()
        {
            if (Session == null || !_insertPending || _crumpling > 0) return;

            _insertPending = false;
            Refresh(animateNew: true);
        }

        private void OnGained(ItemDefinition item)
        {
            _insertPending = true;

            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            FlushPending();
        }

        private void OnUsed(ItemDefinition item)
        {
            var slot = FindSlot(item);
            if (slot == null)
            {
                Refresh(animateNew: false);
                return;
            }

            _crumpling++;
            slot.PlayUse(() =>
            {
                _crumpling--;
                if (_crumpling > 0) return;

                var animateNew = _insertPending;
                _insertPending = false;
                Refresh(animateNew);
            });
        }

        private ItemSlotView FindSlot(ItemDefinition item)
        {
            for (var i = 0; i < _panel.SlotCount; i++)
            {
                var slot = _panel.GetSlot(i);
                if (slot.Item == item && slot.gameObject.activeSelf) return slot;
            }

            return null;
        }

        /// <summary>슬롯을 보유 상태에 맞춘다. animateNew면 지난번 표시 이후 늘어난 카드만 끼워지는 움직임을 재생한다.</summary>
        private void Refresh(bool animateNew)
        {
            var held = Session.Items.Held;
            var capacity = Session.Items.Capacity;
            var firstNew = animateNew && held.Count > _shownCount ? _shownCount : int.MaxValue;

            for (var i = 0; i < _panel.SlotCount; i++)
            {
                var slot = _panel.GetSlot(i);
                if (i >= capacity) slot.Hide();
                else if (i < held.Count) slot.Render(held[i], insert: i >= firstNew);
                else slot.SetEmpty();
            }

            _shownCount = held.Count;
        }

        private void OnSlotClicked(ItemDefinition item)
        {
            if (Session == null || Session.Runner.Outcome != StageOutcome.InProgress) return;
            Session.Runner.UseItem(item);
        }
    }
}
