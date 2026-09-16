using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Stability
{
    public interface IEmotionEvaluator
    {
        int Evaluate(TagSet finalTags);
    }

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

    /// <summary>특성(예: 예민)의 감도 배율을 이동량에 곱한다.</summary>
    public sealed class TraitAwareEmotionEvaluator : IEmotionEvaluator
    {
        private readonly IEmotionEvaluator _inner;
        private readonly TraitBoard _traits;

        public TraitAwareEmotionEvaluator(IEmotionEvaluator inner, TraitBoard traits)
        {
            _inner = inner;
            _traits = traits;
        }

        public int Evaluate(TagSet finalTags) => _inner.Evaluate(finalTags) * _traits.SensitivityMultiplier;
    }
}
