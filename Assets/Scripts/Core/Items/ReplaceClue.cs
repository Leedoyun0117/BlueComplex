using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>선택적 기억 — 고른 보유 단서를 풀에서 뽑은 랜덤 단서로 바꾼다(고른 단서는 풀로 돌아간다). 풀이 비어 있으면 바꿀 수 없다.</summary>
    public sealed class ReplaceClue : IItemBehaviour, IItemTargeting
    {
        public bool IsValidTarget(ItemTarget target, ItemActivationContext context) =>
            target is ClueTarget { Card: var card } && context.Hand.CanReplace(card);

        public void OnActivate(ItemActivationContext context)
        {
            var target = (ClueTarget)context.Target;
            if (!context.Hand.TryReplaceRandom(target.Card, out _))
                throw new InvalidOperationException("바꿀 단서가 풀에 없습니다.");
        }
    }
}
