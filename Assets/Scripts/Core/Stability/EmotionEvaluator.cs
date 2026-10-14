using System;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Stability
{
    /// <summary>흥분 태그 +TagMagnitude, 침체 태그 -TagMagnitude. 중복 감정은 개수만큼 누적된다.</summary>
    public sealed class EmotionEvaluator : IEmotionEvaluator
    {
        /// <summary>태그 하나가 심박수에 미치는 영향력. 기본값은 200 스케일 기준 ±10.</summary>
        public const int DefaultTagMagnitude = 10;

        private readonly IEmotionPolarityTable _polarityTable;
        private readonly int _tagMagnitude;

        public EmotionEvaluator(IEmotionPolarityTable polarityTable, int tagMagnitude = DefaultTagMagnitude)
        {
            _polarityTable = polarityTable;
            _tagMagnitude = tagMagnitude;
        }

        public int Evaluate(TagSet finalTags) =>
            finalTags.EnumerateEmotionsFlat().Sum(e => (int)_polarityTable.GetPolarity(e)) * _tagMagnitude;
    }
}
