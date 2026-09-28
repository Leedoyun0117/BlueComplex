using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Turn
{
    /// <summary>단서를 내기 직전의 판정 결과. 거절되면 카드는 손패에 그대로 남고 턴은 진행되지 않는다.</summary>
    public readonly struct PlayVerdict
    {
        public static PlayVerdict Allow { get; } = new(true, false, PlayRejection.None, null);

        public bool Allowed { get; }

        /// <summary>
        /// 거절이지만 내기 전에 막지 않는다 — 결과를 끝까지 보여 준 뒤 되돌린다(<see cref="IPlayGate.RollBackTrial"/>). <see cref="Allowed"/>는 false다:
        /// 실제로 <see cref="TurnRunner.PlayClue"/>를 부르지 않고, 결과 연출은 미리보기(<see cref="TurnRunner.PreviewPlay"/>)로 재생한다.
        /// 튜토리얼 턴 4의 오답(키를 못 얻는 수)만 이렇게 한다 — 턴 1·3의 오답은 지금처럼 내기 전에 거절한다.
        /// </summary>
        public bool IsTrial { get; }

        public PlayRejection Reason { get; }

        /// <summary>거절된 단서를 냈다면 나왔을 최종 감정(컴플렉스 해석 뒤). 화면이 "이 감정은 ○○였네"를 만드는 재료다. 허용이거나 감정을 알릴 필요가 없으면 빈 목록.</summary>
        public IReadOnlyList<EmotionTag> ObservedEmotions { get; }

        private PlayVerdict(bool allowed, bool isTrial, PlayRejection reason, IReadOnlyList<EmotionTag> observedEmotions)
        {
            Allowed = allowed;
            IsTrial = isTrial;
            Reason = reason;
            ObservedEmotions = observedEmotions ?? System.Array.Empty<EmotionTag>();
        }

        public static PlayVerdict Reject(PlayRejection reason, IReadOnlyList<EmotionTag> observedEmotions = null) =>
            new(false, false, reason, observedEmotions);

        /// <summary>결과를 보여 준 뒤 되돌리는 거절(<see cref="IsTrial"/>).</summary>
        public static PlayVerdict Trial(PlayRejection reason, IReadOnlyList<EmotionTag> observedEmotions = null) =>
            new(false, true, reason, observedEmotions);
    }

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
