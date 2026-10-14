using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>침체 태그와 흥분 태그의 개수(중첩 포함)가 서로 다르면 성립 — 어느 쪽이 더 많은지는 <see cref="EmotionPolarityCounts"/>로 효과가 다시 가른다. (스테이지 3 자아 부정)</summary>
    public sealed class HasDominantPolarity : IComplexCondition
    {
        private readonly IEmotionPolarityTable _polarityTable;
        public HasDominantPolarity(IEmotionPolarityTable polarityTable) => _polarityTable = polarityTable;

        public bool Evaluate(ComplexContext context)
        {
            var (depressed, excited) = EmotionPolarityCounts.Of(context.Tags, _polarityTable);
            if (depressed == excited) return false;
            foreach (var emotion in context.Tags.Emotions.Keys) context.MarkEmotion(emotion);
            return true;
        }
    }
}
