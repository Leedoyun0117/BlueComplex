using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>조건이 매칭한 감정들을 지정 감정으로 변환. (예: 행복/사랑 → 혐오)</summary>
    public sealed class ConvertMatchedEmotionsTo : IComplexEffect
    {
        private readonly EmotionTag _target;
        public ConvertMatchedEmotionsTo(EmotionTag target) => _target = target;

        public void Apply(ComplexContext context)
        {
            foreach (var emotion in context.MatchedEmotions)
            {
                if (emotion == _target) continue;
                context.Tags.ReplaceEmotion(emotion, _target);
            }
        }
    }
}
