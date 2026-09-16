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

        /// <summary>손패에서 풀로 되돌린다. (아이템 '회상')</summary>
        public void Return(ClueDefinition definition) => _available.Add(definition);
    }

    /// <summary>플레이어가 보유한 단서 4장.</summary>
    public sealed class ClueHand
    {
        public const int HandSize = 4;

        private readonly List<ClueInstance> _cards = new();
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

        public void Use(ClueInstance card)
        {
            card.ConsumeUse();
            if (!card.IsExhausted) return;

            // 뽑는 순간 풀에서 빠지므로, 소멸한 단서는 그대로 돌려놓지 않는 것으로 영구 삭제가 된다.
            _cards.Remove(card);
            CardDestroyed?.Invoke(card);
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
