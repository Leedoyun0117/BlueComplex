using System;

namespace BlueComplex.Core.Save
{
    /// <summary>
    /// 저장된 진행도(<see cref="SaveData.TutorialCleared"/>/<see cref="SaveData.HighestStageCleared"/>)에서
    /// "취조시작"을 누르면 이어할 지점을 고른다. Unity 의존 없음 — EditMode 테스트로 검증한다.
    /// </summary>
    public static class StageProgress
    {
        /// <returns>튜토리얼부터 시작해야 하면 null. 아니면 이어할 스테이지 번호(1~<paramref name="lastStageNumber"/>) —
        /// 튜토리얼만 클리어했으면 스테이지 1, 스테이지 N까지 클리어했으면 N+1, 전부 클리어했으면 마지막 스테이지에 머문다.</returns>
        public static int? ResumeStage(bool tutorialCleared, int highestStageCleared, int lastStageNumber)
        {
            if (lastStageNumber < 1) throw new ArgumentOutOfRangeException(nameof(lastStageNumber));
            if (!tutorialCleared) return null;

            var next = Math.Max(highestStageCleared + 1, 1);
            return Math.Min(next, lastStageNumber);
        }
    }
}
