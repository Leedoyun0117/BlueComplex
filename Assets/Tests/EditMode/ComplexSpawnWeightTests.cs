using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>컴플렉스 발현 배율(StageConfig.ComplexWeight) — 프로토타입 스테이지는 1.125(구간 확률 +12.5%), 안정 구간은 배율과 무관하게 0%.</summary>
    public class ComplexSpawnWeightTests
    {
        /// <summary>NextDouble이 항상 같은 값을 돌려주는 난수원 — 발현 확률의 경계를 결정적으로 잰다.</summary>
        private sealed class FixedRandom : IRandomSource
        {
            private readonly double _value;
            public FixedRandom(double value) => _value = value;
            public int Range(int minInclusive, int maxExclusive) => minInclusive;
            public double NextDouble() => _value;
        }

        private static bool Spawns(double roll, double weight, int heartbeat) =>
            new ZoneBasedSpawnPolicy(new FixedRandom(roll), new HeartbeatZone(), weight).ShouldSpawn(heartbeat);

        [Test]
        public void PrototypeStage_UsesRaisedComplexWeight()
        {
            var config = PrototypeContent.PrototypeStage(new DefaultEmotionPolarityTable());

            Assert.AreEqual(1.125, config.ComplexWeight, 1e-9);
            Assert.AreEqual(PrototypeContent.PrototypeComplexWeight, config.ComplexWeight, 1e-9);
        }

        [Test]
        public void RaisedWeight_ScalesZoneChances_ByTwelvePointFivePercent()
        {
            const double weight = 1.125;

            // 침체(40~70)·흥분(101~150) 30% → 33.75%
            foreach (var heartbeat in new[] { 55, 120 })
            {
                Assert.IsTrue(Spawns(0.337, weight, heartbeat), $"{heartbeat}: 33.7% 굴림은 발현해야 한다");
                Assert.IsFalse(Spawns(0.338, weight, heartbeat), $"{heartbeat}: 33.8% 굴림은 발현하면 안 된다");
            }

            // 매우 침체(10~39)·매우 흥분(151~190) 50% → 56.25%
            foreach (var heartbeat in new[] { 25, 170 })
            {
                Assert.IsTrue(Spawns(0.562, weight, heartbeat), $"{heartbeat}: 56.2% 굴림은 발현해야 한다");
                Assert.IsFalse(Spawns(0.563, weight, heartbeat), $"{heartbeat}: 56.3% 굴림은 발현하면 안 된다");
            }
        }

        [Test]
        public void StableZone_NeverSpawns_WhateverTheWeight()
        {
            Assert.IsFalse(Spawns(0.0, 1.125, 80));
            Assert.IsFalse(Spawns(0.0, 10.0, 100));
        }

        [Test]
        public void DefaultWeight_KeepsOriginalChances()
        {
            Assert.IsTrue(Spawns(0.299, 1.0, 55));
            Assert.IsFalse(Spawns(0.301, 1.0, 55));
            Assert.IsTrue(Spawns(0.499, 1.0, 25));
            Assert.IsFalse(Spawns(0.501, 1.0, 25));
        }
    }
}
