using System;
using System.Collections;
using BlueComplex.Core.Turn;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 턴 결과를 화면에 반영하는 계약. 지금은 연출 버전(CinematicTurnResultPresenter) 하나만 쓴다 —
    /// 인터페이스로 분리해 둔 건 호출자(각 컨트롤러가 TurnResolved를 구독해 Present를 호출하는 지점)가
    /// 구현체를 몰라도 되게 하기 위해서다.
    /// </summary>
    public interface ITurnResultPresenter
    {
        void Present(TurnReport report);

        /// <summary>
        /// 실제로 내지 않은 카드의 미리보기 결과(<see cref="TurnRunner.PreviewPlay"/>)를 실제 턴처럼 끝까지 연출한다(튜토리얼 턴 4 오답).
        /// 결과 연출이 끝나면 <paramref name="afterResult"/>(안내 대사 → 세션 되돌리기)를 돌리고, 그 뒤 화면을 코어의 현재 상태로 되돌린다. 그동안 <see cref="IsPresenting"/>은 true다.
        /// 포스트잇 갱신은 없다(턴이 넘어가지 않는다).
        /// </summary>
        void PresentTrial(TurnReport report, Func<IEnumerator> afterResult);

        /// <summary>연출이 아직 재생 중이면 true — 그동안 새 단서 제시를 막는 데 쓴다
        /// (MemorySpaceDropZone). Present가 동기적으로 끝나는 구현체는 항상 false면 된다.</summary>
        bool IsPresenting { get; }
    }
}
