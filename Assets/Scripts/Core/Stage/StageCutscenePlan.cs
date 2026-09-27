using System;
using System.Collections;
using System.Collections.Generic;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 스테이지가 끝난 뒤 재생할 컷신 번호의 순서. 컷신 내용은 컷신 담당 영역이 채우고, 여기서는 "어느 스테이지 종료에 몇 번이 어떤 순서로" 만 정한다.
    /// 이 표는 스테이지의 컷신 단계 전체다 — 실패로 끝나면 시도마다 한 단계씩만 나오고, 클리어해서 다음 단계로 넘어가는 시점에는 아직 못 본
    /// 단계를 전부 몰아서 이어 재생한다(<see cref="StageCutsceneProgress"/>). 한 단계(step)에 번호가 둘 이상이면 사이에 끊김 없이 이어 재생하는 통합 컷신이다(예: 6+7).
    /// Unity 의존 없음 — EditMode 테스트로 검증한다.
    ///
    /// 번호는 컷신 담당의 프리팹 번호(Assets/TimeLine/Prefabs/01~11)를 따르며 확정된 체계다. 노션 "컷신 목록"(09/26)은 1~9번뿐이고 조건 배정이 이 표와 다르지만 이 표가 우선한다.
    /// </summary>
    public static class StageCutscenePlan
    {
        private static readonly int[][] None = Array.Empty<int[]>();

        // 스테이지 1 종료: 1, 2, 3, 4.
        // ※ 4번은 노션에 "2 스테이지 클리어 -1"로 적혀 있으나, 스테이지 1 종료 직후(스테이지 2로 넘어가는 브릿지)에 재생한다 — 확정된 의도.
        private static readonly int[][] Stage1 = { new[] { 1 }, new[] { 2 }, new[] { 3 }, new[] { 4 } };

        // 스테이지 2 종료: 5, (6+7 통합), 8, 9.
        // ※ 번호 9 충돌(미해결): 엔딩 텍스트 컷신 "붙잡히는 유키"(EndingCutsceneDirector·EndingContent의 "컷신 9")와 KTH 프리팹 09_B_Training이 같은 번호를 쓴다.
        //   김태호 확인 뒤 다음 지시로 정리한다 — 그때까지 코드는 그대로 둔다(여기 9는 09_B_Training 쪽).
        private static readonly int[][] Stage2 = { new[] { 5 }, new[] { 6, 7 }, new[] { 8 }, new[] { 9 } };

        // 스테이지 3 클리어: (10+11 통합) → 엔딩 시퀀스. 10, 11번은 노션에 아직 없다 — 내용이 채워질 때까지 훅이 비어 있으면 건너뛴다.
        private static readonly int[][] Stage3Clear = { new[] { 10, 11 } };

        /// <summary>
        /// 끝난 스테이지의 컷신 단계들. 스테이지 1·2는 성공/실패와 무관하게 같고, 스테이지 3은 성공했을 때만 있다(실패는 컷신 없이 재시도).
        /// 컷신이 없는 스테이지 번호는 빈 목록.
        /// </summary>
        public static IReadOnlyList<int[]> For(int stageNumber, StageOutcome outcome)
        {
            if (outcome == StageOutcome.InProgress) throw new ArgumentException("끝나지 않은 스테이지에는 컷신이 없다.", nameof(outcome));

            switch (stageNumber)
            {
                case 1: return Stage1;
                case 2: return Stage2;
                case 3: return outcome == StageOutcome.Cleared ? Stage3Clear : None;
                default: return None;
            }
        }
    }

    /// <summary>
    /// 컷신 단계들을 순서대로 재생하는 시퀀서. 번호마다 <paramref name="provider"/>에 물어 재생할 코루틴을 받고, 없으면(null 반환·훅 자체가 없음) 건너뛴다 — 흐름이 끊기지 않는다.
    /// 각 호출은 <paramref name="log"/>로 남는다(내용이 없어도 순서·호출을 확인할 수 있게).
    /// </summary>
    public static class CutsceneSequencer
    {
        /// <param name="steps">재생 순서(<see cref="StageCutscenePlan"/>).</param>
        /// <param name="provider">번호 → 그 컷신을 재생하는 코루틴(끝날 때까지 yield). 재생할 게 없으면 null.</param>
        /// <param name="log">호출 기록(null이면 안 남긴다).</param>
        public static IEnumerator Play(IReadOnlyList<int[]> steps, Func<int, IEnumerator> provider, Action<string> log = null)
        {
            if (steps == null) yield break;

            foreach (var step in steps)
            {
                var label = string.Join("+", step);
                log?.Invoke($"[Cutscene] 단계 {label} 시작");

                foreach (var number in step)
                {
                    var routine = provider?.Invoke(number);
                    if (routine == null)
                    {
                        log?.Invoke($"[Cutscene] #{number} 건너뜀(연결된 컷신 없음)");
                        continue;
                    }

                    log?.Invoke($"[Cutscene] #{number} 재생");
                    yield return routine;
                    log?.Invoke($"[Cutscene] #{number} 끝");
                }
            }
        }
    }
}
