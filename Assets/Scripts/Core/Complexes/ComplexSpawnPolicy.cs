using System.Collections.Generic;
using BlueComplex.Core.Clues;

namespace BlueComplex.Core.Complexes
{
    public interface IComplexSpawnPolicy
    {
        bool ShouldSpawn(int distanceFromCenter);
    }

    /// <summary>
    /// 중앙에서 벗어난 칸 수별 발현 확률.
    /// 기본값 1칸 5% / 2칸 15% / 3칸 30% / 4칸 30% / 5칸 10%.
    /// 스테이지별 가중치는 weight로 조정한다. (가라앉다=낮음, 무제=높음)
    /// </summary>
    public sealed class DistanceBasedSpawnPolicy : IComplexSpawnPolicy
    {
        private static readonly Dictionary<int, double> DefaultTable = new()
        {
            { 0, 0.00 },
            { 1, 0.05 },
            { 2, 0.15 },
            { 3, 0.30 },
            { 4, 0.30 },
            { 5, 0.10 }
        };

        private readonly IRandomSource _random;
        private readonly IReadOnlyDictionary<int, double> _table;
        private readonly double _weight;

        public DistanceBasedSpawnPolicy(IRandomSource random,
                                        double weight = 1.0,
                                        IReadOnlyDictionary<int, double> table = null)
        {
            _random = random;
            _weight = weight;
            _table = table ?? DefaultTable;
        }

        public bool ShouldSpawn(int distanceFromCenter)
        {
            if (!_table.TryGetValue(distanceFromCenter, out var chance)) return false;
            return _random.NextDouble() < chance * _weight;
        }
    }

    /// <summary>스테이지 컴플렉스 목록에서 아직 붙지 않은 것을 하나 뽑는다.</summary>
    public sealed class ComplexSpawner
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
