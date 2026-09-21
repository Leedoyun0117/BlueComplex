using System.Linq;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Background;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 2단계용 ITurnResultPresenter 구현 — 연출 없이 세 요소를 즉시 갱신한다.
    /// TurnRunner.TurnResolved를 구독하는 건 이 클래스뿐이다: 개별 View(ComplexListView,
    /// DialogueText)는 코어 이벤트를 직접 구독하지 않고 여기서 호출받는다.
    /// 3단계는 CinematicTurnResultPresenter로 갈아끼운다(씬/프리팹 배선) — 이 클래스는 그대로 둔다.
    /// </summary>
    public sealed class ImmediateTurnResultPresenter : SessionBoundView, ITurnResultPresenter
    {
        [SerializeField] private ComplexListView _complexList;
        [SerializeField] private DialogueText _dialogue;
        [SerializeField] private ClueCardTray _clueTray;
        [SerializeField] private HeartRateController _heartRate;
        [SerializeField] private MemorySpaceBubble _memoryBubble;
        [SerializeField] private ComplexStatusController _complexStatus;
        [SerializeField] private TraitStatusView _traitStatus;
        [SerializeField] private ClockController _clock;

        /// <summary>즉시 끝나므로 연출 재생 중인 프레임이 없다 — 입력을 잠글 이유가 없다.</summary>
        public bool IsPresenting => false;

        protected override void Awake()
        {
            // HeartRateController는 더 이상 Heartbeat.Changed를 직접 구독하지 않는다(연출 순서를
            // Presenter가 쥐도록 바뀜) — 그래서 이 필드가 없으면 심박수 바가 세션 시작 후로 영영
            // 안 움직인다. 이미 구워진 프리팹에 새로 추가된 필드라 인스펙터에 안 물려 있을 수
            // 있으니 같은 MainHud 아래에서 찾는다. _memoryBubble(최종 감정 고정 표시)도 같은 이유로 셀프힐한다.
            if (_heartRate == null) _heartRate = transform.root.GetComponentInChildren<HeartRateController>(true);
            if (_memoryBubble == null) _memoryBubble = transform.root.GetComponentInChildren<MemorySpaceBubble>(true);
            if (_complexStatus == null) _complexStatus = transform.root.GetComponentInChildren<ComplexStatusController>(true);
            if (_traitStatus == null) _traitStatus = transform.root.GetComponentInChildren<TraitStatusView>(true);
            // 벽시계는 HUD가 아니라 3D 배경 리그에 있어 root 아래에서 못 찾는다.
            if (_clock == null) _clock = FindFirstObjectByType<ClockController>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void Subscribe(StageSession session) => session.Runner.TurnResolved += Present;
        protected override void Unsubscribe(StageSession session) => session.Runner.TurnResolved -= Present;

        protected override void Render()
        {
            _complexList.Refresh(Session.Complexes.InPriorityOrder().ToList());
        }

        public void Present(TurnReport report)
        {
            _complexList.Refresh(Session.Complexes.InPriorityOrder().ToList());
            _complexStatus?.Refresh();
            _traitStatus?.Refresh();
            _dialogue.PlayTyped(TurnSummaryFormatter.Build(report));
            _heartRate?.PlayTurnResult(report);
            if (_clock != null) _clock.AdvanceTo(report.Turn, animate: false);
            if (!report.IsPass)
                _memoryBubble?.SetPersistentSummary(TurnSummaryFormatter.BuildFinalEmotionSummary(report));
            _clueTray.RefreshAll(Session.Hand.Cards, Session.Ledger, Session.Censorship.Level);
        }
    }
}
