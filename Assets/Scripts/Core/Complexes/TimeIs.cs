using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public sealed class TimeIs : IComplexCondition
    {
        private readonly TimeTag _time;
        public TimeIs(TimeTag time) => _time = time;

        public bool Evaluate(ComplexContext context)
        {
            if (!context.Tags.HasTime(_time)) return false;
            context.MarkTime(_time);
            return true;
        }
    }
}
