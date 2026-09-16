using System;
using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
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

    /// <summary>한 번의 단서 제시가 만들어낸 모든 결과. UI 연출은 이걸 그대로 재생하면 된다.</summary>
    public sealed class TurnReport
    {
        public int Turn { get; }
        public ClueDefinition Clue { get; }
        public InterpretationResult Interpretation { get; }
        public TagSet FinalTags { get; }
        public int IndicatorDelta { get; }
        public int IndicatorPosition { get; }
        public ComplexInstance SpawnedComplex { get; }
        public StageOutcome Outcome { get; }

        public TurnReport(int turn,
                          ClueDefinition clue,
                          InterpretationResult interpretation,
                          TagSet finalTags,
                          int indicatorDelta,
                          int indicatorPosition,
                          ComplexInstance spawnedComplex,
                          StageOutcome outcome)
        {
            Turn = turn;
            Clue = clue;
            Interpretation = interpretation;
            FinalTags = finalTags;
            IndicatorDelta = indicatorDelta;
            IndicatorPosition = indicatorPosition;
            SpawnedComplex = spawnedComplex;
            Outcome = outcome;
        }
    }

    /// <summary>
    /// 게임 루프의 순서만 책임진다.
    /// 판정·변환·확률·자원 관리는 전부 주입받은 시스템이 수행한다.
    /// </summary>
    public sealed class TurnRunner
    {
        private readonly ClueHand _hand;
        private readonly ComplexBoard _complexBoard;
        private readonly ComplexResolver _resolver;
        private readonly ComplexSpawner _spawner;
        private readonly IComplexSpawnPolicy _spawnPolicy;
        private readonly StabilityIndicator _indicator;
        private readonly IEmotionEvaluator _evaluator;
        private readonly ItemInventory _items;
        private readonly ActiveItemBoard _activeItems;
        private readonly TraitBoard _traits;
        private readonly KeyProgress _keys;
        private readonly IKeyZonePlacer _keyPlacer;
        private readonly ClueKnowledgeLedger _ledger;
        private readonly int _totalTurns;

        public int CurrentTurn { get; private set; }
        public StageOutcome Outcome { get; private set; } = StageOutcome.InProgress;

        public event Action<int> TurnBegan;
        public event Action<TurnReport> TurnResolved;
        public event Action<StageOutcome> StageEnded;

        public TurnRunner(ClueHand hand,
                          ComplexBoard complexBoard,
                          ComplexResolver resolver,
                          ComplexSpawner spawner,
                          IComplexSpawnPolicy spawnPolicy,
                          StabilityIndicator indicator,
                          IEmotionEvaluator evaluator,
                          ItemInventory items,
                          ActiveItemBoard activeItems,
                          TraitBoard traits,
                          KeyProgress keys,
                          IKeyZonePlacer keyPlacer,
                          ClueKnowledgeLedger ledger,
                          int totalTurns)
        {
            _hand = hand;
            _complexBoard = complexBoard;
            _resolver = resolver;
            _spawner = spawner;
            _spawnPolicy = spawnPolicy;
            _indicator = indicator;
            _evaluator = evaluator;
            _items = items;
            _activeItems = activeItems;
            _traits = traits;
            _keys = keys;
            _keyPlacer = keyPlacer;
            _ledger = ledger;
            _totalTurns = totalTurns;
        }

        public void StartStage()
        {
            CurrentTurn = 0;
            Outcome = StageOutcome.InProgress;

            // 모든 키 턴의 구역을 스테이지 시작 시점에 한꺼번에 확정한다 — 플레이어는 턴 1부터 전부 볼 수 있다.
            var zones = new Dictionary<int, KeyZone>();
            foreach (var turn in _keys.KeyTurns)
                zones[turn] = _keyPlacer.Place(_indicator.Position, turn - 1);
            _keys.PrepareZones(zones);

            _hand.Refill();
            BeginTurn();
        }

        private void BeginTurn()
        {
            CurrentTurn++;
            _items.TryGainRandom(out _);

            if (_keys.IsKeyTurn(CurrentTurn))
                _keys.OpenZone(CurrentTurn);

            TurnBegan?.Invoke(CurrentTurn);
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
            _indicator.Move(delta);

            _keys.Judge(_indicator.Position);
            _hand.Use(card);

            _complexBoard.TickDurations();
            _traits.TickDurations();
            _activeItems.TickDurations();

            ComplexInstance spawned = null;
            if (_spawnPolicy.ShouldSpawn(_indicator.DistanceFromCenter))
                _spawner.TrySpawn(_complexBoard, out spawned);

            _hand.Refill();
            Outcome = JudgeOutcome();

            var report = new TurnReport(CurrentTurn, card.Definition, interpretation, finalTags,
                delta, _indicator.Position, spawned, Outcome);
            TurnResolved?.Invoke(report);

            if (Outcome == StageOutcome.InProgress) BeginTurn();
            else StageEnded?.Invoke(Outcome);

            return report;
        }

        private StageOutcome JudgeOutcome()
        {
            if (_keys.IsComplete) return StageOutcome.Cleared;
            if (CurrentTurn >= _totalTurns) return StageOutcome.Failed;
            return StageOutcome.InProgress;
        }
    }
}
