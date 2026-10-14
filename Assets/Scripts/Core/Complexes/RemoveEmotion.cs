using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>감정 태그를 amount개 뺀다. 없으면 아무 일도 없다. (스테이지 2 과거 부정 "슬픔 -1", 피해 망상 "행복 -1")</summary>
    public sealed class RemoveEmotion : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly int _amount;

        public RemoveEmotion(EmotionTag emotion, int amount = 1)
        {
            _emotion = emotion;
            _amount = amount;
        }

        public void Apply(ComplexContext context) => context.Tags.RemoveEmotion(_emotion, _amount);
    }
}
