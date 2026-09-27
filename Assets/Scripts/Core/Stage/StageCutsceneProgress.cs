using System;
using System.Collections.Generic;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 스테이지 종료 컷신을 "시도마다 하나씩" 보여 주는 진행 기록. 실패로 끝난 시도는 그 스테이지의 컷신 단계
    /// (<see cref="StageCutscenePlan"/>) 중 아직 안 본 첫 단계 하나만 재생한다(<see cref="Take"/>) — N번째 실패 시도에서 N번째 단계.
    /// 반면 클리어해서 다음 단계로 넘어가는 시점에는 아직 못 본 단계를 전부 몰아서 재생한다(<see cref="TakeRemaining"/>) — 반복 실패로
    /// 못 봤던 컷신이 클리어 순간에 한꺼번에 나온다. 단계를 다 본 뒤의 시도에는 컷신이 없다. 컷신이 없는 종료(스테이지 3 실패)는 시도로 세지 않는다.
    /// 기록은 앱을 실행하는 동안만 유지된다(스테이지마다 따로 — 다음 스테이지로 넘어가면 새 스테이지 번호가 0부터 다시 센다). Unity 의존 없음 — EditMode 테스트로 검증한다.
    /// </summary>
    public sealed class StageCutsceneProgress
    {
        private readonly Dictionary<int, int> _shown = new();

        /// <summary>이 스테이지에서 지금까지 재생한 컷신 단계 수.</summary>
        public int Shown(int stageNumber) => _shown.TryGetValue(stageNumber, out var count) ? count : 0;

        /// <summary>이번 스테이지 종료(실패)에 재생할 컷신 단계 하나(없으면 빈 목록)를 돌려주고 진행을 한 칸 넘긴다.</summary>
        public IReadOnlyList<int[]> Take(int stageNumber, StageOutcome outcome)
        {
            var steps = StageCutscenePlan.For(stageNumber, outcome);
            if (steps.Count == 0) return steps;

            var shown = Shown(stageNumber);
            if (shown >= steps.Count) return Array.Empty<int[]>();

            _shown[stageNumber] = shown + 1;
            return new[] { steps[shown] };
        }

        /// <summary>클리어해서 다음 단계로 넘어갈 때 아직 못 본 컷신 단계를 전부(하나도 안 봤으면 처음부터, 몇 개 봤으면 그 뒤부터) 돌려주고 진행을 끝까지 채운다.
        /// 이미 다 봤으면 빈 목록.</summary>
        public IReadOnlyList<int[]> TakeRemaining(int stageNumber, StageOutcome outcome)
        {
            var steps = StageCutscenePlan.For(stageNumber, outcome);
            if (steps.Count == 0) return steps;

            var shown = Shown(stageNumber);
            if (shown >= steps.Count) return Array.Empty<int[]>();

            _shown[stageNumber] = steps.Count;
            if (shown == 0) return steps;

            var remaining = new int[steps.Count - shown][];
            for (var i = 0; i < remaining.Length; i++) remaining[i] = steps[shown + i];
            return remaining;
        }
    }
}
