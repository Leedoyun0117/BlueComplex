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
    /// StageSession의 턴 이벤트를 구독해 EditMode의 10턴 통합 테스트와 같은 형식으로 Debug.Log를 남긴다.
    /// 순수하게 로그 조립만 담당하며 게임 규칙에는 관여하지 않는다.
    /// </summary>
    public sealed class StageTurnLogger : IDisposable
    {
        private readonly StageSession _session;
        private readonly Dictionary<int, KeyZone> _zoneByTurn = new();
        private readonly Dictionary<int, string> _judgeByTurn = new();
        private int? _censorshipStartTurn;

        public StageTurnLogger(StageSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));

            _session.Keys.ZoneOpened += OnZoneOpened;
            _session.Keys.KeyCollected += OnKeyCollected;
            _session.Keys.ZoneMissed += OnZoneMissed;
            _session.Censorship.CensorshipChanged += OnCensorshipChanged;
            _session.Runner.TurnResolved += OnTurnResolved;
        }

        public void Dispose()
        {
            _session.Keys.ZoneOpened -= OnZoneOpened;
            _session.Keys.KeyCollected -= OnKeyCollected;
            _session.Keys.ZoneMissed -= OnZoneMissed;
            _session.Censorship.CensorshipChanged -= OnCensorshipChanged;
            _session.Runner.TurnResolved -= OnTurnResolved;
        }

        private void OnZoneOpened(KeyZone zone) => _zoneByTurn[_session.Runner.CurrentTurn] = zone;
        private void OnKeyCollected(int count) => _judgeByTurn[_session.Runner.CurrentTurn] = $"성공(누적 {count})";
        private void OnZoneMissed() => _judgeByTurn[_session.Runner.CurrentTurn] = "실패";

        private void OnCensorshipChanged(bool censored)
        {
            if (censored)
            {
                _censorshipStartTurn = _session.Runner.CurrentTurn;
            }
            else
            {
                _censorshipStartTurn = null;
            }
        }

        private void OnTurnResolved(TurnReport report)
        {
            var log = new StringBuilder();
            var originalTags = report.Clue.CreateOriginalTagSet();
            var positionBefore = report.IndicatorPosition - report.IndicatorDelta;

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
            log.AppendLine($"  이동: {report.IndicatorDelta}  인디케이터: {positionBefore} → {report.IndicatorPosition}");

            var zoneText = _zoneByTurn.TryGetValue(report.Turn, out var zone)
                ? $"{zone.StartSlot}~{zone.StartSlot + zone.Width - 1}"
                : "없음";
            var judgeText = _judgeByTurn.TryGetValue(report.Turn, out var judge) ? judge : "-";
            log.AppendLine($"  키 구역: {zoneText}  판정: {judgeText}");

            var censorText = _session.Censorship.IsCensored
                ? $"진행중 ({report.Turn - _censorshipStartTurn.Value + 1}턴째)"
                : "해제";
            log.AppendLine($"  검열: {censorText}");

            log.AppendLine($"  신규 컴플렉스: {(report.SpawnedComplex != null ? report.SpawnedComplex.Definition.DisplayName : "없음")}");
            log.Append($"  결과: {report.Outcome}");

            UnityEngine.Debug.Log(log.ToString());

            if (report.Outcome != StageOutcome.InProgress)
                UnityEngine.Debug.Log($"=== 종료: {report.Outcome}, 총 {_session.Runner.CurrentTurn}턴, 획득 키 {_session.Keys.Collected} ===");
        }

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
    }
}
