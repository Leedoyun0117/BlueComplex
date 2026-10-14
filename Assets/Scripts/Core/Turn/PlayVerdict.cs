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
}
