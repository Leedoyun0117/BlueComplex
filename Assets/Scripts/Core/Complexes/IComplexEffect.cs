using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public interface IComplexEffect
    {
        void Apply(ComplexContext context);
    }
}
