using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    public sealed class StageSession
    {
        /// <summary>이 세션을 만든 스테이지 설정 — 스테이지별로 달라지는 UI(배경음 등)가 id를 여기서 읽는다.</summary>
        public StageConfig Config { get; init; }
        public TurnRunner Runner { get; init; }
        public ClueHand Hand { get; init; }
        public ComplexBoard Complexes { get; init; }
        public Heartbeat Heartbeat { get; init; }
        public HeartbeatZone Zone { get; init; }
        public ItemInventory Items { get; init; }
        public TraitBoard Traits { get; init; }
        public KeyProgress Keys { get; init; }
        public ClueKnowledgeLedger Ledger { get; init; }
        public ActiveItemBoard ActiveItems { get; init; }
    }

    public static class StageFactory
    {
        public static StageSession Create(StageConfig config,
                                          IRandomSource random,
                                          ClueKnowledgeLedger ledger,
                                          IEmotionPolarityTable polarityTable = null,
                                          int heartbeatStartValue = Heartbeat.DefaultStartValue,
                                          IKeyZonePlacer keyPlacer = null)
        {
            polarityTable ??= new DefaultEmotionPolarityTable();

            var pool = new CluePool(config.Clues, random);
            var hand = new ClueHand(pool);

            var complexBoard = new ComplexBoard(config.MaxComplexSlots);
            var startingComplex = ChooseStartingComplex(config, random);
            if (startingComplex != null)
                complexBoard.TryAttach(new ComplexInstance(startingComplex, priority: 0));

            var traits = new TraitBoard(config.Traits, random);
            var heartbeat = new Heartbeat(heartbeatStartValue);
            var zone = config.ComplexSpawnChances == null
                ? new HeartbeatZone()
                : new HeartbeatZone(HeartbeatZone.WithSpawnChances(config.ComplexSpawnChances));
            var evaluator = new TraitAwareEmotionEvaluator(polarityTable, traits);

            var activeItems = new ActiveItemBoard();
            var items = new ItemInventory(config.ItemPool, random, config.ItemSlots);

            var keys = new KeyProgress(config.RequiredKeys, config.Quarters);
            keyPlacer ??= CreateKeyPlacer(config, random, zone, polarityTable);

            var runner = new TurnRunner(
                hand,
                complexBoard,
                new ComplexResolver(complexBoard),
                new ComplexSpawner(config.ComplexPool, random),
                new ZoneBasedSpawnPolicy(random, zone, config.ComplexSpawnChances == null ? config.ComplexWeight : 1.0),
                heartbeat,
                zone,
                evaluator,
                items,
                activeItems,
                traits,
                keys,
                keyPlacer,
                ledger,
                config.ItemParameters,
                config.KeyHandBias == null ? null : new KeyZoneHandBiasRule(zone, polarityTable, config.KeyHandBias));

            return new StageSession
            {
                Config = config,
                Runner = runner,
                Hand = hand,
                Complexes = complexBoard,
                Heartbeat = heartbeat,
                Zone = zone,
                Items = items,
                Traits = traits,
                Keys = keys,
                Ledger = ledger,
                ActiveItems = activeItems
            };
        }

        /// <summary>고정 시작 컴플렉스가 있으면 그것, 없고 무작위가 켜져 있으면 풀에서 시드 난수로 하나. 난수는 스테이지당 한 번만 쓴다.</summary>
        private static ComplexDefinition ChooseStartingComplex(StageConfig config, IRandomSource random)
        {
            if (config.StartingComplex != null) return config.StartingComplex;
            if (!config.RandomStartingComplex || config.ComplexPool.Count == 0) return null;

            return config.ComplexPool[random.Range(0, config.ComplexPool.Count)];
        }

        /// <summary>
        /// 이 스테이지의 카드 풀이 만들 수 있는 이동량으로 도달 범위를 잡는 배치기. 단서는 한 번 쓰면 사라지므로
        /// 턴당 최대 이동폭을 곱하는 것보다 카드 풀의 실제 구성(상승 카드가 몇 장인지)이 훨씬 촘촘한 상한을 준다.
        /// 상승 여력은 카드 자체의 이동량, 하강 여력은 컴플렉스가 최악으로 겹칠 때의 이동량으로 잡는다(CardPoolReachModel 참고).
        /// 구역의 절반 이상이 범위 안에 들어와야 도달 가능으로 본다.
        /// </summary>
        public static ReachabilityKeyZonePlacer CreateKeyPlacer(StageConfig config,
                                                                IRandomSource random,
                                                                HeartbeatZone zone,
                                                                IEmotionPolarityTable polarityTable)
        {
            var evaluator = new EmotionEvaluator(polarityTable);

            var complexes = config.ComplexPool.ToList();
            if (config.StartingComplex != null && complexes.All(c => c.Id != config.StartingComplex.Id))
                complexes.Add(config.StartingComplex);

            var reach = new CardPoolReachModel(
                config.Clues.Select(clue => (
                    Best: evaluator.Evaluate(clue.CreateOriginalTagSet()),
                    Worst: WorstCaseDelta(clue, complexes, evaluator, config.MaxComplexSlots))),
                ReachabilityKeyZonePlacer.DefaultMaxMovePerTurn,
                ReachabilityKeyZonePlacer.DefaultMaxDropPerTurn);

            return new ReachabilityKeyZonePlacer(random, new KeyZoneLayout(zone, keyWidth: config.KeyWidth),
                reach: reach, minOverlapFraction: 0.5);
        }

        /// <summary>
        /// 컴플렉스가 어떻게 붙어 있든(개수·우선순위 순서 모두) 이 단서가 만들 수 있는 가장 낮은 이동량.
        /// 풀에서 최대 maxSlots개를 순서까지 구분해 붙이는 모든 경우의 최솟값이지만, 실제로 전수 조사하지는 않는다 —
        /// 발동하지 않는 컴플렉스는 결과를 바꾸지 않으므로 붙이지 않은 경우와 같다(붙이는 개수는 0개부터 가능하다).
        /// 그래서 "직전 상태에서 실제로 발동하는 컴플렉스"만 골라 깊이 maxSlots까지 내려가면 같은 최솟값이 나오고,
        /// 풀이 10개일 때 단서당 수천 번이던 해석이 수십 번으로 줄어든다(스테이지를 만들 때마다 도는 계산이다).
        /// </summary>
        public static int WorstCaseDelta(ClueDefinition clue,
                                         IReadOnlyList<ComplexDefinition> pool,
                                         IEmotionEvaluator evaluator,
                                         int maxSlots)
        {
            var worst = int.MaxValue;
            var used = new bool[pool.Count];

            void Visit(TagSet state, int depth)
            {
                worst = Math.Min(worst, evaluator.Evaluate(state));
                if (depth == maxSlots) return;

                for (var i = 0; i < pool.Count; i++)
                {
                    if (used[i]) continue;

                    var next = state.Clone();
                    if (!pool[i].TryInterpret(new ComplexContext(next))) continue;

                    used[i] = true;
                    Visit(next, depth + 1);
                    used[i] = false;
                }
            }

            Visit(clue.CreateOriginalTagSet(), 0);
            return worst;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}