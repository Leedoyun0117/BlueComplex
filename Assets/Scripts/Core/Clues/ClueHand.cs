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

        public void Refill()
        {
            while (_cards.Count < HandSize && _pool.TryDraw(out var definition))
            {
                var card = new ClueInstance(definition);
                _cards.Add(card);
                CardAdded?.Invoke(card);
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

        /// <summary>풀에 바꿔 올 단서가 남아 있는가.</summary>
        public bool CanReplaceOne => _pool.HasAny;

        /// <summary>
        /// 아이템 '선택적 기억' — 고른 단서를 풀에서 뽑은 랜덤 단서로 같은 자리에서 바꾼다. 새 단서를 먼저 뽑고 그 뒤에 고른 단서를 풀에 돌려놓으므로
        /// 방금 고른 단서가 바로 다시 나오지는 않는다. 풀이 비어 있으면 아무것도 바꾸지 않고 false.
        /// </summary>
        public bool TryReplaceRandom(ClueInstance card, out ClueInstance replacement)
        {
            replacement = null;
            var index = _cards.IndexOf(card);
            if (index < 0) throw new InvalidOperationException($"{card.Definition.Id} 는 손패에 없습니다.");
            if (!_pool.TryDraw(out var definition)) return false;

            _pool.Return(card.Definition);
            replacement = new ClueInstance(definition);
            _cards[index] = replacement;

            CardDestroyed?.Invoke(card);
            CardAdded?.Invoke(replacement);
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
