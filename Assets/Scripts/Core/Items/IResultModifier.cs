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
    /// <summary>최종 의미가 확정된 뒤 결과를 손보는 효과.</summary>
    public interface IResultModifier
    {
        void Modify(TagSet finalTags);
    }
}
