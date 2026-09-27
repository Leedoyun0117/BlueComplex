using System;
using System.Collections;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 종료 연출이 다른 담당 영역(컷신, 시작 화면)에 걸어 두는 훅. 이 프로젝트에는 아직 그 둘이 없어서, 연결할 쪽이 이 값을 채우면 된다 — 비어 있으면
    /// 컷신은 건너뛰고(클리어 → 대사 → 바로 다음 단계), 시작 화면 복귀는 기존 결과 패널(다시 시작 버튼)로 대신한다.
    /// 연출은 <see cref="StageClearDirector"/>가 코루틴으로 그대로 기다린다 — 핸들러는 끝날 때까지 yield하면 되고, 끝나면 화면이 밝은 상태(컷신)로 돌려줘야 한다.
    /// 정적 값이라 씬을 나갈 때 스스로 <c>null</c>로 되돌려야 한다.
    /// </summary>
    public static class StageFlowHooks
    {
        /// <summary>스테이지 종료 컷신 하나를 번호로 재생한다(번호·순서는 <see cref="BlueComplex.Core.Stage.StageCutscenePlan"/>, 시퀀서는 <see cref="BlueComplex.Core.Stage.CutsceneSequencer"/>).
        /// 재생할 컷신이 아직 없는 번호는 null을 돌려주면 건너뛴다. 훅 자체가 null이면 전부 건너뛴다. 컷신은 게임 화면을 스스로 검게 덮고 시작하며, 끝난 뒤에도 덮은 채로 남는다(<see cref="CutsceneCovering"/>·<see cref="ReleaseCutscene"/>).
        /// 컷신이 끝났음을 알려 줄 수 있어야 한다(코루틴이 끝날 때까지 yield) — 끝을 알려 주지 않으면 흐름이 거기서 멈춘다.</summary>
        public static Func<int, IEnumerator> PlayCutscene { get; set; }

        /// <summary>컷신이 화면을 덮고 있는가(컷신 재생 중이거나 통합 컷신 사이). 컷신들이 끝난 뒤 종료 연출이 <see cref="ReleaseCutscene"/> 전에 묻는다 —
        /// 덮고 있다면 게임 화면 위에 검은 막을 먼저 깔고 컷신을 걷어야 밝은 프레임이 비치지 않는다. 훅이 없으면 null(덮은 적 없음).</summary>
        public static Func<bool> CutsceneCovering { get; set; }

        /// <summary>컷신이 덮고 있던 화면을 걷어 게임 화면으로 돌아온다. 컷신 사이(통합 컷신 6+7 같은)에는 부르지 않는다 — 스테이지 종료 연출이 컷신을 다 재생한 뒤 한 번 부른다.</summary>
        public static Action ReleaseCutscene { get; set; }

        /// <summary>실패한 스테이지에서 완전한 암전 뒤 시작 화면으로 돌아간다. 화면은 완전히 어두운 상태에서 시작한다. 연결이 없으면(null) 결과 패널을 보여 준다.</summary>
        public static Func<IEnumerator> ReturnToStart { get; set; }

        /// <summary>튜토리얼이 시작되기 전에 재생하는 시작 컷신(암흑 → 뉴스 → 경찰서). 내용은 컷신 담당 영역(<see cref="IntroCutsceneDirector"/>)이 채운다.
        /// 튜토리얼 세션이 만들어진 뒤, 첫 턴이 시작되기 전에 불린다. 끝나도 경찰서 그림은 화면에 남고(<see cref="IntroCutsceneDirector.HoldsRoom"/>) 오프닝 대화가 그 위에서 이어진다 —
        /// 그림을 걷는 것(<see cref="IntroCutsceneDirector.Dismiss"/>)은 대화가 끝난 뒤 부트스트래퍼가 한다. null이면 건너뛰고 바로 튜토리얼이 시작된다.</summary>
        public static Func<IEnumerator> PlayTutorialIntro { get; set; }
    }
}
