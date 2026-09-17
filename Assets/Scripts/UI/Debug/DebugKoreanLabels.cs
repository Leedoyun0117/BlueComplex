using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>디버그 플레이 화면 전용 한글 라벨. 정식 UI 텍스트와는 별도로 관리한다.</summary>
    internal static class DebugKoreanLabels
    {
        public static string Time(TimeTag time) => time switch
        {
            TimeTag.Past => "과거",
            TimeTag.Present => "현재",
            TimeTag.Future => "미래",
            _ => "-"
        };

        public static string Person(PersonTag person) => person switch
        {
            PersonTag.Family => "가족",
            PersonTag.Other => "타인",
            PersonTag.Friend => "친구",
            _ => "-"
        };

        public static string Emotion(EmotionTag emotion) => emotion switch
        {
            EmotionTag.Sadness => "슬픔",
            EmotionTag.Disgust => "혐오",
            EmotionTag.Fear => "공포",
            EmotionTag.Happiness => "행복",
            EmotionTag.Love => "사랑",
            EmotionTag.Anger => "분노",
            _ => emotion.ToString()
        };

        public static string State(HeartbeatState state) => state switch
        {
            HeartbeatState.Fatal => "즉시 패배",
            HeartbeatState.VeryDepressed => "매우 침체",
            HeartbeatState.Depressed => "침체",
            HeartbeatState.Stable => "안정",
            HeartbeatState.Excited => "흥분",
            HeartbeatState.VeryExcited => "매우 흥분",
            _ => "-"
        };

        public static string Censorship(CensorshipLevel level) => level switch
        {
            CensorshipLevel.None => "없음",
            CensorshipLevel.Partial => "일부",
            CensorshipLevel.Full => "전체",
            _ => "-"
        };

        public static string Outcome(BlueComplex.Core.Turn.StageOutcome outcome) => outcome switch
        {
            BlueComplex.Core.Turn.StageOutcome.Cleared => "클리어",
            BlueComplex.Core.Turn.StageOutcome.Failed => "실패",
            _ => "진행 중"
        };
    }
}
