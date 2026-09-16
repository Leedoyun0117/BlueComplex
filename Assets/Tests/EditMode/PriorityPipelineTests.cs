using NUnit.Framework;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Tests
{
    /// <summary>B. 우선순위 파이프라인 — 낮은 우선순위부터 순서대로 해석되며, 순서가 최종 결과를 바꾼다.</summary>
    public class PriorityPipelineTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        // 비행기 티켓: 과거/가족/슬픔,행복 — 트라우마(침체 감정 필요)와 반 과거(행복 필요) 둘 다 조건을 만족할 수 있는 단서.
        private static TagSet TicketTags() =>
            new(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Sadness, EmotionTag.Happiness });

        [Test]
        public void TraumaFirst_BreaksAntiPastPastCondition()
        {
            var board = new ComplexBoard();
            var trauma = new ComplexInstance(PrototypeContent.Trauma(Polarity), priority: 0);
            var antiPast = new ComplexInstance(PrototypeContent.AntiPast(Polarity), priority: 1);
            board.TryAttach(trauma);
            board.TryAttach(antiPast);

            var resolver = new ComplexResolver(board);
            var result = resolver.Resolve(TicketTags());

            Assert.AreEqual(2, result.Steps.Count);
            Assert.AreSame(trauma, result.Steps[0].Complex, "낮은 우선순위(트라우마)가 먼저 해석되어야 한다.");
            Assert.IsTrue(result.Steps[0].Triggered, "트라우마는 과거+가족+슬픔 조건을 만족해 발동해야 한다.");

            Assert.AreSame(antiPast, result.Steps[1].Complex);
            Assert.IsFalse(result.Steps[1].Triggered,
                "트라우마가 시간을 현재로 바꿔버렸으므로 반 과거의 '과거' 조건이 깨져야 한다.");

            Assert.AreEqual(TimeTag.Present, result.Final.Time);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Happiness), "반 과거가 발동하지 않았으므로 행복은 그대로 남아야 한다.");
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear));
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Disgust));
        }

        [Test]
        public void AntiPastFirst_BothTrigger_ButFinalResultDiffersFromTraumaFirst()
        {
            var board = new ComplexBoard();
            var antiPast = new ComplexInstance(PrototypeContent.AntiPast(Polarity), priority: 0);
            var trauma = new ComplexInstance(PrototypeContent.Trauma(Polarity), priority: 1);
            board.TryAttach(antiPast);
            board.TryAttach(trauma);

            var resolver = new ComplexResolver(board);
            var result = resolver.Resolve(TicketTags());

            Assert.AreSame(antiPast, result.Steps[0].Complex, "낮은 우선순위(반 과거)가 먼저 해석되어야 한다.");
            Assert.IsTrue(result.Steps[0].Triggered);

            Assert.AreSame(trauma, result.Steps[1].Complex);
            Assert.IsTrue(result.Steps[1].Triggered,
                "반 과거가 먼저 행복→혐오 변환을 해도, 슬픔이 남아있으므로 트라우마는 여전히 발동해야 한다.");

            Assert.AreEqual(TimeTag.Present, result.Final.Time);
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Happiness), "행복은 반 과거에 의해 혐오로 바뀌어 사라져야 한다.");
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Disgust));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear));
        }

        [Test]
        public void AttachOrderDoesNotAffectResolutionOrder_OnlyPriorityDoes()
        {
            // TryAttach를 우선순위 역순으로 붙여도 InPriorityOrder / Resolve 순서는 우선순위를 따라야 한다.
            var board = new ComplexBoard();
            var antiPast = new ComplexInstance(PrototypeContent.AntiPast(Polarity), priority: 1);
            var trauma = new ComplexInstance(PrototypeContent.Trauma(Polarity), priority: 0);
            board.TryAttach(antiPast); // 우선순위 1을 먼저 붙임
            board.TryAttach(trauma);   // 우선순위 0을 나중에 붙임

            var resolver = new ComplexResolver(board);
            var result = resolver.Resolve(TicketTags());

            Assert.AreSame(trauma, result.Steps[0].Complex, "부착 순서가 아니라 우선순위 순서로 해석되어야 한다.");
            Assert.AreSame(antiPast, result.Steps[1].Complex);
        }
    }
}
