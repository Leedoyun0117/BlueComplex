using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Traits
{
    public sealed class TraitInstance
    {
        public TraitDefinition Definition { get; }

        /// <summary>남은 턴. 특수 특성은 턴으로 줄지 않으므로 항상 <see cref="TraitBoard.UntilCured"/>다.</summary>
        public int RemainingTurns { get; private set; }

        /// <summary>이미 같은 특성이 붙어 있는 상태에서 다시 붙여 갈아끼운 것이면 true — "새로 발현"이 아니라 지속이 갱신된 것이다.</summary>
        public bool RenewedExisting { get; internal set; }

        public bool IsExpired => Definition.Kind == TraitKind.Normal && RemainingTurns <= 0;

        public TraitInstance(TraitDefinition definition, int duration)
        {
            Definition = definition;
            RemainingTurns = definition.Kind == TraitKind.Special ? TraitBoard.UntilCured : duration;
        }

        public void Tick()
        {
            if (Definition.Kind == TraitKind.Normal) RemainingTurns--;
        }
    }
}
