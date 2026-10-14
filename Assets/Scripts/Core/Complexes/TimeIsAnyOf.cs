using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>지정된 시간 중 하나라도 붙어 있으면 성립. (예: 회피 — '현재 또는 미래') 여러 개가 붙어 있으면 후보 순서상 첫 번째만 기록한다.</summary>
    public sealed class TimeIsAnyOf : IComplexCondition
    {
        private readonly IReadOnlyList<TimeTag> _candidates;
        public TimeIsAnyOf(params TimeTag[] candidates) => _candidates = candidates;

        public bool Evaluate(ComplexContext context)
        {
            foreach (var candidate in _candidates)
            {
                if (!context.Tags.HasTime(candidate)) continue;
                context.MarkTime(candidate);
                return true;
            }

            return false;
        }
    }
}
