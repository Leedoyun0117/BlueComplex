using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Complexes
{
    /// <summary>스테이지 컴플렉스 목록에서 아직 붙지 않은 것을 하나 뽑는다.</summary>
    public sealed class ComplexSpawner : IComplexSpawner
    {
        private readonly IReadOnlyList<ComplexDefinition> _candidates;
        private readonly IRandomSource _random;
        private readonly int _defaultPriority;

        public ComplexSpawner(IReadOnlyList<ComplexDefinition> candidates,
                              IRandomSource random,
                              int defaultPriority = 100)
        {
            _candidates = candidates;
            _random = random;
            _defaultPriority = defaultPriority;
        }

        public bool TrySpawn(ComplexBoard board, out ComplexInstance spawned)
        {
            spawned = null;
            if (board.IsFull || _candidates.Count == 0) return false;

            var pick = _candidates[_random.Range(0, _candidates.Count)];
            var instance = new ComplexInstance(pick, _defaultPriority + board.Slots.Count);

            if (!board.TryAttach(instance)) return false;

            spawned = instance;
            return true;
        }
    }
}
