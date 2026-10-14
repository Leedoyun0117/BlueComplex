using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 대화 한 줄이 어떤 클릭으로 넘어가는지. 일반 줄은 (타이핑이 끝난 뒤) 아무 곳이나 눌러도 넘어가고, 선택지 줄은 선택지 자체를 눌러야만 넘어간다
    /// (배경을 눌러서 플레이어의 대답을 건너뛰지 못한다).
    /// </summary>
    public static class DialogueAdvanceRule
    {
        public static bool Accepts(bool lineIsChoice, bool clickedTheChoice) => !lineIsChoice || clickedTheChoice;
    }
}
