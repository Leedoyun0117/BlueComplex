using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>지정된 인물 중 하나라도 있으면 성립. 매칭된 인물만 기록한다.</summary>
    public sealed class HasAnyPersonOf : IComplexCondition
    {
        private readonly IReadOnlyList<PersonTag> _candidates;
        public HasAnyPersonOf(params PersonTag[] candidates) => _candidates = candidates;

        public bool Evaluate(ComplexContext context)
        {
            var matched = _candidates.Where(context.Tags.HasPerson).ToList();
            if (matched.Count == 0) return false;
            foreach (var person in matched) context.MarkPerson(person);
            return true;
        }
    }
}
