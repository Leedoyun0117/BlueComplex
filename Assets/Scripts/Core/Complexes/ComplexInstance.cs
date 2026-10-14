using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Complexes
{
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
