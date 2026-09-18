using System.Collections;
using System.Linq;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 3단계 연출 버전 ITurnResultPresenter. Present() 순서:
    /// 엑스레이 판넬 Open() → 컴플렉스 순차 발광 → 대사 타이핑 →
    /// (남은 감정 태그 상승/소멸 + 심박수 이동을 동시에) → 판넬 Close() → 손패 갱신.
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

            if (_complexList == null || _dialogue == null || _clueTray == null || _heartRate == null ||
                _xrayPanel == null || _memoryBubble == null)
                Debug.LogWarning("[CinematicTurnResultPresenter] 필수 참조를 하나 이상 못 찾았다 — " +
                                  "연출이 중간에 멈출 수 있다.", this);
        }

        protected override void Subscribe(StageSession session) => session.Runner.TurnResolved += Present;
        protected override void Unsubscribe(StageSession session) => session.Runner.TurnResolved -= Present;

        protected override void Render()
        {
            _complexList.Refresh(Session.Complexes.InPriorityOrder().ToList());
        }

        public void Present(TurnReport report) => StartCoroutine(PresentRoutine(report));

        private IEnumerator PresentRoutine(TurnReport report)
        {
            IsPresenting = true;

            var openTween = _xrayPanel.Open();
            if (openTween != null) yield return openTween.WaitForCompletion(true);

            // TickDurations/스폰이 Resolve 이후에 일어나므로, 발광 전에 먼저 행 배치를 최신 보드
            // 상태로 맞춰야 한다(ComplexListView.PlaySequence 문서 참고).
            _complexList.Refresh(Session.Complexes.InPriorityOrder().ToList());
            var glowSequence = _complexList.PlaySequence(report.Interpretation);
            yield return glowSequence.WaitForCompletion(true);

            yield return PlayDialogue(TurnSummaryFormatter.Build(report));

            yield return PlayTagsAndHeartbeatTogether(report);

            var closeTween = _xrayPanel.Close();
            if (closeTween != null) yield return closeTween.WaitForCompletion(true);

            _clueTray.RefreshAll(Session.Hand.Cards, Session.Ledger, Session.Censorship.Level);

            IsPresenting = false;
        }

        private IEnumerator PlayDialogue(string line)
        {
            var done = false;
            void OnComplete() => done = true;

            _dialogue.TypingComplete += OnComplete;
            _dialogue.PlayTyped(line);
            yield return new WaitUntil(() => done);
            _dialogue.TypingComplete -= OnComplete;
        }

        /// <summary>UI 가이드 원문: "태그가 위로 올라가며 사라지며, 그와 동시에 인디케이터가 움직인다."
        /// 두 연출을 같은 프레임에 시작하고, 더 긴 쪽(태그 상승)이 끝날 때까지 기다린다 — 심박수
        /// 마커 이동(0.25초, HeartRateBarView.MoveMarker)이 태그 상승(0.9초)보다 항상 짧다.</summary>
        private IEnumerator PlayTagsAndHeartbeatTogether(TurnReport report)
        {
            var labels = report.FinalTags.EnumerateEmotionsFlat()
                .Select(KoreanLabels.Emotion)
                .ToList();

            var tagSequence = _memoryBubble.PlayRemainingTags(labels);
            _heartRate.PlayTurnResult(report.HeartbeatValue);

            yield return tagSequence.WaitForCompletion(true);
        }
    }
}
