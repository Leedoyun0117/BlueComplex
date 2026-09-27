using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Complexes
{
    public interface IComplexSpawnPolicy
    {
        bool ShouldSpawn(int heartbeatValue);
    }

    /// <summary>
    /// 심박수 구간별 발현 확률. 안정 구간은 0%, 벗어날수록(침체/흥분 → 매우 침체/매우 흥분) 위험해진다.
    /// 구간별 확률표는 <see cref="HeartbeatZone"/>이 가지고 있으며, 스테이지별 가중치는 weight로 조정한다.
    /// (가라앉다=낮음, 무제=높음)
    /// </summary>
    public sealed class ZoneBasedSpawnPolicy : IComplexSpawnPolicy
    {
        private readonly IRandomSource _random;
        private readonly HeartbeatZone _zone;
        private readonly double _weight;

        public ZoneBasedSpawnPolicy(IRandomSource random, HeartbeatZone zone = null, double weight = 1.0)
        {
            _random = random;
            _zone = zone ?? new HeartbeatZone();
            _weight = weight;
        }

        public bool ShouldSpawn(int heartbeatValue)
        {
            var chance = _zone.ComplexSpawnChanceOf(heartbeatValue);
            if (chance <= 0) return false;
            return _random.NextDouble() < chance * _weight;
        }
    }

    /// <summary>발현이 결정된 턴에 어떤 컴플렉스를 붙일지 정한다. 기본 구현은 무작위(<see cref="ComplexSpawner"/>), 튜토리얼은 정해진 순서(<see cref="ScriptedComplexSchedule"/>).</summary>
    public interface IComplexSpawner
    {
        bool TrySpawn(ComplexBoard board, out ComplexInstance spawned);
    }

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
