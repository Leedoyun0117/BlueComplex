using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>청장의 안내 한 줄과 그 줄이 무엇을 가리키고 언제 끝나는지.</summary>
    public sealed class TutorialGuideStep
    {
        public string Id { get; }
        public string Line { get; }
        public GuideTarget Target { get; }
        public GuideAdvance Advance { get; }

        /// <summary><see cref="GuideAdvance.TurnResolved"/>일 때 몇 번째 턴이 결산되면 끝나는지.</summary>
        public int WaitTurn { get; }

        /// <summary>이 줄이 나오는 순간 함께 터지는 효과. 대부분 없음.</summary>
        public GuideEffect Effect { get; }

        /// <summary>이 단계가 끝나려면 <see cref="Advance"/> 사건이 몇 번 일어나야 하는지(예: 아이템 둘을 모두 써야 넘어간다). 기본 1.</summary>
        public int RequiredCount { get; }

        public TutorialGuideStep(string id, string line, GuideTarget target = GuideTarget.None,
                                 GuideAdvance advance = GuideAdvance.Read, int waitTurn = 0, GuideEffect effect = GuideEffect.None,
                                 int requiredCount = 1)
        {
            Effect = effect;
            Id = id;
            Line = line;
            Target = target;
            Advance = advance;
            WaitTurn = waitTurn;
            RequiredCount = Math.Max(1, requiredCount);
        }
    }
}
