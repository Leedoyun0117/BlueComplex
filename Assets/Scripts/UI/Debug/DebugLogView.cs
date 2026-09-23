using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>최근 몇 턴의 해석 결과(발동 컴플렉스, 최종 태그, 심박 이동)를 누적 표시한다. StageTurnLogger와 같은 정보를 화면에도 띄운다.</summary>
    internal sealed class DebugLogView : DebugSessionView
    {
        private const int MaxLines = 60;

        private ScrollRect _scrollRect;
        private RectTransform _content;
        private Text _logText;
        private readonly List<string> _lines = new();

        protected override void BuildUI()
        {
            var go = gameObject;
            var panel = DebugUIFactory.CreatePanel(transform, "Viewport", new Color(0.07f, 0.07f, 0.09f));
            DebugUIFactory.StretchFull(panel);
            panel.gameObject.AddComponent<RectMask2D>();
            _scrollRect = go.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.viewport = panel;

            _logText = DebugUIFactory.CreateText(panel, "LogText", 13);
            _content = (RectTransform)_logText.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = new Vector2(-12f, 0f); // 좌우 6px씩 여백. 세로는 ContentSizeFitter가 채운다.
            _content.anchoredPosition = Vector2.zero;
            _logText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _content;
        }

        protected override void SubscribeSession(StageSession session)
        {
            session.Runner.TurnResolved += OnTurnResolved;
            session.Runner.StageEnded += OnStageEnded;
        }

        protected override void UnsubscribeSession(StageSession session)
        {
            session.Runner.TurnResolved -= OnTurnResolved;
            session.Runner.StageEnded -= OnStageEnded;
        }

        private void OnTurnResolved(TurnReport report)
        {
            AppendLine(FormatReport(report));
        }

        private void OnStageEnded(StageOutcome outcome)
        {
            AppendLine($"=== 종료: {DebugKoreanLabels.Outcome(outcome)} · 총 {Session.Runner.CurrentTurn}턴 · 획득 키 {Session.Keys.Collected}/{Session.Keys.Required} ===");
        }

        protected override void Render()
        {
            _lines.Clear();
            _logText.text = string.Empty;
        }

        private void AppendLine(string line)
        {
            _lines.Add(line);
            while (_lines.Count > MaxLines) _lines.RemoveAt(0);

            _logText.text = string.Join("\n", _lines);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private static string FormatReport(TurnReport report)
        {
            var sb = new StringBuilder();

            if (report.IsPass)
            {
                sb.Append($"[턴 {report.Turn}] 손패 없음 · 넘어감 · 심박 {report.HeartbeatValue}");
            }
            else
            {
                var triggered = report.Interpretation.Steps.Where(s => s.Triggered).ToList();
                var chain = triggered.Count == 0
                    ? "없음"
                    : string.Join(" → ", triggered.Select(s => s.Complex.Definition.DisplayName));

                var heartbeatBefore = report.HeartbeatValue - report.HeartbeatDelta;

                sb.Append($"[턴 {report.Turn}] {report.Clue.DisplayName} · 발동: {chain}\n");
                sb.Append($"  최종: {FormatFinalTags(report)} · 심박 {heartbeatBefore}→{report.HeartbeatValue} ({report.HeartbeatDelta:+0;-0;0})");
            }

            if (report.SpawnedComplex != null)
                sb.Append($"\n  신규 컴플렉스: {report.SpawnedComplex.Definition.DisplayName}");

            if (report.KeyResult is { } key)
                sb.Append($"\n  {key.Quarter}쿼터 키 판정: {(key.Success ? "성공" : "실패")} " +
                          $"(구역 {key.Zone.StartSlot}~{key.Zone.StartSlot + key.Zone.Width - 1}, 심박 {key.Position})");

            return sb.ToString();
        }

        private static string FormatFinalTags(TurnReport report)
        {
            var tags = report.FinalTags;
            var time = DebugKoreanLabels.Time(tags.Time);
            var persons = tags.Persons.Count == 0 ? "-" : string.Join(",", tags.Persons.Select(DebugKoreanLabels.Person));
            var emotions = tags.Emotions.Count == 0
                ? "-"
                : string.Join(",", tags.Emotions.Select(p => DebugKoreanLabels.Emotion(p.Key) + (p.Value > 1 ? "x" + p.Value : "")));
            return $"{time}/{persons}/{emotions}";
        }
    }
}
