using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>
    /// 공존감 — 최종 결과에 <see cref="Person"/> 태그(기획: 타인)가 붙어 있으면 그 결과에 지정한 감정(기획: 행복)을 amount개 더한다.
    /// 무관심과 같은 이유로 "타인에 관한 단서"를 "결과에 타인 태그가 있는 것"으로 본다. 결과 보정 단계에서 더하므로 뒤이어 켜진 논리적 설득이 중복을 접을 수 있다.
    /// </summary>
    public sealed class AddEmotionTowardPerson : IItemBehaviour, IResultModifier
    {
        public PersonTag Person { get; }
        private readonly EmotionTag _emotion;
        private readonly int _amount;

        public AddEmotionTowardPerson(PersonTag person, EmotionTag emotion, int amount)
        {
            Person = person;
            _emotion = emotion;
            _amount = amount;
        }

        public void OnActivate(ItemActivationContext context) => context.AddModifier(this);

        public void Modify(TagSet finalTags)
        {
            if (finalTags.HasPerson(Person)) finalTags.AddEmotion(_emotion, _amount);
        }
    }
}
