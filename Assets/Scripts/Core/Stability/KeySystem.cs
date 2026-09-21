using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

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
    /// startPosition은 인디케이터의 스테이지 시작 위치, turnsUntilKey는 그 구역이 판정되기까지 확보되는 이동 횟수다.
    /// (테스트/비교용으로 남겨둔 <see cref="RandomKeyZonePlacer"/>는 이 값을 쓰지 않는다.)
    /// </summary>
    public interface IKeyZonePlacer
    {
        KeyZone Place(int startPosition, int turnsUntilKey);

        /// <summary>
        /// 한 스테이지의 모든 판정 턴 구역을 한꺼번에 배치한다. 반환 키는 판정 턴 번호.
        /// 판정은 그 턴의 이동까지 끝난 뒤에 이뤄지므로 이동 횟수는 판정 턴 번호 그대로다.
        /// 기본 구현은 턴마다 <see cref="Place"/> 를 독립 호출한다 — 구역 사이의 관계(분산 등)를 다루려는 정책만 재정의한다.
        /// </summary>
        IReadOnlyDictionary<int, KeyZone> PlaceAll(int startPosition, IEnumerable<int> keyTurns)
        {
            var zones = new Dictionary<int, KeyZone>();
            foreach (var turn in keyTurns)
                zones[turn] = Place(startPosition, turn);
            return zones;
        }
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

    /// <summary>시작 위치에서 moves번 움직였을 때 심박수가 닿을 수 있는 범위(양 끝 포함).</summary>
    public interface IReachModel
    {
        (int Low, int High) Range(int startPosition, int moves);
    }

    /// <summary>한 번 움직일 때마다 최대 rise만큼 오르고 최대 drop만큼 내린다고 보는 단순 모델. 매 턴 최선의 카드가 있다고 가정하므로 후한 편이다.</summary>
    public sealed class PerTurnReachModel : IReachModel
    {
        private readonly int _maxRisePerMove;
        private readonly int _maxDropPerMove;

        public PerTurnReachModel(int maxRisePerMove, int maxDropPerMove)
        {
            _maxRisePerMove = maxRisePerMove;
            _maxDropPerMove = maxDropPerMove;
        }

        public (int Low, int High) Range(int startPosition, int moves)
        {
            var count = Math.Max(0, moves);
            return (startPosition - count * _maxDropPerMove, startPosition + count * _maxRisePerMove);
        }
    }

    /// <summary>
    /// 단서는 한 번 쓰면 사라진다 — 그래서 moves번 움직여 닿는 최대 상승은 "가진 카드 중 이동량이 큰 moves장의 합"이고
    /// 최대 하강은 작은 moves장의 합이다(카드가 모자라면 남은 턴은 넘어가 0). 매 턴 최선의 카드가 나온다고 보는
    /// <see cref="PerTurnReachModel"/> 과 달리 카드 풀의 실제 구성이 상한을 정한다.
    /// 카드마다 두 값을 받는다: 상승 쪽은 컴플렉스가 없을 때의 이동량(Best), 하강 쪽은 컴플렉스가 최악으로 겹칠 때의
    /// 이동량(Worst). 컴플렉스는 대체로 방해 요소라 상승 여력에서는 뺐고, 하강은 그대로 열어 둔다 —
    /// 안 그러면 카드를 다 쓴 뒤의 위치가 한 점으로 고정돼 실제로는 닿는 아래쪽 구역이 도달 불가로 분류된다.
    /// 한 장의 이동량은 한 턴 최대 이동폭으로 자른다.
    /// </summary>
    public sealed class CardPoolReachModel : IReachModel
    {
        private readonly int[] _bestDescending;
        private readonly int[] _worstAscending;

        public CardPoolReachModel(IEnumerable<(int Best, int Worst)> cards, int maxRisePerMove, int maxDropPerMove)
        {
            var list = cards.ToList();
            _bestDescending = list.Select(c => Math.Clamp(c.Best, -maxDropPerMove, maxRisePerMove))
                .OrderByDescending(d => d).ToArray();
            _worstAscending = list.Select(c => Math.Clamp(c.Worst, -maxDropPerMove, maxRisePerMove))
                .OrderBy(d => d).ToArray();
        }

        public (int Low, int High) Range(int startPosition, int moves)
        {
            var count = Math.Min(Math.Max(0, moves), _bestDescending.Length);
            return (startPosition + _worstAscending.Take(count).Sum(), startPosition + _bestDescending.Take(count).Sum());
        }
    }

    /// <summary>
    /// 심박수가 실제로 도달할 수 있는 범위 안에서만 키 구역을 고른다.
    /// 도달 범위(<see cref="IReachModel"/>)와 충분히 겹치는 후보가 여럿이면 무작위로, 하나도 없으면 가장 가까운 후보를 고른다.
    /// "충분히"는 구역 폭의 minOverlapFraction 이상이 범위 안에 들어오는 것이다 — 범위 끝자락만 스치는 구역은 최선의 경우에도
    /// 한 칸 닿을까 말까라 사실상 도달 불가다(0이면 한 칸만 겹쳐도 도달 가능).
    ///
    /// 여러 구역을 배치할 때(<see cref="PlaceAll"/>)는 좌우로 분산한다. 분산은 두 쪽이 모두 도달 가능한 턴에만 적용하고,
    /// 한쪽만 도달 가능한 턴은 그쪽에 둔다. 같은 쪽에 여러 구역이 몰리면 그 안에서 서로 다른 위치에 벌려 놓는다.
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
            var options = BuildOptions(startPosition, turnsUntilKey);

            var reachable = new List<Candidate>();
            if (options.LeftReachable) reachable.AddRange(options.Left);
            if (options.RightReachable) reachable.AddRange(options.Right);
            if (reachable.Count > 0)
                return reachable[_random.Range(0, reachable.Count)].Zone;

            // 도달 가능한 후보가 하나도 없으면 도달 범위에 가장 가까운 후보를 고른다.
            return options.LeftGap <= options.RightGap ? options.Left[0].Zone : options.Right[0].Zone;
        }

        /// <summary>
        /// 모든 판정 턴의 구역을 배치한다.
        /// 한쪽만 도달 가능한 턴은 그쪽으로 정해지고, 두 쪽 다 도달 가능한 턴만 분산 대상이 된다 — 좌/우 각각 최소 ⌊n/2⌋개가
        /// 되도록 그런 턴들 중에서 배정하고 나머지는 무작위다(3개면 1개, 4개면 2개). 도달 가능한 쪽이 한쪽뿐이라 몰리면 어쩔 수 없다.
        /// 같은 쪽에 구역이 둘 이상이면 그 쪽 구간 안에서 위치가 고르게 퍼지도록 배치한다
        /// (구역 폭이 구간 폭에 비해 커서 완전히 안 겹치게는 못 하지만 겹침을 최소화한다).
        /// </summary>
        public IReadOnlyDictionary<int, KeyZone> PlaceAll(int startPosition, IEnumerable<int> keyTurns)
        {
            var turns = keyTurns.Distinct().OrderBy(t => t).ToList();
            var options = turns.Select(t => BuildOptions(startPosition, t)).ToList();
            var useLeft = AssignSides(options);
            return PickZones(turns, options, useLeft);
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

        /// <summary>한 판정 턴의 좌/우 후보. 도달 가능한 후보가 없는 쪽은 도달 범위에 가장 가까운 후보 하나만 남기고 그 거리를 Gap에 담는다.</summary>
        private sealed class SideOptions
        {
            public List<Candidate> Left;
            public List<Candidate> Right;
            public bool LeftReachable;
            public bool RightReachable;
            public int LeftGap;
            public int RightGap;
        }

        private SideOptions BuildOptions(int startPosition, int moves)
        {
            var (low, high) = _reach.Range(startPosition, Math.Max(0, moves));
            var required = Math.Max(1, (int)Math.Ceiling(_layout.KeyWidth * _minOverlapFraction));

            var left = new List<Candidate>();
            var right = new List<Candidate>();
            for (var offset = 0; offset < _layout.OffsetCount; offset++)
            {
                left.Add(new Candidate(offset, _layout.LeftZone(offset)));
                right.Add(new Candidate(offset, _layout.RightZone(offset)));
            }

            var options = new SideOptions();
            (options.Left, options.LeftReachable, options.LeftGap) = Filter(left, low, high, required);
            (options.Right, options.RightReachable, options.RightGap) = Filter(right, low, high, required);
            return options;
        }

        private static (List<Candidate> list, bool reachable, int gap) Filter(List<Candidate> all, int low, int high, int required)
        {
            var reachable = all.Where(c => OverlapSlots(c.Zone, low, high) >= required).ToList();
            if (reachable.Count > 0) return (reachable, true, 0);

            var nearest = all.OrderBy(c => DistanceToRange(c.Zone, low, high)).First();
            return (new List<Candidate> { nearest }, false, DistanceToRange(nearest.Zone, low, high));
        }

        private bool[] AssignSides(List<SideOptions> options)
        {
            var count = options.Count;
            var quota = count / 2;

            // 도달 가능한 쪽이 하나뿐인 턴은 그쪽에 고정. 둘 다 도달 불가면 범위에 더 가까운 쪽(동률이면 좌측).
            var fixedSide = new bool?[count];
            var free = new List<int>();
            for (var i = 0; i < count; i++)
            {
                var o = options[i];
                if (o.LeftReachable && o.RightReachable) free.Add(i);
                else if (o.LeftReachable) fixedSide[i] = true;
                else if (o.RightReachable) fixedSide[i] = false;
                else fixedSide[i] = o.LeftGap <= o.RightGap;
            }

            var leftFixed = fixedSide.Count(s => s == true);
            var rightFixed = fixedSide.Count(s => s == false);
            var needLeft = Math.Max(0, quota - leftFixed);
            var needRight = Math.Max(0, quota - rightFixed);

            for (var i = free.Count - 1; i > 0; i--)
            {
                var j = _random.Range(0, i + 1);
                (free[i], free[j]) = (free[j], free[i]);
            }

            for (var k = 0; k < free.Count; k++)
            {
                if (k < needLeft) fixedSide[free[k]] = true;
                else if (k < needLeft + needRight) fixedSide[free[k]] = false;
                else fixedSide[free[k]] = _random.Range(0, 2) == 0;
            }

            return fixedSide.Select(s => s.Value).ToArray();
        }

        private Dictionary<int, KeyZone> PickZones(List<int> turns, List<SideOptions> options, bool[] useLeft)
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
                    var pool = side ? options[group[g]].Left : options[group[g]].Right;
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

        private static int DistanceToRange(KeyZone zone, int low, int high)
        {
            var lastSlot = zone.StartSlot + zone.Width - 1;
            if (lastSlot < low) return low - lastSlot;
            if (zone.StartSlot > high) return zone.StartSlot - high;
            return 0;
        }
    }

    /// <summary>쿼터 하나의 키 판정 결과. 쿼터 마지막 턴이 끝난 시점의 심박수로 정해진다.</summary>
    public readonly struct KeyJudgement
    {
        /// <summary>쿼터 번호(1부터).</summary>
        public int Quarter { get; }

        /// <summary>판정이 일어난 턴(그 쿼터의 마지막 턴).</summary>
        public int Turn { get; }

        public KeyZone Zone { get; }

        /// <summary>판정에 쓰인 심박수.</summary>
        public int Position { get; }

        public bool Success { get; }

        public KeyJudgement(int quarter, int turn, KeyZone zone, int position, bool success)
        {
            Quarter = quarter;
            Turn = turn;
            Zone = zone;
            Position = position;
            Success = success;
        }
    }

    /// <summary>
    /// 키 획득 진행 상황. 쿼터마다 목표 구역이 하나 있고, 그 쿼터의 마지막 턴이 끝난 시점에만 판정한다
    /// (쿼터 중간에 구역을 스쳐 지나가는 것은 의미 없다). 목표 개수를 채우면 스테이지 클리어.
    /// 구역은 판정 턴(쿼터 마지막 턴)을 키로 보관한다 — UI 탭 라벨이 그 턴 번호("4턴·8턴·12턴")를 그대로 읽는다.
    /// </summary>
    public sealed class KeyProgress
    {
        private readonly Dictionary<int, KeyZone> _zonesByTurn = new();
        private readonly bool?[] _results;
        private int _activeQuarter;

        public int Required { get; }
        public int Collected { get; private set; }
        public QuarterSchedule Schedule { get; }

        /// <summary>진행 중인 쿼터의 목표 구역. 쿼터가 시작될 때 열리고, 판정 후 닫힌다.</summary>
        public KeyZone? ActiveZone { get; private set; }

        /// <summary>판정이 일어나는 턴 목록(각 쿼터의 마지막 턴). 스테이지 시작 시 이 턴 전부의 구역을 한꺼번에 확정한다.</summary>
        public IReadOnlyCollection<int> KeyTurns { get; }

        /// <summary>판정 턴별로 확정된 키 구역. UI가 스테이지 시작부터 전체 쿼터의 목표를 보여주는 데 쓴다.</summary>
        public IReadOnlyDictionary<int, KeyZone> Zones => _zonesByTurn;

        /// <summary>쿼터별 판정 결과(인덱스 0 = 1쿼터). null=아직 판정 전, true=키 획득, false=실패.</summary>
        public IReadOnlyList<bool?> Results => _results;

        public bool IsComplete => Collected >= Required;

        /// <summary>쿼터가 시작되어 그 쿼터의 목표 구역이 열릴 때.</summary>
        public event Action<KeyZone> ZoneOpened;
        public event Action<int> KeyCollected;
        public event Action ZoneMissed;

        public KeyProgress(int required, QuarterSchedule schedule)
        {
            Required = required;
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            KeyTurns = schedule.QuarterEndTurns().ToList();
            _results = new bool?[schedule.QuarterCount];
        }

        /// <summary>이 턴이 끝나면 키를 판정하는가(쿼터의 마지막 턴).</summary>
        public bool IsKeyTurn(int turn) => Schedule.IsQuarterEnd(turn);

        /// <summary>스테이지 시작 시 모든 쿼터의 구역을 한꺼번에 확정한다(판정 턴이 키). 이후에는 바뀌지 않는다.</summary>
        public void PrepareZones(IReadOnlyDictionary<int, KeyZone> zones)
        {
            foreach (var pair in zones) _zonesByTurn[pair.Key] = pair.Value;
        }

        /// <summary>미리 확정된 구역을 그 쿼터의 활성 구역으로 연다.</summary>
        public void OpenQuarter(int quarter)
        {
            var zone = _zonesByTurn[Schedule.LastTurnOf(quarter)];
            _activeQuarter = quarter;
            ActiveZone = zone;
            ZoneOpened?.Invoke(zone);
        }

        /// <summary>쿼터 마지막 턴 종료 시점의 인디케이터 위치로 획득 여부를 판정한다. 열린 구역이 없으면 null.</summary>
        public KeyJudgement? Judge(int indicatorPosition)
        {
            if (ActiveZone == null) return null;

            var zone = ActiveZone.Value;
            var success = zone.Contains(indicatorPosition);
            var judgement = new KeyJudgement(_activeQuarter, Schedule.LastTurnOf(_activeQuarter), zone, indicatorPosition, success);

            _results[_activeQuarter - 1] = success;
            ActiveZone = null;

            if (success)
            {
                Collected++;
                KeyCollected?.Invoke(Collected);
            }
            else
            {
                ZoneMissed?.Invoke();
            }

            return judgement;
        }
    }
}
