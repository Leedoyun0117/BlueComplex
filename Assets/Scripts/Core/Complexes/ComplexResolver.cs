using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>단서를 우선순위대로 통과시켜 최종 의미를 만든다.</summary>
    public sealed class ComplexResolver
    {
        private readonly ComplexBoard _board;

        public ComplexResolver(ComplexBoard board) => _board = board;

        public InterpretationResult Resolve(TagSet originalTags, IComplexFilter filter = null)
        {
            var original = originalTags.Clone();
            var working = originalTags.Clone();
            var context = new ComplexContext(working);
            var steps = new List<InterpretationStep>();

            foreach (var complex in _board.InEvaluationOrder())
            {
                if (filter != null && filter.ShouldIgnore(complex))
                {
                    steps.Add(new InterpretationStep(complex, false, working.Clone(),
                        TimeTag.None, Array.Empty<PersonTag>(), Array.Empty<EmotionTag>()));
                    continue;
                }

                var triggered = complex.Definition.TryInterpret(context);
                steps.Add(new InterpretationStep(
                    complex,
                    triggered,
                    working.Clone(),
                    context.MatchedTime,
                    context.MatchedPersons.ToList(),
                    context.MatchedEmotions.ToList()));
            }

            return new InterpretationResult(original, working, steps, _board.InPriorityOrder().ToList());
        }
    }
}
