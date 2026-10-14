using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>조건이 매칭한 인물들을 지정 인물로 변환. (예: 친구 → 연인, 연인/친구 → 타인)</summary>
    public sealed class ConvertMatchedPersonsTo : IComplexEffect
    {
        private readonly PersonTag _target;
        public ConvertMatchedPersonsTo(PersonTag target) => _target = target;

        public void Apply(ComplexContext context)
        {
            foreach (var person in context.MatchedPersons)
            {
                if (person == _target) continue;
                context.Tags.ReplacePerson(person, _target);
            }
        }
    }
}
