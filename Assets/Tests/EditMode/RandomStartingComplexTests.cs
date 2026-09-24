using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>스테이지 1 시작 컴플렉스는 런마다 풀 10종 중 시드로 뽑는다 — 같은 시드는 같은 결과, 다른 시드는 다른 결과.</summary>
    public class RandomStartingComplexTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static string StartingId(StageConfig config, int seed)
        {
            var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), Polarity);
            return string.Join("+", session.Complexes.InPriorityOrder().Select(c => c.Definition.Id));
        }

        [Test]
        public void PrototypeStage_StartsWithExactlyOneComplex_FromThePool()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            Assert.IsTrue(config.RandomStartingComplex);
            Assert.IsNull(config.StartingComplex, "고정 시작 컴플렉스는 더 이상 없다.");

            var poolIds = config.ComplexPool.Select(c => c.Id).ToHashSet();
            for (var seed = 0; seed < 50; seed++)
            {
                var id = StartingId(config, seed);
                Assert.IsTrue(poolIds.Contains(id), $"seed {seed}: 시작 컴플렉스 '{id}'는 풀에 있어야 한다(정확히 하나).");
            }
        }

        [Test]
        public void SameSeed_GivesTheSameStartingComplex()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            for (var seed = 0; seed < 50; seed++)
                Assert.AreEqual(StartingId(config, seed), StartingId(config, seed), $"seed {seed}: 같은 시드(RestartWithSameSeed)면 같은 컴플렉스여야 한다.");
        }

        [Test]
        public void DifferentSeeds_CoverEveryPoolComplex()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            var seen = Enumerable.Range(0, 500).Select(seed => StartingId(config, seed)).Distinct().ToList();
            Assert.AreEqual(config.ComplexPool.Count, seen.Count, "500시드면 풀 10종이 모두 시작 컴플렉스로 나와야 한다(RestartWithNewSeed가 다른 컴플렉스를 낸다).");
        }

        [Test]
        public void FixedStartingComplex_WinsOverRandomSelection()
        {
            var random = PrototypeContent.PrototypeStage(Polarity);
            var fixedConfig = new StageConfig(random.Id, random.DisplayName, random.Quarters.QuarterCount, random.Quarters.TurnsPerQuarter,
                random.RequiredKeys, random.ComplexWeight, random.Clues, random.ComplexPool, PrototypeContent.AntiPast(Polarity), random.ItemPool,
                random.KeyWidth, random.MaxComplexSlots, random.Traits, random.ItemSlots, random.ItemParameters, randomStartingComplex: true);

            for (var seed = 0; seed < 20; seed++)
                Assert.AreEqual("complex_anti_past", StartingId(fixedConfig, seed));
        }
    }
}
