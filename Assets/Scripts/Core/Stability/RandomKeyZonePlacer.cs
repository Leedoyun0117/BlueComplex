using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>기본(구) 정책. 좌우와 구역 내 offset을 모두 무작위로 고른다 — 도달 가능 여부는 고려하지 않는다.</summary>
    public sealed class RandomKeyZonePlacer : IKeyZonePlacer
    {
        private readonly IRandomSource _random;
        private readonly KeyZoneLayout _layout;

        public RandomKeyZonePlacer(IRandomSource random, KeyZoneLayout layout = null)
        {
            _random = random;
            _layout = layout ?? new KeyZoneLayout(new HeartbeatZone());
        }

        public KeyZone Place(int startPosition, int turnsUntilKey)
        {
            var useLeft = _random.Range(0, 2) == 0;
            var offset = _random.Range(0, _layout.OffsetCount);
            return useLeft ? _layout.LeftZone(offset) : _layout.RightZone(offset);
        }
    }
}
