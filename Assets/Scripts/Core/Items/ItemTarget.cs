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
    /// <summary>
    /// 아이템 대상 하나. 대상 종류가 달라도(컴플렉스, 단서) <c>TurnRunner.UseItem(item, target)</c> 시그니처는 하나로 두려고 만든 얇은 상자다 —
    /// 종류는 <see cref="Kind"/>로 알고, 아이템 행동은 자기에게 맞는 서브클래스로만 꺼내 쓴다.
    /// </summary>
    public abstract class ItemTarget
    {
        public abstract ItemTargetKind Kind { get; }

        public static ComplexTarget Of(ComplexInstance complex) => new(complex);
        public static ClueTarget Of(ClueInstance card) => new(card);
    }
}
