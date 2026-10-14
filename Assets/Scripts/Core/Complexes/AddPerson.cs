using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>인물 태그를 amount개 더한다. 변형(<see cref="ConvertMatchedPersonsTo"/>)과 달리 있던 태그는 그대로 두고 더하기만 한다. (스테이지 2 합리화 "타인 +1", 전이 "가족 +1")</summary>
    public sealed class AddPerson : IComplexEffect
    {
        private readonly PersonTag _person;
        private readonly int _amount;

        public AddPerson(PersonTag person, int amount = 1)
        {
            _person = person;
            _amount = amount;
        }

        public void Apply(ComplexContext context) => context.Tags.AddPerson(_person, _amount);
    }
}
