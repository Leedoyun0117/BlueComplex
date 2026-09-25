using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 12턴 3쿼터 통합 실행 — 시드 고정, 매 턴 손패의 첫 카드를 내는 단순 전략으로
    /// 프로토타입 스테이지를 끝까지 자동 진행한다. 예외 없이 완주하는 것이 1차 목표이며,
    /// 로그는 밸런스(특히 컴플렉스 지속 턴 수)를 눈으로 확인하기 위한 참고 자료다.
    /// </summary>
    public class QuarterStageIntegrationTest
    {
        private static string FormatTime(TimeTag time) => time switch
        {
            TimeTag.Past => "과거",
            TimeTag.Present => "현재",
            TimeTag.Future => "미래",
            _ => "-"
        };

        private static string FormatPerson(PersonTag person) => person switch
        {
            PersonTag.Family => "가족",
            PersonTag.Other => "타인",
            PersonTag.Friend => "친구",
            PersonTag.Lover => "연인",
            _ => "-"
        };

        private static string FormatEmotion(EmotionTag emotion) => emotion switch
        {
            EmotionTag.Sadness => "슬픔",
            EmotionTag.Disgust => "혐오",
            EmotionTag.Fear => "공포",
            EmotionTag.Happiness => "행복",
            EmotionTag.Love => "사랑",
            EmotionTag.Anger => "분노",
            _ => emotion.ToString()
        };

        private static string FormatPersons(IEnumerable<PersonTag> persons)
        {
            var list = persons.Select(FormatPerson).ToList();
            return list.Count == 0 ? "-" : string.Join(",", list);
        }

        private static string FormatEmotions(IReadOnlyDictionary<EmotionTag, int> emotions)
        {
            if (emotions.Count == 0) return "-";
            return string.Join(",", emotions.Select(p => FormatEmotion(p.Key) + (p.Value > 1 ? "x" + p.Value : "")));
        }

        private static string FormatTags(TagSet tags) =>
            $"{FormatTime(tags.Time)}/{FormatPersons(tags.Persons)}/{FormatEmotions(tags.Emotions)}";

        private static string FormatState(HeartbeatState state) => state switch
        {
            HeartbeatState.Fatal => "즉시 패배",
            HeartbeatState.VeryDepressed => "매우 침체",
            HeartbeatState.Depressed => "침체",
            HeartbeatState.Stable => "안정",
            HeartbeatState.Excited => "흥분",
            HeartbeatState.VeryExcited => "매우 흥분",
            _ => "-"
        };

        [TestCase(20260916)]
        [TestCase(1)]
        [TestCase(12345)]
        [TestCase(777)]
        public void PrototypeStage_RunsToCompletion_Over12Turns_WithFixedSeed(int seed)
        {
            var random = new SystemRandomSource(seed);
            var polarityTable = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarityTable);
            var ledger = new ClueKnowledgeLedger();

            var session = StageFactory.Create(config, random, ledger, polarityTable);

            var log = new StringBuilder();
            log.AppendLine($"=== 12턴 3쿼터 통합 실행 (seed={seed}) ===");

            session.Runner.StartStage();

            log.AppendLine("=== 쿼터별 키 구역 (스테이지 시작 시 확정, 쿼터 마지막 턴 종료 시점에 판정) ===");
            foreach (var pair in session.Keys.Zones.OrderBy(p => p.Key))
                log.AppendLine($"  {config.Quarters.QuarterOf(pair.Key)}쿼터({pair.Key}턴 종료 시): " +
                               $"{pair.Value.StartSlot}~{pair.Value.StartSlot + pair.Value.Width - 1}");

            var passReports = new List<TurnReport>();
            session.Runner.TurnResolved += r =>
            {
                if (r.IsPass) passReports.Add(r);
            };

            var totalTurns = config.TotalTurns;
            var guard = 0;
            while (session.Runner.Outcome == StageOutcome.InProgress && guard < totalTurns)
            {
                guard++;
                Assert.Greater(session.Hand.Cards.Count, 0, $"턴 {session.Runner.CurrentTurn} 시작 시 손패가 비어 있으면 안 된다.");

                // 저작된 단서 수(12)가 스테이지 전체 턴 수(12)와 같으므로, 쿼터 시작마다 지난 쿼터에
                // 낸 단서가 되돌아와 손패가 항상 가득 차야 한다(ClueHand.RefillForNewQuarter).
                if (config.Quarters.IsQuarterStart(session.Runner.CurrentTurn))
                    Assert.AreEqual(ClueHand.HandSize, session.Hand.Cards.Count,
                        $"쿼터 시작 턴({session.Runner.CurrentTurn})에는 손패가 가득 차 있어야 한다.");

                var card = session.Hand.Cards[0];
                var originalTags = card.Definition.CreateOriginalTagSet();
                var heartbeatBefore = session.Heartbeat.Value;

                TurnReport report = null;
                Assert.DoesNotThrow(() => report = session.Runner.PlayClue(card),
                    $"턴 {session.Runner.CurrentTurn} 진행 중 예외 없이 처리되어야 한다.");

                log.AppendLine($"[턴 {report.Turn}] 단서: {card.Definition.DisplayName} ({FormatTags(originalTags)})");

                var triggeredSteps = report.Interpretation.Steps.Where(s => s.Triggered).ToList();
                if (triggeredSteps.Count == 0)
                {
                    log.AppendLine("  발동: 없음");
                }
                else
                {
                    var chain = string.Join(" → ", triggeredSteps.Select(s => s.Complex.Definition.DisplayName));
                    log.AppendLine($"  발동: {chain}");
                    foreach (var step in triggeredSteps)
                    {
                        var emotions = step.ObservedEmotions.Count == 0
                            ? "-"
                            : string.Join(",", step.ObservedEmotions.Select(FormatEmotion));
                        log.AppendLine($"    - {step.Complex.Definition.DisplayName}: 시간={FormatTime(step.ObservedTime)}, " +
                                        $"인물={FormatPersons(step.ObservedPersons)}, 감정매칭={emotions}");
                    }
                }

                log.AppendLine($"  최종: {FormatTags(report.FinalTags)}");
                log.AppendLine($"  이동: {report.HeartbeatDelta}  심박수: {heartbeatBefore} → {report.HeartbeatValue}");

                var judgeText = report.KeyResult is { } judgement
                    ? $"{judgement.Quarter}쿼터 {(judgement.Success ? "성공" : "실패")}" +
                      $"(구역 {judgement.Zone.StartSlot}~{judgement.Zone.StartSlot + judgement.Zone.Width - 1}, 누적 {session.Keys.Collected})"
                    : "-";
                log.AppendLine($"  쿼터 {report.Quarter} · {report.TurnInQuarter}/{config.Quarters.TurnsPerQuarter}턴  키 판정: {judgeText}");

                var state = session.Zone.StateOf(report.HeartbeatValue);
                log.AppendLine($"  상태: {FormatState(state)}");

                log.AppendLine($"  신규 컴플렉스: {(report.SpawnedComplex != null ? report.SpawnedComplex.Definition.DisplayName : "없음")}");
                log.AppendLine($"  결과: {report.Outcome}");

                // 손패가 비어 넘어간 턴은 PlayClue 호출 안에서 이어서 처리되므로 낸 턴 로그 뒤에 이어 붙인다.
                foreach (var pass in passReports)
                {
                    var passJudge = pass.KeyResult is { } j
                        ? $"{j.Quarter}쿼터 {(j.Success ? "성공" : "실패")}(구역 {j.Zone.StartSlot}~{j.Zone.StartSlot + j.Zone.Width - 1})"
                        : "-";
                    log.AppendLine($"[턴 {pass.Turn}] 손패 없음 — 넘어감  심박수: {pass.HeartbeatValue}  키 판정: {passJudge}  결과: {pass.Outcome}");
                }
                passReports.Clear();
            }

            log.AppendLine($"=== 종료: {session.Runner.Outcome}, 총 {session.Runner.CurrentTurn}턴, " +
                            $"획득 키 {session.Keys.Collected}/{config.RequiredKeys} ===");

            Debug.Log(log.ToString());

            Assert.AreNotEqual(StageOutcome.InProgress, session.Runner.Outcome, "마지막 턴 이내에 Cleared 또는 Failed 로 종료되어야 한다.");
            Assert.LessOrEqual(session.Runner.CurrentTurn, totalTurns);
        }
    }
}
