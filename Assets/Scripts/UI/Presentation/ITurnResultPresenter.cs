 using BlueComplex.Core.Turn;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 턴 결과를 화면에 반영하는 계약. 2단계는 즉시 반영하는 구현체(ImmediateTurnResultPresenter)를
    /// 쓰고, 3단계는 이 인터페이스를 구현하는 연출 버전으로 갈아끼운다 — 호출자(각 컨트롤러가
    /// TurnResolved를 구독해 Present를 호출하는 지점)는 그대로 둔다.
    /// </summary>
    public interface ITurnResultPresenter
    {
        void Present(TurnReport report);

        /// <summary>연출이 아직 재생 중이면 true — 그동안 새 단서 제시를 막는 데 쓴다
        /// (MemorySpaceDropZone). Present가 동기적으로 끝나는 구현체는 항상 false면 된다.</summary>
        bool IsPresenting { get; }
    }
}
