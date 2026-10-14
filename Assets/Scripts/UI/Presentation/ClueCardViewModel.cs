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
}
