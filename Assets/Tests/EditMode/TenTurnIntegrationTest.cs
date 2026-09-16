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
    /// 10턴 통합 실행 — 시드 고정, 매 턴 손패의 첫 카드를 내는 단순 전략으로
    /// 프로토타입 스테이지를 끝까지 자동 진행한다. 예외 없이 완주하는 것이 1차 목표이며,
    /// 로그는 밸런스(특히 검열 지속 턴 수)를 눈으로 확인하기 위한 참고 자료다.
    /// </summary>
    public class TenTurnIntegrationTest
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

        [TestCase(20260916)]
        [TestCase(1)]
        [TestCase(12345)]
        [TestCase(777)]
        public void PrototypeStage_RunsToCompletion_Over10Turns_WithFixedSeed(int seed)
        {
            var random = new SystemRandomSource(seed);
            var polarityTable = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarityTable);
            var ledger = new ClueKnowledgeLedger();

            var session = StageFactory.Create(config, random, ledger, polarityTable);

            // OpenZone/Judge, CensorshipChanged 는 TurnRunner 내부에서만 호출되고 TurnReport에
            // 직접 노출되지 않으므로, 로그 출력을 위해 이벤트로 턴별 상태를 따로 수집한다.
            var zoneByTurn = new Dictionary<int, KeyZone>();
            var judgeByTurn = new Dictionary<int, string>();
            session.Keys.ZoneOpened += zone => zoneByTurn[session.Runner.CurrentTurn] = zone;
            session.Keys.KeyCollected += count => judgeByTurn[session.Runner.CurrentTurn] = $"성공(누적 {count})";
            session.Keys.ZoneMissed += () => judgeByTurn[session.Runner.CurrentTurn] = "실패";

            int? censorshipStartTurn = null;
            var censorshipStreaks = new List<int>();
            session.Censorship.CensorshipChanged += censored =>
            {
                if (censored)
                {
                    censorshipStartTurn = session.Runner.CurrentTurn;
                }
                else if (censorshipStartTurn.HasValue)
                {
                    censorshipStreaks.Add(session.Runner.CurrentTurn - censorshipStartTurn.Value);
                    censorshipStartTurn = null;
                }
            };

            var log = new StringBuilder();
            log.AppendLine($"=== 10턴 통합 실행 (seed={seed}) ===");

            session.Runner.StartStage();

            var guard = 0;
            while (session.Runner.Outcome == StageOutcome.InProgress && guard < 10)
            {
                guard++;
                Assert.Greater(session.Hand.Cards.Count, 0, $"턴 {session.Runner.CurrentTurn} 시작 시 손패가 비어 있으면 안 된다.");

                var card = session.Hand.Cards[0];
                var originalTags = card.Definition.CreateOriginalTagSet();
                var positionBefore = session.Indicator.Position;

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
                log.AppendLine($"  이동: {report.IndicatorDelta}  인디케이터: {positionBefore} → {report.IndicatorPosition}");

                var zoneText = zoneByTurn.TryGetValue(report.Turn, out var zone)
                    ? $"{zone.StartSlot}~{zone.StartSlot + zone.Width - 1}"
                    : "없음";
                var judgeText = judgeByTurn.TryGetValue(report.Turn, out var judge) ? judge : "-";
                log.AppendLine($"  키 구역: {zoneText}  판정: {judgeText}");

                var censorText = session.Censorship.IsCensored
                    ? $"진행중 ({report.Turn - censorshipStartTurn.Value + 1}턴째)"
                    : "해제";
                log.AppendLine($"  검열: {censorText}");

                log.AppendLine($"  신규 컴플렉스: {(report.SpawnedComplex != null ? report.SpawnedComplex.Definition.DisplayName : "없음")}");
                log.AppendLine($"  결과: {report.Outcome}");
            }

            // 스테이지가 검열 상태인 채로 끝난 경우, 마지막 구간도 집계에 포함한다.
            if (censorshipStartTurn.HasValue)
                censorshipStreaks.Add(session.Runner.CurrentTurn - censorshipStartTurn.Value + 1);

            var streakSummary = censorshipStreaks.Count == 0
                ? "없음"
                : string.Join(", ", censorshipStreaks.Select(n => $"{n}턴")) + $" (합계 {censorshipStreaks.Sum()}턴)";

            log.AppendLine($"=== 종료: {session.Runner.Outcome}, 총 {session.Runner.CurrentTurn}턴, " +
                            $"획득 키 {session.Keys.Collected}/{config.RequiredKeys}, 검열 지속 구간: {streakSummary} ===");

            Debug.Log(log.ToString());

            Assert.AreNotEqual(StageOutcome.InProgress, session.Runner.Outcome, "10턴 이내에 Cleared 또는 Failed 로 종료되어야 한다.");
            Assert.LessOrEqual(session.Runner.CurrentTurn, 10);
        }
    }
}
