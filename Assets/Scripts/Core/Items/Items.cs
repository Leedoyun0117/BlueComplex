using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Items
{
    /// <summary>아이템이 손댈 수 있는 것들만 모은 좁은 창구.</summary>
    public sealed class ItemActivationContext
    {
        public ClueHand Hand { get; }
        public TraitBoard Traits { get; }
        public ActiveItemBoard ActiveItems { get; }

        public ItemActivationContext(ClueHand hand, TraitBoard traits, ActiveItemBoard activeItems)
        {
            Hand = hand;
            Traits = traits;
            ActiveItems = activeItems;
        }
    }

    public interface IItemBehaviour
    {
        void OnActivate(ItemActivationContext context);
    }

    /// <summary>최종 의미가 확정된 뒤 결과를 손보는 효과.</summary>
    public interface IResultModifier
    {
        void Modify(TagSet finalTags);
    }

    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Duration { get; }
        public IItemBehaviour Behaviour { get; }

        public ItemDefinition(string id, string displayName, string description, int duration, IItemBehaviour behaviour)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Duration = duration;
            Behaviour = behaviour;
        }
    }

    public sealed class ActiveItem
    {
        public ItemDefinition Definition { get; }
        public int RemainingTurns { get; private set; }
        public bool IsExpired => RemainingTurns <= 0;

        public ActiveItem(ItemDefinition definition)
        {
            Definition = definition;
            RemainingTurns = definition.Duration;
        }

        public void Tick() => RemainingTurns--;
    }

    /// <summary>
    /// 지속 중인 아이템 효과를 모아 컴플렉스 필터와 결과 보정으로 노출한다.
    /// 아이템 개별 규칙은 각 Behaviour가 갖고, 여기서는 집계만 한다.
    /// </summary>
    public sealed class ActiveItemBoard : IComplexFilter, IResultModifier
    {
        private readonly List<ActiveItem> _active = new();
        private readonly List<IComplexFilter> _filters = new();
        private readonly List<IResultModifier> _modifiers = new();

        public IReadOnlyList<ActiveItem> Active => _active;

        public event Action<ActiveItem> Expired;

        public void Register(ItemDefinition definition) => _active.Add(new ActiveItem(definition));

        public void AddFilter(IComplexFilter filter) => _filters.Add(filter);
        public void AddModifier(IResultModifier modifier) => _modifiers.Add(modifier);

        public bool ShouldIgnore(ComplexInstance complex) => _filters.Any(f => f.ShouldIgnore(complex));

        public void Modify(TagSet finalTags)
        {
            foreach (var modifier in _modifiers) modifier.Modify(finalTags);
        }

        public void TickDurations()
        {
            foreach (var item in _active) item.Tick();

            foreach (var expired in _active.Where(a => a.IsExpired).ToList())
            {
                _active.Remove(expired);
                Expired?.Invoke(expired);
            }

            if (_active.Count != 0) return;
            _filters.Clear();
            _modifiers.Clear();
        }
    }

    /// <summary>보유 아이템. 프로토타입 기준 최대 2개, 턴마다 1개 랜덤 획득.</summary>
    public sealed class ItemInventory
    {
        private readonly List<ItemDefinition> _held = new();
        private readonly IReadOnlyList<ItemDefinition> _pool;
        private readonly IRandomSource _random;

        public int Capacity { get; }
        public IReadOnlyList<ItemDefinition> Held => _held;
        public bool IsFull => _held.Count >= Capacity;

        public event Action<ItemDefinition> Gained;
        public event Action<ItemDefinition> Used;

        public ItemInventory(IReadOnlyList<ItemDefinition> pool, IRandomSource random, int capacity = 2)
        {
            _pool = pool;
            _random = random;
            Capacity = capacity;
        }

        public bool TryGainRandom(out ItemDefinition gained)
        {
            gained = null;
            if (IsFull || _pool.Count == 0) return false;

            gained = _pool[_random.Range(0, _pool.Count)];
            _held.Add(gained);
            Gained?.Invoke(gained);
            return true;
        }

        public void Use(ItemDefinition item, ItemActivationContext context)
        {
            if (!_held.Remove(item)) throw new InvalidOperationException($"{item.Id} 을(를) 보유하고 있지 않습니다.");

            item.Behaviour.OnActivate(context);
            if (item.Duration > 0) context.ActiveItems.Register(item);
            Used?.Invoke(item);
        }
    }
}
