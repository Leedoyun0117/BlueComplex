using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public sealed class HasAnyPerson : IComplexCondition
    {
        public bool Evaluate(ComplexContext context)
        {
            if (!context.Tags.HasAnyPerson()) return false;
            foreach (var person in context.Tags.Persons) context.MarkPerson(person);
            return true;
        }
    }
}
