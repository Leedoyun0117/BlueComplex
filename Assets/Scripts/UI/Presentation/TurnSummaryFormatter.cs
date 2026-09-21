using System.Linq;
using System.Text;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;

namespace BlueComplex.UI.Presentation
{
    /// <summary>턴 결과를 대사창 한 줄 요약으로 바꾼다. Immediate/Cinematic 두 Presenter가 같이 쓴다.</summary>
    public static class TurnSummaryFormatter
    {
        /// <summary>컴플렉스가 발동하는 순간 대사창에 뜨는 짧은 이벤트 대사. 컴플렉스별 대사 원고가 아직 없어서 이름만 넣은 공통 문구다 —
        /// 원고가 생기면 이 메서드 한 곳에서 컴플렉스 id로 갈라 쓰면 된다. 표시 이름에 이미 "컴플렉스"가 들어 있다("죄책감 컴플렉스").</summary>
        public static string BuildComplexEventLine(ComplexInstance complex) =>
            $"\"{complex.Definition.DisplayName}\"이(가) 반응했다!";

        public static string Build(TurnReport report)
        {
            var sb = new StringBuilder();

            if (report.IsPass)
            {
                sb.Append($"[{report.Turn}턴] 손에 낼 단서가 없다. 시간만 흘렀다({report.HeartbeatValue}).");
            }
            else
            {
                sb.Append($"[{report.Turn}턴] \"{report.Clue.DisplayName}\"을(를) 냈다. ");

                var triggeredCount = report.Interpretation.Steps.Count(step => step.Triggered);
                if (triggeredCount > 0) sb.Append($"컴플렉스 {triggeredCount}개가 반응했다. ");

                sb.Append(report.HeartbeatDelta >= 0
                    ? $"심박수가 {report.HeartbeatDelta}만큼 올랐다({report.HeartbeatValue})."
                    : $"심박수가 {-report.HeartbeatDelta}만큼 떨어졌다({report.HeartbeatValue}).");
            }

            if (report.SpawnedComplex != null)
                sb.Append($" 새로운 컴플렉스 \"{report.SpawnedComplex.Definition.DisplayName}\"이(가) 나타났다.");

            if (report.KeyResult is { } key)
                sb.Append(key.Success ? $" {key.Quarter}쿼터 키를 얻었다!" : $" {key.Quarter}쿼터 키를 놓쳤다.");

            if (report.Outcome != StageOutcome.InProgress)
                sb.Append(report.Outcome == StageOutcome.Cleared ? " 스테이지 클리어!" : " 스테이지 실패...");

            return sb.ToString();
        }

        /// <summary>MemorySpaceBubble의 고정 표시용 — "혐오 ×2, 슬픔  ▲ 흥분" 형태.
        /// 침체/흥분 구분은 이번 턴 심박수 변화 방향(HeartbeatDelta)으로 색을 입힌다.</summary>
        public static string BuildFinalEmotionSummary(TurnReport report)
        {
            var parts = report.FinalTags.Emotions
                .Select(pair => pair.Value > 1
                    ? $"{KoreanLabels.Emotion(pair.Key)} ×{pair.Value}"
                    : KoreanLabels.Emotion(pair.Key));

            var body = string.Join(", ", parts);
            if (string.IsNullOrEmpty(body)) body = "(없음)";

            var direction = report.HeartbeatDelta switch
            {
                > 0 => "<color=#D97B4A>▲ 흥분</color>",
                < 0 => "<color=#5B8FB0>▼ 침체</color>",
                _ => "<color=#AAAAAA>- 변화 없음</color>"
            };

            return $"{body}  {direction}";
        }
    }
}
