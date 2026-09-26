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
        /// <summary>클리어한 스테이지의 컷신. 인자는 클리어한 스테이지 id. 화면은 밝은 상태에서 시작하고, 끝나면 밝은 상태로 돌려준다(다음 어두워짐은 이 연출이 한다).</summary>
        public static Func<string, IEnumerator> PlayCutscene { get; set; }

        /// <summary>실패한 스테이지에서 완전한 암전 뒤 시작 화면으로 돌아간다. 화면은 완전히 어두운 상태에서 시작한다. 연결이 없으면(null) 결과 패널을 보여 준다.</summary>
        public static Func<IEnumerator> ReturnToStart { get; set; }

        /// <summary>튜토리얼이 시작되기 전에 재생하는 시작 컷신(암흑 → 뉴스 → 경찰서). 내용은 컷신 담당 영역(<see cref="IntroCutsceneDirector"/>)이 채운다.
        /// 튜토리얼 세션이 만들어진 뒤, 첫 턴이 시작되기 전에 불린다. 끝나도 경찰서 그림은 화면에 남고(<see cref="IntroCutsceneDirector.HoldsRoom"/>) 오프닝 대화가 그 위에서 이어진다 —
        /// 그림을 걷는 것(<see cref="IntroCutsceneDirector.Dismiss"/>)은 대화가 끝난 뒤 부트스트래퍼가 한다. null이면 건너뛰고 바로 튜토리얼이 시작된다.</summary>
        public static Func<IEnumerator> PlayTutorialIntro { get; set; }
    }
}
