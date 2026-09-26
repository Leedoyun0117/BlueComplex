using System.Linq;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 단서 제시가 게이트(<see cref="IPlayGate"/>)에 걸렸을 때의 안내. 코어는 "왜 거절됐는지"(사유·나왔을 감정)만 알려 주고, 문장은 여기서 만든다.
    /// 임시로 유키의 대사창(<see cref="DialogueText"/>)에 이벤트 글자색으로 띄운다 — 안내자(청장) 화자와 가이드 오버레이가 붙으면 그쪽으로 옮긴다.
    /// 카드는 손패로 되돌아가고(드롭 영역이 받아들이지 않는다) 턴은 진행되지 않는다.
    /// </summary>
    public static class PlayGateFeedback
    {
        /// <summary>"이런, 이 감정은 ‘분노’였네. 다시 한 번 생각해 봐." 감정이 여럿이면 가운뎃점으로 잇는다.</summary>
        public static string Message(PlayVerdict verdict)
        {
            switch (verdict.Reason)
            {
                case PlayRejection.WrongDirection:
                    var emotions = string.Join("·", verdict.ObservedEmotions.Select(KoreanLabels.Emotion));
                    return emotions.Length == 0
                        ? "이런, 이 단서로는 안 되겠어. 다시 한 번 생각해 봐."
                        : $"이런, 이 감정은 ‘{emotions}’{(EndsWithBatchim(emotions) ? "이었네" : "였네")}. 다시 한 번 생각해 봐.";

                case PlayRejection.ItemNeeded:
                    return "도움이 될 아이템을 하나 넣어 두었으니, 설명을 읽고 함께 활용해 봐.";

                default:
                    return null;
            }
        }

        /// <summary>마지막 글자에 받침이 있는가 — "행복이었네 / 공포였네"처럼 서술격 조사 형태를 고른다(한글 음절이 아니면 받침 없음으로 본다).</summary>
        private static bool EndsWithBatchim(string text)
        {
            var last = text[text.Length - 1];
            return last >= '가' && last <= '힣' && (last - '가') % 28 != 0;
        }

        /// <param name="anyInCanvas">캔버스 아래 아무 트랜스폼(대사창을 찾는 출발점). null이면 콘솔에만 남긴다.</param>
        public static void Show(Transform anyInCanvas, PlayVerdict verdict)
        {
            var message = Message(verdict);
            if (message == null) return;

            var dialogue = anyInCanvas != null ? anyInCanvas.root.GetComponentInChildren<DialogueText>(true) : null;
            if (dialogue != null) dialogue.PlayTyped(message, isEvent: true);
            else Debug.Log(message);
        }
    }
}
