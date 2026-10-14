using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>해당 극성 감정 중 하나가 minStack개 이상 겹쳐 있으면 성립(슬픔 ×2 같은 중첩). 겹친 감정만 기록한다. (스테이지 3 낭떠러지 — "침체 감정 중첩")</summary>
    public sealed class HasStackedEmotionOfPolarity : IComplexCondition
    {
        private readonly Polarity _polarity;
        private readonly IEmotionPolarityTable _polarityTable;
        private readonly int _minStack;

        public HasStackedEmotionOfPolarity(Polarity polarity, IEmotionPolarityTable polarityTable, int minStack = 2)
        {
            _polarity = polarity;
            _polarityTable = polarityTable;
            _minStack = minStack;
        }

        public bool Evaluate(ComplexContext context)
        {
            var matched = context.Tags.Emotions
                .Where(pair => _polarityTable.GetPolarity(pair.Key) == _polarity && pair.Value >= _minStack)
                .Select(pair => pair.Key)
                .ToList();
            if (matched.Count == 0) return false;
            foreach (var emotion in matched) context.MarkEmotion(emotion);
            return true;
        }
    }
}
