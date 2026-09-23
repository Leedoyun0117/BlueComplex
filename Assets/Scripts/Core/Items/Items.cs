using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Items
{
    /// <summary>아이템이 사용 전에 대상을 골라야 하는지, 그렇다면 무엇을 고르는지.</summary>
    public enum ItemTargetKind
    {
        None,
        Complex,
        Clue
    }

    /// <summary>
    /// 아이템 대상 하나. 대상 종류가 달라도(컴플렉스, 단서) <c>TurnRunner.UseItem(item, target)</c> 시그니처는 하나로 두려고 만든 얇은 상자다 —
    /// 종류는 <see cref="Kind"/>로 알고, 아이템 행동은 자기에게 맞는 서브클래스로만 꺼내 쓴다.
    /// </summary>
    public abstract class ItemTarget
    {
        public abstract ItemTargetKind Kind { get; }

        public static ComplexTarget Of(ComplexInstance complex) => new(complex);
        public static ClueTarget Of(ClueInstance card) => new(card);
    }

    public sealed class ComplexTarget : ItemTarget
    {
        public ComplexInstance Complex { get; }
        public override ItemTargetKind Kind => ItemTargetKind.Complex;
        public ComplexTarget(ComplexInstance complex) => Complex = complex;
    }

    public sealed class ClueTarget : ItemTarget
    {
        public ClueInstance Card { get; }
        public override ItemTargetKind Kind => ItemTargetKind.Clue;
        public ClueTarget(ClueInstance card) => Card = card;
    }

    /// <summary>아이템이 손댈 수 있는 것들만 모은 좁은 창구.</summary>
    public sealed class ItemActivationContext
    {
        public ClueHand Hand { get; }
        public TraitBoard Traits { get; }
        public ActiveItemBoard ActiveItems { get; }
        public ComplexBoard Complexes { get; }
        public Heartbeat Heartbeat { get; }
        public HeartbeatZone Zone { get; }

        /// <summary>스테이지가 아이템에 넘기는 값(예: 감정적 설득이 무시할 컴플렉스 id 목록). StageConfig.ItemParameters에서 온다.</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> StageParameters { get; }

        /// <summary>사용자가 고른 대상. 대상이 필요 없는 아이템은 null.</summary>
        public ItemTarget Target { get; }

        /// <summary>이 사용으로 만들어지는 지속 효과 상자(지속 시간이 0이면 null — 즉시 효과만 낼 수 있다).</summary>
        internal ActiveItem Active { get; set; }

        public ItemActivationContext(ClueHand hand,
                                     TraitBoard traits,
                                     ActiveItemBoard activeItems,
                                     ComplexBoard complexes = null,
                                     Heartbeat heartbeat = null,
                                     HeartbeatZone zone = null,
                                     IReadOnlyDictionary<string, IReadOnlyList<string>> stageParameters = null,
                                     ItemTarget target = null)
        {
            Hand = hand;
            Traits = traits;
            ActiveItems = activeItems;
            Complexes = complexes;
            Heartbeat = heartbeat;
            Zone = zone;
            StageParameters = stageParameters;
            Target = target;
        }

        /// <summary>스테이지 파라미터 하나. 없으면 빈 목록.</summary>
        public IReadOnlyList<string> Parameter(string key) =>
            StageParameters != null && StageParameters.TryGetValue(key, out var values) ? values : Array.Empty<string>();

        /// <summary>이 사용이 살아 있는 동안 컴플렉스를 무시하는 필터를 건다.</summary>
        public void AddFilter(IComplexFilter filter) => RequireActive().Filters.Add(filter);

        /// <summary>이 사용이 살아 있는 동안 최종 결과에 걸리는 보정을 건다.</summary>
        public void AddModifier(IResultModifier modifier) => RequireActive().Modifiers.Add(modifier);

        private ActiveItem RequireActive() =>
            Active ?? throw new InvalidOperationException("지속 시간이 0인 아이템은 지속 효과(필터/보정)를 걸 수 없습니다.");
    }

    public interface IItemBehaviour
    {
        void OnActivate(ItemActivationContext context);
    }

    /// <summary>대상이 필요한 아이템 행동이 고를 수 있는 대상을 좁힌다(예: 남은 턴이 2 이상인 컴플렉스만).</summary>
    public interface IItemTargeting
    {
        bool IsValidTarget(ItemTarget target, ItemActivationContext context);
    }

    /// <summary>최종 의미가 확정된 뒤 결과를 손보는 효과.</summary>
    public interface IResultModifier
    {
        void Modify(TagSet finalTags);
    }

    /// <summary>
    /// 아이템 한 종류의 데이터. 행동(<see cref="Behaviour"/>)은 숫자·id를 인자로 받는 범용 조각이고, 이 클래스가 무엇을 조합하는지 적는다 —
    /// 대상 종류, 지속 시간, 부여하는 특성(<see cref="GrantedTraitId"/>)이 전부 데이터다.
    /// </summary>
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }

        /// <summary>지속 효과(컴플렉스 무시, 결과 보정)가 붙어 있는 턴 수. 0이면 즉시 효과만 있다.</summary>
        public int Duration { get; }
        public IItemBehaviour Behaviour { get; }

        /// <summary>사용 전에 고르는 대상의 종류. None이면 클릭 한 번으로 바로 쓴다.</summary>
        public ItemTargetKind TargetKind { get; }

        /// <summary>사용하면 함께 붙는 특성의 id(없으면 null). 특성 자체의 지속 턴과 효과는 TraitDefinition이 쥔다.</summary>
        public string GrantedTraitId { get; }

        public ItemDefinition(string id, string displayName, string description, int duration, IItemBehaviour behaviour,
                              ItemTargetKind targetKind = ItemTargetKind.None, string grantedTraitId = null)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Duration = duration;
            Behaviour = behaviour;
            TargetKind = targetKind;
            GrantedTraitId = grantedTraitId;
        }
    }

    /// <summary>지속 중인 아이템 사용 하나. 자기가 건 필터·보정을 자기가 들고 있어서, 만료되면 그 효과만 사라진다.</summary>
    public sealed class ActiveItem
    {
        public ItemDefinition Definition { get; }
        public int RemainingTurns { get; private set; }
        public bool IsExpired => RemainingTurns <= 0;

        internal List<IComplexFilter> Filters { get; } = new();
        internal List<IResultModifier> Modifiers { get; } = new();

        public ActiveItem(ItemDefinition definition)
        {
            Definition = definition;
            RemainingTurns = definition.Duration;
        }

        public void Tick() => RemainingTurns--;
    }

    /// <summary>
    /// 지속 중인 아이템 효과를 모아 컴플렉스 필터와 결과 보정으로 노출한다.
    /// 아이템 개별 규칙은 각 Behaviour가 갖고, 여기서는 집계만 한다. 필터·보정은 사용마다 따로 들고 있어서 아이템이 겹쳐도 만료된 것만 빠진다.
    /// </summary>
    public sealed class ActiveItemBoard : IComplexFilter, IResultModifier
    {
        private readonly List<ActiveItem> _active = new();

        public IReadOnlyList<ActiveItem> Active => _active;

        public event Action<ActiveItem> Expired;

        public void Register(ActiveItem item) => _active.Add(item);

        public bool ShouldIgnore(ComplexInstance complex) =>
            _active.Any(item => item.Filters.Any(filter => filter.ShouldIgnore(complex)));

        public void Modify(TagSet finalTags)
        {
            foreach (var item in _active)
                foreach (var modifier in item.Modifiers)
                    modifier.Modify(finalTags);
        }

        public void TickDurations()
        {
            foreach (var item in _active) item.Tick();

            foreach (var expired in _active.Where(a => a.IsExpired).ToList())
            {
                _active.Remove(expired);
                Expired?.Invoke(expired);
            }
        }
    }

    /// <summary>
    /// 보유 아이템. 스테이지 시작 때(TurnRunner.StartStage)와 이후 매 쿼터 경계(TurnRunner.BeginTurn)마다
    /// <see cref="Refill"/>로 빈 칸만 <see cref="Capacity"/>개까지 채운다. 쓴 칸만 다른 아이템으로 채워지고,
    /// 안 쓴 아이템은 그대로 유지된다(손패처럼 전체를 새로 뽑지 않는다 — ClueHand.RefillForNewQuarter와 다름).
    /// </summary>
    public sealed class ItemInventory
    {
        public const int DefaultCapacity = 4;

        private readonly List<ItemDefinition> _held = new();
        private readonly List<ItemDefinition> _usedSinceRefill = new();
        private readonly IReadOnlyList<ItemDefinition> _pool;
        private readonly IRandomSource _random;

        public int Capacity { get; }
        public IReadOnlyList<ItemDefinition> Held => _held;
        public bool IsFull => _held.Count >= Capacity;

        public event Action<ItemDefinition> Gained;
        public event Action<ItemDefinition> Used;

        public ItemInventory(IReadOnlyList<ItemDefinition> pool, IRandomSource random, int capacity = DefaultCapacity)
        {
            _pool = pool;
            _random = random;
            Capacity = capacity;
        }

        public bool Holds(ItemDefinition item) => _held.Contains(item);

        /// <summary>빈 칸을 랜덤 아이템으로 채운다. 지금 들고 있는 종류와 직전 쿼터에 쓴 종류는 되도록 피하고("다른 랜덤 아이템"),
        /// 종류가 모자라면 그 제한을 차례로 푼다(쓴 종류 허용 → 보유 종류 중복 허용). 채운 개수를 돌려준다.</summary>
        public int Refill()
        {
            var added = 0;
            while (_held.Count < Capacity && _pool.Count > 0)
            {
                var candidates = _pool.Where(p => !_held.Contains(p) && !_usedSinceRefill.Contains(p)).ToList();
                if (candidates.Count == 0) candidates = _pool.Where(p => !_held.Contains(p)).ToList();
                if (candidates.Count == 0) candidates = _pool.ToList();

                var pick = candidates[_random.Range(0, candidates.Count)];
                _held.Add(pick);
                added++;
                Gained?.Invoke(pick);
            }

            _usedSinceRefill.Clear();
            return added;
        }

        /// <summary>아이템을 쓴다: 보유에서 빼고 → 행동 실행(즉시 효과 + 지속 효과 등록) → 부여 특성을 붙인다. 대상·상태 검증은 호출자(TurnRunner)가 이미 했다.</summary>
        public void Use(ItemDefinition item, ItemActivationContext context)
        {
            if (!_held.Remove(item)) throw new InvalidOperationException($"{item.Id} 을(를) 보유하고 있지 않습니다.");

            var active = item.Duration > 0 ? new ActiveItem(item) : null;
            context.Active = active;

            item.Behaviour.OnActivate(context);

            if (item.GrantedTraitId != null) context.Traits.Grant(item.GrantedTraitId);
            if (active != null) context.ActiveItems.Register(active);

            _usedSinceRefill.Add(item);
            Used?.Invoke(item);
        }
    }
}
