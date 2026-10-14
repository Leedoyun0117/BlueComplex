using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public sealed class ShiftTime : IComplexEffect
    {
        private readonly TimeTag _time;
        public ShiftTime(TimeTag time) => _time = time;

        public void Apply(ComplexContext context) => context.Tags.SetTime(_time);
    }
}
