using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>시간 태그를 하나 더 붙인다. 기존 시간 태그는 그대로다. (스테이지 2 과한 기대 — "시간 태그 '미래' 추가")</summary>
    public sealed class AddTime : IComplexEffect
    {
        private readonly TimeTag _time;
        public AddTime(TimeTag time) => _time = time;

        public void Apply(ComplexContext context) => context.Tags.AddTime(_time);
    }
}
