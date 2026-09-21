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

        // 회피(과거+공포/혐오 → 현재)와 반 과거(과거+인물+행복/사랑 → 혐오)가 둘 다 조건을 만족할 수 있는 단서.
        private static TagSet FearAndHappinessInThePast() =>
            new(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Fear, EmotionTag.Happiness });

        [Test]
        public void AvoidanceFirst_BreaksAntiPastPastCondition()
        {
            var board = new ComplexBoard();
            var avoidance = new ComplexInstance(PrototypeContent.Avoidance(), priority: 0);
            var antiPast = new ComplexInstance(PrototypeContent.AntiPast(Polarity), priority: 1);
            board.TryAttach(avoidance);
            board.TryAttach(antiPast);

            var resolver = new ComplexResolver(board);
            var result = resolver.Resolve(FearAndHappinessInThePast());

            Assert.AreEqual(2, result.Steps.Count);
            Assert.AreSame(avoidance, result.Steps[0].Complex, "낮은 우선순위(회피)가 먼저 해석되어야 한다.");
            Assert.IsTrue(result.Steps[0].Triggered, "회피는 과거+공포 조건을 만족해 발동해야 한다.");

            Assert.AreSame(antiPast, result.Steps[1].Complex);
            Assert.IsFalse(result.Steps[1].Triggered,
                "회피가 시간을 현재로 바꿔버렸으므로 반 과거의 '과거' 조건이 깨져야 한다.");

            Assert.AreEqual(TimeTag.Present, result.Final.Time);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Happiness), "반 과거가 발동하지 않았으므로 행복은 그대로 남아야 한다.");
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear));
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Disgust));
        }

        [Test]
        public void AntiPastFirst_BothTrigger_ButFinalResultDiffersFromAvoidanceFirst()
        {
            var board = new ComplexBoard();
            var antiPast = new ComplexInstance(PrototypeContent.AntiPast(Polarity), priority: 0);
            var avoidance = new ComplexInstance(PrototypeContent.Avoidance(), priority: 1);
            board.TryAttach(antiPast);
            board.TryAttach(avoidance);

            var resolver = new ComplexResolver(board);
            var result = resolver.Resolve(FearAndHappinessInThePast());

            Assert.AreSame(antiPast, result.Steps[0].Complex, "낮은 우선순위(반 과거)가 먼저 해석되어야 한다.");
            Assert.IsTrue(result.Steps[0].Triggered);

            Assert.AreSame(avoidance, result.Steps[1].Complex);
            Assert.IsTrue(result.Steps[1].Triggered,
                "반 과거는 시간을 바꾸지 않으므로 회피는 여전히 발동해야 한다.");

            Assert.AreEqual(TimeTag.Present, result.Final.Time);
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Happiness), "행복은 반 과거에 의해 혐오로 바뀌어 사라져야 한다.");
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Disgust));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear));
        }

        [Test]
        public void ChildhoodFriendThenOthering_ChainsFriendToLoverToOther()
        {
            // 꽃 한 송이: 과거/친구/사랑 — 소꿉친구가 친구를 연인으로 바꾼 뒤 타자화가 연인을 타인으로 바꾼다.
            var board = new ComplexBoard();
            var childhoodFriend = new ComplexInstance(PrototypeContent.ChildhoodFriend(), priority: 0);
            var othering = new ComplexInstance(PrototypeContent.Othering(), priority: 1);
            board.TryAttach(childhoodFriend);
            board.TryAttach(othering);

            var result = new ComplexResolver(board).Resolve(
                new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love }));

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsTrue(result.Steps[1].Triggered);
            CollectionAssert.AreEqual(new[] { PersonTag.Lover }, result.Steps[1].ObservedPersons,
                "타자화가 참조한 것은 소꿉친구가 만든 '연인'이다 — 원본 태그(친구)가 아니다.");
            CollectionAssert.AreEqual(new[] { PersonTag.Other }, result.Final.Persons);
        }

        [Test]
        public void OtheringFirst_ConsumesTheFriend_SoChildhoodFriendDoesNotTrigger()
        {
            var board = new ComplexBoard();
            var othering = new ComplexInstance(PrototypeContent.Othering(), priority: 0);
            var childhoodFriend = new ComplexInstance(PrototypeContent.ChildhoodFriend(), priority: 1);
            board.TryAttach(othering);
            board.TryAttach(childhoodFriend);

            var result = new ComplexResolver(board).Resolve(
                new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love }));

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsFalse(result.Steps[1].Triggered, "타자화가 먼저 친구를 타인으로 바꿨으니 소꿉친구가 볼 친구 태그가 없다.");
            CollectionAssert.AreEqual(new[] { PersonTag.Other }, result.Final.Persons);
        }

        [Test]
        public void AttachOrderDoesNotAffectResolutionOrder_OnlyPriorityDoes()
        {
            // TryAttach를 우선순위 역순으로 붙여도 InPriorityOrder / Resolve 순서는 우선순위를 따라야 한다.
            var board = new ComplexBoard();
            var antiPast = new ComplexInstance(PrototypeContent.AntiPast(Polarity), priority: 1);
            var avoidance = new ComplexInstance(PrototypeContent.Avoidance(), priority: 0);
            board.TryAttach(antiPast); // 우선순위 1을 먼저 붙임
            board.TryAttach(avoidance); // 우선순위 0을 나중에 붙임

            var resolver = new ComplexResolver(board);
            var result = resolver.Resolve(FearAndHappinessInThePast());

            Assert.AreSame(avoidance, result.Steps[0].Complex, "부착 순서가 아니라 우선순위 순서로 해석되어야 한다.");
            Assert.AreSame(antiPast, result.Steps[1].Complex);
        }
    }
}
