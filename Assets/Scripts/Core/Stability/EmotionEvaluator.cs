using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Stability
{
    public interface IEmotionEvaluator
    {
        int Evaluate(TagSet finalTags);
    }

    /// <summary>흥분 태그 +1, 침체 태그 -1. 중복 감정은 개수만큼 누적된다.</summary>
    public sealed class EmotionEvaluator : IEmotionEvaluator
    {
        private readonly IEmotionPolarityTable _polarityTable;

        public EmotionEvaluator(IEmotionPolarityTable polarityTable) => _polarityTable = polarityTable;

        public int Evaluate(TagSet finalTags) =>
            finalTags.EnumerateEmotionsFlat().Sum(e => (int)_polarityTable.GetPolarity(e));
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
