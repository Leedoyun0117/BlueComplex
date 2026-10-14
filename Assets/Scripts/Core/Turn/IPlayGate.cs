using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Turn
{
    /// <summary>
    /// 단서를 내기 전에 규칙이 끼어드는 자리(튜토리얼의 "오답이면 카드를 돌려주고 다시 생각하게 한다"). 스테이지에 게이트가 없으면(null) 언제나 허용이다.
    /// 판정은 반드시 <see cref="TurnRunner.PlayClue"/> 전에 한다 — 심박수·손패·관찰 기록·지속 시간이 한 번 움직이면 되돌릴 수 없으므로 오답은 실제로 내지 않는다.
    /// 결과를 보여 주는 거절(<see cref="PlayVerdict.IsTrial"/>)도 실제로 내지 않고 미리보기로 연출한 뒤, 그 턴에 플레이어가 한 일(아이템 사용)만 <see cref="RollBackTrial"/>로 되돌린다.
    /// 카드를 내는 모든 입구(드롭 영역, 디버그 숫자키)가 <see cref="Stage.StageSession.CheckPlay"/>를 거친다.
    /// </summary>
    public interface IPlayGate
    {
        PlayVerdict Check(ClueInstance card);

        /// <summary>결과를 보여 준 거절(<see cref="PlayVerdict.IsTrial"/>) 뒤에 부른다 — 세션을 이번 턴이 시작된 상태(아이템을 쓰기 전)로 되돌린다.</summary>
        void RollBackTrial();
    }
}
