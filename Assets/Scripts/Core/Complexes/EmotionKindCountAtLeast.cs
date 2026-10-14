using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>서로 다른 감정 종류가 count개 이상 붙어 있으면 성립. 같은 감정이 중첩된 것(슬픔 ×2)은 한 종류로 센다. (스테이지 2 복합 감정)</summary>
    public sealed class EmotionKindCountAtLeast : IComplexCondition
    {
        private readonly int _count;
        public EmotionKindCountAtLeast(int count) => _count = count;

        public bool Evaluate(ComplexContext context)
        {
            if (context.Tags.Emotions.Count < _count) return false;
            foreach (var emotion in context.Tags.Emotions.Keys) context.MarkEmotion(emotion);
            return true;
        }
    }
}
