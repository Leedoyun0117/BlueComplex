using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Complexes
{
    /// <summary>컴플렉스 저작 데이터. 조건이 모두 성립하면 효과를 순서대로 적용한다.</summary>
    public sealed class ComplexDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        /// <summary>UI에 노출되는 설명. 공략이 아니라 성향만 알려준다.</summary>
        public string Description { get; }
        public int DefaultDuration { get; }

        /// <summary>
        /// 지정하면 이 컴플렉스는 해석(평가)할 때 그 id의 컴플렉스 바로 뒤에 온다(둘 다 붙어 있을 때). 다른 컴플렉스끼리의 순서와 화면에 보이는 순서(<see cref="ComplexBoard.InPriorityOrder"/>)는 그대로다.
        /// 앞 컴플렉스가 만든 태그를 조건으로 쓰는 컴플렉스를 위한 내부 규칙이다(스테이지 2 가족애 ← 전이). 앵커가 붙어 있지 않으면 보통의 우선순위 자리에서 평가된다.
        /// </summary>
        public string EvaluatedRightAfterId { get; }

        private readonly IReadOnlyList<IComplexCondition> _conditions;
        private readonly IReadOnlyList<IComplexEffect> _effects;

        public ComplexDefinition(string id,
                                 string displayName,
                                 string description,
                                 int defaultDuration,
                                 IReadOnlyList<IComplexCondition> conditions,
                                 IReadOnlyList<IComplexEffect> effects,
                                 string evaluatedRightAfterId = null)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            DefaultDuration = defaultDuration;
            _conditions = conditions;
            _effects = effects;
            EvaluatedRightAfterId = evaluatedRightAfterId;
        }

        /// <summary>조건 판정 후 성립하면 효과 적용. 발동 여부를 반환한다.</summary>
        public bool TryInterpret(ComplexContext context)
        {
            context.ClearMatches();

            // 조건 중 하나라도 실패하면 이번 해석에서 참조한 정보는 무효로 본다.
            if (_conditions.Any(condition => !condition.Evaluate(context)))
            {
                context.ClearMatches();
                return false;
            }

            foreach (var effect in _effects) effect.Apply(context);
            return true;
        }
    }

    /// <summary>실제 상대에게 붙어 있는 컴플렉스. 우선순위와 남은 턴을 가진다.</summary>
    public sealed class ComplexInstance
    {
        public ComplexDefinition Definition { get; }
        public int Priority { get; }
        public int RemainingTurns { get; private set; }

        public bool IsExpired => RemainingTurns <= 0;

        public ComplexInstance(ComplexDefinition definition, int priority, int? duration = null)
        {
            Definition = definition;
            Priority = priority;
            RemainingTurns = duration ?? definition.DefaultDuration;
        }

        public void Tick() => RemainingTurns--;

        /// <summary>남은 지속 시간을 절반으로 줄인다(올림 — 3턴이면 2턴). 남은 턴이 1이면 그대로다: 절반으로 줄이다 0이 되어 곧바로 사라지는 일은 없다. 줄었는지를 돌려준다.</summary>
        public bool HalveRemainingTurns()
        {
            var halved = (RemainingTurns + 1) / 2;
            if (halved >= RemainingTurns) return false;

            RemainingTurns = halved;
            return true;
        }
    }
}
