using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>침체 태그가 더 많으면 <c>ifDepressed</c>, 흥분 태그가 더 많으면 <c>ifExcited</c>, 같으면 아무것도 안 한다(개수는 중첩 포함, 효과 적용 전 값). (스테이지 3 자아 부정)</summary>
    public sealed class ByDominantPolarity : IComplexEffect
    {
        private readonly IEmotionPolarityTable _polarityTable;
        private readonly IComplexEffect _ifDepressed;
        private readonly IComplexEffect _ifExcited;

        public ByDominantPolarity(IEmotionPolarityTable polarityTable, IComplexEffect ifDepressed, IComplexEffect ifExcited)
        {
            _polarityTable = polarityTable;
            _ifDepressed = ifDepressed;
            _ifExcited = ifExcited;
        }

        public void Apply(ComplexContext context)
        {
            var (depressed, excited) = EmotionPolarityCounts.Of(context.Tags, _polarityTable);
            if (depressed > excited) _ifDepressed.Apply(context);
            else if (excited > depressed) _ifExcited.Apply(context);
        }
    }
}
