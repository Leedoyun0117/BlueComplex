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

        /// <summary>정해 준 아이템을 빈 칸에 바로 쥐여 준다(무작위 지급이 아니다 — 튜토리얼이 "이 턴에 이 아이템"을 정할 때 쓴다). 칸이 없으면 false.</summary>
        public bool TryGrant(ItemDefinition item)
        {
            if (IsFull) return false;

            _held.Add(item);
            Gained?.Invoke(item);
            return true;
        }

        /// <summary>
        /// 앞서 떠 둔 보유 목록(<see cref="Held"/>의 사본)으로 되돌린다 — 그 사이에 쓴 아이템이 원래 칸 순서대로 돌아온다(튜토리얼의 오답 되돌리기).
        /// 돌아온 아이템마다 <see cref="Gained"/>를 쏜다(화면이 칸에 다시 끼운다). 쓴 기록(다음 리필이 피할 종류)에서도 뺀다.
        /// </summary>
        public void RestoreHeld(IReadOnlyList<ItemDefinition> snapshot)
        {
            var returned = snapshot.Where(item => !_held.Contains(item)).ToList();

            _held.Clear();
            _held.AddRange(snapshot);
            foreach (var item in returned) _usedSinceRefill.Remove(item);
            foreach (var item in returned) Gained?.Invoke(item);
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
