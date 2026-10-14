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
    /// <summary>아이템이 사용 전에 대상을 골라야 하는지, 그렇다면 무엇을 고르는지.</summary>
    public enum ItemTargetKind
    {
        None,
        Complex,
        Clue
    }
}
