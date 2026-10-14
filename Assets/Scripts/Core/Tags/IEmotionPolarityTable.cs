using System.Collections.Generic;

namespace BlueComplex.Core.Tags
{
    public interface IEmotionPolarityTable
    {
        Polarity GetPolarity(EmotionTag emotion);
    }
}
