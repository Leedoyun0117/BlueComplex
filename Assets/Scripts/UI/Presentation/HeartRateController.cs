using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 심박수 모니터(심전도 파형 + 목표 띠 + BPM 숫자·상태 배지) + 쿼터 HUD(키 표시 창, 쿼터 진행 창, 전체 스테이지 오버레이)를 코어에 연결한다.
    ///
    /// Heartbeat.Changed는 구독하지 않는다 — 코드상 그 이벤트는 TurnRunner.PlayClue의 턴 해석
    /// 경로(Heartbeat.Change 호출은 TurnRunner.cs 한 곳뿐)에서만 발동하는데, 거기 반응해서 파형을
    /// 바로 움직이면 컴플렉스 발광/태그 연출보다 심박수가 먼저 움직여 순서가 뒤집힌다.
    /// UI 가이드: "태그가 위로 올라가며 사라지며, 그와 동시에 인디케이터가 움직인다" — 그 타이밍의
    /// 제어권은 3단계 CinematicTurnResultPresenter가 PlayTurnResult()를 부르는 시점이 쥔다.
    /// 쿼터 HUD도 같은 규칙이다 — 키 아이콘은 PlayTurnResult로, 하얀 점은 SyncTurnState로만 움직인다.
    ///
    /// 이 컨트롤러가 직접 다루는 건 "턴 밖" 동기화뿐이다: Render()(세션 바인드 = 스테이지
    /// 시작/재시작, 애니메이션 없이 스냅)와 OnTurnBegan(목표 구역·현재 쿼터 갱신), StageEnded(오버레이 닫기).
    /// 다만 TurnBegan은 TurnResolved 직후 동기로 쏘아지므로, 연출이 재생 중이면 현재 쿼터 전환을 미루고
    /// Presenter가 연출을 마친 뒤 <see cref="SyncTurnState"/> 를 불러 반영한다 — 안 그러면 쿼터 마지막 턴의
    /// 키 판정 연출 도중에 다음 쿼터 구역으로 바뀌어 버린다.
    /// </summary>
    public sealed class HeartRateController : SessionBoundView
    {
        [SerializeField] private HeartRateBarView _bar;
        [SerializeField] private BpmDisplay _bpm;

        private ITurnResultPresenter _presenter;
        private QuarterHud _quarterHud;

        /// <summary>DLJ: 화면에 심박수를 반영하는 시점. 상태 이펙트도 태그/파형 연출과 동기화한다.</summary>
        public event System.Action<int, bool> HeartbeatPresented;

        private QuarterHud Hud => _quarterHud != null ? _quarterHud : _quarterHud = QuarterHud.GetOrCreate(transform.root);

        protected override void Subscribe(StageSession session)
        {
            session.Runner.TurnBegan += OnTurnBegan;
            session.Runner.StageEnded += OnStageEnded;
            session.Items.Used += OnItemUsed;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Runner.TurnBegan -= OnTurnBegan;
            session.Runner.StageEnded -= OnStageEnded;
            session.Items.Used -= OnItemUsed;
        }

        protected override void Render()
        {
            // 새 세션이면 쿼터 HUD와 바의 목표 구역을 처음 상태로 되돌린다(같은 시드 재시작이면 구역 배치가 똑같아
            // 이전 판의 결과 표시가 남을 수 있다).
            _bar.BindScale(Session.Zone);
            _bar.ResetTargetZone();
            Hud.Bind(Session, Bootstrapper);

            // Session.Keys.Zones는 TurnRunner.StartStage()가 실제로 채우는데, SessionStarted는
            // StartStage()보다 먼저 발동해서 여기선 아직 비어 있다 — OnTurnBegan에서 다시 채운다.
            SyncTurnState();
            ShowBpm(Session.Heartbeat.Value, snap: true);
        }

        /// <summary>턴 결과의 심박수 이동 + 그 턴이 쿼터의 마지막 턴이었다면 키 판정 결과를 반영한다 —
        /// 호출 시점은 Presenter가 쥔다. 판정은 코어가 이미 내려 TurnReport.KeyResult에 담아 두었으므로
        /// 여기서 다시 계산하지 않고 그대로 표시만 한다(KeyProgress 이벤트도 구독하지 않는다).</summary>
        public void PlayTurnResult(TurnReport report)
        {
            ShowBpm(report.HeartbeatValue, snap: false);

            if (report.KeyResult is not { } judgement) return;

            _bar.RecordKeyResult(judgement.Quarter, judgement.Success);
            Hud.RecordKeyResult(judgement.Quarter, judgement.Success);
        }

        /// <summary>BPM 숫자·상태 배지·심전도 파형을 한 번에 갱신한다 — 셋 다 같은 시점(Presenter가 정한다)에 바뀌고 같은 시간 동안 넘어가야 어긋나 보이지 않는다.
        /// 파형이 불규칙해지는 구간은 코어 구간표의 검열 수준이 아니라 상태(매우 침체·매우 흥분·즉사)로 정한다.</summary>
        private void ShowBpm(int value, bool snap)
        {
            var state = Session.Zone.StateOf(value);
            var color = HeartbeatVisuals.TextColor(state);
            var irregular = state is HeartbeatState.VeryDepressed or HeartbeatState.VeryExcited or HeartbeatState.Fatal;

            _bpm.SetValue(value, color, KoreanLabels.State(state), animate: !snap);
            _bar.SetPulse(value, color, irregular, snap);
            HeartbeatPresented?.Invoke(value, snap);
        }

        /// <summary>코어의 현재 턴 상태(현재 쿼터의 목표 구역, 쿼터 내 턴 위치)를 바와 쿼터 HUD에 반영한다.
        /// 연출이 끝난 뒤 Presenter가 부른다.</summary>
        public void SyncTurnState()
        {
            var runner = Session.Runner;
            var quarter = runner.CurrentQuarter;

            // 스테이지 시작 전(quarter 0)이나 구역이 아직 확정되기 전에는 그릴 구역이 없다.
            KeyZone? zone = null;
            if (quarter > 0 && Session.Keys.Zones.TryGetValue(Session.Keys.Schedule.LastTurnOf(quarter), out var found))
                zone = found;

            var isKeyTurn = quarter > 0 && runner.CurrentTurnInQuarter == Session.Keys.Schedule.TurnsPerQuarter;
            _bar.SetTargetZone(quarter, zone);
            _bar.SetKeyTurn(isKeyTurn);
            Hud.Sync(quarter, runner.CurrentTurnInQuarter);
        }

        private void OnTurnBegan(int turn)
        {
            // 연출 중이면 Presenter가 끝나고 SyncTurnState를 부른다.
            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            SyncTurnState();
        }

        /// <summary>아이템이 심박수를 직접 바꿀 수 있다(착한 사마리아인 +10). 사용은 플레이어의 클릭이라 턴 해석 밖에서 일어나므로 바로 반영한다.</summary>
        private void OnItemUsed(ItemDefinition item)
        {
            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            ShowBpm(Session.Heartbeat.Value, snap: false);
        }

        private void OnStageEnded(StageOutcome outcome) => Hud.CloseOverview();
    }
}
