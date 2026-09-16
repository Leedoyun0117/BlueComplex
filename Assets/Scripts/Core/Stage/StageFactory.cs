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
        public Heartbeat Heartbeat { get; init; }
        public HeartbeatZone Zone { get; init; }
        public ItemInventory Items { get; init; }
        public TraitBoard Traits { get; init; }
        public KeyProgress Keys { get; init; }
        public ClueKnowledgeLedger Ledger { get; init; }
        public CensorshipState Censorship { get; init; }
    }

    public static class StageFactory
    {
        public static StageSession Create(StageConfig config,
                                          IRandomSource random,
                                          ClueKnowledgeLedger ledger,
                                          IEmotionPolarityTable polarityTable = null,
                                          int heartbeatStartValue = Heartbeat.DefaultStartValue)
        {
            polarityTable ??= new DefaultEmotionPolarityTable();

            var pool = new CluePool(config.Clues, random);
            var hand = new ClueHand(pool);

            var complexBoard = new ComplexBoard();
            if (config.StartingComplex != null)
                complexBoard.TryAttach(new ComplexInstance(config.StartingComplex, priority: 0));

            var traits = new TraitBoard();
            var heartbeat = new Heartbeat(heartbeatStartValue);
            var zone = new HeartbeatZone();
            var evaluator = new TraitAwareEmotionEvaluator(new EmotionEvaluator(polarityTable), traits);

            var censorship = new CensorshipState(heartbeat, zone);

            var activeItems = new ActiveItemBoard();
            var items = new ItemInventory(config.ItemPool, random);

            var keys = new KeyProgress(config.RequiredKeys, config.KeyTurns);
            var keyPlacer = new ReachabilityKeyZonePlacer(random, new KeyZoneLayout());

            var runner = new TurnRunner(
                hand,
                complexBoard,
                new ComplexResolver(complexBoard),
                new ComplexSpawner(config.ComplexPool, random),
                new ZoneBasedSpawnPolicy(random, zone, config.ComplexWeight),
                heartbeat,
                zone,
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
                Heartbeat = heartbeat,
                Zone = zone,
                Items = items,
                Traits = traits,
                Keys = keys,
                Ledger = ledger,
                Censorship = censorship
            };
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}