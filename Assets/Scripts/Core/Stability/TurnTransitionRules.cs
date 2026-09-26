namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// "연출 목록" 기획의 턴 진행 연출 트리거 판정. UI(<c>CinematicTurnResultPresenter</c>)는 이 판정 결과로 어떤 연출을 틀지만 고르고,
    /// 판정 자체는 여기서만 한다(Unity 의존 없음, EditMode 테스트로 검증 가능).
    /// </summary>
    public static class TurnTransitionRules
    {
        /// <summary>쿼터 안의 <paramref name="turnInQuarter"/>번째 턴(1부터)이 마지막 턴(키 턴)인가.</summary>
        public static bool IsKeyTurn(int turnInQuarter, int turnsPerQuarter) =>
            turnsPerQuarter > 0 && turnInQuarter == turnsPerQuarter;
    }
}
