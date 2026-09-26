using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Tags;
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

        /// <summary>이 턴에 카드를 내려면 이미 켜져 있어야 하는 아이템 id. null이면 상관없다.</summary>
        public string RequiredActiveItemId { get; }

        /// <summary>이 턴이 시작될 때 손에 쥐여 주는 아이템(빈 칸이 있을 때만). null이면 아무것도 주지 않는다.
        /// 아이템을 언제 처음 만나게 할지는 여기서 정한다 — 그 전에는 아이템이 손에 없어서 미리 쓸 수 없다.</summary>
        public ItemDefinition GrantItem { get; }

        public TutorialStep(int turn,
                            IReadOnlyList<ClueDefinition> hand = null,
                            Polarity? targetPolarity = null,
                            string requiredActiveItemId = null,
                            ItemDefinition grantItem = null)
        {
            Turn = turn;
            Hand = hand;
            TargetPolarity = targetPolarity;
            RequiredActiveItemId = requiredActiveItemId;
            GrantItem = grantItem;
        }
    }

    /// <summary>
    /// 튜토리얼을 코어 위에서 스크립트대로 굴리는 부품. 세션은 일반 스테이지와 똑같이 만들어지고(<see cref="StageFactory"/>), 이 클래스가
    /// 턴이 시작될 때 손패를 정해 준 카드로 바꾸고(<see cref="ClueHand.ReplaceWith"/>) 카드를 내기 전에 오답을 걸러 낸다(<see cref="IPlayGate"/>).
    /// 턴 진행·해석·심박수·키 판정은 코어가 그대로 한다 — 이 클래스는 판정을 대신하지 않고 "어떤 카드를 내도 되는지"만 정한다.
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
            session.PlayGate = new Gate(this, session);
            session.Runner.TurnBegan += turn => ApplyStep(session, turn);
        }

        private void ApplyStep(StageSession session, int turn)
        {
            var step = StepOf(turn);
            if (step == null) return;

            if (step.Hand != null) session.Hand.ReplaceWith(step.Hand);
            if (step.GrantItem != null && !session.Items.Holds(step.GrantItem)) session.Items.TryGrant(step.GrantItem);
        }

        /// <summary>이 카드를 지금 냈다면 나올 최종 결과 태그. 실제 턴과 같은 순서(특성 → 컴플렉스 → 아이템 보정)로 계산하지만 아무 상태도 바꾸지 않는다.</summary>
        internal static TagSet Preview(StageSession session, ClueDefinition clue)
        {
            var input = session.Traits.ApplyToOriginal(clue.CreateOriginalTagSet());
            var result = new ComplexResolver(session.Complexes).Resolve(input, session.ActiveItems);
            var final = result.Final;
            session.ActiveItems.Modify(final);
            return final;
        }

        private sealed class Gate : IPlayGate
        {
            private readonly TutorialScript _script;
            private readonly StageSession _session;

            public Gate(TutorialScript script, StageSession session)
            {
                _script = script;
                _session = session;
            }

            public PlayVerdict Check(ClueInstance card)
            {
                var step = _script.StepOf(_session.Runner.CurrentTurn);
                if (step == null) return PlayVerdict.Allow;

                if (step.TargetPolarity.HasValue)
                {
                    var final = Preview(_session, card.Definition);
                    var direction = final.EnumerateEmotionsFlat().Sum(e => (int)_script._polarityTable.GetPolarity(e));
                    var matches = step.TargetPolarity == Polarity.Excited ? direction > 0 : direction < 0;
                    if (!matches)
                        return PlayVerdict.Reject(PlayRejection.WrongDirection, final.Emotions.Keys.ToList());
                }

                if (step.RequiredActiveItemId != null
                    && !_session.ActiveItems.Active.Any(a => a.Definition.Id == step.RequiredActiveItemId))
                    return PlayVerdict.Reject(PlayRejection.ItemNeeded);

                return PlayVerdict.Allow;
            }
        }
    }
}
