using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>지정한 인물 태그가 count개 이상 붙어 있으면 성립(같은 종류가 더해져 겹친 개수까지 센다). (스테이지 2 가족애 — "가족 + 2")</summary>
    public sealed class PersonCountOfAtLeast : IComplexCondition
    {
        private readonly PersonTag _person;
        private readonly int _count;

        public PersonCountOfAtLeast(PersonTag person, int count)
        {
            _person = person;
            _count = count;
        }

        public bool Evaluate(ComplexContext context)
        {
            if (context.Tags.CountOfPerson(_person) < _count) return false;
            context.MarkPerson(_person);
            return true;
        }
    }
}
