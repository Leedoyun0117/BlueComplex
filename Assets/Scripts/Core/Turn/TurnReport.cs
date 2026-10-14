using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Turn
{
    /// <summary>한 턴이 만들어낸 모든 결과. UI 연출은 이걸 그대로 재생하면 된다.</summary>
    public sealed class TurnReport
    {
        public int Turn { get; }
        public int Quarter { get; }
        public int TurnInQuarter { get; }

        /// <summary>낸 단서. 손패가 비어 넘어간 턴(<see cref="IsPass"/>)이면 null.</summary>
        public ClueDefinition Clue { get; }

        /// <summary>넘어간 턴이면 null.</summary>
        public InterpretationResult Interpretation { get; }

        /// <summary>넘어간 턴이면 null.</summary>
        public TagSet FinalTags { get; }

        public int HeartbeatDelta { get; }
        public int HeartbeatValue { get; }
        public ComplexInstance SpawnedComplex { get; }

        /// <summary>컴플렉스가 나타날 차례였지만 이미 최대 중첩이라 붙지 못했다 — 특수 특성(고기능 우울증/과흥분) 발현 조건.
        /// 침체 구간이었다면 고기능 우울증, 흥분 구간이었다면 과흥분 쪽이다(<see cref="HeartbeatValue"/>로 구분).</summary>
        public bool ComplexOverflowed { get; }

        /// <summary>이 턴에 컴플렉스 초과로 새로 붙은 특수 특성(없으면 null). 초과했어도 그 구간 쪽 특수 특성이 카탈로그에 없으면 null.</summary>
        public TraitDefinition SpecialTraitGranted { get; }

        /// <summary>이 턴의 결과에 "처음" 나타나는 특성들(발현 순서) — 지난 턴 결과 이후 이번 턴 결과까지 새로 붙은 것: 아이템 사용으로 발현한 일반 특성과 이 턴에 컴플렉스 초과로
        /// 붙은 특수 특성. 이미 걸려 있던 특성(지속 턴이 남은 채 이어지는 것)은 들어 있지 않다. 결과 화면이 특성 이름 태그를 띄우는 기준이다.</summary>
        public IReadOnlyList<TraitDefinition> TraitsManifested { get; }
        public StageOutcome Outcome { get; }

        /// <summary>이 턴이 쿼터의 마지막 턴이라 키를 판정했다면 그 결과. 아니면 null.</summary>
        public KeyJudgement? KeyResult { get; }

        /// <summary>손패가 비어 단서 없이 시간만 흐른 턴인가.</summary>
        public bool IsPass => Clue == null;

        public TurnReport(int turn,
                          int quarter,
                          int turnInQuarter,
                          ClueDefinition clue,
                          InterpretationResult interpretation,
                          TagSet finalTags,
                          int heartbeatDelta,
                          int heartbeatValue,
                          ComplexInstance spawnedComplex,
                          StageOutcome outcome,
                          KeyJudgement? keyResult,
                          bool complexOverflowed = false,
                          TraitDefinition specialTraitGranted = null,
                          IReadOnlyList<TraitDefinition> traitsManifested = null)
        {
            Turn = turn;
            Quarter = quarter;
            TurnInQuarter = turnInQuarter;
            Clue = clue;
            Interpretation = interpretation;
            FinalTags = finalTags;
            HeartbeatDelta = heartbeatDelta;
            HeartbeatValue = heartbeatValue;
            SpawnedComplex = spawnedComplex;
            ComplexOverflowed = complexOverflowed;
            SpecialTraitGranted = specialTraitGranted;
            TraitsManifested = traitsManifested ?? Array.Empty<TraitDefinition>();
            Outcome = outcome;
            KeyResult = keyResult;
        }
    }
}
