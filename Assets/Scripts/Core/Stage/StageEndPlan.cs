using System;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>스테이지가 끝난 뒤 화면이 어디로 이어지는가.</summary>
    public enum StageEndRoute
    {
        /// <summary>모든 자물쇠가 열렸고 다음 스테이지가 있다 — 클리어 대사 → 컷신 → 다음 스테이지.</summary>
        ClearToNextStage,

        /// <summary>모든 자물쇠가 열렸지만 다음 스테이지가 없다 — 클리어 대사 → 컷신 → 결과 패널.</summary>
        ClearToEnd,

        /// <summary>자물쇠가 다 열리지 못했다 — 암전 → 시작 화면으로 복귀.</summary>
        FailToStart
    }

    /// <summary>
    /// "스테이지 클리어 연출"의 자물쇠 계획. 자물쇠는 클리어에 필요한 키 개수만큼 있고, 획득한 키 수만큼 앞에서부터 열린다.
    /// 판정 자체는 코어가 이미 내렸으므로(<see cref="StageOutcome"/>) 이 구조체는 그 결과를 연출 언어(몇 개 중 몇 개, 어디로 이어지나)로 옮기기만 한다.
    /// Unity 의존 없음 — EditMode 테스트로 검증한다.
    /// </summary>
    public readonly struct StageEndPlan
    {
        public int LockCount { get; }
        public int OpenedCount { get; }
        public StageEndRoute Route { get; }

        public bool AllOpened => OpenedCount >= LockCount;

        private StageEndPlan(int lockCount, int openedCount, StageEndRoute route)
        {
            LockCount = lockCount;
            OpenedCount = openedCount;
            Route = route;
        }

        /// <param name="outcome">끝난 스테이지의 결과(InProgress는 안 된다).</param>
        /// <param name="requiredKeys">클리어에 필요한 키 수 = 자물쇠 수.</param>
        /// <param name="collectedKeys">획득한 키 수.</param>
        /// <param name="hasNextStage">이어질 다음 스테이지가 있는가.</param>
        public static StageEndPlan Create(StageOutcome outcome, int requiredKeys, int collectedKeys, bool hasNextStage)
        {
            if (outcome == StageOutcome.InProgress) throw new ArgumentException("끝나지 않은 스테이지에는 종료 연출이 없다.", nameof(outcome));
            if (requiredKeys < 1) throw new ArgumentOutOfRangeException(nameof(requiredKeys));

            // 코어는 키가 다 모이면 곧바로 Cleared로 끝내므로 Cleared면 자물쇠는 전부 열린 것이다(획득 수 표기가 어긋나도 결과를 따른다).
            var opened = outcome == StageOutcome.Cleared ? requiredKeys : Math.Max(0, Math.Min(collectedKeys, requiredKeys - 1));
            var route = outcome != StageOutcome.Cleared
                ? StageEndRoute.FailToStart
                : hasNextStage ? StageEndRoute.ClearToNextStage : StageEndRoute.ClearToEnd;
            return new StageEndPlan(requiredKeys, opened, route);
        }
    }
}
