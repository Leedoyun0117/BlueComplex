using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>붙어 있는 감정 태그를 전부 배로 만든다(중첩 포함, 슬픔 ×2 → ×4). (스테이지 2 사고 과다 "감정 * 2")</summary>
    public sealed class DoubleEmotions : IComplexEffect
    {
        public void Apply(ComplexContext context)
        {
            foreach (var pair in context.Tags.Emotions.ToList())
                context.Tags.AddEmotion(pair.Key, pair.Value);
        }
    }
}
