using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    public readonly struct BotRunResult
    {
        public StageOutcome Outcome { get; }
        public int KeysCollected { get; }
        public int KeyRequired { get; }

        /// <summary>실제로 판정까지 간 쿼터 수.</summary>
        public int KeyAttempts { get; }
        public int KeyHits { get; }
        public int TurnsPlayed { get; }

        /// <summary>쿼터별 판정 결과(인덱스 0 = 1쿼터). null=판정 전에 스테이지가 끝남.</summary>
        public IReadOnlyList<bool?> QuarterResults { get; }

        /// <summary>쿼터별 목표 구역(인덱스 0 = 1쿼터).</summary>
        public IReadOnlyList<KeyZone> Zones { get; }

        public BotRunResult(StageOutcome outcome, int keysCollected, int keyRequired, int keyAttempts, int keyHits,
                            int turnsPlayed, IReadOnlyList<bool?> quarterResults, IReadOnlyList<KeyZone> zones)
        {
            Outcome = outcome;
            KeysCollected = keysCollected;
            KeyRequired = keyRequired;
            KeyAttempts = keyAttempts;
            KeyHits = keyHits;
            TurnsPlayed = turnsPlayed;
            QuarterResults = quarterResults;
            Zones = zones;
        }
    }

    /// <summary>
    /// 밸런스 측정용 봇. 코어 상태를 전혀 변형하지 않고 미리보기만 한다(ComplexResolver.Resolve는 순수 함수).
    /// 무전략 봇은 "손패 첫 장", 휴리스틱 봇은 쿼터 구조를 안다 — 키는 쿼터 마지막 턴 종료 시점에만 판정되므로
    /// 중간 턴의 위치는 의미가 없고, 남은 쿼터 동안 낼 카드 순서 전체를 미리 시뮬레이션해 마지막 위치를 구역에 맞춘다.
    /// </summary>
    public static class QuarterBots
    {
        public static ClueInstance ChooseFirstCard(StageSession session) => session.Hand.Cards[0];

        /// <summary>
        /// 현재 쿼터의 남은 턴 수만큼 손패에서 카드를 골라 내는 모든 순서를 미리 돌려보고(컴플렉스 지속 시간 감소까지 반영),
        /// 쿼터 마지막 턴 종료 시점의 심박수가 현재 쿼터 구역 안에 들어가는 순서의 첫 카드를 낸다.
        /// 이번 쿼터를 놓칠 수밖에 없다면 그 구역을 쫓지 않고 다음 쿼터 구역에 가까운 순서를 고른다. Fatal 구간을 밟는 순서는 피한다.
        /// 스폰(무작위)과 아이템 사용은 예측하지 않는다.
        /// </summary>
        public static ClueInstance ChooseHeuristicCard(StageSession session) =>
            ChooseHeuristicCard(session, card => card.Definition.CreateOriginalTagSet());

        /// <summary>
        /// 위 휴리스틱과 같되, 카드의 원본 태그를 <paramref name="tagsOf"/>가 준 것으로 본다 — 기본은 진짜 태그(완전 정보)이고,
        /// 해금 상태만 아는 봇은 자기가 믿는 태그를 넘긴다(KnowledgeBots). 컴플렉스·심박수·구역은 화면에 다 보이므로 진짜 값을 쓴다.
        /// </summary>
        public static ClueInstance ChooseHeuristicCard(StageSession session, Func<ClueInstance, TagSet> tagsOf)
        {
            var schedule = session.Runner.Schedule;
            var quarter = session.Runner.CurrentQuarter;
            var target = session.Keys.Zones[schedule.LastTurnOf(quarter)];
            KeyZone? next = quarter < schedule.QuarterCount ? session.Keys.Zones[schedule.LastTurnOf(quarter + 1)] : null;

            var turnsLeft = schedule.LastTurnOf(quarter) - session.Runner.CurrentTurn + 1;
            var hand = session.Hand.Cards;
            var picks = Math.Min(turnsLeft, hand.Count);

            var evaluator = new TraitAwareEmotionEvaluator(new DefaultEmotionPolarityTable(), session.Traits);

            ClueInstance best = null;
            (int fatal, int miss, int nextDistance, int distance) bestScore = (int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);

            foreach (var sequence in Sequences(hand, picks))
            {
                var (finalPosition, fatal) = Simulate(session, sequence, evaluator, tagsOf);
                var distance = DistanceToZone(finalPosition, target);

                // 이번 쿼터를 맞출 수 있으면 맞추는 순서가 최우선. 못 맞춘다면 쫓아가지 않고(도달 불가능한 구역을
                // 향해 가면 다음 쿼터만 망친다) 다음 쿼터 구역에 가까이 가는 순서를 고른다. 마지막 쿼터는 그냥 가장 가깝게.
                var miss = distance > 0 ? 1 : 0;
                var nextDistance = miss == 1 && next.HasValue ? DistanceToZone(finalPosition, next.Value) : 0;
                var score = (fatal ? 1 : 0, miss, nextDistance, distance);

                if (best == null || score.CompareTo(bestScore) < 0)
                {
                    best = sequence[0];
                    bestScore = score;
                }
            }

            return best;
        }

        private static (int finalPosition, bool fatal) Simulate(StageSession session, IReadOnlyList<ClueInstance> sequence,
                                                                 IEmotionEvaluator evaluator, Func<ClueInstance, TagSet> tagsOf)
        {
            var board = CloneBoard(session.Complexes);
            var resolver = new ComplexResolver(board);
            var position = session.Heartbeat.Value;
            var plain = new EmotionEvaluator(new DefaultEmotionPolarityTable());

            for (var i = 0; i < sequence.Count; i++)
            {
                // 지금 붙어 있는 일반 특성과 지속 중인 아이템 효과는 바로 다음 한 턴에만 적용된다(1턴 지속) — 첫 카드만 그걸 반영해 예측한다.
                var first = i == 0;
                var original = tagsOf(sequence[i]).Clone();
                var interpretation = resolver.Resolve(first ? session.Traits.ApplyToOriginal(original) : original,
                    first ? session.ActiveItems : null);
                if (first) session.ActiveItems.Modify(interpretation.Final);

                var delta = first ? evaluator.Evaluate(interpretation.Final) : plain.Evaluate(interpretation.Final);
                position = Math.Clamp(position + delta, Heartbeat.MinValue, Heartbeat.MaxValue);
                if (session.Zone.IsFatal(position)) return (position, true);
                board.TickDurations();
            }

            return (position, false);
        }

        private static ComplexBoard CloneBoard(ComplexBoard source)
        {
            var clone = new ComplexBoard(source.MaxSlots);
            foreach (var slot in source.Slots)
                clone.TryAttach(new ComplexInstance(slot.Definition, slot.Priority, slot.RemainingTurns));
            return clone;
        }

        /// <summary>손패에서 count장을 고르는 모든 순서(순열).</summary>
        private static IEnumerable<List<ClueInstance>> Sequences(IReadOnlyList<ClueInstance> hand, int count)
        {
            if (count == 0)
            {
                yield return new List<ClueInstance>();
                yield break;
            }

            for (var i = 0; i < hand.Count; i++)
            {
                var rest = hand.Where((_, index) => index != i).ToList();
                foreach (var tail in Sequences(rest, count - 1))
                {
                    tail.Insert(0, hand[i]);
                    yield return tail;
                }
            }
        }

        private static int DistanceToZone(int position, KeyZone zone)
        {
            if (zone.Contains(position)) return 0;
            var lastSlot = zone.StartSlot + zone.Width - 1;
            return Math.Min(Math.Abs(position - zone.StartSlot), Math.Abs(position - lastSlot));
        }

        /// <param name="log">null이면 상세 로그를 남기지 않는다(대표본 실행용).</param>
        /// <param name="placerFactory">null이면 StageFactory 기본 배치기. 배치 정책을 바꿔 비교할 때 넘긴다(같은 난수원을 받는다).</param>
        public static BotRunResult RunStage(StageConfig config, int seed, string label,
                                            Func<StageSession, ClueInstance> chooseCard, StringBuilder log,
                                            Func<StageConfig, IRandomSource, HeartbeatZone, IEmotionPolarityTable, IKeyZonePlacer> placerFactory = null)
        {
            var random = new SystemRandomSource(seed);
            var polarityTable = new DefaultEmotionPolarityTable();
            var placer = placerFactory?.Invoke(config, random, new HeartbeatZone(), polarityTable);
            var session = StageFactory.Create(config, random, new ClueKnowledgeLedger(), polarityTable, keyPlacer: placer);
            var schedule = config.Quarters;

            var passReports = new List<TurnReport>();
            session.Runner.TurnResolved += report =>
            {
                if (report.IsPass) passReports.Add(report);
            };

            log?.AppendLine($"--- {label} (seed={seed}) ---");
            session.Runner.StartStage();
            log?.AppendLine("  쿼터 구역(확정): " + string.Join(", ",
                session.Keys.Zones.OrderBy(p => p.Key)
                    .Select(p => $"{schedule.QuarterOf(p.Key)}쿼터:{p.Value.StartSlot}~{p.Value.StartSlot + p.Value.Width - 1}")));

            var guard = 0;
            while (session.Runner.Outcome == StageOutcome.InProgress && guard < config.TotalTurns)
            {
                guard++;
                Assert.Greater(session.Hand.Cards.Count, 0,
                    $"[{label} seed={seed}] 턴 {session.Runner.CurrentTurn} 시작 시 손패가 비어 있으면 안 된다(비면 러너가 넘긴다).");

                var card = chooseCard(session);
                var before = session.Heartbeat.Value;

                TurnReport report = null;
                Assert.DoesNotThrow(() => report = session.Runner.PlayClue(card),
                    $"[{label} seed={seed}] 턴 {session.Runner.CurrentTurn} 진행 중 예외 없이 처리되어야 한다.");

                log?.AppendLine($"  [{report.Quarter}쿼터 {report.TurnInQuarter}/{schedule.TurnsPerQuarter} · 턴 {report.Turn}] " +
                                $"{card.Definition.DisplayName} 이동:{report.HeartbeatDelta} ({before}→{report.HeartbeatValue})" +
                                $"{DescribeJudgement(report)} 결과:{report.Outcome}");

                foreach (var pass in passReports)
                {
                    log?.AppendLine($"  [{pass.Quarter}쿼터 {pass.TurnInQuarter}/{schedule.TurnsPerQuarter} · 턴 {pass.Turn}] " +
                                    $"손패 없음 — 넘어감 (심박수 {pass.HeartbeatValue}){DescribeJudgement(pass)} 결과:{pass.Outcome}");
                }
                passReports.Clear();
            }

            var judged = session.Keys.Results.Count(r => r.HasValue);
            log?.AppendLine($"  => {session.Runner.Outcome}, 키 {session.Keys.Collected}/{config.RequiredKeys} " +
                            $"(쿼터 성공 {session.Keys.Collected}/{judged}), 총 {session.Runner.CurrentTurn}턴");

            Assert.AreNotEqual(StageOutcome.InProgress, session.Runner.Outcome,
                $"[{label} seed={seed}] {config.TotalTurns}턴 이내에 종료되어야 한다.");

            return new BotRunResult(session.Runner.Outcome, session.Keys.Collected, config.RequiredKeys,
                judged, session.Keys.Collected, session.Runner.CurrentTurn, session.Keys.Results.ToList(),
                schedule.QuarterEndTurns().Select(t => session.Keys.Zones[t]).ToList());
        }

        private static string DescribeJudgement(TurnReport report) =>
            report.KeyResult is { } j
                ? $" ▶ {j.Quarter}쿼터 키 {(j.Success ? "성공" : "실패")}(구역 {j.Zone.StartSlot}~{j.Zone.StartSlot + j.Zone.Width - 1})"
                : string.Empty;
    }
}
