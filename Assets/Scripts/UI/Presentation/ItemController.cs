using BlueComplex.Core.Items;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using UnityEngine;
using UnityEngine.UI;

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
        private ItemTargetSelector _selector;

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
            // DLJ: 접혀 있어도 획득 연출과 빈칸이 보이도록 패널을 펼친다.
            if (firstNew != int.MaxValue) _panel.SetFolded(false);

            for (var i = 0; i < _panel.SlotCount; i++)
            {
                var slot = _panel.GetSlot(i);
                if (i >= capacity) slot.Hide();
                else if (i < held.Count) slot.Render(held[i], insert: i >= firstNew);
                else slot.SetEmpty();
            }

            // 슬롯을 한 프레임 안에 여럿 SetActive/내용 변경하면 VerticalLayoutGroup이 한 번의 자동 리빌드로 전부
            // 수렴하지 못하고 마지막 슬롯(들)의 가로 위치가 이전 프레임 값으로 남는 경우가 있다(배치모드 진단으로 재현·확인함:
            // 강제 리빌드를 두 번 연달아 불러야 안정적으로 고쳐졌다 — 한 번은 그대로 어긋난 값을 냈다).
            // 그래서 매 Refresh마다 명시적으로 두 번 강제 리빌드한다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel.Root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel.Root);

            _shownCount = held.Count;
        }

        /// <summary>
        /// 아이템 클릭. 대상이 필요 없는 아이템은 바로 쓰고, 대상이 필요한 아이템은 대상 선택 모드로 들어간다 —
        /// 고를 수 있는 대상(코어의 GetItemTargets)이 강조되고, 하나를 누르면 그 대상에 사용한다. 같은 아이템을 다시 누르거나 Esc/우클릭이면 취소.
        /// 대상 선택 중에 다른 아이템을 누르면 그 아이템으로 갈아탄다. 턴 연출이 재생 중이면 아이템은 쓸 수 없다(카드를 낼 수 없는 것과 같은 규칙).
        /// </summary>
        private void OnSlotClicked(ItemDefinition item)
        {
            if (Session == null || Session.Runner.Outcome != StageOutcome.InProgress) return;

            var selector = Selector;
            if (selector.IsSelecting && selector.Item == item)
            {
                selector.Cancel();
                return;
            }

            selector.Cancel();

            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            var runner = Session.Runner;
            var slot = FindSlot(item);

            if (!runner.CanUseItem(item))
            {
                slot?.PlayRejected();
                selector.ShowMessage(item.TargetKind == ItemTargetKind.None
                    ? "지금은 쓸 수 없는 아이템입니다"
                    : $"\"{item.DisplayName}\" — 쓸 수 있는 대상이 없습니다");
                return;
            }

            if (item.TargetKind == ItemTargetKind.None)
            {
                runner.UseItem(item);
                return;
            }

            selector.Begin(item, runner.GetItemTargets(item), slot, target =>
            {
                if (Session != null && Session.Runner.Outcome == StageOutcome.InProgress && Session.Runner.CanUseItem(item))
                    Session.Runner.UseItem(item, target);
            });
        }

        private ItemTargetSelector Selector => _selector != null
            ? _selector
            : _selector = GetComponent<ItemTargetSelector>() ?? gameObject.AddComponent<ItemTargetSelector>();

        protected override void Unsubscribe(StageSession session)
        {
            _selector?.Cancel();
            session.Items.Gained -= OnGained;
            session.Items.Used -= OnUsed;
        }
    }
}
