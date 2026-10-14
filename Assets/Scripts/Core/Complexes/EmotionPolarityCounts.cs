using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>단서의 침체 감정 태그 수와 흥분 감정 태그 수(중첩 포함).</summary>
    public static class EmotionPolarityCounts
    {
        public static (int Depressed, int Excited) Of(TagSet tags, IEmotionPolarityTable polarityTable)
        {
            var depressed = 0;
            var excited = 0;
            foreach (var pair in tags.Emotions)
            {
                if (polarityTable.GetPolarity(pair.Key) == Polarity.Depressed) depressed += pair.Value;
                else excited += pair.Value;
            }

            return (depressed, excited);
        }
    }
}
