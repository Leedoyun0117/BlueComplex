using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>
    /// 해당 극성 감정 태그를 하나하나 한 개씩 더 붙인다 — 중첩된 태그도 낱개마다 세므로 슬픔 ×2는 ×4가 된다.
    /// (스테이지 2 자책 — "동일한 침체 감정 태그 모두 +1씩 추가 (중복 포함)")
    /// </summary>
    public sealed class RepeatEachEmotionOfPolarity : IComplexEffect
    {
        private readonly Polarity _polarity;
        private readonly IEmotionPolarityTable _polarityTable;

        public RepeatEachEmotionOfPolarity(Polarity polarity, IEmotionPolarityTable polarityTable)
        {
            _polarity = polarity;
            _polarityTable = polarityTable;
        }

        public void Apply(ComplexContext context)
        {
            foreach (var pair in context.Tags.Emotions.ToList())
            {
                if (_polarityTable.GetPolarity(pair.Key) != _polarity) continue;
                context.Tags.AddEmotion(pair.Key, pair.Value);
            }
        }
    }
}
