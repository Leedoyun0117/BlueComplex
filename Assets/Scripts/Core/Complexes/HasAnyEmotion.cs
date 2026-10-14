using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>지정된 감정 중 하나라도 있으면 성립. 매칭된 감정만 기록한다.</summary>
    public sealed class HasAnyEmotion : IComplexCondition
    {
        private static readonly EmotionTag[] AllEmotions = (EmotionTag[])System.Enum.GetValues(typeof(EmotionTag));

        private readonly IReadOnlyList<EmotionTag> _candidates;
        public HasAnyEmotion(params EmotionTag[] candidates) => _candidates = candidates;

        /// <summary>종류를 가리지 않고 감정 태그가 하나라도 붙어 있으면 성립. (예: "감정 태그가 붙어있다면")</summary>
        public static HasAnyEmotion Any() => new(AllEmotions);

        public bool Evaluate(ComplexContext context)
        {
            var matched = _candidates.Where(context.Tags.HasEmotion).ToList();
            if (matched.Count == 0) return false;
            foreach (var emotion in matched) context.MarkEmotion(emotion);
            return true;
        }
    }
}
