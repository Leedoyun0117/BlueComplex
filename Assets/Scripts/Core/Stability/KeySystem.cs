using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;

namespace BlueComplex.Core.Stability
{
    /// <summary>심박수 축 위에 나타나는 키 구역. 폭(Width)만큼의 심박수 구간을 차지한다.</summary>
    public readonly struct KeyZone
    {
        public int StartSlot { get; }
        public int Width { get; }

        public KeyZone(int startSlot, int width)
        {
            StartSlot = startSlot;
            Width = width;
        }

        public bool Contains(int position) => position >= StartSlot && position < StartSlot + Width;
    }

    /// <summary>
    /// 키 구역의 물리적 규격. 배치 정책은 이 규격 안에서 offset만 고른다.
    /// Min/Max는 키가 놓일 수 있는 생존 가능 범위(양 끝을 포함)로, 항상 <see cref="HeartbeatZone"/> 에서
    /// 가져와야 한다 — Fatal 구간과 겹치는 키 구역이 생기면 도달 자체가 불가능해지기 때문이다.
    /// </summary>
    public sealed class KeyZoneLayout
    {
        /// <summary>10칸 기준 양끝 3칸을 200 스케일로 환산한 값(3 × 20).</summary>
        public const int DefaultEdgeWidth = 60;

        /// <summary>생존 구간(폭 181) 대비 원래 비율(10칸 중 2칸)을 유지한 값(181 × 0.2 ≈ 36).</summary>
        public const int DefaultKeyWidth = 36;

        /// <summary>키가 놓일 수 있는 생존 구간의 최솟값(포함).</summary>
        public int Min { get; }

        /// <summary>키가 놓일 수 있는 생존 구간의 최댓값(포함).</summary>
        public int Max { get; }

        public int EdgeWidth { get; }
        public int KeyWidth { get; }

        /// <summary>생존 구간 전체 칸 수.</summary>
        public int Slots => Max - Min + 1;

        /// <summary>한쪽 끝 구역 안에서 키 시작 칸의 후보 개수.</summary>
        public int OffsetCount => EdgeWidth - KeyWidth + 1;

        public KeyZoneLayout(int min, int max, int edgeWidth = DefaultEdgeWidth, int keyWidth = DefaultKeyWidth)
        {
            if (max < min)
                throw new ArgumentException($"max({max})는 min({min}) 이상이어야 합니다.", nameof(max));
            if (keyWidth > edgeWidth)
                throw new ArgumentException($"keyWidth({keyWidth})는 edgeWidth({edgeWidth}) 이하여야 합니다.", nameof(keyWidth));
            if (edgeWidth > max - min + 1)
                throw new ArgumentException($"edgeWidth({edgeWidth})가 생존 구간 폭({max - min + 1})보다 큽니다.", nameof(edgeWidth));

            Min = min;
            Max = max;
            EdgeWidth = edgeWidth;
            KeyWidth = keyWidth;
        }

        /// <summary>생존 구간을 아는 <see cref="HeartbeatZone"/> 으로부터 레이아웃을 만든다.</summary>
        public KeyZoneLayout(HeartbeatZone zone, int edgeWidth = DefaultEdgeWidth, int keyWidth = DefaultKeyWidth)
            : this(zone.SurvivableMin, zone.SurvivableMax, edgeWidth, keyWidth)
        {
        }

        public KeyZone LeftZone(int offset) => new(Min + offset, KeyWidth);

        public KeyZone RightZone(int offset) => new(Max - EdgeWidth + 1 + offset, KeyWidth);
    }

    /// <summary>
    /// 키 구역을 어디에 열지 결정하는 정책.
    /// startPosition은 인디케이터의 스테이지 시작 위치, turnsUntilKey는 그 키 턴 전까지 확보되는 이동 턴 수다.
    /// (테스트/비교용으로 남겨둔 <see cref="RandomKeyZonePlacer"/>는 이 값을 쓰지 않는다.)
    /// </summary>
    public interface IKeyZonePlacer
    {
        KeyZone Place(int startPosition, int turnsUntilKey);
    }

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

    /// <summary>
    /// 심박수가 남은 턴 안에 실제로 도달할 수 있는 범위 안에서만 키 구역을 고른다.
    /// 도달 범위와 겹치는 후보가 여럿이면 무작위로, 하나도 없으면 가장 가까운 후보를 고른다.
    /// 한 턴 최대 이동폭(maxMovePerTurn)은 감정 조합 최대 개수(3) × 태그 하나의 영향력(EmotionEvaluator.DefaultTagMagnitude)에서
    /// 온 설계 상수이므로 하드코딩하지 않고 생성자 주입으로 받는다.
    /// </summary>
    public sealed class ReachabilityKeyZonePlacer : IKeyZonePlacer
    {
        /// <summary>감정 조합 최대 개수(3) × 태그 하나의 영향력 = 한 턴 최대 이동폭.</summary>
        public const int DefaultMaxMovePerTurn = 3 * EmotionEvaluator.DefaultTagMagnitude;

