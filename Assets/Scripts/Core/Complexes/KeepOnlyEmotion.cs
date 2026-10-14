using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>지정한 감정을 뺀 나머지 감정 태그를 전부 지운다. (스테이지 2 과대 해석 — "다른 감정 태그 모두 제거")</summary>
    public sealed class KeepOnlyEmotion : IComplexEffect
    {
        private readonly EmotionTag _keep;
        public KeepOnlyEmotion(EmotionTag keep) => _keep = keep;

        public void Apply(ComplexContext context)
        {
            foreach (var emotion in context.Tags.Emotions.Keys.ToList())
            {
                if (emotion == _keep) continue;
                context.Tags.RemoveEmotion(emotion, context.Tags.CountOf(emotion));
            }
        }
    }
}
