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
        public TurnRunner Runner { get; init; }
        public ClueHand Hand { get; init; }
        public ComplexBoard Complexes { get; init; }
        public StabilityIndicator Indicator { get; init; }
        public ItemInventory Items { get; init; }
        public TraitBoard Traits { get; init; }
        public KeyProgress Keys { get; init; }
        public ClueKnowledgeLedger Ledger { get; init; }
    }

    public static class StageFactory
    {
        public static StageSession Create(StageConfig config,
                                          IRandomSource random,
                                          ClueKnowledgeLedger ledger,
                                          IEmotionPolarityTable polarityTable = null,
                                          int indicatorSlots = 10)
        {
            polarityTable ??= new DefaultEmotionPolarityTable();

            var pool = new CluePool(config.Clues, random);
            var hand = new ClueHand(pool);

            var complexBoard = new ComplexBoard();
            if (config.StartingComplex != null)
                complexBoard.TryAttach(new ComplexInstance(config.StartingComplex, priority: 0));

            var traits = new TraitBoard();
            var indicator = new StabilityIndicator(indicatorSlots);
            var evaluator = new TraitAwareEmotionEvaluator(new EmotionEvaluator(polarityTable), traits);

            var activeItems = new ActiveItemBoard();
            var items = new ItemInventory(config.ItemPool, random);

            var keys = new KeyProgress(config.RequiredKeys, config.KeyTurns);
            var keyPlacer = new RandomKeyZonePlacer(random, new KeyZoneLayout(indicatorSlots));

            var runner = new TurnRunner(
                hand,
                complexBoard,
                new ComplexResolver(complexBoard),
                new ComplexSpawner(config.ComplexPool, random),
                new DistanceBasedSpawnPolicy(random, config.ComplexWeight),
                indicator,
                evaluator,
                items,
                activeItems,
                traits,
                keys,
                keyPlacer,
                ledger,
                config.TotalTurns);

            return new StageSession
            {
                Runner = runner,
                Hand = hand,
                Complexes = complexBoard,
                Indicator = indicator,
                Items = items,
                Traits = traits,
                Keys = keys,
                Ledger = ledger
            };
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}