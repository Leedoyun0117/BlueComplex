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

        /// <summary>연출이 아직 재생 중이면 true — 그동안 새 단서 제시를 막는 데 쓴다
        /// (MemorySpaceDropZone). Present가 동기적으로 끝나는 구현체는 항상 false면 된다.</summary>
        bool IsPresenting { get; }
    }
}
