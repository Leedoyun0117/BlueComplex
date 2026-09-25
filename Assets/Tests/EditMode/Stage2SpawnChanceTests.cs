using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>스테이지 2 전용 컴플렉스 발현 확률표(80/50/0/50/80%) — 스테이지 1의 기본표×1.125와 분리돼 있다.</summary>
    public class Stage2SpawnChanceTests
    {
        private static StageSession Create(StageConfig config) =>
            StageFactory.Create(config, new SystemRandomSource(1), new ClueKnowledgeLedger());

        [Test]
        public void Stage2_UsesDedicatedZoneChances()
        {
            var zone = Create(Stage2Content.Stage2(new DefaultEmotionPolarityTable())).Zone;

            Assert.AreEqual(0.8, zone.ComplexSpawnChanceOf(25), 1e-9);   // 매우 침체 10~39
            Assert.AreEqual(0.5, zone.ComplexSpawnChanceOf(55), 1e-9);   // 침체 40~70
            Assert.AreEqual(0.0, zone.ComplexSpawnChanceOf(85), 1e-9);   // 안정 71~100
            Assert.AreEqual(0.5, zone.ComplexSpawnChanceOf(120), 1e-9);  // 흥분 101~150
            Assert.AreEqual(0.8, zone.ComplexSpawnChanceOf(170), 1e-9);  // 매우 흥분 151~190
            Assert.AreEqual(0.0, zone.ComplexSpawnChanceOf(5), 1e-9);    // 즉사
            Assert.AreEqual(0.0, zone.ComplexSpawnChanceOf(195), 1e-9);
        }

        [Test]
        public void Stage2_OnlyChangesSpawnChance_NotZoneBoundaries()
        {
            var custom = Create(Stage2Content.Stage2(new DefaultEmotionPolarityTable())).Zone;
            var stock = new HeartbeatZone();

            for (var value = 0; value <= 200; value++)
            {
                Assert.AreEqual(stock.StateOf(value), custom.StateOf(value), $"심박수 {value}");
            }
        }

        [Test]
        public void Stage1_KeepsDefaultTable_WithWeight()
        {
            var config = PrototypeContent.PrototypeStage(new DefaultEmotionPolarityTable());
            var zone = Create(config).Zone;

            Assert.IsNull(config.ComplexSpawnChances);
            Assert.AreEqual(1.125, config.ComplexWeight, 1e-9);
            Assert.AreEqual(0.5, zone.ComplexSpawnChanceOf(25), 1e-9);   // 기본표 그대로(배율은 발현 정책이 곱한다)
            Assert.AreEqual(0.3, zone.ComplexSpawnChanceOf(55), 1e-9);
        }
    }
}
