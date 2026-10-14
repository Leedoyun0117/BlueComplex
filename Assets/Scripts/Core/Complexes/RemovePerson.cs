using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>인물 태그를 amount개 뗀다. 없으면 아무 일도 없다. (스테이지 3 밀착 "타인 -1")</summary>
    public sealed class RemovePerson : IComplexEffect
    {
        private readonly PersonTag _person;
        private readonly int _amount;

        public RemovePerson(PersonTag person, int amount = 1)
        {
            _person = person;
            _amount = amount;
        }

        public void Apply(ComplexContext context) => context.Tags.RemovePerson(_person, _amount);
    }
}
