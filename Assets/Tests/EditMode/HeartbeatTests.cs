using NUnit.Framework;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Tests
{
    /// <summary>C. 심박수 — 감정 극성 합산(±10), 클램프(0~200), 예민 특성 배율, 구간 판정.</summary>
    public class HeartbeatTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        [Test]
        public void Evaluate_ExcitedTagSum_IsTenPerTag()
        {
            var tags = new TagSet(emotions: new[] { EmotionTag.Happiness, EmotionTag.Anger });
            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(20, evaluator.Evaluate(tags));
        }

        [Test]
        public void Evaluate_DepressedTagSum_IsNegativeTenPerTag()
        {
            var tags = new TagSet(emotions: new[] { EmotionTag.Fear, EmotionTag.Sadness });
            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(-20, evaluator.Evaluate(tags));
        }

        [Test]
        public void Evaluate_DuplicateEmotionsAccumulateByCount()
        {
            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 2);

            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(-20, evaluator.Evaluate(tags), "혐오 2개는 -20(태그당 -10)으로 누적되어야 한다.");
        }

        [Test]
        public void Evaluate_MixedPolarities_NetsOut()
        {
            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 2); // -20
            tags.AddEmotion(EmotionTag.Love, 1);    // +10

            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(-10, evaluator.Evaluate(tags));
        }

        [Test]
        public void TraitAwareEvaluator_WithoutSensitive_MultiplierIsOne()
        {
            var traits = new TraitBoard();
            var evaluator = new TraitAwareEmotionEvaluator(Polarity, traits);

            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 1); // base -10

            Assert.AreEqual(-10, evaluator.Evaluate(tags));
        }

        [Test]
        public void TraitAwareEvaluator_WithSensitive_MultipliesByThree()
        {
            var traits = new TraitBoard(PrototypeContent.Traits());
            traits.Grant(PrototypeContent.TraitSensitive);
            var evaluator = new TraitAwareEmotionEvaluator(Polarity, traits);

            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 1); // base -10

            Assert.AreEqual(-30, evaluator.Evaluate(tags), "침체 태그 1개 + 예민 특성은 -30(3배)이어야 한다.");
        }

        [Test]
        public void Change_ClampsAtUpperBound()
        {
            var heartbeat = new Heartbeat();

            heartbeat.Change(1000);

            Assert.AreEqual(200, heartbeat.Value);
        }

        [Test]
        public void Change_ClampsAtLowerBound()
        {
            var heartbeat = new Heartbeat();

            heartbeat.Change(-1000);

            Assert.AreEqual(0, heartbeat.Value);
        }

        [Test]
        public void Change_FiresChangedEvent_WithFromAndTo()
        {
            var heartbeat = new Heartbeat(); // 시작 80
            (int from, int to)? captured = null;
            heartbeat.Changed += (from, to) => captured = (from, to);

            heartbeat.Change(-20);

            Assert.AreEqual(60, heartbeat.Value);
            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual((80, 60), captured.Value);
        }

        [Test]
        public void Change_AtClampedBoundary_DoesNotFireEvent_WhenValueUnchanged()
        {
            var heartbeat = new Heartbeat();
            heartbeat.Change(-1000); // 0으로 클램프
            Assert.AreEqual(0, heartbeat.Value);

            var fired = false;
            heartbeat.Changed += (_, _) => fired = true;

            heartbeat.Change(-20); // 이미 0이므로 더 내려갈 수 없다.

            Assert.IsFalse(fired, "값이 실제로 변하지 않으면 Changed 이벤트가 발생하지 않아야 한다.");
            Assert.AreEqual(0, heartbeat.Value);
        }

        [TestCase(0, HeartbeatState.Fatal)]
        [TestCase(9, HeartbeatState.Fatal)]
        [TestCase(10, HeartbeatState.VeryDepressed)]
        [TestCase(39, HeartbeatState.VeryDepressed)]
        [TestCase(40, HeartbeatState.Depressed)]
        [TestCase(70, HeartbeatState.Depressed)]
        [TestCase(71, HeartbeatState.Stable)]
        [TestCase(80, HeartbeatState.Stable)]
        [TestCase(100, HeartbeatState.Stable)]
        [TestCase(101, HeartbeatState.Excited)]
        [TestCase(150, HeartbeatState.Excited)]
        [TestCase(151, HeartbeatState.VeryExcited)]
        [TestCase(190, HeartbeatState.VeryExcited)]
        [TestCase(191, HeartbeatState.Fatal)]
        [TestCase(200, HeartbeatState.Fatal)]
        public void StateOf_MatchesZoneTable_AtEveryBoundary(int value, HeartbeatState expected)
        {
            var zone = new HeartbeatZone();

            Assert.AreEqual(expected, zone.StateOf(value), $"심박수 {value}");
        }

        [Test]
        public void ComplexSpawnChanceOf_StableZone_IsZero()
        {
            var zone = new HeartbeatZone();

            Assert.AreEqual(0.0, zone.ComplexSpawnChanceOf(80));
            Assert.AreEqual(0.0, zone.ComplexSpawnChanceOf(71));
            Assert.AreEqual(0.0, zone.ComplexSpawnChanceOf(100));
        }

        [TestCase(25, 0.5)]   // 매우 침체
        [TestCase(55, 0.3)]   // 침체
        [TestCase(125, 0.3)]  // 흥분
        [TestCase(170, 0.5)]  // 매우 흥분
        public void ComplexSpawnChanceOf_NonStableZones_MatchTable(int value, double expected)
        {
            var zone = new HeartbeatZone();

            Assert.AreEqual(expected, zone.ComplexSpawnChanceOf(value));
        }

        [Test]
        public void IsFatal_TrueOnlyAtExtremes()
        {
            var zone = new HeartbeatZone();

            Assert.IsTrue(zone.IsFatal(0));
            Assert.IsTrue(zone.IsFatal(9));
            Assert.IsTrue(zone.IsFatal(191));
            Assert.IsTrue(zone.IsFatal(200));
            Assert.IsFalse(zone.IsFatal(10));
            Assert.IsFalse(zone.IsFatal(190));
            Assert.IsFalse(zone.IsFatal(80));
        }
    }
}
