using NUnit.Framework;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Tests
{
    /// <summary>C. 인디케이터 — 감정 극성 합산, 클램프, 예민 특성 배율.</summary>
    public class IndicatorTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        [Test]
        public void Evaluate_ExcitedTagsSumPositive()
        {
            var tags = new TagSet(emotions: new[] { EmotionTag.Happiness, EmotionTag.Anger });
            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(2, evaluator.Evaluate(tags));
        }

        [Test]
        public void Evaluate_DepressedTagsSumNegative()
        {
            var tags = new TagSet(emotions: new[] { EmotionTag.Fear, EmotionTag.Sadness });
            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(-2, evaluator.Evaluate(tags));
        }

        [Test]
        public void Evaluate_DuplicateEmotionsAccumulateByCount()
        {
            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 2);

            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(-2, evaluator.Evaluate(tags), "혐오 2개는 -2로 누적되어야 한다.");
        }

        [Test]
        public void Evaluate_MixedPolarities_NetsOut()
        {
            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 2); // -2
            tags.AddEmotion(EmotionTag.Love, 1);    // +1

            var evaluator = new EmotionEvaluator(Polarity);

            Assert.AreEqual(-1, evaluator.Evaluate(tags));
        }

        [Test]
        public void Move_ClampsAtUpperBound()
        {
            var indicator = new StabilityIndicator(10); // 0~9, 중앙 5

            indicator.Move(100);

            Assert.AreEqual(9, indicator.Position);
        }

        [Test]
        public void Move_ClampsAtLowerBound()
        {
            var indicator = new StabilityIndicator(10);

            indicator.Move(-100);

            Assert.AreEqual(0, indicator.Position);
        }

        [Test]
        public void Move_FiresMovedEvent_WithFromAndTo()
        {
            var indicator = new StabilityIndicator(10); // 중앙 5
            (int from, int to)? captured = null;
            indicator.Moved += (from, to) => captured = (from, to);

            indicator.Move(-2);

            Assert.AreEqual(3, indicator.Position);
            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual((5, 3), captured.Value);
        }

        [Test]
        public void Move_AtClampedBoundary_DoesNotFireEvent_WhenPositionUnchanged()
        {
            var indicator = new StabilityIndicator(10);
            indicator.Move(-100); // 0으로 클램프
            Assert.AreEqual(0, indicator.Position);

            var fired = false;
            indicator.Moved += (_, _) => fired = true;

            indicator.Move(-5); // 이미 0이므로 더 내려갈 수 없다.

            Assert.IsFalse(fired, "위치가 실제로 변하지 않으면 Moved 이벤트가 발생하지 않아야 한다.");
            Assert.AreEqual(0, indicator.Position);
        }

        [Test]
        public void TraitAwareEvaluator_WithoutSensitive_MultiplierIsOne()
        {
            var traits = new TraitBoard();
            var evaluator = new TraitAwareEmotionEvaluator(new EmotionEvaluator(Polarity), traits);

            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 2); // base -2

            Assert.AreEqual(-2, evaluator.Evaluate(tags));
        }

        [Test]
        public void TraitAwareEvaluator_WithSensitive_MultipliesByThree()
        {
            var traits = new TraitBoard();
            traits.Grant(TraitType.Sensitive, duration: 3);
            var evaluator = new TraitAwareEmotionEvaluator(new EmotionEvaluator(Polarity), traits);

            var tags = new TagSet();
            tags.AddEmotion(EmotionTag.Disgust, 2); // base -2

            Assert.AreEqual(-6, evaluator.Evaluate(tags), "예민 특성은 이동량을 3배로 만들어야 한다.");
        }
    }
}
