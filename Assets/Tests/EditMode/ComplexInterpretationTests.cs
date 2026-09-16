using NUnit.Framework;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Tests
{
    /// <summary>A. 컴플렉스 해석 — 기획서 판정 표 검증.</summary>
    public class ComplexInterpretationTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static ComplexBoard BoardWith(params ComplexDefinition[] defs)
        {
            var board = new ComplexBoard();
            for (var i = 0; i < defs.Length; i++)
                board.TryAttach(new ComplexInstance(defs[i], priority: i));
            return board;
        }

        [Test]
        public void AntiPast_PastPersonHappyLove_BecomesDisgustTimesTwo()
        {
            // 헤진 토끼 인형: 과거/가족/행복,사랑
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Family },
                new[] { EmotionTag.Happiness, EmotionTag.Love });

            var board = BoardWith(PrototypeContent.AntiPast(Polarity));
            var resolver = new ComplexResolver(board);

            var result = resolver.Resolve(tags);

            Assert.IsTrue(result.Steps[0].Triggered, "반 과거는 조건이 성립해야 발동한다.");
            Assert.AreEqual(TimeTag.Past, result.Final.Time, "반 과거는 시간을 바꾸지 않는다.");
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Happiness));
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Love));
            Assert.AreEqual(2, result.Final.CountOf(EmotionTag.Disgust), "행복+사랑 2개가 모두 혐오로 바뀌어 누적되어야 한다.");
        }

        [Test]
        public void Stockholm_PersonFear_BecomesLove()
        {
            var tags = new TagSet(TimeTag.Present, new[] { PersonTag.Other },
                new[] { EmotionTag.Fear, EmotionTag.Sadness });

            var board = BoardWith(PrototypeContent.Stockholm());
            var resolver = new ComplexResolver(board);

            var result = resolver.Resolve(tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Fear));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Love));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness), "관련 없는 감정은 그대로 남아야 한다.");
        }

        [Test]
        public void Trauma_PastPersonDepressedEmotion_ShiftsToPresentAndAddsFear()
        {
            // 유골함: 과거/가족/슬픔
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Sadness });

            var board = BoardWith(PrototypeContent.Trauma(Polarity));
            var resolver = new ComplexResolver(board);

            var result = resolver.Resolve(tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.AreEqual(TimeTag.Present, result.Final.Time, "트라우마는 시간을 현재로 바꿔야 한다.");
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness), "기존 슬픔은 그대로 남아야 한다.");
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear), "공포가 추가되어야 한다.");
        }

        [Test]
        public void AntiPast_PresentTime_ConditionFails_OriginalUnchanged()
        {
            // 입양 동의서: 현재/타인/공포,슬픔 — 시간이 현재라 반 과거 조건이 성립하지 않는다.
            var tags = new TagSet(TimeTag.Present, new[] { PersonTag.Other },
                new[] { EmotionTag.Fear, EmotionTag.Sadness });

            var board = BoardWith(PrototypeContent.AntiPast(Polarity));
            var resolver = new ComplexResolver(board);

            var result = resolver.Resolve(tags);

            Assert.IsFalse(result.Steps[0].Triggered, "시간이 과거가 아니므로 반 과거는 발동하지 않아야 한다.");
            Assert.AreEqual(result.Original.Time, result.Final.Time);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness));
            CollectionAssert.AreEquivalent(result.Original.Persons, result.Final.Persons);
        }
    }
}
