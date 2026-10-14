using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 좌/우는 도달 가능 여부와 무관하게 균등 확률(동전 던지기)로 먼저 정하고, 그 쪽 구간 안에서 위치를 고른다.
    /// 위치는 도달 범위(<see cref="IReachModel"/>)와 충분히 겹치는 후보가 있으면 그 중에서, 그 쪽에 도달 가능한 후보가
    /// 하나도 없으면 구간 전체에서 고르게 뽑는다(가장 가까운 후보 하나로 고정하지 않는다 — 그러면 도달 불가한 쪽은 위치가 항상 같아진다).
    /// "충분히"는 구역 폭의 minOverlapFraction 이상이 범위 안에 들어오는 것이다(0이면 한 칸만 겹쳐도 도달 가능).
    /// 그래서 카드 풀이 상승 여력을 얻으면 별도 수정 없이 우측 구역도 도달 가능한 위치로 좁혀 뽑힌다.
    ///
    /// 좌우 분산 보장(양쪽에 최소 ⌊n/2⌋개)은 두지 않는다 — 구역마다 독립인 동전이 곧 이 정책의 규칙이고,
    /// 보장을 얹으면 구역끼리 독립이 아니게 되어 "각 쿼터가 좌/우 랜덤"이라는 규칙과 어긋난다. 한쪽에 몰릴 확률은 (1/2)^(n-1)이다.
    /// 같은 쪽에 구역이 둘 이상이면 그 쪽 구간 안에서 서로 다른 위치로 벌려 놓는다(<see cref="PlaceAll"/>).
    /// 한 턴 최대 이동폭은 단서 하나에 붙는 감정이 최대 2개(저작 규칙)라는 사실에서 온 값이라 하드코딩하지 않고
    /// 생성자로 주입받는다.
    /// </summary>
    public sealed class ReachabilityKeyZonePlacer : IKeyZonePlacer
    {
        /// <summary>한 턴 최대 상승폭 = 단서 하나의 감정 최대 2개(저작 규칙) × 태그 하나의 영향력. 카드 한 장의 이동량을 자르는 한도다.</summary>
        public const int DefaultMaxMovePerTurn = 2 * EmotionEvaluator.DefaultTagMagnitude;

        /// <summary>한 턴 최대 하강폭 = 3 × 태그 영향력. 카드 한 장의 하강 여력을 자르는 한도이며, 컴플렉스가 감정을 더하는 경우를 위해 원본 최대(2개)보다 1개 여유를 둔 값이다.</summary>
        public const int DefaultMaxDropPerTurn = 3 * EmotionEvaluator.DefaultTagMagnitude;

        private readonly IRandomSource _random;
        private readonly KeyZoneLayout _layout;
        private readonly IReachModel _reach;
        private readonly double _minOverlapFraction;

        /// <summary>한 턴 최대 상승폭.</summary>
        public int MaxMovePerTurn { get; }

        public ReachabilityKeyZonePlacer(IRandomSource random,
                                         KeyZoneLayout layout = null,
                                         int maxMovePerTurn = DefaultMaxMovePerTurn,
                                         IReachModel reach = null,
                                         double minOverlapFraction = 0.0)
        {
            _random = random;
            _layout = layout ?? new KeyZoneLayout(new HeartbeatZone());
            MaxMovePerTurn = maxMovePerTurn;
            _reach = reach ?? new PerTurnReachModel(maxMovePerTurn, DefaultMaxDropPerTurn);
            _minOverlapFraction = minOverlapFraction;
        }

        public KeyZone Place(int startPosition, int turnsUntilKey)
        {
            var useLeft = _random.Range(0, 2) == 0;
            var pool = BuildPool(startPosition, turnsUntilKey, useLeft);
            return pool[_random.Range(0, pool.Count)].Zone;
        }

        /// <summary>
        /// 모든 판정 턴의 구역을 배치한다. 턴 번호 순으로 좌/우를 독립적으로 정한 뒤(같은 시드는 키 턴 입력 순서와 무관하게 같은 배치),
        /// 같은 쪽에 둘 이상이면 그 쪽 구간 안에서 위치가 고르게 퍼지도록 배치한다
        /// (구역 폭이 구간 폭에 비해 커서 완전히 안 겹치게는 못 하지만 겹침을 최소화한다).
        /// </summary>
        public IReadOnlyDictionary<int, KeyZone> PlaceAll(int startPosition, IEnumerable<int> keyTurns)
        {
            var turns = keyTurns.Distinct().OrderBy(t => t).ToList();
            var useLeft = turns.Select(_ => _random.Range(0, 2) == 0).ToArray();
            var pools = turns.Select((t, i) => BuildPool(startPosition, t, useLeft[i])).ToList();
            return PickZones(turns, pools, useLeft);
        }

        private readonly struct Candidate
        {
            public int Offset { get; }
            public KeyZone Zone { get; }

            public Candidate(int offset, KeyZone zone)
            {
                Offset = offset;
                Zone = zone;
            }
        }

        /// <summary>한 판정 턴에서 정해진 쪽의 후보. 도달 가능한 후보가 있으면 그것만, 하나도 없으면 그 쪽 전체.</summary>
        private List<Candidate> BuildPool(int startPosition, int moves, bool left)
        {
            var (low, high) = _reach.Range(startPosition, Math.Max(0, moves));
            var required = Math.Max(1, (int)Math.Ceiling(_layout.KeyWidth * _minOverlapFraction));

            var all = new List<Candidate>();
            for (var offset = 0; offset < _layout.OffsetCount; offset++)
                all.Add(new Candidate(offset, left ? _layout.LeftZone(offset) : _layout.RightZone(offset)));

            var reachable = all.Where(c => OverlapSlots(c.Zone, low, high) >= required).ToList();
            return reachable.Count > 0 ? reachable : all;
        }

        private Dictionary<int, KeyZone> PickZones(List<int> turns, List<List<Candidate>> pools, bool[] useLeft)
        {
            var zones = new Dictionary<int, KeyZone>();

            foreach (var side in new[] { true, false })
            {
                var group = Enumerable.Range(0, turns.Count).Where(i => useLeft[i] == side).ToList();
                if (group.Count == 0) continue;

                var anchors = SpreadAnchors(group.Count);
                var jitter = group.Count > 1 ? Math.Max(0, (_layout.OffsetCount - 1) / (group.Count - 1) / 4) : 0;

                for (var g = 0; g < group.Count; g++)
                {
                    var pool = pools[group[g]];
                    zones[turns[group[g]]] = group.Count == 1
                        ? pool[_random.Range(0, pool.Count)].Zone
                        : PickNear(pool, anchors[g], jitter).Zone;
                }
            }

            return zones;
        }

        /// <summary>같은 쪽 구역 count개의 목표 offset. 구간 전체에 고르게 펼치고(0, 끝, 그 사이 등분) 어느 턴이 어느 위치를 맡을지는 섞는다.</summary>
        private List<int> SpreadAnchors(int count)
        {
            var anchors = new List<int>();
            for (var i = 0; i < count; i++)
                anchors.Add(count == 1 ? 0 : (int)Math.Round(i * (_layout.OffsetCount - 1) / (double)(count - 1)));

            for (var i = anchors.Count - 1; i > 0; i--)
            {
                var j = _random.Range(0, i + 1);
                (anchors[i], anchors[j]) = (anchors[j], anchors[i]);
            }

            return anchors;
        }

        private Candidate PickNear(List<Candidate> pool, int anchor, int jitter)
        {
            var near = pool.Where(c => Math.Abs(c.Offset - anchor) <= jitter).ToList();
            if (near.Count == 0)
            {
                var best = pool.Min(c => Math.Abs(c.Offset - anchor));
                near = pool.Where(c => Math.Abs(c.Offset - anchor) == best).ToList();
            }

            return near[_random.Range(0, near.Count)];
        }

        /// <summary>구역이 도달 범위 [low, high] 안에 걸치는 칸 수.</summary>
        private static int OverlapSlots(KeyZone zone, int low, int high)
        {
            var lastSlot = zone.StartSlot + zone.Width - 1;
            return Math.Max(0, Math.Min(lastSlot, high) - Math.Max(zone.StartSlot, low) + 1);
        }
    }
}
