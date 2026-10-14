using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>침체/흥분 극성에 해당하는 감정이 하나라도 있으면 성립.</summary>
    public sealed class HasEmotionOfPolarity : IComplexCondition
    {
        private readonly Polarity _polarity;
        private readonly IEmotionPolarityTable _polarityTable;

        public HasEmotionOfPolarity(Polarity polarity, IEmotionPolarityTable polarityTable)
        {
            _polarity = polarity;
            _polarityTable = polarityTable;
        }

        public bool Evaluate(ComplexContext context)
        {
            var matched = context.Tags.Emotions.Keys
                .Where(e => _polarityTable.GetPolarity(e) == _polarity)
                .ToList();
            if (matched.Count == 0) return false;
            foreach (var emotion in matched) context.MarkEmotion(emotion);
            return true;
        }
    }
}
