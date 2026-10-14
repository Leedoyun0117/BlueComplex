using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>가이드 말풍선이 가리키는 화면 요소. <see cref="None"/>이면 강조 없이 글만 띄운다.</summary>
    public enum GuideTarget
    {
        None,

        /// <summary>손패의 단서 카드들.</summary>
        Cards,

        /// <summary>단서 정보 책의 "메뉴얼" 탭 버튼.</summary>
        ManualTab,

        /// <summary>컴플렉스 엑스레이 판넬.</summary>
        Xray,

        /// <summary>아이템 슬롯.</summary>
        Items
    }
}
