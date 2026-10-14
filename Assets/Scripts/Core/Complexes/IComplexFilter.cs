using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>아이템 등이 특정 컴플렉스를 일시적으로 무시시킬 때 사용.</summary>
    public interface IComplexFilter
    {
        bool ShouldIgnore(ComplexInstance complex);
    }
}
