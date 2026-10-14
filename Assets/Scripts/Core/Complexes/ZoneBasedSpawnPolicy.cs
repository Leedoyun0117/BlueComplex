using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Complexes
{
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
}
