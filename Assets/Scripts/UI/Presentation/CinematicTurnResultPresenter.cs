using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Background;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 3단계 연출 버전 ITurnResultPresenter. Present() 순서:
    /// 엑스레이 판넬 Open()(관절 팔이 펴지며 뇌가 드러난다) → 컴플렉스가 우선순위 순서로 뇌에서 빛남(발동할 때마다 대사창에 짧은 이벤트 대사) →
    /// 남은 감정 태그가 풍선 안에 나타남(요약 대사가 타이핑되는 동안, 다 나타난 뒤 최소 tagHold초 정지) →
    /// (태그 상승·페이드 + 심박수 모니터 변화를 동시에) → 풍선이 지워짐 + 판넬 Close() → 손패 갱신 →
    /// 두 포스트잇(컴플렉스·대화)이 떼어짐 → 글자 쓰는 소리(그 사이 포스트잇 내용 갱신) → 갱신된 포스트잇이 붙음 →
    /// (모든 연출이 끝나면) 아이템 칸 갱신. 다음 턴이 쿼터의 마지막 턴(키 턴)이면 떼어지며 화면이 어두워지고 나츠의 독백이 나온 뒤 밝아지며 붙는다(<see cref="PostitDirector"/>).
    ///
    /// TurnRunner.PlayClue는 TurnResolved를 동기(synchronous)로 쏘고 그 직후 바로 다음 턴을
    /// 시작한다 — 연출은 여러 프레임에 걸쳐야 하므로 Present()는 코루틴을 발사만 하고 즉시
    /// 리턴한다. 그래서 연출이 끝나기 전엔 IsPresenting으로 새 단서 제시를 막는다
    /// (MemorySpaceDropZone이 이걸 확인한다) — 안 그러면 다음 턴 결과가 지금 재생 중인 연출과
    /// 겹쳐서 순서가 뒤섞인다.
    /// </summary>
    public sealed class CinematicTurnResultPresenter : SessionBoundView, ITurnResultPresenter
    {
        [SerializeField] private BrainView _brain;
        [SerializeField] private PortraitXrayView _portrait;
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

        /// <summary>키 턴(쿼터의 마지막 턴)에 들어갈 때 나츠가 하는 독백. 화면이 어두워진 동안 나온다.</summary>
        [SerializeField] private string _keyTurnMonologue = "이번 대화에서 키를 얻어야 해.";
        [SerializeField] private string _monologueSpeaker = "나츠";

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
            if (_brain == null) _brain = root.GetComponentInChildren<BrainView>(true);
            // 초상화는 순수 장식이라 없어도 연출은 그대로 돈다 — 필수 참조 경고 목록에도 안 넣는다.
            if (_portrait == null) _portrait = root.GetComponentInChildren<PortraitXrayView>(true);
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

            if (_brain == null || _dialogue == null || _clueTray == null || _heartRate == null ||
                _xrayPanel == null || _memoryBubble == null)
                Debug.LogWarning("[CinematicTurnResultPresenter] 필수 참조를 하나 이상 못 찾았다 — " +
                                  "연출이 중간에 멈출 수 있다.", this);
        }

        protected override void Subscribe(StageSession session) => session.Runner.TurnResolved += Present;
        protected override void Unsubscribe(StageSession session) => session.Runner.TurnResolved -= Present;

        protected override void Render()
        {
            _pending.Clear(); // 재시작 시 이전 스테이지의 대기 중 연출은 버린다.

            // 재시작이 연출 도중이면 옛 코루틴이 새 세션 화면을 계속 만지지 않게 끊고, 떼어져 있던 포스트잇·암전 막을 원래대로 돌린다.
            if (IsPresenting)
            {
                StopAllCoroutines();
                IsPresenting = false;
            }

            PostitDirector.GetOrCreate(transform.root).ResetAll();
            _brain.Refresh(Session.Complexes.InPriorityOrder().ToList());
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
            do
            {
                while (_pending.Count > 0)
                {
                    var report = _pending.Dequeue();
                    yield return report.IsPass ? PassRoutine(report) : PresentRoutine(report);
                }

                yield return PostitRoutine();
                _traitStatus?.Refresh();

                // 포스트잇이 떼어져 있는 사이에 새 결과가 들어왔으면(디버그 키 입력 등) 그 결과도 이어서 재생한다.
            } while (_pending.Count > 0);

            _memoryBubble.SetEngaged(false);
            IsPresenting = false;

            // 이 턴들의 연출이 모두 끝난 뒤에야 새 아이템 카드가 빈 칸에 끼워진다(획득은 턴 해석 도중에 일어난다).
            if (_items != null) _items.FlushPending();
        }

        /// <summary>
        /// 턴 결과 연출이 다 끝난 뒤 두 포스트잇을 떼었다 붙인다. 쿼터 경계 등 다음 턴 시작 상태(현재 쿼터·목표 구역·턴 칸)와 컴플렉스 목록은
        /// 포스트잇이 떼어져 있는 사이에 반영한다 — 결과 연출 도중엔 이전 상태가 그대로 보이고 새로 붙는 포스트잇에 갱신된 상태가 적혀 있다.
        /// 다음 턴이 쿼터의 마지막 턴이면 키 턴 연출(암전 + 나츠 독백)이다. 스테이지가 끝났으면 다음 대화가 없어 떼지 않고 상태만 반영한다.
        /// </summary>
        private IEnumerator PostitRoutine()
        {
            void RefreshContent()
            {
                _heartRate.SyncTurnState();
                _complexStatus?.RefreshForTurn();
            }

            var runner = Session.Runner;
            if (runner.Outcome != StageOutcome.InProgress)
            {
                RefreshContent();
                yield break;
            }

            var keyTurn = runner.CurrentTurnInQuarter == Session.Keys.Schedule.TurnsPerQuarter;
            yield return PostitDirector.GetOrCreate(transform.root)
                .PlayRefresh(_monologueSpeaker, keyTurn ? _keyTurnMonologue : null, RefreshContent);

            _complexStatus?.RevealNewMarks();
        }

        /// <summary>단서 없이 시간만 흐른 턴 — 컴플렉스/태그 연출 없이 결과(대사, 심박수, 키 판정)만 보여준다.</summary>
        private IEnumerator PassRoutine(TurnReport report)
        {
            _heartRate.PlayTurnResult(report);
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

            // TickDurations/스폰이 Resolve 이후에 일어나므로, 발광 전에 먼저 뇌의 영역 배치를 최신 보드
            // 상태로 맞춰야 한다(BrainView.PlayGlow 문서 참고).
            _brain.Refresh(Session.Complexes.InPriorityOrder().ToList());
            yield return PlayComplexReactions(report);

            yield return PlaySummaryWithResultTags(report);

            yield return PlayTagsAndHeartbeatTogether(report);

            // 태그가 올라가 사라지면 풍선을 지우개로 지우고, 판넬은 그와 함께 접힌다.
            _memoryBubble.SetEngaged(false);

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
                if (!step.Triggered || !_brain.PlayGlow(step.Complex)) continue;

                _portrait?.Flash();
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

        /// <summary>UI 가이드: "시퀀스가 끝나고, 남은 감정 태그도 생각 공간에 나타난다." 컴플렉스 발광이 끝나면 태그 칩이 풍선 안에 나타나고,
        /// 그동안 요약 대사가 타이핑된다. 칩이 다 나타난 뒤에도 최소 <c>tagHold</c>초는 가만히 둔다 — 읽을 시간 없이 바로 떠오르지 않게.</summary>
        private IEnumerator PlaySummaryWithResultTags(TurnReport report)
        {
            var tags = BuildResultTags(report);
            var appearedAt = Time.unscaledTime;
            var show = _memoryBubble.ShowResultTags(tags);

            yield return PlayDialogue(TurnSummaryFormatter.Build(report));

            if (tags.Count == 0) yield break;

            var readyAt = appearedAt + show.Duration() + UiMotion.Settings.tagHold;
            yield return new WaitUntil(() => Time.unscaledTime >= readyAt);
        }

        /// <summary>남은 감정 태그를 칩 목록으로 — 같은 감정은 "슬픔 ×2"처럼 하나로 묶고, 색은 침체/흥분 쪽을 따른다.</summary>
        private static List<MemorySpaceBubble.ResultTag> BuildResultTags(TurnReport report)
        {
            var tags = new List<MemorySpaceBubble.ResultTag>();
            if (report.FinalTags == null) return tags;

            foreach (var pair in report.FinalTags.Emotions)
            {
                var text = pair.Value > 1 ? $"{KoreanLabels.Emotion(pair.Key)} ×{pair.Value}" : KoreanLabels.Emotion(pair.Key);
                tags.Add(new MemorySpaceBubble.ResultTag(text, EmotionVisuals.ChipColor(pair.Key)));
            }

            return tags;
        }

        /// <summary>UI 가이드 원문: "태그가 위로 올라가며 사라지며, 그와 동시에 인디케이터가 움직인다."
        /// 두 연출을 같은 프레임에 시작하고, 더 긴 쪽(태그 상승)이 끝날 때까지 기다린다 — 심박수
        /// 파형·숫자의 심박수 전환 시간(UiMotionSettings.heartTransition, 기본 0.9초)을 태그 상승과 맞춰 두었다.</summary>
        private IEnumerator PlayTagsAndHeartbeatTogether(TurnReport report)
        {
            var tagSequence = _memoryBubble.RiseResultTags();
            _heartRate.PlayTurnResult(report);
            // 컴플렉스 포스트잇(남은 턴·새로 붙거나 만료된 컴플렉스)은 여기서 안 바뀐다 — 연출이 다 끝나고 포스트잇이 떼어져 있는 사이에 갱신된다.
            _traitStatus?.Refresh();
            AdvanceClock(report);
            _memoryBubble.SetPersistentSummary(TurnSummaryFormatter.BuildFinalEmotionSummary(report));

            yield return tagSequence.WaitForCompletion(true);
        }
    }
}
