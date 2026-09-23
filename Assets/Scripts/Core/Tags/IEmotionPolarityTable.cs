using System.Collections.Generic;

namespace BlueComplex.Core.Tags
{
    public interface IEmotionPolarityTable
    {
        Polarity GetPolarity(EmotionTag emotion);
    }

    public sealed class DefaultEmotionPolarityTable : IEmotionPolarityTable
    {
        private static readonly Dictionary<EmotionTag, Polarity> Table = new()
        {
            { EmotionTag.Sadness, Polarity.Depressed },
            { EmotionTag.Disgust, Polarity.Depressed },
            { EmotionTag.Fear, Polarity.Depressed },
            { EmotionTag.Happiness, Polarity.Excited },
            { EmotionTag.Love, Polarity.Excited },
            { EmotionTag.Anger, Polarity.Excited }
        };

        public Polarity GetPolarity(EmotionTag emotion) => Table[emotion];
    }
}
