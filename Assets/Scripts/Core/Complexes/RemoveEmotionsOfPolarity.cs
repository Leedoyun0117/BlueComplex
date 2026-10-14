using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>해당 극성 감정 태그를 중첩까지 전부 지운다. (스테이지 3 부모 애착 — "침체 감정 모두 제거")</summary>
    public sealed class RemoveEmotionsOfPolarity : IComplexEffect
    {
        private readonly Polarity _polarity;
        private readonly IEmotionPolarityTable _polarityTable;

        public RemoveEmotionsOfPolarity(Polarity polarity, IEmotionPolarityTable polarityTable)
        {
            _polarity = polarity;
            _polarityTable = polarityTable;
        }

        public void Apply(ComplexContext context)
        {
            foreach (var pair in context.Tags.Emotions.ToList())
            {
                if (_polarityTable.GetPolarity(pair.Key) != _polarity) continue;
                context.Tags.RemoveEmotion(pair.Key, pair.Value);
            }
        }
    }
}
