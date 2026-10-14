using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>논리적 설득 — 결과에서 중복된 감정을 하나씩만 남긴다.</summary>
    public sealed class CollapseDuplicateEmotions : IItemBehaviour, IResultModifier
    {
        public void OnActivate(ItemActivationContext context) => context.AddModifier(this);

        public void Modify(TagSet finalTags) => finalTags.CollapseDuplicates();
    }
}
