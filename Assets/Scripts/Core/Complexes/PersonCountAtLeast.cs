using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>서로 다른 인물 태그가 count개 이상 붙어 있으면 성립. (스테이지 2 의식 분산 — "인물 태그 2개 이상")</summary>
    public sealed class PersonCountAtLeast : IComplexCondition
    {
        private readonly int _count;
        public PersonCountAtLeast(int count) => _count = count;

        public bool Evaluate(ComplexContext context)
        {
            if (context.Tags.Persons.Count < _count) return false;
            foreach (var person in context.Tags.Persons) context.MarkPerson(person);
            return true;
        }
    }
}
