using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Turn
{
    public enum StageOutcome
    {
        InProgress,
        Cleared,
        Failed
    }

    /// <summary>한 턴이 만들어낸 모든 결과. UI 연출은 이걸 그대로 재생하면 된다.</summary>
    public sealed class TurnReport
    {
        public int Turn { get; }
        public int Quarter { get; }
        public int TurnInQuarter { get; }

        /// <summary>낸 단서. 손패가 비어 넘어간 턴(<see cref="IsPass"/>)이면 null.</summary>
        public ClueDefinition Clue { get; }

        /// <summary>넘어간 턴이면 null.</summary>
        public InterpretationResult Interpretation { get; }

        /// <summary>넘어간 턴이면 null.</summary>
        public TagSet FinalTags { get; }

        public int HeartbeatDelta { get; }
        public int HeartbeatValue { get; }
        public ComplexInstance SpawnedComplex { get; }

        /// <summary>컴플렉스가 나타날 차례였지만 이미 최대 중첩이라 붙지 못했다 — 특수 특성(고기능 우울증/과흥분) 발현 조건.
        /// 침체 구간이었다면 고기능 우울증, 흥분 구간이었다면 과흥분 쪽이다(<see cref="HeartbeatValue"/>로 구분).</summary>
        public bool ComplexOverflowed { get; }

        /// <summary>이 턴에 컴플렉스 초과로 새로 붙은 특수 특성(없으면 null). 초과했어도 그 구간 쪽 특수 특성이 카탈로그에 없으면 null.</summary>
        public TraitDefinition SpecialTraitGranted { get; }
        public StageOutcome Outcome { get; }

        /// <summary>이 턴이 쿼터의 마지막 턴이라 키를 판정했다면 그 결과. 아니면 null.</summary>
        public KeyJudgement? KeyResult { get; }

        /// <summary>손패가 비어 단서 없이 시간만 흐른 턴인가.</summary>
        public bool IsPass => Clue == null;

        public TurnReport(int turn,
                          int quarter,
                          int turnInQuarter,
                          ClueDefinition clue,
                          InterpretationResult interpretation,
                          TagSet finalTags,
                          int heartbeatDelta,
                          int heartbeatValue,
                          ComplexInstance spawnedComplex,
                          StageOutcome outcome,
                          KeyJudgement? keyResult,
                          bool complexOverflowed = false,
                          TraitDefinition specialTraitGranted = null)
        {
            Turn = turn;
            Quarter = quarter;
            TurnInQuarter = turnInQuarter;
            Clue = clue;
            Interpretation = interpretation;
            FinalTags = finalTags;
            HeartbeatDelta = heartbeatDelta;
            HeartbeatValue = heartbeatValue;
            SpawnedComplex = spawnedComplex;
            ComplexOverflowed = complexOverflowed;
            SpecialTraitGranted = specialTraitGranted;
            Outcome = outcome;
            KeyResult = keyResult;
        }
    }

    /// <summary>
    /// 게임 루프의 순서만 책임진다.
    /// 판정·변환·확률·자원 관리는 전부 주입받은 시스템이 수행한다.
    /// 쿼터 경계는 KeyProgress.Schedule에서 읽는다 — 쿼터가 시작될 때 손패를 채우고 그 쿼터의 목표 구역을 열며,
    /// 쿼터의 마지막 턴이 끝날 때 키를 판정한다.
    /// </summary>
    public sealed class TurnRunner
    {
        private readonly ClueHand _hand;
        private readonly ComplexBoard _complexBoard;
        private readonly ComplexResolver _resolver;
        private readonly ComplexSpawner _spawner;
        private readonly IComplexSpawnPolicy _spawnPolicy;
        private readonly Heartbeat _heartbeat;
        private readonly HeartbeatZone _zone;
        private readonly IEmotionEvaluator _evaluator;
        private readonly ItemInventory _items;
        private readonly ActiveItemBoard _activeItems;
        private readonly TraitBoard _traits;
        private readonly KeyProgress _keys;
        private readonly IKeyZonePlacer _keyPlacer;
        private readonly ClueKnowledgeLedger _ledger;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _itemParameters;

        public int CurrentTurn { get; private set; }
        public QuarterSchedule Schedule => _keys.Schedule;
        public int TotalTurns => Schedule.TotalTurns;
        public StageOutcome Outcome { get; private set; } = StageOutcome.InProgress;

        /// <summary>지금 진행 중인 쿼터(1부터). 스테이지 시작 전에는 0.</summary>
        public int CurrentQuarter => CurrentTurn < 1 ? 0 : Schedule.QuarterOf(CurrentTurn);

        /// <summary>지금 턴이 쿼터 안에서 몇 번째인지(1부터). 스테이지 시작 전에는 0.</summary>
        public int CurrentTurnInQuarter => CurrentTurn < 1 ? 0 : Schedule.TurnInQuarter(CurrentTurn);

        public event Action<int> TurnBegan;
        public event Action<TurnReport> TurnResolved;
        public event Action<StageOutcome> StageEnded;

        public TurnRunner(ClueHand hand,
                          ComplexBoard complexBoard,
                          ComplexResolver resolver,
                          ComplexSpawner spawner,
                          IComplexSpawnPolicy spawnPolicy,
                          Heartbeat heartbeat,
                          HeartbeatZone zone,
                          IEmotionEvaluator evaluator,
                          ItemInventory items,
                          ActiveItemBoard activeItems,
                          TraitBoard traits,
                          KeyProgress keys,
                          IKeyZonePlacer keyPlacer,
                          ClueKnowledgeLedger ledger,
                          IReadOnlyDictionary<string, IReadOnlyList<string>> itemParameters = null)
        {
            _hand = hand;
            _complexBoard = complexBoard;
            _resolver = resolver;
            _spawner = spawner;
            _spawnPolicy = spawnPolicy;
            _heartbeat = heartbeat;
            _zone = zone;
            _evaluator = evaluator;
            _items = items;
            _activeItems = activeItems;
            _traits = traits;
            _keys = keys;
            _keyPlacer = keyPlacer;
            _ledger = ledger;
            _itemParameters = itemParameters;
        }

        public void StartStage()
        {
            CurrentTurn = 0;
            Outcome = StageOutcome.InProgress;

            // 모든 쿼터의 목표 구역을 스테이지 시작 시점에 한꺼번에 확정한다 — 플레이어는 처음부터 전부 볼 수 있다.
            _keys.PrepareZones(_keyPlacer.PlaceAll(_heartbeat.Value, _keys.KeyTurns));

            // 아이템도 손패처럼 쿼터마다 빈 칸을 채운다 — 스테이지 시작 시점의 첫 지급은 여기서 이루어진다
            // (이후 쿼터 경계 리필은 BeginTurn의 쿼터 시작 처리에서 이루어진다).
            _items.Refill();

            BeginTurn();
        }

        private void BeginTurn()
        {
            CurrentTurn++;

            // 손패는 쿼터가 시작될 때만 채운다. 쿼터 중에는 낸 만큼 줄어든 채로 진행된다.
            // RefillForNewQuarter로 채운다 — 지난 쿼터에 낸 단서까지 풀로 되돌려 손패가 항상 가득 차게 한다
            // (저작된 단서 수가 스테이지 전체 턴 수보다 적기 때문. ClueHand 클래스 주석 참고).
            // 아이템도 손패와 마찬가지로 쓴 칸만 다른 아이템으로 채운다(안 쓴 아이템은 그대로 유지) — Items.cs의 ItemInventory.Refill 참고.
            if (Schedule.IsQuarterStart(CurrentTurn))
            {
                _hand.RefillForNewQuarter();
                _items.Refill();
                _keys.OpenQuarter(Schedule.QuarterOf(CurrentTurn));
            }

            TurnBegan?.Invoke(CurrentTurn);

            // 낼 단서가 없으면 플레이어가 할 수 있는 게 없으므로 시간만 흘려보낸다(풀이 마른 뒤에만 생긴다).
            if (_hand.Cards.Count == 0) PassTurn();
        }

        // ------------------------------------------------------------------
        // 아이템 사용
        // ------------------------------------------------------------------

        private ItemActivationContext CreateItemContext(ItemTarget target) =>
            new(_hand, _traits, _activeItems, _complexBoard, _heartbeat, _zone, _itemParameters, target);

        /// <summary>
        /// 이 아이템으로 고를 수 있는 대상들(대상이 필요 없는 아이템은 빈 목록). 종류별 후보(붙은 컴플렉스 / 손패 단서)를 모은 뒤
        /// 아이템 행동이 스스로 정한 조건(<see cref="IItemTargeting"/>)으로 거른다 — 예: 극복은 남은 턴이 2 이상인 컴플렉스만.
        /// </summary>
        public IReadOnlyList<ItemTarget> GetItemTargets(ItemDefinition item)
        {
            var candidates = item.TargetKind switch
            {
                ItemTargetKind.Complex => _complexBoard.Slots.Select(c => (ItemTarget)ItemTarget.Of(c)),
                ItemTargetKind.Clue => _hand.Cards.Select(c => (ItemTarget)ItemTarget.Of(c)),
                _ => Enumerable.Empty<ItemTarget>()
            };

            var context = CreateItemContext(null);
            var targeting = item.Behaviour as IItemTargeting;
            return candidates.Where(target => targeting == null || targeting.IsValidTarget(target, context)).ToList();
        }

        /// <summary>지금 이 아이템을 쓸 수 있는가: 진행 중이고, 보유하고 있고, 대상이 필요하면 고를 수 있는 대상이 하나라도 있다.</summary>
        public bool CanUseItem(ItemDefinition item) =>
            Outcome == StageOutcome.InProgress && _items.Holds(item)
            && (item.TargetKind == ItemTargetKind.None || GetItemTargets(item).Count > 0);

        /// <summary>
        /// 아이템을 쓴다. 대상이 필요한 아이템(<see cref="ItemDefinition.TargetKind"/>)은 <see cref="GetItemTargets"/>가 돌려준 대상 중 하나를 넘겨야 하고,
        /// 필요 없는 아이템에 대상을 넘기면 오류다. 쓰는 시점은 자유다(턴 사이) — 지속 효과와 특성은 다음에 내는 단서에 적용된다.
        /// </summary>
        public void UseItem(ItemDefinition item, ItemTarget target = null)
        {
            if (Outcome != StageOutcome.InProgress)
                throw new InvalidOperationException("이미 종료된 스테이지입니다.");
            if (!_items.Holds(item))
                throw new InvalidOperationException($"{item.Id} 을(를) 보유하고 있지 않습니다.");

            if (item.TargetKind == ItemTargetKind.None)
            {
                if (target != null) throw new ArgumentException($"{item.Id} 은(는) 대상이 필요 없는 아이템입니다.", nameof(target));
            }
            else
            {
                if (target == null || target.Kind != item.TargetKind)
                    throw new ArgumentException($"{item.Id} 은(는) {item.TargetKind} 대상이 필요합니다.", nameof(target));

                if (item.Behaviour is IItemTargeting targeting && !targeting.IsValidTarget(target, CreateItemContext(target)))
                    throw new ArgumentException($"{item.Id} 의 대상으로 쓸 수 없는 대상입니다.", nameof(target));
            }

            _items.Use(item, CreateItemContext(target));

            // 아이템이 심박수를 올려 안정 구간에 들어섰다면 특수 특성도 바로 치유된다.
            CureSpecialTraitsIfStable();
        }

        /// <summary>안정 구간에 들어서는 즉시 특수 특성을 치유한다(심박수가 바뀔 때마다 부른다).</summary>
        private void CureSpecialTraitsIfStable()
        {
            if (_zone.IsStable(_heartbeat.Value)) _traits.CureSpecial();
        }

        // ------------------------------------------------------------------
        // 단서 제시
        // ------------------------------------------------------------------

        /// <summary>
        /// 단서를 제시한다. 한 턴의 본 행동. 특성·아이템이 겹칠 때의 계산 순서는 아래 번호 그대로다 — 앞 단계의 출력이 다음 단계의 입력이다.
        ///
        ///  1. 과대 망상  — 단서의 <b>원래</b> 감정 개수 ×2(TraitBoard.ApplyToOriginal). 컴플렉스가 배가된 감정을 보고 해석한다.
        ///  2. 컴플렉스 해석 — 우선순위 순서로. 지속 중인 아이템(감정적 설득)이 지정한 컴플렉스는 건너뛴다.
        ///  3. 아이템 결과 보정 — 기억 공감(슬픔 1씩 제거), 무관심(타인 결과의 침체 감정 무시), 공존감(타인 결과에 행복 1 추가), 논리적 설득(중복 감정 하나씩). 지속 중인 아이템의 보정이 걸린 순서대로.
        ///  4. 환각       — 감정 하나하나의 극성을 뒤집는다(TraitAwareEmotionEvaluator).
        ///  5. 특수 특성  — 뒤집힌 뒤의 극성 기준으로 감정 하나의 영향력을 줄인다(고기능 우울증 = 흥분 ×1/2, 과흥분 = 침체 ×1/2).
        ///  6. 예민/무력  — 합계에 ×3 / ×1/2. 5)와 6)은 곱셈이라 순서를 바꿔도 같다(실수로 계산하고 마지막에 한 번 반올림).
        ///  7. 심박수 이동 — 클램프(0~200) 후 안정 구간이면 특수 특성 치유(CompleteTurn 앞에서).
        /// 일반 특성은 붙은 턴 동안 이 표에 그대로 참여하고, 턴이 끝날 때(CompleteTurn) 지속 턴이 줄어 만료된다.
        /// </summary>
        public TurnReport PlayClue(ClueInstance card)
        {
            if (Outcome != StageOutcome.InProgress)
                throw new InvalidOperationException("이미 종료된 스테이지입니다.");

            var input = _traits.ApplyToOriginal(card.Definition.CreateOriginalTagSet());
            var interpretation = _resolver.Resolve(input, _activeItems);
            var finalTags = interpretation.Final;
            _activeItems.Modify(finalTags);

            _ledger.RecordInterpretation(card.Definition.Id, interpretation);

            var delta = _evaluator.Evaluate(finalTags);
            _heartbeat.Change(delta);
            CureSpecialTraitsIfStable();

            _hand.Use(card);

            return CompleteTurn(card.Definition, interpretation, finalTags, delta);
        }

        /// <summary>손패가 비어 낼 단서가 없는 턴 — 심박수는 그대로 두고 지속 시간과 쿼터 경계만 처리한다.</summary>
        private void PassTurn() => CompleteTurn(null, null, null, 0);

        private TurnReport CompleteTurn(ClueDefinition clue, InterpretationResult interpretation, TagSet finalTags, int delta)
        {
            _complexBoard.TickDurations();
            _traits.TickDurations();
            _activeItems.TickDurations();

            ComplexInstance spawned = null;
            TraitInstance specialTrait = null;
            var overflowed = false;
            if (_spawnPolicy.ShouldSpawn(_heartbeat.Value))
            {
                // 가득 찬 보드에서는 스포너가 난수를 쓰기 전에 거절하므로, 여기서 먼저 걸러도 난수 소비 순서는 같다.
                if (_complexBoard.IsFull)
                {
                    // 최대 중첩을 넘쳐 발현됐다. 처리 방침: 새 컴플렉스는 붙이지 않고(기존 컴플렉스를 밀어내지 않고) 그 구간 쪽 특수 특성만 붙인다.
                    // 침체 구간이면 고기능 우울증, 흥분 구간이면 과흥분. 안정 구간은 발현 확률이 0이라 여기까지 오지 않는다.
                    overflowed = true;
                    var side = _zone.PolarityOf(_heartbeat.Value);
                    if (side.HasValue) specialTrait = _traits.GrantOverflow(side.Value);
                }
                else
                {
                    _spawner.TrySpawn(_complexBoard, out spawned);
                }
            }

            // 키 판정은 쿼터의 마지막 턴이 끝난 시점에만 한다.
            KeyJudgement? keyResult = _keys.IsKeyTurn(CurrentTurn) ? _keys.Judge(_heartbeat.Value) : null;

            Outcome = JudgeOutcome();

            var report = new TurnReport(CurrentTurn, Schedule.QuarterOf(CurrentTurn), Schedule.TurnInQuarter(CurrentTurn),
                clue, interpretation, finalTags, delta, _heartbeat.Value, spawned, Outcome, keyResult, overflowed, specialTrait?.Definition);
            TurnResolved?.Invoke(report);

            if (Outcome == StageOutcome.InProgress) BeginTurn();
            else StageEnded?.Invoke(Outcome);

            return report;
        }

        private StageOutcome JudgeOutcome()
        {
            if (_keys.IsComplete) return StageOutcome.Cleared;
            // Fatal 구간(즉시 패배)은 턴이 남아 있어도 마지막 턴 소진보다 우선해 즉시 종료된다.
            if (_zone.IsFatal(_heartbeat.Value)) return StageOutcome.Failed;
            if (CurrentTurn >= TotalTurns) return StageOutcome.Failed;
            return StageOutcome.InProgress;
        }
    }
}
