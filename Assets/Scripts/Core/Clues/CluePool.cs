using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Clues
{
    /// <summary>스테이지의 단서 풀. 소멸한 단서는 여기서도 영구 삭제된다.</summary>
    public sealed class CluePool
    {
        private readonly List<ClueDefinition> _available;
        private readonly IRandomSource _random;

        public int Count => _available.Count;

        public CluePool(IEnumerable<ClueDefinition> definitions, IRandomSource random)
        {
            _available = definitions.ToList();
            _random = random;
        }

        /// <summary>조건을 만족하는(set에 속하지 않은) 단서 하나를 무작위로 뽑는다. 없으면 false. 손패 편향(<see cref="HandBias"/>)이 쓴다.</summary>
        public bool TryDrawWhere(Func<ClueDefinition, bool> predicate, out ClueDefinition definition)
        {
            definition = null;
            var candidates = new List<int>();
            for (var i = 0; i < _available.Count; i++)
                if (_available[i].SetId == null && predicate(_available[i])) candidates.Add(i);
            if (candidates.Count == 0) return false;

            var index = candidates[_random.Range(0, candidates.Count)];
            definition = _available[index];
            _available.RemoveAt(index);
            return true;
        }

        public bool TryDraw(out ClueDefinition definition)
        {
            definition = null;
            if (_available.Count == 0) return false;
            var index = _random.Range(0, _available.Count);
            definition = _available[index];
            _available.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// set를 인식하는 뽑기. 풀에서 한 장을 뽑고, 그 단서가 set에 속하면 같은 set의 동료를 함께 뽑아 <paramref name="group"/>에 담는다(첫 원소가 처음 뽑힌 단서).
        /// 동료 수는 <see cref="ClueDefinition.SetPickCount"/> − 1 − (이미 손에 든 같은 set 단서 수)이고 풀에 남은 동료 수를 넘지 않는다 —
        /// 3개 이상인 set은 뽑힌 단서와 랜덤 동료 하나, 곧 set에서 랜덤 2개가 나오고 나머지는 풀에 남는다.
        /// 묶음이 <paramref name="freeSlots"/>에 들어가지 않는 set 단서와, 이미 <see cref="ClueDefinition.SetPickCount"/>장이 손에 있는 set의 나머지는
        /// 후보에서 뺀다 — set를 쪼개지도, 손패를 넘치게 하지도, 한 set가 2장을 넘겨 나오지도 않는다.
        /// 들어갈 후보가 없으면 false(풀은 그대로).
        /// 풀에 set 단서가 하나도 없으면 <see cref="TryDraw"/>와 똑같은 난수 한 번만 쓴다(set 없는 스테이지의 뽑기 결과는 그대로다).
        /// </summary>
        public bool TryDrawGroup(int freeSlots, IReadOnlyCollection<ClueInstance> held, List<ClueDefinition> group)
        {
            group.Clear();
            if (freeSlots <= 0 || _available.Count == 0) return false;

            if (!_available.Any(d => d.SetId != null))
            {
                var plain = _random.Range(0, _available.Count);
                group.Add(_available[plain]);
                _available.RemoveAt(plain);
                return true;
            }

            var candidates = FittingIndices(freeSlots, held);
            if (candidates.Count == 0) return false;

            var trigger = _available[candidates[_random.Range(0, candidates.Count)]];
            var extra = ExtraNeeded(trigger, held);
            _available.Remove(trigger);
            group.Add(trigger);

            for (var i = 0; i < extra; i++)
            {
                var mates = _available.Where(d => d.SetId == trigger.SetId).ToList();
                var mate = mates[_random.Range(0, mates.Count)];
                _available.Remove(mate);
                group.Add(mate);
            }

            return true;
        }

        /// <summary><see cref="TryDrawGroup"/>가 지금 성공할지. 난수를 쓰지 않는다.</summary>
        public bool CanDrawGroup(int freeSlots, IReadOnlyCollection<ClueInstance> held) =>
            freeSlots > 0 && FittingIndices(freeSlots, held).Count > 0;

        private List<int> FittingIndices(int freeSlots, IReadOnlyCollection<ClueInstance> held)
        {
            var result = new List<int>();
            for (var i = 0; i < _available.Count; i++)
            {
                var definition = _available[i];
                // 이미 SetPickCount장이 손에 있는 set는 다 제공된 것이다 — 풀에 남은 나머지는 낱개처럼 끼어들지 못한다.
                if (definition.SetId != null && HeldInSet(definition.SetId, held) >= ClueDefinition.SetPickCount) continue;
                if (1 + ExtraNeeded(definition, held) <= freeSlots) result.Add(i);
            }
            return result;
        }

        private static int HeldInSet(string setId, IReadOnlyCollection<ClueInstance> held) =>
            held.Count(c => c.Definition.SetId == setId);

        private int ExtraNeeded(ClueDefinition definition, IReadOnlyCollection<ClueInstance> held)
        {
            if (definition.SetId == null) return 0;

            var heldMates = HeldInSet(definition.SetId, held);
            var poolMates = _available.Count(d => d.SetId == definition.SetId && d != definition);
            return Math.Min(Math.Max(0, ClueDefinition.SetPickCount - 1 - heldMates), poolMates);
        }

        /// <summary>풀에 남은 단서가 있는지(아이템 '선택적 기억'이 새 단서를 뽑을 수 있는지).</summary>
        public bool HasAny => _available.Count > 0;

        /// <summary>손패에서 풀로 되돌린다. (아이템 '회상')</summary>
        public void Return(ClueDefinition definition) => _available.Add(definition);
    }
}
