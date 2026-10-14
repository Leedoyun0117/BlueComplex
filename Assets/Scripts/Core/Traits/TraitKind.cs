using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Traits
{
    /// <summary>일반 특성은 아이템 사용으로 발현되고 정해진 턴 동안만 간다. 특수 특성은 컴플렉스가 최대 중첩을 넘쳐 발현될 때 붙고, 안정 구간에 들어설 때까지 간다.</summary>
    public enum TraitKind
    {
        Normal,
        Special
    }
}
