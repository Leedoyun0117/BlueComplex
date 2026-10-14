using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public interface IComplexCondition
    {
        bool Evaluate(ComplexContext context);
    }
}
