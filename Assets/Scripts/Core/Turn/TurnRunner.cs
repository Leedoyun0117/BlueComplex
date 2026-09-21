using System;
using System.Collections.Generic;
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
                          KeyJudgement? keyResult)
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
                          ClueKnowledgeLedger ledger)
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
        }

        public void StartStage()
        {
            CurrentTurn = 0;
            Outcome = StageOutcome.InProgress;

            // 모든 쿼터의 목표 구역을 스테이지 시작 시점에 한꺼번에 확정한다 — 플레이어는 처음부터 전부 볼 수 있다.
            _keys.PrepareZones(_keyPlacer.PlaceAll(_heartbeat.Value, _keys.KeyTurns));

            BeginTurn();
        }

        private void BeginTurn()
        {
            CurrentTurn++;

            // 손패는 쿼터가 시작될 때만 채운다. 쿼터 중에는 낸 만큼 줄어든 채로 진행된다.
            if (Schedule.IsQuarterStart(CurrentTurn))
            {
                _hand.Refill();
                _keys.OpenQuarter(Schedule.QuarterOf(CurrentTurn));
            }

            _items.TryGainRandom(out _);

            TurnBegan?.Invoke(CurrentTurn);

            // 낼 단서가 없으면 플레이어가 할 수 있는 게 없으므로 시간만 흘려보낸다(풀이 마른 뒤에만 생긴다).
            if (_hand.Cards.Count == 0) PassTurn();
        }

        public void UseItem(ItemDefinition item) =>
            _items.Use(item, new ItemActivationContext(_hand, _traits, _activeItems));

        /// <summary>단서를 제시한다. 한 턴의 본 행동.</summary>
        public TurnReport PlayClue(ClueInstance card)
        {
            if (Outcome != StageOutcome.InProgress)
                throw new InvalidOperationException("이미 종료된 스테이지입니다.");

            var interpretation = _resolver.Resolve(card.Definition.CreateOriginalTagSet(), _activeItems);
            var finalTags = interpretation.Final;
            _activeItems.Modify(finalTags);

            _ledger.RecordInterpretation(card.Definition.Id, interpretation);

            var delta = _evaluator.Evaluate(finalTags);
            _heartbeat.Change(delta);

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
            if (_spawnPolicy.ShouldSpawn(_heartbeat.Value))
                _spawner.TrySpawn(_complexBoard, out spawned);

            // 키 판정은 쿼터의 마지막 턴이 끝난 시점에만 한다.
            KeyJudgement? keyResult = _keys.IsKeyTurn(CurrentTurn) ? _keys.Judge(_heartbeat.Value) : null;

            Outcome = JudgeOutcome();

            var report = new TurnReport(CurrentTurn, Schedule.QuarterOf(CurrentTurn), Schedule.TurnInQuarter(CurrentTurn),
                clue, interpretation, finalTags, delta, _heartbeat.Value, spawned, Outcome, keyResult);
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
