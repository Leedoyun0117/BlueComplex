using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Items
{
    /// <summary>지속 중인 아이템 사용 하나. 자기가 건 필터·보정을 자기가 들고 있어서, 만료되면 그 효과만 사라진다.</summary>
    public sealed class ActiveItem
    {
        public ItemDefinition Definition { get; }
        public int RemainingTurns { get; private set; }
        public bool IsExpired => RemainingTurns <= 0;

        internal List<IComplexFilter> Filters { get; } = new();
        internal List<IResultModifier> Modifiers { get; } = new();

        public ActiveItem(ItemDefinition definition)
        {
            Definition = definition;
            RemainingTurns = definition.Duration;
        }

        public void Tick() => RemainingTurns--;
    }
}
