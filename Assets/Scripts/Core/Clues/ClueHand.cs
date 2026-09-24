using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Clues
{
    public interface IRandomSource
    {
        int Range(int minInclusive, int maxExclusive);
        double NextDouble();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;
        public SystemRandomSource(int? seed = null) => _random = seed.HasValue ? new Random(seed.Value) : new Random();
        public int Range(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
        public double NextDouble() => _random.NextDouble();
    }

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

    /// <summary>
    /// 플레이어가 보유한 단서. 낸 카드는 그 쿼터 안에서는 영구 소멸하고, 빈 칸은 <see cref="Refill"/> 이
    /// 불릴 때만 채워진다 — 호출 시점은 TurnRunner가 쿼터 시작마다 쥔다(쿼터 중에는 손패가 줄어든 채로 진행된다).
    ///
    /// 저작된 단서 정의 수(현재 10개)가 스테이지 전체 턴 수(3쿼터 × 4턴 = 12)보다 적으므로, 매 쿼터 손패를
    /// 가득 채우려면 지난 쿼터에서 낸 단서가 다음 쿼터에 다시 나올 수 있어야 한다 — 그래서 낸 단서는
    /// <see cref="Use"/> 시점엔 풀로 돌아가지 않고 무덤(_graveyard)에 쌓였다가, <see cref="RefillForNewQuarter"/>가
    /// 쿼터 경계에서 그 무덤을 풀로 되돌린다. 같은 쿼터 안에서는 절대 다시 나오지 않는다(무덤 비우기는 쿼터 시작 때만 일어난다).
    /// </summary>
    public sealed class ClueHand
    {
        public const int HandSize = 4;

        private readonly List<ClueInstance> _cards = new();
        private readonly List<ClueDefinition> _graveyard = new();
        private readonly CluePool _pool;

        public IReadOnlyList<ClueInstance> Cards => _cards;

        public event Action<ClueInstance> CardAdded;
        public event Action<ClueInstance> CardDestroyed;

        public ClueHand(CluePool pool) => _pool = pool;

        /// <summary>빈 칸을 채운다. set 단서는 동료와 함께(묶음이 남은 칸에 들어갈 때만) 들어오고, 안 들어가면 그 set는 이번엔 건너뛴다 —
        /// 남은 후보가 전부 안 들어가는 set뿐이면 칸이 빈 채로 끝난다(<see cref="CluePool.TryDrawGroup"/>).</summary>
        public void Refill()
        {
            var group = new List<ClueDefinition>();
            while (_cards.Count < HandSize && _pool.TryDrawGroup(HandSize - _cards.Count, _cards, group))
            {
                foreach (var definition in group)
                {
                    var card = new ClueInstance(definition);
                    _cards.Add(card);
                    CardAdded?.Invoke(card);
                }
            }
        }

        /// <summary>쿼터가 새로 시작될 때 부른다. 지난 쿼터들에서 낸(무덤에 쌓인) 단서를 전부 풀로 되돌린 뒤
        /// 손패를 채운다 — 정의된 단서 수가 손패 크기보다 적어도 매 쿼터 손패가 가득 차는 것을 보장한다.
        /// 회상 등 쿼터 중 손패 갱신(<see cref="Refill"/>, <see cref="RedrawAll"/>)은 무덤을 건드리지 않으므로
        /// 같은 쿼터 안에서 이미 낸 단서는 여전히 돌아오지 않는다.</summary>
        public void RefillForNewQuarter()
        {
            foreach (var definition in _graveyard) _pool.Return(definition);
            _graveyard.Clear();
            Refill();
        }

        /// <summary>단서를 낸다. 손패에서 빠지고 이번 쿼터의 풀로는 돌아가지 않는다(무덤에 쌓여 다음 쿼터에만 되돌아간다).
        /// 자동으로 채우지 않는다.</summary>
        public void Use(ClueInstance card)
        {
            if (!_cards.Remove(card))
                throw new InvalidOperationException($"{card.Definition.Id} 는 손패에 없습니다.");
            _graveyard.Add(card.Definition);
            CardDestroyed?.Invoke(card);
        }

        /// <summary>풀에 바꿔 올 단서가 남아 있는가. (set 단서가 칸에 안 들어가는 경우까지 보려면 <see cref="CanReplace"/>)</summary>
        public bool CanReplaceOne => _pool.HasAny;

        /// <summary>이 카드를 <see cref="TryReplaceRandom"/>으로 바꿀 수 있는가 — 고른 카드를 뺀 자리에 들어갈 후보가 풀에 있는가.
        /// set 단서는 묶음 전체가 들어갈 자리가 있어야 후보가 된다.</summary>
        public bool CanReplace(ClueInstance card)
        {
            if (!_cards.Contains(card)) return false;
            var rest = _cards.Where(c => c != card).ToList();
            return _pool.CanDrawGroup(HandSize - rest.Count, rest);
        }

        /// <summary>
        /// 아이템 '선택적 기억' — 고른 단서를 풀에서 뽑은 랜덤 단서로 같은 자리에서 바꾼다. 새 단서를 먼저 뽑고 그 뒤에 고른 단서를 풀에 돌려놓으므로
        /// 방금 고른 단서가 바로 다시 나오지는 않는다. 새 단서가 set에 속하면 그 동료가 바로 뒤 칸에 함께 들어온다(손패가 <see cref="HandSize"/>를 넘지 않는
        /// 후보만 뽑힌다). 풀이 비어 있거나 후보가 모두 안 들어가면 아무것도 바꾸지 않고 false.
        /// </summary>
        public bool TryReplaceRandom(ClueInstance card, out ClueInstance replacement)
        {
            replacement = null;
            var index = _cards.IndexOf(card);
            if (index < 0) throw new InvalidOperationException($"{card.Definition.Id} 는 손패에 없습니다.");

            var rest = _cards.Where(c => c != card).ToList();
            var group = new List<ClueDefinition>();
            if (!_pool.TryDrawGroup(HandSize - rest.Count, rest, group)) return false;

            _pool.Return(card.Definition);
            replacement = new ClueInstance(group[0]);
            _cards[index] = replacement;
            var companions = group.Skip(1).Select(d => new ClueInstance(d)).ToList();
            _cards.InsertRange(index + 1, companions);

            CardDestroyed?.Invoke(card);
            CardAdded?.Invoke(replacement);
            foreach (var companion in companions) CardAdded?.Invoke(companion);
            return true;
        }

        /// <summary>아이템 '회상' — 손패 전부를 풀에 돌려놓고 다시 뽑는다.</summary>
        public void RedrawAll()
        {
            foreach (var card in _cards) _pool.Return(card.Definition);
            _cards.Clear();
            Refill();
        }
    }
}
