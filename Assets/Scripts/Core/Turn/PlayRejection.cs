using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Turn
{
    public enum PlayRejection
    {
        None,

        /// <summary>결과의 방향(흥분/침체)이 이번 턴의 목표와 다르다.</summary>
        WrongDirection,

        /// <summary>이번 턴에 함께 써야 하는 아이템이 켜져 있지 않다.</summary>
        ItemNeeded,

        /// <summary>이 카드를 내면 이번 쿼터의 키를 못 얻는다(결과를 끝까지 보여 준 뒤 되돌리는 턴에서).</summary>
        KeyMissed
    }
}
