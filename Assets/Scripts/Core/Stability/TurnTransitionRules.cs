namespace BlueComplex.Core.Stability
{
    /// <summary>턴이 넘어갈 때 포스트잇을 떼었다 붙이는 연출에 무엇이 곁들여지는가.</summary>
    public readonly struct TurnTransitionPlan
    {
        /// <summary>다음 턴이 쿼터의 마지막 턴(키 턴)이다 — 암전 + 나츠의 독백.</summary>
        public bool KeyTurn { get; }

        /// <summary>이 턴을 지나며 심박수 구간이 바뀌었다 — 카메라가 심박수 표시기로 확대되는 연출.</summary>
        public bool HeartbeatChanged { get; }

        public TurnTransitionPlan(bool keyTurn, bool heartbeatChanged)
        {
            KeyTurn = keyTurn;
            HeartbeatChanged = heartbeatChanged;
        }
    }

    /// <summary>
    /// "연출 목록" 기획의 턴 진행 연출 트리거 판정. UI(<c>CinematicTurnResultPresenter</c>)는 이 판정 결과로 어떤 연출을 틀지만 고르고,
    /// 판정 자체는 여기서만 한다(Unity 의존 없음, EditMode 테스트로 검증 가능).
    ///
    /// 심박수 변화 연출의 트리거는 기획에 명시돼 있지 않다 — "심박수가 바뀔 때마다"로 읽으면 단서를 낼 때마다(거의 매 턴) 확대 연출이 돌아
    /// 게임 흐름이 끊기므로, 심박수의 <b>구간(상태)</b>이 바뀐 턴만으로 해석했다(침체/흥분 진입·복귀를 나츠·배경음이 이미 같은 기준으로 다룬다).
    /// </summary>
    public static class TurnTransitionRules
    {
        /// <summary>쿼터 안의 <paramref name="turnInQuarter"/>번째 턴(1부터)이 마지막 턴(키 턴)인가.</summary>
        public static bool IsKeyTurn(int turnInQuarter, int turnsPerQuarter) =>
            turnsPerQuarter > 0 && turnInQuarter == turnsPerQuarter;

        /// <summary>구간이 바뀌었는가. 같은 구간 안에서 값만 움직인 것은 변화가 아니다.</summary>
        public static bool HeartbeatChanged(HeartbeatState before, HeartbeatState after) => before != after;

        /// <summary>다음 턴이 시작되는 시점의 연출 계획. <paramref name="stateBefore"/>는 지난 턴 결과가 반영되기 전 구간, <paramref name="stateAfter"/>는 반영된 뒤 구간이다.</summary>
        public static TurnTransitionPlan Plan(int nextTurnInQuarter, int turnsPerQuarter, HeartbeatState stateBefore, HeartbeatState stateAfter) =>
            new(IsKeyTurn(nextTurnInQuarter, turnsPerQuarter), HeartbeatChanged(stateBefore, stateAfter));
    }
}
