using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Traits
{
    /// <summary>
    /// 특성 한 종류의 데이터. 효과는 숫자 필드로만 적혀 있고 <see cref="TraitBoard"/>와 평가기가 그 값을 모아 쓴다 — 특성 이름으로 갈라지는 코드는 없다.
    /// 기본값(1, false)은 "영향 없음"이라 필요한 필드만 채우면 된다.
    /// </summary>
    public sealed class TraitDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public TraitKind Kind { get; }

        /// <summary>일반 특성이 붙어 있는 턴 수(이 값 이후 만료). 특수 특성은 무시된다 — 치유될 때까지 간다.</summary>
        public int DefaultDuration { get; init; } = 1;

        /// <summary>심박수 변화량에 곱하는 배율(예민 ×3, 무력 ×1/2). 여러 특성이면 곱한다.</summary>
        public double HeartbeatMultiplier { get; init; } = 1.0;

        /// <summary>단서의 원래 감정 개수에 곱하는 배율(과대 망상 ×2). 컴플렉스 해석 <b>전</b>에 적용된다.</summary>
        public int EmotionCountMultiplier { get; init; } = 1;

        /// <summary>침체 감정과 흥분 감정을 반대로 받아들인다(환각) — 심박수가 원래와 반대로 변한다.</summary>
        public bool InvertsPolarity { get; init; }

        /// <summary>흥분 감정 한 개의 영향력에 곱하는 배율(고기능 우울증 ×1/2). 환각이 있으면 뒤집힌 뒤의 극성 기준이다.</summary>
        public double ExcitedInfluence { get; init; } = 1.0;

        /// <summary>침체 감정 한 개의 영향력에 곱하는 배율(과흥분 ×1/2).</summary>
        public double DepressedInfluence { get; init; } = 1.0;

        /// <summary>특수 특성이 어느 쪽 구간에서 컴플렉스가 넘칠 때 붙는지(고기능 우울증 = 침체, 과흥분 = 흥분). 일반 특성은 null.</summary>
        public Polarity? OverflowSide { get; init; }

        public TraitDefinition(string id, string displayName, string description, TraitKind kind)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Kind = kind;
        }
    }
}
