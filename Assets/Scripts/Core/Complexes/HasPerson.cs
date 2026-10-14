using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public sealed class HasPerson : IComplexCondition
    {
        private readonly PersonTag _person;
        public HasPerson(PersonTag person) => _person = person;

        public bool Evaluate(ComplexContext context)
        {
            if (!context.Tags.HasPerson(_person)) return false;
            context.MarkPerson(_person);
            return true;
        }
    }
}
