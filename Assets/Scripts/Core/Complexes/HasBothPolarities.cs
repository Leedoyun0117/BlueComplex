using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>침체 감정과 흥분 감정이 동시에 하나 이상씩 붙어 있으면 성립. (스테이지 2 자기 분노 — "감정 태그 침체 + 흥분")</summary>
    public sealed class HasBothPolarities : IComplexCondition
    {
        private readonly IEmotionPolarityTable _polarityTable;
        public HasBothPolarities(IEmotionPolarityTable polarityTable) => _polarityTable = polarityTable;

        public bool Evaluate(ComplexContext context)
        {
            var emotions = context.Tags.Emotions.Keys.ToList();
            var hasDepressed = emotions.Any(e => _polarityTable.GetPolarity(e) == Polarity.Depressed);
            var hasExcited = emotions.Any(e => _polarityTable.GetPolarity(e) == Polarity.Excited);
            if (!hasDepressed || !hasExcited) return false;
            foreach (var emotion in emotions) context.MarkEmotion(emotion);
            return true;
        }
    }
}
