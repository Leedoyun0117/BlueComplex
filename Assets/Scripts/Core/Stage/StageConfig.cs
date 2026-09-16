using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;

namespace BlueComplex.Core.Stage
{
    public sealed class StageConfig
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int TotalTurns { get; }
        public int RequiredKeys { get; }
        public IReadOnlyList<int> KeyTurns { get; }
        public double ComplexWeight { get; }

        public IReadOnlyList<ClueDefinition> Clues { get; }
        public IReadOnlyList<ComplexDefinition> ComplexPool { get; }
        public ComplexDefinition StartingComplex { get; }
        public IReadOnlyList<ItemDefinition> ItemPool { get; }

        public StageConfig(string id,
                           string displayName,
                           int totalTurns,
                           int requiredKeys,
                           IReadOnlyList<int> keyTurns,
                           double complexWeight,
                           IReadOnlyList<ClueDefinition> clues,
                           IReadOnlyList<ComplexDefinition> complexPool,
                           ComplexDefinition startingComplex,
                           IReadOnlyList<ItemDefinition> itemPool)
        {
            Id = id;
            DisplayName = displayName;
            TotalTurns = totalTurns;
            RequiredKeys = requiredKeys;
            KeyTurns = keyTurns;
            ComplexWeight = complexWeight;
            Clues = clues;
            ComplexPool = complexPool;
            StartingComplex = startingComplex;
            ItemPool = itemPool;
        }
    }
}
