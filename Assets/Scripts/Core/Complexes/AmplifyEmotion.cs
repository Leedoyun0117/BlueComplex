using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>이미 존재하는 감정만 증폭한다. (예: 슬픔이 있다면 슬픔 +2)</summary>
    public sealed class AmplifyEmotion : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly int _amount;

        public AmplifyEmotion(EmotionTag emotion, int amount)
        {
            _emotion = emotion;
            _amount = amount;
        }

        public void Apply(ComplexContext context)
        {
            if (!context.Tags.HasEmotion(_emotion)) return;
            context.Tags.AddEmotion(_emotion, _amount);
        }
    }
}
