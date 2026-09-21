using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 키 구역 도달 모델의 '컴플렉스가 최악으로 겹칠 때 이동량'(StageFactory.WorstCaseDelta)이
    /// 모든 부착 개수·순서를 하나하나 돌려본 결과와 같은지 확인한다.
    /// </summary>
    public class WorstCaseDeltaTests
    {
        /// <summary>풀에서 최대 maxSize개를 순서까지 구분해 붙이는 모든 경우(아무것도 안 붙은 경우 포함).</summary>
        private static IEnumerable<List<ComplexDefinition>> OrderedAttachments(IReadOnlyList<ComplexDefinition> pool, int maxSize)
        {
            yield return new List<ComplexDefinition>();
            if (maxSize == 0) yield break;

            foreach (var first in pool)
            {
                var rest = pool.Where(c => c != first).ToList();
                foreach (var tail in OrderedAttachments(rest, maxSize - 1))
                {
                    tail.Insert(0, first);
                    yield return tail;
                }
            }
        }

        private static int BruteForceWorst(ClueDefinition clue, IReadOnlyList<ComplexDefinition> pool,
                                           EmotionEvaluator evaluator, int maxSlots)
        {
            var worst = int.MaxValue;
            foreach (var attached in OrderedAttachments(pool, maxSlots))
            {
                var board = new ComplexBoard();
                for (var i = 0; i < attached.Count; i++)
                    board.TryAttach(new ComplexInstance(attached[i], priority: i));

                worst = System.Math.Min(worst, evaluator.Evaluate(new ComplexResolver(board).Resolve(clue.CreateOriginalTagSet()).Final));
            }

            return worst;
        }

        [Test]
        public void WorstCaseDelta_MatchesExhaustiveSearch_ForEveryStage1Clue()
        {
            var polarity = new DefaultEmotionPolarityTable();
            var evaluator = new EmotionEvaluator(polarity);
            var pool = PrototypeContent.Complexes(polarity);

            foreach (var clue in PrototypeContent.Clues())
            {
                Assert.AreEqual(
                    BruteForceWorst(clue, pool, evaluator, ComplexBoard.MaxSlots),
                    StageFactory.WorstCaseDelta(clue, pool, evaluator, ComplexBoard.MaxSlots),
                    $"{clue.DisplayName}: 최악 이동량이 전수 조사와 다르다.");
            }
        }

        [Test]
        public void WorstCaseDelta_WithNoComplexes_IsTheOriginalDelta()
        {
            var evaluator = new EmotionEvaluator(new DefaultEmotionPolarityTable());
            var clue = PrototypeContent.Clues().First(c => c.DisplayName == "개학 날짜 달력");

            Assert.AreEqual(evaluator.Evaluate(clue.CreateOriginalTagSet()),
                StageFactory.WorstCaseDelta(clue, new ComplexDefinition[0], evaluator, ComplexBoard.MaxSlots));
        }

        [Test]
        public void Stage1Clues_OriginalDelta_NeverExceedsTheRiseClamp()
        {
            // 도달 모델은 카드 한 장의 상승량을 DefaultMaxMovePerTurn 으로 자른다.
            // 상승 쪽은 컴플렉스를 뺀 원본 이동량을 쓰므로, 한도에 걸리는 카드가 없어야 카드 풀이 있는 그대로 반영된다.
            // (하강 쪽은 컴플렉스 4개가 최악으로 겹치면 한도를 넘는 카드가 있다 — 가족사진 액자 -40. 튜닝 값이라 여기서 단언하지 않는다.)
            var evaluator = new EmotionEvaluator(new DefaultEmotionPolarityTable());

            foreach (var clue in PrototypeContent.Clues())
            {
                Assert.LessOrEqual(evaluator.Evaluate(clue.CreateOriginalTagSet()), ReachabilityKeyZonePlacer.DefaultMaxMovePerTurn,
                    $"{clue.DisplayName}: 상승 한도에 걸린다.");
            }
        }

        [Test]
        public void CreateKeyPlacer_WithTenComplexPool_IsFast()
        {
            // 컴플렉스 10종 풀에서 스테이지를 만들 때마다 도는 계산이다. 수백 번 만들어도 금방 끝나야 한다.
            var polarity = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarity);
            var zone = new HeartbeatZone();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (var seed = 0; seed < 100; seed++)
                StageFactory.CreateKeyPlacer(config, new SystemRandomSource(seed), zone, polarity);
            stopwatch.Stop();

            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, $"100회 생성에 {stopwatch.ElapsedMilliseconds}ms — 너무 느리다.");
        }
    }
}
