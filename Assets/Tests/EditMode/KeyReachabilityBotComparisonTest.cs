using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 무전략 봇("손패 첫 장") vs 휴리스틱 봇("키 구역을 보고 ComplexResolver로 미리 해석해 방향을 고른다")을
    /// 같은 시드 4개로 나란히 돌려 키 성공률 개선폭을 확인한다.
    /// 휴리스틱은 ComplexBoard/TraitBoard를 직접 참조해 실제 턴 해석과 동일한 조건으로 미리보기만 하고,
    /// 코어 상태를 전혀 변형하지 않는다(ComplexResolver.Resolve는 순수 함수).
    /// </summary>
    public class KeyReachabilityBotComparisonTest
    {
        private static readonly int[] Seeds = { 20260916, 1, 12345, 777 };

        private readonly struct RunResult
        {
            public StageOutcome Outcome { get; }
            public int KeysCollected { get; }
            public int KeyRequired { get; }
            public int KeyAttempts { get; }
            public int KeyHits { get; }
            public int TurnsPlayed { get; }

            public RunResult(StageOutcome outcome, int keysCollected, int keyRequired,
                             int keyAttempts, int keyHits, int turnsPlayed)
            {
                Outcome = outcome;
                KeysCollected = keysCollected;
                KeyRequired = keyRequired;
                KeyAttempts = keyAttempts;
                KeyHits = keyHits;
                TurnsPlayed = turnsPlayed;
            }
        }

        private static ClueInstance ChooseFirstCard(StageSession session) => session.Hand.Cards[0];

        /// <summary>
        /// 다음 키 구역(이미 이번 턴이 키 턴이면 그 구역)과 현재 위치를 비교해 목표를 정하고,
        /// 손패 각 카드를 ComplexResolver로 미리 해석해 실제 이동량을 계산한 뒤
        /// 목표에 가장 가깝게(이미 구역 안이면 이동량이 0에 가장 가깝게) 만드는 카드를 고른다.
        /// </summary>
        private static ClueInstance ChooseHeuristicCard(StageSession session)
        {
            var heartbeat = session.Heartbeat;
            var currentPosition = heartbeat.Value;

            KeyZone? target = null;
            foreach (var turn in session.Keys.Zones.Keys.OrderBy(t => t))
            {
                if (turn < session.Runner.CurrentTurn) continue;
                target = session.Keys.Zones[turn];
                break;
            }
            var targetZone = target ?? new KeyZone(heartbeat.StartValue, 1); // 남은 키 턴이 없으면 시작값(안정)을 목표로.

            var previewResolver = new ComplexResolver(session.Complexes);
            var evaluator = new TraitAwareEmotionEvaluator(
                new EmotionEvaluator(new DefaultEmotionPolarityTable()), session.Traits);

            // 우선순위: (1) 이동 후 구역까지의 거리를 최소화 — 이미 구역 안이면 거리 0을 유지하는 카드가 최우선이 된다.
            // (2) 거리가 같다면(대표적으로 "이미 구역 안 → 여러 카드가 다 구역을 지킨다") 이동량이 0에 가장 가까운 카드.
            // 거리만 보고 |delta|를 무시하면, 구역 경계(예: 폭2 구역의 끝 칸)에서 부호를 놓쳐 오히려 구역을 벗어나는
            // 카드를 고를 수 있으므로 항상 "이동 후 예측 위치" 기준으로 판단한다.
            ClueInstance best = null;
            var bestDistance = int.MaxValue;
            var bestAbsDelta = int.MaxValue;
            foreach (var card in session.Hand.Cards)
            {
                var interpretation = previewResolver.Resolve(card.Definition.CreateOriginalTagSet());
                var delta = evaluator.Evaluate(interpretation.Final);
                var predicted = Math.Clamp(currentPosition + delta, Heartbeat.MinValue, Heartbeat.MaxValue);
                var distance = DistanceToZone(predicted, targetZone);
                var absDelta = Math.Abs(delta);

                if (distance < bestDistance || (distance == bestDistance && absDelta < bestAbsDelta))
                {
                    bestDistance = distance;
                    bestAbsDelta = absDelta;
                    best = card;
                }
            }

            return best;
        }

        private static int DistanceToZone(int position, KeyZone zone)
        {
            if (zone.Contains(position)) return 0;
            var lastSlot = zone.StartSlot + zone.Width - 1;
            return Math.Min(Math.Abs(position - zone.StartSlot), Math.Abs(position - lastSlot));
        }

        private static RunResult RunStage(int seed, string label, Func<StageSession, ClueInstance> chooseCard, StringBuilder log)
        {
            var random = new SystemRandomSource(seed);
            var polarityTable = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarityTable);
            var session = StageFactory.Create(config, random, new ClueKnowledgeLedger(), polarityTable);

            var keyAttempts = 0;
            var keyHits = 0;
            session.Keys.ZoneOpened += _ => keyAttempts++;
            session.Keys.KeyCollected += _ => keyHits++;

            log.AppendLine($"--- {label} (seed={seed}) ---");
            session.Runner.StartStage();
            log.AppendLine("  키 구역(확정): " + string.Join(", ",
                session.Keys.Zones.OrderBy(p => p.Key)
                    .Select(p => $"{p.Key}턴:{p.Value.StartSlot}~{p.Value.StartSlot + p.Value.Width - 1}")));

            var guard = 0;
            while (session.Runner.Outcome == StageOutcome.InProgress && guard < 10)
            {
                guard++;
                Assert.Greater(session.Hand.Cards.Count, 0,
                    $"[{label} seed={seed}] 턴 {session.Runner.CurrentTurn} 시작 시 손패가 비어 있으면 안 된다.");

                var card = chooseCard(session);
                var before = session.Heartbeat.Value;

                TurnReport report = null;
                Assert.DoesNotThrow(() => report = session.Runner.PlayClue(card),
                    $"[{label} seed={seed}] 턴 {session.Runner.CurrentTurn} 진행 중 예외 없이 처리되어야 한다.");

                log.AppendLine($"  [턴 {report.Turn}] {card.Definition.DisplayName} " +
                                $"이동:{report.HeartbeatDelta} ({before}→{report.HeartbeatValue}) 결과:{report.Outcome}");
            }

            log.AppendLine($"  => {session.Runner.Outcome}, 키 {session.Keys.Collected}/{config.RequiredKeys} " +
                            $"(성공 {keyHits}/{keyAttempts}), 총 {session.Runner.CurrentTurn}턴");

            Assert.AreNotEqual(StageOutcome.InProgress, session.Runner.Outcome,
                $"[{label} seed={seed}] 10턴 이내에 종료되어야 한다.");

            return new RunResult(session.Runner.Outcome, session.Keys.Collected, config.RequiredKeys,
                keyAttempts, keyHits, session.Runner.CurrentTurn);
        }

        [Test]
        public void HeuristicBot_ImprovesKeySuccessRate_ComparedToFirstCardBot()
        {
            var log = new StringBuilder();
            log.AppendLine("=== 무전략 봇 vs 휴리스틱 봇 비교 ===");

            var naiveResults = new List<RunResult>();
            var heuristicResults = new List<RunResult>();

            foreach (var seed in Seeds)
            {
                naiveResults.Add(RunStage(seed, "무전략(첫 장)", ChooseFirstCard, log));
                heuristicResults.Add(RunStage(seed, "휴리스틱(키 지향)", ChooseHeuristicCard, log));
            }

            var naiveHits = naiveResults.Sum(r => r.KeyHits);
            var naiveAttempts = naiveResults.Sum(r => r.KeyAttempts);
            var heuristicHits = heuristicResults.Sum(r => r.KeyHits);
            var heuristicAttempts = heuristicResults.Sum(r => r.KeyAttempts);
            var naiveCleared = naiveResults.Count(r => r.Outcome == StageOutcome.Cleared);
            var heuristicCleared = heuristicResults.Count(r => r.Outcome == StageOutcome.Cleared);

            log.AppendLine("=== 요약 ===");
            log.AppendLine($"  {"시드",-10} {"무전략",-22} {"휴리스틱",-22}");
            for (var i = 0; i < Seeds.Length; i++)
            {
                var n = naiveResults[i];
                var h = heuristicResults[i];
                log.AppendLine($"  {Seeds[i],-10} " +
                                $"{n.Outcome + " " + n.KeysCollected + "/" + n.KeyRequired + "키(" + n.KeyHits + "/" + n.KeyAttempts + ")",-22} " +
                                $"{h.Outcome + " " + h.KeysCollected + "/" + h.KeyRequired + "키(" + h.KeyHits + "/" + h.KeyAttempts + ")",-22}");
            }

            var naiveRate = naiveAttempts == 0 ? 0 : 100.0 * naiveHits / naiveAttempts;
            var heuristicRate = heuristicAttempts == 0 ? 0 : 100.0 * heuristicHits / heuristicAttempts;
            log.AppendLine($"  키 성공률: 무전략 {naiveHits}/{naiveAttempts} ({naiveRate:F0}%) → " +
                            $"휴리스틱 {heuristicHits}/{heuristicAttempts} ({heuristicRate:F0}%)");
            log.AppendLine($"  Cleared 런: 무전략 {naiveCleared}/{Seeds.Length} → 휴리스틱 {heuristicCleared}/{Seeds.Length}");

            Debug.Log(log.ToString());

            Assert.Greater(heuristicHits, naiveHits,
                "같은 시드에서 휴리스틱 봇이 무전략 봇보다 키를 더 많이 획득해야 한다.");
        }
    }
}
