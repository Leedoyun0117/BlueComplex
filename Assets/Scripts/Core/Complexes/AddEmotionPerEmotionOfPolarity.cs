using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>
    /// 해당 극성 감정 태그의 개수(중첩 포함)에 배수를 곱한 만큼 지정 감정을 더한다. 개수는 효과를 적용하기 전 값으로 센다.
    /// (스테이지 2 자기 분노 — "감정 태그 '행복'을 단서의 흥분 태그 갯수 * 2만큼 추가")
    /// </summary>
    public sealed class AddEmotionPerEmotionOfPolarity : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly Polarity _polarity;
        private readonly int _multiplier;
        private readonly IEmotionPolarityTable _polarityTable;

        public AddEmotionPerEmotionOfPolarity(EmotionTag emotion, Polarity polarity, int multiplier, IEmotionPolarityTable polarityTable)
        {
            _emotion = emotion;
            _polarity = polarity;
            _multiplier = multiplier;
            _polarityTable = polarityTable;
        }

        public void Apply(ComplexContext context)
        {
            var count = context.Tags.Emotions
                .Where(pair => _polarityTable.GetPolarity(pair.Key) == _polarity)
                .Sum(pair => pair.Value);
            if (count > 0) context.Tags.AddEmotion(_emotion, count * _multiplier);
        }
    }
}