        private readonly IRandomSource _random;
        private readonly KeyZoneLayout _layout;
        private readonly int _maxMovePerTurn;

        public int MaxMovePerTurn => _maxMovePerTurn;

        public ReachabilityKeyZonePlacer(IRandomSource random, KeyZoneLayout layout = null, int maxMovePerTurn = DefaultMaxMovePerTurn)
        {
            _random = random;
            _layout = layout ?? new KeyZoneLayout(new HeartbeatZone());
            _maxMovePerTurn = maxMovePerTurn;
        }

        public KeyZone Place(int startPosition, int turnsUntilKey)
        {
            var reach = Math.Max(0, turnsUntilKey) * _maxMovePerTurn;
            var reachLow = startPosition - reach;
            var reachHigh = startPosition + reach;

            var candidates = AllCandidates().ToList();
            var overlapping = candidates.Where(z => Overlaps(z, reachLow, reachHigh)).ToList();

            if (overlapping.Count > 0)
                return overlapping[_random.Range(0, overlapping.Count)];

            // 겹치는 후보가 하나도 없으면 가장 가까운 후보를 고른다.
            return candidates.OrderBy(z => DistanceToRange(z, reachLow, reachHigh)).First();
        }

        private IEnumerable<KeyZone> AllCandidates()
        {
            for (var offset = 0; offset < _layout.OffsetCount; offset++)
            {
                yield return _layout.LeftZone(offset);
                yield return _layout.RightZone(offset);
            }
        }

        private static bool Overlaps(KeyZone zone, int low, int high)
        {
            var lastSlot = zone.StartSlot + zone.Width - 1;
            return zone.StartSlot <= high && lastSlot >= low;
        }

        private static int DistanceToRange(KeyZone zone, int low, int high)
        {
            var lastSlot = zone.StartSlot + zone.Width - 1;
            if (lastSlot < low) return low - lastSlot;
            if (zone.StartSlot > high) return zone.StartSlot - high;
            return 0;
        }
    }

    /// <summary>키 획득 진행 상황. 목표 개수를 채우면 스테이지 클리어.</summary>
    public sealed class KeyProgress
    {
        private readonly HashSet<int> _keyTurns;
        private readonly Dictionary<int, KeyZone> _zonesByTurn = new();

        public int Required { get; }
        public int Collected { get; private set; }
        public KeyZone? ActiveZone { get; private set; }

        /// <summary>구역이 열리는 턴 목록. 스테이지 시작 시 이 턴 전부의 구역을 한꺼번에 확정한다.</summary>
        public IReadOnlyCollection<int> KeyTurns => _keyTurns;

        /// <summary>턴별로 확정된 키 구역. UI가 턴 1부터 전체를 미리 보여주는 데 쓴다.</summary>
        public IReadOnlyDictionary<int, KeyZone> Zones => _zonesByTurn;

        public bool IsComplete => Collected >= Required;

        public event Action<KeyZone> ZoneOpened;
        public event Action<int> KeyCollected;
        public event Action ZoneMissed;

        public KeyProgress(int required, IEnumerable<int> keyTurns)
        {
            Required = required;
            _keyTurns = keyTurns.ToHashSet();
        }

        public bool IsKeyTurn(int turn) => _keyTurns.Contains(turn);

        /// <summary>스테이지 시작 시 모든 키 턴의 구역을 한꺼번에 확정한다. 이후에는 바뀌지 않는다.</summary>
        public void PrepareZones(IReadOnlyDictionary<int, KeyZone> zones)
        {
            foreach (var pair in zones) _zonesByTurn[pair.Key] = pair.Value;
        }

        /// <summary>미리 확정된 구역을 이번 턴의 활성 구역으로 연다.</summary>
        public void OpenZone(int turn)
        {
            var zone = _zonesByTurn[turn];
            ActiveZone = zone;
            ZoneOpened?.Invoke(zone);
        }

        /// <summary>턴 종료 시점의 인디케이터 위치로 획득 여부를 판정한다.</summary>
        public void Judge(int indicatorPosition)
        {
            if (ActiveZone == null) return;

            if (ActiveZone.Value.Contains(indicatorPosition))
            {
                Collected++;
                KeyCollected?.Invoke(Collected);
            }
            else
            {
                ZoneMissed?.Invoke();
            }

            ActiveZone = null;
        }
    }
}
