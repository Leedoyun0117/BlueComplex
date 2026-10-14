using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>가이드 한 단계가 끝나는 조건. 화면(UI)이 일어난 일을 <see cref="TutorialGuideFlow.Notify"/>로 알린다.</summary>
    public enum GuideAdvance
    {
        /// <summary>다 읽으면(화면이 시간을 재거나 말풍선을 누르면) 넘어간다.</summary>
        Read,

        /// <summary>단서 정보 책이 열렸다.</summary>
        BookOpened,

        /// <summary>책의 "메뉴얼" 탭이 펼쳐졌다.</summary>
        ManualTabShown,

        /// <summary>책이 닫혔다.</summary>
        BookClosed,

        /// <summary>컴플렉스 엑스레이 판넬이 열렸다.</summary>
        XrayOpened,

        /// <summary>아이템을 썼다.</summary>
        ItemUsed,

        /// <summary>턴이 결산됐다(<see cref="TutorialGuideStep.WaitTurn"/>번째 이상). 카드를 낸 것은 <see cref="TurnRunner.TurnResolved"/>로 안다.</summary>
        TurnResolved
    }
}
