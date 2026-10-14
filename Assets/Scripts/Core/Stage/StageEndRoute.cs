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

        /// <summary>자물쇠가 다 열리지 못했다(마지막 스테이지 포함) — 암전 → 메인 화면으로 복귀.</summary>
        FailToStart,

        /// <summary>마지막 스테이지를 클리어했다 — 클리어 대사 → 컷신 → 엔딩 시퀀스.</summary>
        ClearToEnding
    }
}
