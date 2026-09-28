using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Items;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>튜토리얼 한 턴의 진행 규칙. 턴은 1부터 센다.</summary>
    public sealed class TutorialStep
    {
        public int Turn { get; }

        /// <summary>이 턴이 시작될 때 손패를 통째로 이 단서들로 바꾼다. null이면 손패를 그대로 둔다. 빈 목록이면 손패를 비운다 —
        /// 손패가 비면 코어가 그 턴을 그냥 넘긴다(<see cref="TurnReport.IsPass"/>), 그래서 "아무 것도 내지 않고 시간만 흐르는 턴"이 된다.</summary>
        public IReadOnlyList<ClueDefinition> Hand { get; }

        /// <summary>이 턴에 낸 단서의 최종 결과가 향해야 하는 방향. null이면 어떤 카드든 낼 수 있다.</summary>
        public Polarity? TargetPolarity { get; }

        /// <summary>이 턴에 카드를 내려면 모두 켜져 있어야 하는 아이템 id들. 비어 있으면 상관없다.</summary>
        public IReadOnlyList<string> RequiredActiveItemIds { get; }

        /// <summary>이 턴이 시작될 때 손에 쥐여 주는 아이템들(빈 칸이 있을 때만, 순서대로). 비어 있으면 아무것도 주지 않는다.
        /// 아이템을 언제 처음 만나게 할지는 여기서 정한다 — 그 전에는 아이템이 손에 없어서 미리 쓸 수 없다.</summary>
        public IReadOnlyList<ItemDefinition> GrantItems { get; }

        /// <summary>
        /// true면 이 턴의 오답(이 카드를 내면 쿼터의 키를 못 얻는 수)을 내기 전에 막지 않는다 — 결과를 끝까지 보여 준 뒤 이 턴이 시작된 상태로 되돌린다(<see cref="PlayVerdict.IsTrial"/>).
        /// 방향(<see cref="TargetPolarity"/>) 대신 키 판정으로 정답을 가린다. 키 턴(쿼터의 마지막 턴)에서만 뜻이 있다.
        /// </summary>
        public bool ShowMissThenRollBack { get; }

        public TutorialStep(int turn,
                            IReadOnlyList<ClueDefinition> hand = null,
                            Polarity? targetPolarity = null,
                            IReadOnlyList<string> requiredActiveItemIds = null,
                            IReadOnlyList<ItemDefinition> grantItems = null,
                            bool showMissThenRollBack = false)
        {
            Turn = turn;
            Hand = hand;
            TargetPolarity = targetPolarity;
            RequiredActiveItemIds = requiredActiveItemIds ?? Array.Empty<string>();
            GrantItems = grantItems ?? Array.Empty<ItemDefinition>();
            ShowMissThenRollBack = showMissThenRollBack;
        }
    }

    /// <summary>
    /// 튜토리얼을 코어 위에서 스크립트대로 굴리는 부품. 세션은 일반 스테이지와 똑같이 만들어지고(<see cref="StageFactory"/>), 이 클래스가
    /// 턴이 시작될 때 손패를 정해 준 카드로 바꾸고(<see cref="ClueHand.ReplaceWith"/>) 카드를 내기 전에 오답을 걸러 낸다(<see cref="IPlayGate"/>).
    /// 턴 진행·해석·심박수·키 판정은 코어가 그대로 한다 — 이 클래스는 판정을 대신하지 않고 "어떤 카드를 내도 되는지"만 정한다.
    ///
    /// 결과를 보여 주는 오답(<see cref="TutorialStep.ShowMissThenRollBack"/>)도 실제로 내지 않는다: 화면은 미리보기(<see cref="TurnRunner.PreviewPlay"/>)를 연출하고,
    /// 그 뒤 <see cref="IPlayGate.RollBackTrial"/>이 이 턴에 플레이어가 바꾼 것(아이템 사용 → 보유·지속 효과·특성·심박수)만 턴 시작 때 떠 둔 상태로 돌린다.
    /// 카드를 실제로 내지 않았으니 손패·관찰 기록·컴플렉스 남은 턴·턴 번호·키는 처음부터 움직이지 않는다.
    /// </summary>
    public sealed class TutorialScript
    {
        private readonly IReadOnlyList<TutorialStep> _steps;
        private readonly IEmotionPolarityTable _polarityTable;

        public TutorialScript(IReadOnlyList<TutorialStep> steps, IEmotionPolarityTable polarityTable)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _polarityTable = polarityTable ?? throw new ArgumentNullException(nameof(polarityTable));
        }

        public IReadOnlyList<TutorialStep> Steps => _steps;

        public TutorialStep StepOf(int turn) => _steps.FirstOrDefault(step => step.Turn == turn);

        /// <summary>세션에 스크립트를 붙인다 — 턴 시작 구독과 게이트 연결. 첫 턴이 시작되기 전(<c>Runner.StartStage()</c> 전)에 불러야 한다.</summary>
        public void Attach(StageSession session)
        {
            var gate = new Gate(this, session);
            session.PlayGate = gate;
            session.Runner.TurnBegan += turn =>
            {
                ApplyStep(session, turn);
                gate.CaptureTurnStart();
            };
        }

        private void ApplyStep(StageSession session, int turn)
        {
            var step = StepOf(turn);
            if (step == null) return;

            if (step.Hand != null) session.Hand.ReplaceWith(step.Hand);
            foreach (var item in step.GrantItems)
                if (!session.Items.Holds(item)) session.Items.TryGrant(item);
        }

        /// <summary>턴이 시작된 직후(스크립트가 손패·아이템을 준 뒤)의 상태 — 결과를 보여 준 오답을 되돌릴 때 이리로 돌아온다.</summary>
        private sealed class TurnStart
        {
            public int Heartbeat;
            public List<ItemDefinition> Held;
            public List<ActiveItem> Active;
            public List<TraitInstance> Traits;
        }

        private sealed class Gate : IPlayGate
        {
            private readonly TutorialScript _script;
            private readonly StageSession _session;
            private TurnStart _turnStart;

            public Gate(TutorialScript script, StageSession session)
            {
                _script = script;
                _session = session;
            }

            public void CaptureTurnStart() => _turnStart = new TurnStart
            {
                Heartbeat = _session.Heartbeat.Value,
                Held = _session.Items.Held.ToList(),
                Active = _session.ActiveItems.Active.ToList(),
                Traits = _session.Traits.Traits.ToList()
            };

            public PlayVerdict Check(ClueInstance card)
            {
                var step = _script.StepOf(_session.Runner.CurrentTurn);
                if (step == null) return PlayVerdict.Allow;

                // 아이템부터 본다 — 함께 써야 하는 아이템을 안 켰으면 어떤 카드든 먼저 아이템을 안내한다.
                if (step.RequiredActiveItemIds.Any(id => _session.ActiveItems.Active.All(a => a.Definition.Id != id)))
                    return PlayVerdict.Reject(PlayRejection.ItemNeeded);

                // 미리보기는 실제 턴과 같은 평가기로 심박수 변화까지 낸다 — 환각(극성 반전) 같은 특성도 반영된다.
                var preview = _session.Runner.PreviewPlay(card);
                var observed = preview.FinalTags.Emotions.Keys.ToList();

                if (step.ShowMissThenRollBack)
                {
                    return preview.KeyResult is { Success: true }
                        ? PlayVerdict.Allow
                        : PlayVerdict.Trial(PlayRejection.KeyMissed, observed);
                }

                if (step.TargetPolarity.HasValue)
                {
                    var matches = step.TargetPolarity == Polarity.Excited ? preview.HeartbeatDelta > 0 : preview.HeartbeatDelta < 0;
                    if (!matches) return PlayVerdict.Reject(PlayRejection.WrongDirection, observed);
                }

                return PlayVerdict.Allow;
            }

            public void RollBackTrial()
            {
                if (_turnStart == null) return;

                // 특성을 먼저 떼고(결과 화면의 "새로 발현된 특성"에서도 빠진다) 지속 효과를 거둔 뒤 아이템을 칸에 돌려놓는다 — 돌아온 아이템(Gained)을 받은 화면이 특성 표시까지 맞는 상태를 본다.
                _session.Traits.RestoreTo(_turnStart.Traits);
                _session.ActiveItems.RestoreTo(_turnStart.Active);
                _session.Items.RestoreHeld(_turnStart.Held);
                _session.Heartbeat.Change(_turnStart.Heartbeat - _session.Heartbeat.Value);
            }
        }
    }
}
