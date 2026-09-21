using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Background;
using BlueComplex.UI.Layout;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 3단계 연출 버전 ITurnResultPresenter. Present() 순서:
    /// 엑스레이 판넬 Open() → 컴플렉스 순차 발광(발동할 때마다 대사창에 짧은 이벤트 대사) → 대사 타이핑 →
    /// (남은 감정 태그 상승/소멸 + 심박수 이동을 동시에) → 판넬 Close() → 손패 갱신 → (모든 연출이 끝나면) 아이템 칸 갱신.
    ///
    /// TurnRunner.PlayClue는 TurnResolved를 동기(synchronous)로 쏘고 그 직후 바로 다음 턴을
    /// 시작한다 — 연출은 여러 프레임에 걸쳐야 하므로 Present()는 코루틴을 발사만 하고 즉시
    /// 리턴한다. 그래서 연출이 끝나기 전엔 IsPresenting으로 새 단서 제시를 막는다
    /// (MemorySpaceDropZone이 이걸 확인한다) — 안 그러면 다음 턴 결과가 지금 재생 중인 연출과
    /// 겹쳐서 순서가 뒤섞인다.
    /// </summary>
    public sealed class CinematicTurnResultPresenter : SessionBoundView, ITurnResultPresenter
    {
        [SerializeField] private ComplexListView _complexList;
        [SerializeField] private DialogueText _dialogue;
        [SerializeField] private ClueCardTray _clueTray;
        [SerializeField] private HeartRateController _heartRate;
        [SerializeField] private ComplexXrayPanel _xrayPanel;
        [SerializeField] private MemorySpaceBubble _memoryBubble;
        [SerializeField] private ComplexStatusController _complexStatus;
        [SerializeField] private TraitStatusView _traitStatus;
        [SerializeField] private ClockController _clock;
        [SerializeField] private ItemController _items;

        /// <summary>컴플렉스 이벤트 대사가 다 나온 뒤 다음 컴플렉스로 넘어가기 전의 짧은 쉼(초).</summary>
        [SerializeField] private float _eventLinePause = 0.25f;

        public bool IsPresenting { get; private set; }

        protected override void Awake()
        {
            ResolveReferences();
            base.Awake();
        }

        /// <summary>이 오브젝트가 이미 구워진 "Turn Result Presenter"에 새로 붙는 컴포넌트라 인스펙터
        /// 필드를 手동으로 못 물린 상태로 시작할 수 있다 — 같은 MainHud 아래에서 타입으로 찾아서
        /// 채운다. 인스펙터에서 직접 물렸다면(나중에 프리팹을 다시 구우면) 그 값을 그대로 쓴다.</summary>
        private void ResolveReferences()
        {
            var root = transform.root;
            if (_complexList == null) _complexList = root.GetComponentInChildren<ComplexListView>(true);
            if (_dialogue == null) _dialogue = root.GetComponentInChildren<DialogueText>(true);
            if (_clueTray == null) _clueTray = root.GetComponentInChildren<ClueCardTray>(true);
            if (_heartRate == null) _heartRate = root.GetComponentInChildren<HeartRateController>(true);
            if (_xrayPanel == null) _xrayPanel = root.GetComponentInChildren<ComplexXrayPanel>(true);
            if (_memoryBubble == null) _memoryBubble = root.GetComponentInChildren<MemorySpaceBubble>(true);
            // 상시 표시가 없는 씬에서도 연출은 그대로 돈다.
            if (_complexStatus == null) _complexStatus = root.GetComponentInChildren<ComplexStatusController>(true);
            if (_traitStatus == null) _traitStatus = root.GetComponentInChildren<TraitStatusView>(true);
            if (_items == null) _items = root.GetComponentInChildren<ItemController>(true);
            // 벽시계는 HUD가 아니라 3D 배경 리그에 있어 root 아래에서 못 찾는다. 없어도 연출은 그대로 돈다.
            if (_clock == null) _clock = FindFirstObjectByType<ClockController>(FindObjectsInactive.Include);

            if (_complexList == null || _dialogue == null || _clueTray == null || _heartRate == null ||
                _xrayPanel == null || _memoryBubble == null)
                Debug.LogWarning("[CinematicTurnResultPresenter] 필수 참조를 하나 이상 못 찾았다 — " +
                                  "연출이 중간에 멈출 수 있다.", this);
        }

        protected override void Subscribe(StageSession session) => session.Runner.TurnResolved += Present;
        protected override void Unsubscribe(StageSession session) => session.Runner.TurnResolved -= Present;

        protected override void Render()
        {
            _pending.Clear(); // 재시작 시 이전 스테이지의 대기 중 연출은 버린다.
            _complexList.Refresh(Session.Complexes.InPriorityOrder().ToList());
        }

        /// <summary>한 번의 PlayClue 호출 안에서 TurnResolved가 연달아 올 수 있다(마지막 단서를 낸 턴 뒤에 손패가 비어
        /// 넘어간 턴). 연출은 겹치면 안 되므로 큐에 쌓아 순서대로 재생한다.</summary>
        private readonly Queue<TurnReport> _pending = new();

        public void Present(TurnReport report)
        {
            _pending.Enqueue(report);
            if (IsPresenting) return;

            IsPresenting = true;
            StartCoroutine(DrainRoutine());
        }

        private IEnumerator DrainRoutine()
        {
            while (_pending.Count > 0)
            {
                var report = _pending.Dequeue();
                yield return report.IsPass ? PassRoutine(report) : PresentRoutine(report);
            }

            // 쿼터 경계 등 다음 턴 시작 상태(현재 쿼터·목표 구역)는 연출이 모두 끝난 뒤에 반영한다.
            _heartRate.SyncTurnState();
            _complexStatus?.Refresh();
            _traitStatus?.Refresh();
            _memoryBubble.SetEngaged(false);
            IsPresenting = false;

            // 이 턴들의 연출이 모두 끝난 뒤에야 새 아이템 카드가 빈 칸에 끼워진다(획득은 턴 해석 도중에 일어난다).
            if (_items != null) _items.FlushPending();
        }

        /// <summary>단서 없이 시간만 흐른 턴 — 컴플렉스/태그 연출 없이 결과(대사, 심박수, 키 판정)만 보여준다.</summary>
        private IEnumerator PassRoutine(TurnReport report)
        {
            _heartRate.PlayTurnResult(report);
            _complexStatus?.Refresh();
            _traitStatus?.Refresh();
            AdvanceClock(report);
            yield return PlayDialogue(TurnSummaryFormatter.Build(report));

            // 넘어간 턴에도 손패는 연출이 도는 동안 갱신이 미뤄져 있다(ClueHandController.OnHandChanged 참고).
            _clueTray.RefreshAll(Session.Hand.Cards, Session.Ledger, Session.Censorship.Level);
        }

        /// <summary>시계는 심박수 이동과 같은 타이밍에 돌린다 — 컴플렉스 발광/대사가 끝난 뒤이고,
        /// 분침 회전(0.7초)이 태그 상승(0.9초) 안에 끝나 다음 턴 연출과 겹치지 않는다.</summary>
        private void AdvanceClock(TurnReport report)
        {
            if (_clock != null) _clock.AdvanceTo(report.Turn);
        }

        private IEnumerator PresentRoutine(TurnReport report)
        {
            // 드래그 없이 낸 단서(디버그 숫자키)도 연출 동안은 생각 공간이 켜져 있어야 한다.
            _memoryBubble.SetEngaged(true);

            var openTween = _xrayPanel.Open();
            if (openTween != null) yield return openTween.WaitForCompletion(true);

            // TickDurations/스폰이 Resolve 이후에 일어나므로, 발광 전에 먼저 행 배치를 최신 보드
            // 상태로 맞춰야 한다(ComplexListView.PlayGlow 문서 참고).
            _complexList.Refresh(Session.Complexes.InPriorityOrder().ToList());
            yield return PlayComplexReactions(report);

            yield return PlayDialogue(TurnSummaryFormatter.Build(report));

            yield return PlayTagsAndHeartbeatTogether(report);

            var closeTween = _xrayPanel.Close();
            if (closeTween != null) yield return closeTween.WaitForCompletion(true);

            _clueTray.RefreshAll(Session.Hand.Cards, Session.Ledger, Session.Censorship.Level);
        }

        /// <summary>발동한 컴플렉스를 우선순위 순서(InterpretationResult.Steps 순서)대로 한 번에 하나씩 빛내고, 그때마다 대사창에 짧은 이벤트 대사를 띄운다.
        /// 대사가 다 나와야(클릭으로 건너뛰어도 된다) 다음 컴플렉스로 넘어간다 — 발광과 대사가 서로 끊기지 않는다.</summary>
        private IEnumerator PlayComplexReactions(TurnReport report)
        {
            foreach (var step in report.Interpretation.Steps)
            {
                // 같은 턴에 만료돼 이미 행이 없는 컴플렉스는 조용히 건너뛴다.
                if (!step.Triggered || !_complexList.PlayGlow(step.Complex)) continue;

                yield return PlayDialogue(TurnSummaryFormatter.BuildComplexEventLine(step.Complex), isEvent: true);
                yield return new WaitForSeconds(_eventLinePause);
            }
        }

        private IEnumerator PlayDialogue(string line, bool isEvent = false)
        {
            var done = false;
            void OnComplete() => done = true;

            _dialogue.TypingComplete += OnComplete;
            _dialogue.PlayTyped(line, isEvent);
            yield return new WaitUntil(() => done);
            _dialogue.TypingComplete -= OnComplete;
        }

        /// <summary>UI 가이드 원문: "태그가 위로 올라가며 사라지며, 그와 동시에 인디케이터가 움직인다."
        /// 두 연출을 같은 프레임에 시작하고, 더 긴 쪽(태그 상승)이 끝날 때까지 기다린다 — 심박수
        /// 파형·숫자의 심박수 전환 시간(UiMotionSettings.heartTransition, 기본 0.9초)을 태그 상승과 맞춰 두었다.</summary>
        private IEnumerator PlayTagsAndHeartbeatTogether(TurnReport report)
        {
            var labels = report.FinalTags.EnumerateEmotionsFlat()
                .Select(KoreanLabels.Emotion)
                .ToList();

            var tagSequence = _memoryBubble.PlayRemainingTags(labels);
            _heartRate.PlayTurnResult(report);
            // 남은 턴이 줄고(막대가 줄어든다) 새로 붙거나 만료된 컴플렉스가 반영되는 시점 — 심박수 결과와 함께.
            _complexStatus?.Refresh();
            _traitStatus?.Refresh();
            AdvanceClock(report);
            _memoryBubble.SetPersistentSummary(TurnSummaryFormatter.BuildFinalEmotionSummary(report));

            yield return tagSequence.WaitForCompletion(true);
        }
    }
}
