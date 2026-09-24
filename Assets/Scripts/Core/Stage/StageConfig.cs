using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Traits;
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

        /// <summary>컴플렉스가 동시에 붙을 수 있는 최대 수(최대 중첩). 이 수를 넘겨 새 컴플렉스가 나타나면 특수 특성 발현 조건이 된다.</summary>
        public int MaxComplexSlots { get; }

        public IReadOnlyList<ClueDefinition> Clues { get; }
        public IReadOnlyList<ComplexDefinition> ComplexPool { get; }

        /// <summary>고정 시작 컴플렉스. 비어 있고 <see cref="RandomStartingComplex"/>도 꺼져 있으면 컴플렉스 없이 시작한다.</summary>
        public ComplexDefinition StartingComplex { get; }

        /// <summary>
        /// 켜면 스테이지를 만들 때(<see cref="StageFactory.Create"/>) 시작 컴플렉스를 <see cref="ComplexPool"/>에서 시드 난수로 하나 고른다 —
        /// 같은 시드면 같은 컴플렉스, 다른 시드면 (대체로) 다른 컴플렉스. <see cref="StartingComplex"/>가 채워져 있으면 그쪽이 우선한다.
        /// </summary>
        public bool RandomStartingComplex { get; }

        public IReadOnlyList<ItemDefinition> ItemPool { get; }

        /// <summary>null이 아니면 쿼터 손패를 채울 때 그 쿼터 키 목표 구간에 맞는 감정의 단서를 강제로 포함한다(<see cref="KeyZoneHandBiasRule"/>).</summary>
        public KeyHandBiasSettings KeyHandBias { get; }

        /// <summary>이 스테이지에 나오는 특성 목록(일반 + 특수). 아이템과 컴플렉스 초과가 id·구간 방향으로 여기서 찾는다.</summary>
        public IReadOnlyList<TraitDefinition> Traits { get; }

        /// <summary>스테이지 시작에 지급하고 쿼터 시작마다 채우는 아이템 칸 수.</summary>
        public int ItemSlots { get; }

        /// <summary>
        /// 스테이지별로 다른 아이템 값(키 → 문자열 목록). 예: 감정적 설득이 무시할 컴플렉스 id는 스테이지마다 달라서
        /// 아이템 데이터가 아니라 여기(<see cref="PrototypeContent.PersuasionTargetsKey"/>)에 둔다.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> ItemParameters { get; }

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
                           int keyWidth = KeyZoneLayout.DefaultKeyWidth,
                           int maxComplexSlots = ComplexBoard.DefaultMaxSlots,
                           IReadOnlyList<TraitDefinition> traits = null,
                           int itemSlots = ItemInventory.DefaultCapacity,
                           IReadOnlyDictionary<string, IReadOnlyList<string>> itemParameters = null,
                           bool randomStartingComplex = false,
                           KeyHandBiasSettings keyHandBias = null)
        {
            if (maxComplexSlots < 1)
                throw new ArgumentOutOfRangeException(nameof(maxComplexSlots), "최대 중첩은 1 이상이어야 합니다.");

            if (requiredKeys > quarterCount)
                throw new ArgumentException(
                    $"필요 키({requiredKeys})가 쿼터 수({quarterCount})보다 많으면 클리어할 수 없습니다.", nameof(requiredKeys));

            var loneSet = clues?.Where(c => c.SetId != null).GroupBy(c => c.SetId).FirstOrDefault(g => g.Count() < 2);
            if (loneSet != null)
                throw new ArgumentException($"set '{loneSet.Key}' 는 단서가 2개 이상이어야 합니다.", nameof(clues));

            Id = id;
            DisplayName = displayName;
            Quarters = new QuarterSchedule(quarterCount, turnsPerQuarter);
            RequiredKeys = requiredKeys;
            KeyWidth = keyWidth;
            ComplexWeight = complexWeight;
            MaxComplexSlots = maxComplexSlots;
            Traits = traits ?? Array.Empty<TraitDefinition>();
            ItemSlots = itemSlots;
            ItemParameters = itemParameters ?? new Dictionary<string, IReadOnlyList<string>>();
            Clues = clues;
            ComplexPool = complexPool;
            StartingComplex = startingComplex;
            RandomStartingComplex = randomStartingComplex;
            ItemPool = itemPool;
            KeyHandBias = keyHandBias;
        }
    }
}
