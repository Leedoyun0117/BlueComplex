using System.Linq;
using System.Text;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;

namespace BlueComplex.UI.Presentation
{
    /// <summary>턴 결과를 대사창 한 줄 요약으로 바꾼다. Immediate/Cinematic 두 Presenter가 같이 쓴다.</summary>
    public static class TurnSummaryFormatter
    {
        public static string Build(TurnReport report)
        {
            var sb = new StringBuilder();
            sb.Append($"[{report.Turn}턴] \"{report.Clue.DisplayName}\"을(를) 냈다. ");

            var triggeredCount = report.Interpretation.Steps.Count(step => step.Triggered);
            if (triggeredCount > 0) sb.Append($"컴플렉스 {triggeredCount}개가 반응했다. ");

            sb.Append(report.HeartbeatDelta >= 0
                ? $"심박수가 {report.HeartbeatDelta}만큼 올랐다({report.HeartbeatValue})."
                : $"심박수가 {-report.HeartbeatDelta}만큼 떨어졌다({report.HeartbeatValue}).");

            if (report.SpawnedComplex != null)
                sb.Append($" 새로운 컴플렉스 \"{report.SpawnedComplex.Definition.DisplayName}\"이(가) 나타났다.");

            if (report.Outcome != StageOutcome.InProgress)
                sb.Append(report.Outcome == StageOutcome.Cleared ? " 스테이지 클리어!" : " 스테이지 실패...");

            return sb.ToString();
        }
    }
}
