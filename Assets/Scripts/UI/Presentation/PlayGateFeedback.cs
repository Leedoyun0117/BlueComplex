using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 단서 제시가 게이트(<see cref="IPlayGate"/>)에 걸렸을 때의 안내. 코어는 "왜 거절됐는지"(사유·나왔을 감정)만 알려 주고, 문장은 <see cref="TutorialGuideContent.RejectionLine"/>이 만든다.
    /// 안내가 진행 중이면 청장의 말풍선(<see cref="TutorialGuide"/>)에 띄우고, 아니면 유키의 대사창(<see cref="DialogueText"/>)에 이벤트 글자색으로 띄운다.
    /// 카드는 손패로 되돌아가고(드롭 영역이 받아들이지 않는다) 턴은 진행되지 않는다.
    /// </summary>
    public static class PlayGateFeedback
    {
        /// <param name="anyInCanvas">캔버스 아래 아무 트랜스폼(가이드·대사창을 찾는 출발점). null이면 콘솔에만 남긴다.</param>
        /// <param name="turn">지금 진행 중인 턴(원문이 턴마다 다른 문장을 쓴다).</param>
        public static void Show(Transform anyInCanvas, PlayVerdict verdict, int turn)
        {
            var guide = anyInCanvas != null ? TutorialGuide.Find(anyInCanvas.root) : null;
            if (guide != null && guide.TryShowFeedback(verdict, turn)) return;

            var message = TutorialGuideContent.RejectionLine(verdict, turn, KoreanLabels.Emotion);
            if (message == null) return;

            var dialogue = anyInCanvas != null ? anyInCanvas.root.GetComponentInChildren<DialogueText>(true) : null;
            if (dialogue != null) dialogue.PlayTyped(message, isEvent: true);
            else Debug.Log(message);
        }
    }
}
