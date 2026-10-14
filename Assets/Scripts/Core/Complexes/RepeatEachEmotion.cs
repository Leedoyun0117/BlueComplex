using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>지금 붙어 있는 감정 종류마다 하나씩 더 붙인다. (예: 되새김 — 과거의 감정을 한 번 더 느낀다)</summary>
    public sealed class RepeatEachEmotion : IComplexEffect
    {
        public void Apply(ComplexContext context)
        {
            foreach (var emotion in context.Tags.Emotions.Keys.ToList())
                context.Tags.AddEmotion(emotion);
        }
    }
}
