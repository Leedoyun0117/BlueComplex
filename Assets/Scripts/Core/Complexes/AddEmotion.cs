using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public sealed class AddEmotion : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly int _amount;

        public AddEmotion(EmotionTag emotion, int amount = 1)
        {
            _emotion = emotion;
            _amount = amount;
        }

        public void Apply(ComplexContext context) => context.Tags.AddEmotion(_emotion, _amount);
    }
}
