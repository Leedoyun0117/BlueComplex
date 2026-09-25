using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 밸런스 측정용 아이템 사용 규칙. 코어 상태를 바꾸지 않고 "이 아이템을 쓰고 다음 카드를 냈을 때"를 미리 계산해서(사본 보드·특성으로),
    /// 현재 쿼터 목표 구역에 더 가까워지는 아이템만 쓴다 — 목표 방향에 도움 되는 아이템을 즉시 사용하는 단순 규칙이다.
    ///
    /// 판단은 근시안이다: 다음에 낼 카드 한 장의 결과만 본다(손패에서 가장 좋은 카드). 각 아이템은 자기 효과를 이렇게 흉내 낸다 —
    /// 감정적 설득(스테이지가 지정한 컴플렉스 무시), 기억 공감(슬픔 1씩 제거 + 환각), 무관심(타인 결과의 침체 감정 무시 + 무력), 공존감(타인 결과에 행복 1), 논리적 설득(중복 제거 + 랜덤 특성 하나 제거 — 지워질 수 있는 경우의 평균), 사마리아인(침체면 +10, 무력),
    /// 심호흡(흥분이면 -10, 무력), 자아비대(결과 감정 전체 x2), 명상(흥분 감정이 침체보다 많으면 슬픔 +1, 예민),
    /// 극복(고른 컴플렉스가 없다고 보고 계산 — 지속이 줄어드는 만큼의 이득은 다음 턴 이후라 근사), 회상·선택적 기억(손패가 목표에서 멀어질 때만 무작위 교체).
    /// 이득이 <see cref="MinGain"/> 미만이면 쓰지 않는다.
    /// </summary>
    public static class ItemBots
    {
        /// <summary>쓸 만하다고 보는 최소 이득(구역까지 거리 감소량 — 감정 태그 하나 = 10).</summary>
        public const int MinGain = 10;

        private static readonly IEmotionPolarityTable PolarityTable = new DefaultEmotionPolarityTable();

        private sealed class Hypothesis
        {
            public HashSet<string> IgnoredComplexIds = new();
            public ComplexInstance RemovedComplex;
            public bool RemoveSadness;
            public bool IgnoreDepressedTowardOther;
            public bool AddHappinessTowardOther;
            public bool Collapse;
            public bool DoubleEmotions;
            public bool MeditateSadness;
            public string ExtraTrait;
            public string RemovedTrait;
            public int HeartbeatDelta;
        }

        /// <summary>쓸 만한 아이템이 없을 때까지 하나씩 쓴다. 쓴 아이템 id를 순서대로 돌려준다.</summary>
        public static List<string> UseHelpfulItems(StageSession session, StageConfig config)
        {
            var used = new List<string>();
            for (var guard = 0; guard < session.Items.Capacity; guard++)
            {
                if (session.Runner.Outcome != Turn.StageOutcome.InProgress || session.Hand.Cards.Count == 0) break;

                var choice = ChooseItem(session, config, out var target);
                if (choice == null) break;

                session.Runner.UseItem(choice, target);
                used.Add(choice.Id);
            }

            return used;
        }

        public static ItemDefinition ChooseItem(StageSession session, StageConfig config, out ItemTarget target)
        {
            target = null;
            var zone = CurrentZone(session);
            var baseline = BestDistance(session, new Hypothesis(), zone);

            ItemDefinition best = null;
            var bestGain = MinGain - 1;

            foreach (var item in session.Items.Held)
            {
                if (!session.Runner.CanUseItem(item)) continue;

                var gain = Gain(session, config, item, zone, baseline, out var candidateTarget);
                if (gain <= bestGain) continue;

                best = item;
                bestGain = gain;
                target = candidateTarget;
            }

            return best;
        }

        private static int Gain(StageSession session, StageConfig config, ItemDefinition item, KeyZone zone, int baseline, out ItemTarget target)
        {
            target = null;
            switch (item.Id)
            {
                case "item_persuasion":
                {
                    // 스테이지가 지정한 컴플렉스(StageConfig.ItemParameters)를 무시한다고 보고 계산한다.
                    var ids = config.ItemParameters.TryGetValue(PrototypeContent.PersuasionTargetsKey, out var listed)
                        ? new HashSet<string>(listed)
                        : new HashSet<string>();
                    return baseline - BestDistance(session, new Hypothesis { IgnoredComplexIds = ids }, zone);
                }
                case "item_empathy":
                    return baseline - BestDistance(session,
                        new Hypothesis { RemoveSadness = true, ExtraTrait = PrototypeContent.TraitHallucination }, zone);
                case "item_indifference":
                    return baseline - BestDistance(session,
                        new Hypothesis { IgnoreDepressedTowardOther = true, ExtraTrait = PrototypeContent.TraitLethargy }, zone);
                case "item_coexistence":
                    return baseline - BestDistance(session, new Hypothesis { AddHappinessTowardOther = true }, zone);
                case "item_logic":
                {
                    // 중복 정리 + 붙어 있는 특성 하나를 랜덤으로 지운다 — 어느 것이 지워질지 모르니 지워질 수 있는 경우의 평균 거리로 본다(특성이 없으면 정리 효과만).
                    var held = session.Traits.Traits.Select(t => t.Definition.Id).ToList();
                    if (held.Count == 0) return baseline - BestDistance(session, new Hypothesis { Collapse = true }, zone);

                    var expected = held.Average(id => BestDistance(session, new Hypothesis { Collapse = true, RemovedTrait = id }, zone));
                    return baseline - (int)Math.Round(expected);
                }
                case "item_deep_breath":
                {
                    var lowered = session.Zone.PolarityOf(session.Heartbeat.Value) == Polarity.Excited ? -10 : 0;
                    return baseline - BestDistance(session,
                        new Hypothesis { HeartbeatDelta = lowered, ExtraTrait = PrototypeContent.TraitLethargy }, zone);
                }
                case "item_ego_inflation":
                    return baseline - BestDistance(session, new Hypothesis { DoubleEmotions = true }, zone);
                case "item_meditation":
                    return baseline - BestDistance(session,
                        new Hypothesis { MeditateSadness = true, ExtraTrait = PrototypeContent.TraitSensitive }, zone);
                case "item_samaritan":
                {
                    var raised = session.Zone.PolarityOf(session.Heartbeat.Value) == Polarity.Depressed ? 10 : 0;
                    return baseline - BestDistance(session,
                        new Hypothesis { HeartbeatDelta = raised, ExtraTrait = PrototypeContent.TraitLethargy }, zone);
                }
                case "item_overcome":
                {
                    ItemTarget bestTarget = null;
                    var bestGain = 0;
                    foreach (var candidate in session.Runner.GetItemTargets(item).OfType<ComplexTarget>())
                    {
                        var gain = baseline - BestDistance(session, new Hypothesis { RemovedComplex = candidate.Complex, ExtraTrait = PrototypeContent.TraitSensitive }, zone);
                        if (gain <= bestGain) continue;

                        bestGain = gain;
                        bestTarget = candidate;
                    }

                    target = bestTarget;
                    return bestGain;
                }
                case "item_recollection":
                    // 무작위로 새 손패를 받는다 — 지금 손패의 최선이 목표에서 크게 멀 때만.
                    return baseline >= 30 && !CanImprove(session, zone) ? MinGain : 0;
                case "item_selective_memory":
                {
                    if (baseline < 20 || CanImprove(session, zone)) return 0;

                    // 목표에서 가장 멀어지게 하는 카드를 바꾼다.
                    var worst = session.Hand.Cards
                        .OrderByDescending(card => Distance(session, card, new Hypothesis(), zone))
                        .First();
                    target = ItemTarget.Of(worst);
                    return MinGain;
                }
                default:
                    return 0;
            }
        }

        private static KeyZone CurrentZone(StageSession session)
        {
            var schedule = session.Runner.Schedule;
            return session.Keys.Zones[schedule.LastTurnOf(session.Runner.CurrentQuarter)];
        }

        private static bool CanImprove(StageSession session, KeyZone zone)
        {
            var now = DistanceToZone(session.Heartbeat.Value, zone);
            return session.Hand.Cards.Any(card => Distance(session, card, new Hypothesis(), zone) < now);
        }

        private static int BestDistance(StageSession session, Hypothesis hypothesis, KeyZone zone) =>
            session.Hand.Cards.Min(card => Distance(session, card, hypothesis, zone));

        private static int Distance(StageSession session, ClueInstance card, Hypothesis hypothesis, KeyZone zone)
        {
            var position = Math.Clamp(session.Heartbeat.Value + hypothesis.HeartbeatDelta, Heartbeat.MinValue, Heartbeat.MaxValue);
            var delta = PredictDelta(session, card, hypothesis, position);
            var after = Math.Clamp(position + delta, Heartbeat.MinValue, Heartbeat.MaxValue);

            // 즉사 구간으로 가는 카드는 아주 멀리 있는 것으로 친다.
            return session.Zone.IsFatal(after) ? 1000 : DistanceToZone(after, zone);
        }

        /// <summary>이 카드를 다음 턴에 냈을 때의 심박수 변화량 예측(TurnRunner.PlayClue의 순서를 사본으로 그대로 따른다).</summary>
        private static int PredictDelta(StageSession session, ClueInstance card, Hypothesis hypothesis, int startHeartbeat)
        {
            var traits = new TraitBoard(session.Traits.Catalog);
            foreach (var trait in session.Traits.Traits)
                if (trait.Definition.Id != hypothesis.RemovedTrait) traits.Grant(trait.Definition, trait.RemainingTurns);
            if (hypothesis.ExtraTrait != null) traits.Grant(hypothesis.ExtraTrait);

            var board = new ComplexBoard(session.Complexes.MaxSlots);
            foreach (var slot in session.Complexes.Slots)
            {
                if (slot == hypothesis.RemovedComplex) continue;
                board.TryAttach(new ComplexInstance(slot.Definition, slot.Priority, slot.RemainingTurns));
            }

            var filter = new CombinedFilter(session.ActiveItems, hypothesis.IgnoredComplexIds);
            var input = traits.ApplyToOriginal(card.Definition.CreateOriginalTagSet());
            var final = new ComplexResolver(board).Resolve(input, filter).Final;

            session.ActiveItems.Modify(final);
            if (hypothesis.RemoveSadness) final.RemoveEmotion(EmotionTag.Sadness);

            if (hypothesis.IgnoreDepressedTowardOther && final.HasPerson(PersonTag.Other))
            {
                foreach (var emotion in new[] { EmotionTag.Sadness, EmotionTag.Disgust, EmotionTag.Fear })
                    final.RemoveEmotion(emotion, final.CountOf(emotion));
            }

            if (hypothesis.AddHappinessTowardOther && final.HasPerson(PersonTag.Other)) final.AddEmotion(EmotionTag.Happiness);

            if (hypothesis.Collapse) final.CollapseDuplicates();

            if (hypothesis.DoubleEmotions)
            {
                foreach (var (emotion, count) in final.Emotions.ToList()) final.AddEmotion(emotion, count);
            }

            if (hypothesis.MeditateSadness)
            {
                var excited = final.Emotions.Where(e => PolarityTable.GetPolarity(e.Key) == Polarity.Excited).Sum(e => e.Value);
                var depressed = final.Emotions.Where(e => PolarityTable.GetPolarity(e.Key) == Polarity.Depressed).Sum(e => e.Value);
                if (excited > depressed) final.AddEmotion(EmotionTag.Sadness);
            }

            return new TraitAwareEmotionEvaluator(PolarityTable, traits).Evaluate(final);
        }

        private sealed class CombinedFilter : IComplexFilter
        {
            private readonly IComplexFilter _active;
            private readonly HashSet<string> _ids;

            public CombinedFilter(IComplexFilter active, HashSet<string> ids)
            {
                _active = active;
                _ids = ids;
            }

            public bool ShouldIgnore(ComplexInstance complex) =>
                _active.ShouldIgnore(complex) || (_ids != null && _ids.Contains(complex.Definition.Id));
        }

        private static int DistanceToZone(int position, KeyZone zone)
        {
            if (zone.Contains(position)) return 0;
            var lastSlot = zone.StartSlot + zone.Width - 1;
            return Math.Min(Math.Abs(position - zone.StartSlot), Math.Abs(position - lastSlot));
        }
    }
}
