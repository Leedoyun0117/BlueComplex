using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.UI.Debugging
{
    /// <summary>
    /// StageSession의 턴 이벤트를 구독해 EditMode의 쿼터 통합 테스트와 같은 형식으로 Debug.Log를 남긴다.
    /// 순수하게 로그 조립만 담당하며 게임 규칙에는 관여하지 않는다.
    /// </summary>
    public sealed class StageTurnLogger : IDisposable
    {
        private readonly StageSession _session;

        public StageTurnLogger(StageSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _session.Runner.TurnResolved += OnTurnResolved;
        }

        public void Dispose()
        {
            _session.Runner.TurnResolved -= OnTurnResolved;
        }

        private void OnTurnResolved(TurnReport report)
        {
            var log = new StringBuilder();
            var heartbeatBefore = report.HeartbeatValue - report.HeartbeatDelta;

            if (report.IsPass)
            {
                log.AppendLine($"[턴 {report.Turn}] 손패 없음 — 넘어감  심박수: {report.HeartbeatValue}");
                AppendQuarterAndOutcome(log, report);
                UnityEngine.Debug.Log(log.ToString());
                LogStageEnd(report);
                return;
            }

            var originalTags = report.Clue.CreateOriginalTagSet();

            log.AppendLine($"[턴 {report.Turn}] 단서: {report.Clue.DisplayName} ({FormatTags(originalTags)})");

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

            AppendQuarterAndOutcome(log, report);

            UnityEngine.Debug.Log(log.ToString());
            LogStageEnd(report);
        }

        private void AppendQuarterAndOutcome(StringBuilder log, TurnReport report)
        {
            var judgeText = report.KeyResult is { } j
                ? $"{j.Quarter}쿼터 {(j.Success ? "성공" : "실패")}(구역 {j.Zone.StartSlot}~{j.Zone.StartSlot + j.Zone.Width - 1}, 심박수 {j.Position})"
                : "-";
            log.AppendLine($"  쿼터 {report.Quarter} · {report.TurnInQuarter}/{_session.Runner.Schedule.TurnsPerQuarter}턴  키 판정: {judgeText}");

            var state = _session.Zone.StateOf(report.HeartbeatValue);
            log.AppendLine($"  상태: {FormatState(state)}  검열: {FormatCensorship(_session.Censorship.Level)}");

            log.AppendLine($"  신규 컴플렉스: {(report.SpawnedComplex != null ? report.SpawnedComplex.Definition.DisplayName : "없음")}");
            log.Append($"  결과: {report.Outcome}");
        }

        private void LogStageEnd(TurnReport report)
        {
            if (report.Outcome != StageOutcome.InProgress)
                UnityEngine.Debug.Log($"=== 종료: {report.Outcome}, 총 {_session.Runner.CurrentTurn}턴, 획득 키 {_session.Keys.Collected}/{_session.Keys.Required} ===");
        }

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

        private static string FormatCensorship(CensorshipLevel level) => level switch
        {
            CensorshipLevel.None => "없음",
            CensorshipLevel.Partial => "일부",
            CensorshipLevel.Full => "전체",
            _ => "-"
        };

        private static string FormatTime(TimeTag time) => time switch
        {
            TimeTag.Past => "과거",
            TimeTag.Present => "현재",
            TimeTag.Future => "미래",
            _ => "-"
        };

        private static string FormatTimes(IReadOnlyList<TimeTag> times) =>
            times.Count == 0 ? "-" : string.Join("+", times.Select(FormatTime));

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
            $"{FormatTimes(tags.Times)}/{FormatPersons(tags.Persons)}/{FormatEmotions(tags.Emotions)}";
    }
}
