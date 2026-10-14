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
    public sealed class ClueTarget : ItemTarget
    {
        public ClueInstance Card { get; }
        public override ItemTargetKind Kind => ItemTargetKind.Clue;
        public ClueTarget(ClueInstance card) => Card = card;
    }
}
