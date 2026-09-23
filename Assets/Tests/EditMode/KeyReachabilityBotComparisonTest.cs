using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 무전략 봇("손패 첫 장") vs 휴리스틱 봇(쿼터 구조를 알고 남은 쿼터의 카드 순서를 미리 시뮬레이션한다 — 봇 로직은 QuarterBots)을
    /// 같은 시드로 나란히 돌려 개선폭을 확인한다. 4시드는 상세 로그(밸런스 눈검사)용이고,
    /// 통과 여부는 1000시드 클리어율 배율로 판정한다.
    /// </summary>
    public class KeyReachabilityBotComparisonTest
    {
        /// <summary>상세 로그를 눈으로 보는 용도. 표본이 작아서 단언에는 쓰지 않는다.</summary>
        private static readonly int[] Seeds = { 20260916, 1, 12345, 777 };

        /// <summary>단언과 요약 통계에 쓰는 대표본.</summary>
        private static readonly int[] WideSeeds = Enumerable.Range(1000, 1000).ToArray();

        /// <summary>
        /// 휴리스틱 클리어율이 무전략의 몇 배 이상이어야 하는가. 쿼터 구조는 키 판정이 3번뿐이라 운의 비중이 구조적으로 크다 —
        /// 카드 풀 도달 모델 적용 직후 실측 1.55배(42.7% vs 27.6%)를 실력이 작동하는 증거로 보고 여유를 둔 값이었다.
        /// ClueHand.RefillForNewQuarter로 쿼터 시작마다 손패가 항상 가득 차도록 고친 뒤(이전엔 저작된 단서 수가
        /// 스테이지 전체 턴 수보다 적어 3쿼터 손패가 굶주렸다 — 그만큼 후반 턴이 강제로 넘어가 위험 노출이 줄어 있었다)
        /// 재측정한 1.33배(32.1% vs 24.2%)에 여유를 둔 값이다.
        /// </summary>
        private const double MinClearRateRatio = 1.2;

        [Test]
        public void HeuristicBot_ImprovesClearRate_ComparedToFirstCardBot()
        {
            var config = PrototypeContent.PrototypeStage(new DefaultEmotionPolarityTable());
            var log = new StringBuilder();
            log.AppendLine($"=== 무전략 봇 vs 휴리스틱 봇 비교 ({config.TotalTurns}턴 = {config.Quarters.QuarterCount}쿼터 × {config.Quarters.TurnsPerQuarter}턴, " +
                           $"키 {config.RequiredKeys}개 필요, 폭 {config.KeyWidth}) ===");

            var naiveResults = new List<BotRunResult>();
            var heuristicResults = new List<BotRunResult>();

            foreach (var seed in Seeds)
            {
                naiveResults.Add(QuarterBots.RunStage(config, seed, "무전략(첫 장)", QuarterBots.ChooseFirstCard, log));
                heuristicResults.Add(QuarterBots.RunStage(config, seed, "휴리스틱(쿼터 인지)", QuarterBots.ChooseHeuristicCard, log));
            }

            log.AppendLine("=== 요약 (4시드) ===");
            log.AppendLine($"  {"시드",-10} {"무전략",-24} {"휴리스틱",-24}");
            for (var i = 0; i < Seeds.Length; i++)
            {
                log.AppendLine($"  {Seeds[i],-10} {Describe(naiveResults[i]),-24} {Describe(heuristicResults[i]),-24}");
            }

            log.AppendLine($"  키 성공률: 무전략 {Aggregate(naiveResults)} / 휴리스틱 {Aggregate(heuristicResults)}");

            var wideNaive = WideSeeds.Select(seed => QuarterBots.RunStage(config, seed, "무전략(첫 장)", QuarterBots.ChooseFirstCard, null)).ToList();
            var wideHeuristic = WideSeeds.Select(seed => QuarterBots.RunStage(config, seed, "휴리스틱(쿼터 인지)", QuarterBots.ChooseHeuristicCard, null)).ToList();
            var wideNaiveClearRate = ClearRate(wideNaive);
            var wideHeuristicClearRate = ClearRate(wideHeuristic);
            var ratio = wideNaiveClearRate > 0 ? wideHeuristicClearRate / wideNaiveClearRate : double.PositiveInfinity;

            log.AppendLine($"=== 요약 ({WideSeeds.Length}시드) ===");
            log.AppendLine($"  무전략   : {DescribeAggregate(wideNaive)}");
            log.AppendLine($"  휴리스틱 : {DescribeAggregate(wideHeuristic)}");
            log.AppendLine($"  클리어율 배율: {ratio:F2}배 (통과 기준 {MinClearRateRatio:F1}배 이상)");
            log.AppendLine("  구역 좌/우별 (배치 수 · 판정까지 간 수 · 적중) — 우측=131~ 쪽");
            log.AppendLine($"    무전략   : {DescribeSides(wideNaive)}");
            log.AppendLine($"    휴리스틱 : {DescribeSides(wideHeuristic)}");

            Debug.Log(log.ToString());

            Assert.GreaterOrEqual(wideHeuristicClearRate, wideNaiveClearRate * MinClearRateRatio,
                $"{WideSeeds.Length}시드 기준 휴리스틱 클리어율({wideHeuristicClearRate:P1})이 " +
                $"무전략({wideNaiveClearRate:P1})의 {MinClearRateRatio:F1}배 이상이어야 한다.");
        }

        private static string Describe(BotRunResult r) =>
            $"{r.Outcome} {r.KeysCollected}/{r.KeyRequired}키(쿼터 {r.KeyHits}/{r.KeyAttempts})";

        private static string Aggregate(IReadOnlyCollection<BotRunResult> results)
        {
            var hits = results.Sum(r => r.KeyHits);
            var attempts = results.Sum(r => r.KeyAttempts);
            return $"{hits}/{attempts} ({(attempts == 0 ? 0 : 100.0 * hits / attempts):F0}%)";
        }

        /// <summary>구역이 좌측(시작 100 미만)인지 우측인지로 나눠, 배치된 수와 실제로 판정까지 가서 맞힌 수를 센다.</summary>
        private static string DescribeSides(IReadOnlyCollection<BotRunResult> results)
        {
            var text = new List<string>();
            foreach (var (label, isLeft) in new[] { ("좌", true), ("우", false) })
            {
                var placed = 0;
                var judged = 0;
                var hit = 0;
                foreach (var result in results)
                {
                    for (var quarter = 0; quarter < result.Zones.Count; quarter++)
                    {
                        if ((result.Zones[quarter].StartSlot < 100) != isLeft) continue;
                        placed++;
                        if (!result.QuarterResults[quarter].HasValue) continue;
                        judged++;
                        if (result.QuarterResults[quarter].Value) hit++;
                    }
                }

                text.Add($"{label} {placed} · {judged} · {hit} ({(judged == 0 ? 0 : 100.0 * hit / judged):F1}%)");
            }

            return string.Join(" / ", text);
        }

        private static double ClearRate(IReadOnlyCollection<BotRunResult> results) =>
            results.Count == 0 ? 0 : (double)results.Count(r => r.Outcome == StageOutcome.Cleared) / results.Count;

        private static string DescribeAggregate(IReadOnlyCollection<BotRunResult> results)
        {
            var cleared = results.Count(r => r.Outcome == StageOutcome.Cleared);
            var hits = results.Sum(r => r.KeyHits);
            var attempts = results.Sum(r => r.KeyAttempts);
            var hitRate = attempts == 0 ? 0 : 100.0 * hits / attempts;
            return $"클리어 {cleared}/{results.Count} ({100.0 * cleared / results.Count:F1}%), " +
                   $"키 성공률 {hitRate:F1}% ({hits}/{attempts}), 평균 키 {results.Average(r => (double)r.KeysCollected):F2}";
        }
    }
}
