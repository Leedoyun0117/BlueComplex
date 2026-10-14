using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>시간 태그 하나를 뗀다. 없으면 아무 일도 없다. (스테이지 3 시간 혼합 "과거 -1")</summary>
    public sealed class RemoveTime : IComplexEffect
    {
        private readonly TimeTag _time;
        public RemoveTime(TimeTag time) => _time = time;

        public void Apply(ComplexContext context) => context.Tags.RemoveTime(_time);
    }
}
