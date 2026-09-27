using System.Collections.Generic;
using BlueComplex.Core.Clues;

namespace BlueComplex.UI.Presentation
{
    /// <summary>카드 한 장을 화면에 그리기 위해 필요한 텍스트를 전부 구조화해서 담는다.</summary>
    public readonly struct ClueCardViewModel
    {
        public readonly string Title;
        public readonly string TimeText;
        public readonly IReadOnlyList<string> PersonTexts;
        public readonly IReadOnlyList<string> EmotionTexts;
        public readonly string StoryText;

        public ClueCardViewModel(string title, string timeText, IReadOnlyList<string> personTexts,
            IReadOnlyList<string> emotionTexts, string storyText)
        {
            Title = title;
            TimeText = timeText;
            PersonTexts = personTexts;
            EmotionTexts = emotionTexts;
            StoryText = storyText;
        }
    }

    /// <summary>
    /// 해금 상태(ClueKnowledgeLedger)를 반영해 단서 카드 뷰모델을 만든다. 심박수 구간과 무관하게 스토리는 항상 보인다.
    /// Assets/Scripts/UI/Debug/ClueCardFormatter.cs와 같은 판정 로직이지만,
    /// 카드 UI가 요소별 텍스트 필드를 따로 가지므로 문자열 한 덩어리 대신 구조화된 값을 돌려준다.
    /// 표시만 담당하고 판정에는 관여하지 않는다.
    /// </summary>
    public static class ClueCardFormatter
    {
        public static ClueCardViewModel Format(ClueInstance card, ClueKnowledgeLedger ledger) => Format(card.Definition, ledger);

        /// <summary>손패 카드가 아닌 단서 정의 그대로 — 시작 화면의 단서 노트가 쓴다. 가리는 규칙(<see cref="ClueNote.Reveal"/>)은 카드와 같다.</summary>
        public static ClueCardViewModel Format(ClueDefinition def, ClueKnowledgeLedger ledger)
        {
            var reveal = ClueNote.Reveal(def, ledger);

            var timeText = reveal.TimeRevealed ? KoreanLabels.Times(def.Times) : "?";
            var personTexts = new string[reveal.Persons.Count];
            for (var i = 0; i < personTexts.Length; i++)
            {
                var (person, revealed) = reveal.Persons[i];
                personTexts[i] = revealed ? KoreanLabels.Person(person) : "?";
            }

            var emotionTexts = new string[reveal.Emotions.Count];
            for (var i = 0; i < emotionTexts.Length; i++)
            {
                var (emotion, revealed) = reveal.Emotions[i];
                emotionTexts[i] = revealed ? KoreanLabels.Emotion(emotion) : "?";
            }

            return new ClueCardViewModel(def.DisplayName, timeText, personTexts, emotionTexts, $"\"{def.Story}\"");
        }
    }
}
