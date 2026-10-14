using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>
    /// 자아비대 — 이번 턴 결과 감정 전체(태그마다 겹친 개수 포함)에 <see cref="Factor"/>를 곱한다. 시간·인물 태그는 그대로다.
    /// 결과 보정 단계(계산 순서 3)에서 걸리므로 그 앞의 컴플렉스 해석 결과가 곱해지고, 같은 단계의 다른 보정과는 켜진 순서대로 섞인다.
    /// </summary>
    public sealed class MultiplyEmotions : IItemBehaviour, IResultModifier
    {
        public int Factor { get; }

        public MultiplyEmotions(int factor) => Factor = factor;

        public void OnActivate(ItemActivationContext context) => context.AddModifier(this);

        public void Modify(TagSet finalTags)
        {
            foreach (var (emotion, count) in finalTags.Emotions.ToList())
                finalTags.AddEmotion(emotion, count * (Factor - 1));
        }
    }
}
