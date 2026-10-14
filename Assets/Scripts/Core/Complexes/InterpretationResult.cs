using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public sealed class InterpretationResult
    {
        public TagSet Original { get; }
        public TagSet Final { get; }
        /// <summary>해석한 순서(평가 순서)의 단계들.</summary>
        public IReadOnlyList<InterpretationStep> Steps { get; }

        /// <summary>해석 당시 보드의 컴플렉스를 화면에 보이는 순서(<see cref="ComplexBoard.InPriorityOrder"/>)로 늘어놓은 것. 뇌 영역 배치처럼 "어느 컴플렉스가 어느 칸인가"는 이 순서를 따라야
        /// 평가 순서가 달라져도 칸이 뒤바뀌지 않는다.</summary>
        public IReadOnlyList<ComplexInstance> DisplayOrder { get; }

        public InterpretationResult(TagSet original, TagSet final, IReadOnlyList<InterpretationStep> steps, IReadOnlyList<ComplexInstance> displayOrder = null)
        {
            Original = original;
            Final = final;
            Steps = steps;
            DisplayOrder = displayOrder ?? steps.Select(step => step.Complex).ToList();
        }
    }
}
