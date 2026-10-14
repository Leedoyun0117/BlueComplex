using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Items
{
    /// <summary>대상이 필요한 아이템 행동이 고를 수 있는 대상을 좁힌다(예: 남은 턴이 2 이상인 컴플렉스만).</summary>
    public interface IItemTargeting
    {
        bool IsValidTarget(ItemTarget target, ItemActivationContext context);
    }
}
