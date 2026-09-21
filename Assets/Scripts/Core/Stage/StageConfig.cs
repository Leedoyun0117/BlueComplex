using System;
using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Stage
{
    public sealed class StageConfig
    {
        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>쿼터 수와 쿼터당 턴 수. 총 턴 수는 여기서 나온다.</summary>
        public QuarterSchedule Quarters { get; }
        public int TotalTurns => Quarters.TotalTurns;

        /// <summary>클리어에 필요한 키 개수. 쿼터마다 키 판정이 한 번씩이므로 쿼터 수를 넘을 수 없다.</summary>
        public int RequiredKeys { get; }

        /// <summary>키 구역 폭(심박수 칸 수).</summary>
        public int KeyWidth { get; }

        public double ComplexWeight { get; }

        public IReadOnlyList<ClueDefinition> Clues { get; }
        public IReadOnlyList<ComplexDefinition> ComplexPool { get; }
        public ComplexDefinition StartingComplex { get; }
        public IReadOnlyList<ItemDefinition> ItemPool { get; }

        public StageConfig(string id,
                           string displayName,
                           int quarterCount,
                           int turnsPerQuarter,
                           int requiredKeys,
                           double complexWeight,
                           IReadOnlyList<ClueDefinition> clues,
                           IReadOnlyList<ComplexDefinition> complexPool,
                           ComplexDefinition startingComplex,
                           IReadOnlyList<ItemDefinition> itemPool,
                           int keyWidth = KeyZoneLayout.DefaultKeyWidth)
        {
            if (requiredKeys > quarterCount)
                throw new ArgumentException(
                    $"필요 키({requiredKeys})가 쿼터 수({quarterCount})보다 많으면 클리어할 수 없습니다.", nameof(requiredKeys));

            Id = id;
            DisplayName = displayName;
            Quarters = new QuarterSchedule(quarterCount, turnsPerQuarter);
            RequiredKeys = requiredKeys;
            KeyWidth = keyWidth;
            ComplexWeight = complexWeight;
            Clues = clues;
            ComplexPool = complexPool;
            StartingComplex = startingComplex;
            ItemPool = itemPool;
        }
    }
}
