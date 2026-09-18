using BlueComplex.Core.Stage;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 심박수 바 + BPM 텍스트를 코어에 연결한다.
    ///
    /// Heartbeat.Changed는 구독하지 않는다 — 코드상 그 이벤트는 TurnRunner.PlayClue의 턴 해석
    /// 경로(Heartbeat.Change 호출은 TurnRunner.cs 한 곳뿐)에서만 발동하는데, 거기 반응해서 마커를
    /// 바로 애니메이션하면 컴플렉스 발광/태그 연출보다 심박수가 먼저 움직여 순서가 뒤집힌다.
    /// UI 가이드: "태그가 위로 올라가며 사라지며, 그와 동시에 인디케이터가 움직인다" — 그 타이밍의
    /// 제어권은 3단계 CinematicTurnResultPresenter가 PlayTurnResult()를 부르는 시점이 쥔다.
    ///
    /// 이 컨트롤러가 직접 다루는 건 "턴 밖" 동기화뿐이다: Render()(세션 바인드 = 스테이지
    /// 시작/재시작, 애니메이션 없이 스냅)와 OnTurnBegan(키 구역·활성 턴 갱신).
    /// </summary>
    public sealed class HeartRateController : SessionBoundView
    {
        [SerializeField] private HeartRateBarView _bar;
        [SerializeField] private BpmDisplay _bpm;

        protected override void Subscribe(StageSession session) => session.Runner.TurnBegan += OnTurnBegan;
        protected override void Unsubscribe(StageSession session) => session.Runner.TurnBegan -= OnTurnBegan;

        protected override void Render()
        {
            // Session.Keys.Zones는 TurnRunner.StartStage()가 실제로 채우는데, SessionStarted는
            // StartStage()보다 먼저 발동해서 여기선 아직 비어 있다 — OnTurnBegan에서 다시 채운다.
            _bar.SetKeyZones(Session.Keys.Zones);
            _bar.MoveMarker(Session.Heartbeat.Value, animate: false);
            _bar.SetActiveTurn(Session.Runner.CurrentTurn);
            _bpm.SetValue(Session.Heartbeat.Value);
        }

        /// <summary>턴 결과의 심박수 이동을 애니메이션으로 반영한다 — 호출 시점은 Presenter가 쥔다.</summary>
        public void PlayTurnResult(int newValue)
        {
            _bar.MoveMarker(newValue, animate: true);
            _bpm.SetValue(newValue);
        }

        private void OnTurnBegan(int turn)
        {
            _bar.SetKeyZones(Session.Keys.Zones);
            _bar.SetActiveTurn(turn);
        }
    }
}
