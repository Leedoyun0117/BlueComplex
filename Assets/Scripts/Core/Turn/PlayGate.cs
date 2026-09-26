using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Turn
{
    /// <summary>단서를 내기 직전의 판정 결과. 거절되면 카드는 손패에 그대로 남고 턴은 진행되지 않는다.</summary>
    public readonly struct PlayVerdict
    {
        public static PlayVerdict Allow { get; } = new(true, PlayRejection.None, null);

        public bool Allowed { get; }
        public PlayRejection Reason { get; }

        /// <summary>거절된 단서를 냈다면 나왔을 최종 감정(컴플렉스 해석 뒤). 화면이 "이 감정은 ○○였네"를 만드는 재료다. 허용이거나 감정을 알릴 필요가 없으면 빈 목록.</summary>
        public IReadOnlyList<EmotionTag> ObservedEmotions { get; }

        private PlayVerdict(bool allowed, PlayRejection reason, IReadOnlyList<EmotionTag> observedEmotions)
        {
            Allowed = allowed;
            Reason = reason;
            ObservedEmotions = observedEmotions ?? System.Array.Empty<EmotionTag>();
        }

        public static PlayVerdict Reject(PlayRejection reason, IReadOnlyList<EmotionTag> observedEmotions = null) =>
            new(false, reason, observedEmotions);
    }

    public enum PlayRejection
    {
        None,

        /// <summary>결과의 방향(흥분/침체)이 이번 턴의 목표와 다르다.</summary>
        WrongDirection,

        /// <summary>이번 턴에 함께 써야 하는 아이템이 켜져 있지 않다.</summary>
        ItemNeeded
    }

    /// <summary>
    /// 단서를 내기 전에 규칙이 끼어드는 자리(튜토리얼의 "오답이면 카드를 돌려주고 다시 생각하게 한다"). 스테이지에 게이트가 없으면(null) 언제나 허용이다.
    /// 낸 뒤에 되돌리는 방식이 아니다 — 심박수·손패·관찰 기록·지속 시간이 한 번 움직이면 되돌릴 수 없으므로, 판정은 반드시 <see cref="TurnRunner.PlayClue"/> 전에 한다. 카드를 내는 모든 입구(드롭 영역, 디버그 숫자키)가 <see cref="Stage.StageSession.CheckPlay"/>를 거친다.
    /// </summary>
    public interface IPlayGate
    {
        PlayVerdict Check(ClueInstance card);
    }
}
