using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>
    /// 무관심 — 최종 결과에 <see cref="Person"/> 태그(기획: 타인)가 붙어 있으면 그 결과의 지정한 감정(기획: 침체 감정 전부)을 통째로 무시한다.
    /// "타인에 대한 침체 감정"을 "결과에 타인 태그가 있고 침체 감정이 있는 것"으로 옮겼다 — 태그는 감정마다 대상 인물이 따로 없어서 결과 전체의 인물로 본다.
    /// 무시는 겹친 개수까지 전부 지우는 것이다(기억 공감처럼 1씩이 아니다).
    /// </summary>
    public sealed class IgnoreEmotionsTowardPerson : IItemBehaviour, IResultModifier
    {
        public PersonTag Person { get; }
        private readonly EmotionTag[] _emotions;

        public IgnoreEmotionsTowardPerson(PersonTag person, params EmotionTag[] emotions)
        {
            Person = person;
            _emotions = emotions;
        }

        public void OnActivate(ItemActivationContext context) => context.AddModifier(this);

        public void Modify(TagSet finalTags)
        {
            if (!finalTags.HasPerson(Person)) return;

            foreach (var emotion in _emotions) finalTags.RemoveEmotion(emotion, finalTags.CountOf(emotion));
        }
    }
}
