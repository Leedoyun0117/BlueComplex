using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>회상 — 손패를 전부 풀에 되돌리고 다시 뽑는다.</summary>
    public sealed class RedrawHand : IItemBehaviour
    {
        public void OnActivate(ItemActivationContext context) => context.Hand.RedrawAll();
    }
}
