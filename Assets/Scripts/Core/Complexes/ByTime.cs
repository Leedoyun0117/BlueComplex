using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>시간 태그에 따라 갈라지는 효과. 지정한 시간이 붙어 있으면 <c>ifPresent</c>, 아니면 <c>otherwise</c>. (스테이지 2 사고 과다)</summary>
    public sealed class ByTime : IComplexEffect
    {
        private readonly TimeTag _time;
        private readonly IComplexEffect _ifPresent;
        private readonly IComplexEffect _otherwise;

        public ByTime(TimeTag time, IComplexEffect ifPresent, IComplexEffect otherwise)
        {
            _time = time;
            _ifPresent = ifPresent;
            _otherwise = otherwise;
        }

        public void Apply(ComplexContext context) =>
            (context.Tags.HasTime(_time) ? _ifPresent : _otherwise).Apply(context);
    }
}
