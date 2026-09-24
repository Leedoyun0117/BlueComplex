using NUnit.Framework;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Tests
{
    /// <summary>A. 컴플렉스 해석 — 기획서 스테이지 1 컴플렉스 목록의 판정 표 검증.</summary>
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

        private static InterpretationResult Resolve(ComplexDefinition def, TagSet tags) =>
            new ComplexResolver(BoardWith(def)).Resolve(tags);

        [Test]
        public void AntiPast_PastPersonHappyLove_BecomesDisgustTimesTwo()
        {
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Family },
                new[] { EmotionTag.Happiness, EmotionTag.Love });

            var result = Resolve(PrototypeContent.AntiPast(Polarity), tags);

            Assert.IsTrue(result.Steps[0].Triggered, "반 과거는 조건이 성립해야 발동한다.");
            Assert.AreEqual(TimeTag.Past, result.Final.Time, "반 과거는 시간을 바꾸지 않는다.");
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Happiness));
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Love));
            Assert.AreEqual(2, result.Final.CountOf(EmotionTag.Disgust), "행복+사랑 2개가 모두 혐오로 바뀌어 누적되어야 한다.");
        }

        [Test]
        public void AntiPast_PresentTime_ConditionFails_OriginalUnchanged()
        {
            var tags = new TagSet(TimeTag.Present, new[] { PersonTag.Other },
                new[] { EmotionTag.Fear, EmotionTag.Sadness });

            var result = Resolve(PrototypeContent.AntiPast(Polarity), tags);

            Assert.IsFalse(result.Steps[0].Triggered, "시간이 과거가 아니므로 반 과거는 발동하지 않아야 한다.");
            Assert.AreEqual(result.Original.Time, result.Final.Time);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness));
            CollectionAssert.AreEquivalent(result.Original.Persons, result.Final.Persons);
        }

        [Test]
        public void AntiPast_PastWithoutAnyPerson_DoesNotTrigger()
        {
            // 과거/(인물 없음)/행복 — '임의의 인물 태그'가 필요하므로 발동하지 않는다.
            var tags = new TagSet(TimeTag.Past, null, new[] { EmotionTag.Happiness });

            var result = Resolve(PrototypeContent.AntiPast(Polarity), tags);

            Assert.IsFalse(result.Steps[0].Triggered);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Happiness));
        }

        [Test]
        public void Stockholm_PersonFear_BecomesLove()
        {
            var tags = new TagSet(TimeTag.Present, new[] { PersonTag.Other },
                new[] { EmotionTag.Fear, EmotionTag.Sadness });

            var result = Resolve(PrototypeContent.Stockholm(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Fear));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Love));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness), "관련 없는 감정은 그대로 남아야 한다.");
        }

        [Test]
        public void Stockholm_FearWithoutPerson_DoesNotTrigger()
        {
            // 공포 소설: 현재/(인물 없음)/공포 — 인물 태그가 없으면 스톡홀름은 발동하지 않는다.
            var tags = new TagSet(TimeTag.Present, null, new[] { EmotionTag.Fear });

            var result = Resolve(PrototypeContent.Stockholm(), tags);

            Assert.IsFalse(result.Steps[0].Triggered);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear));
        }

        [Test]
        public void GoodChild_FamilyWithDepressedEmotion_AddsHappiness()
        {
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Sadness });

            var result = Resolve(PrototypeContent.GoodChild(Polarity), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness), "기존 감정은 그대로 남는다.");
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Happiness));
        }

        [Test]
        public void GoodChild_FamilyWithOnlyExcitedEmotion_DoesNotTrigger()
        {
            // 가족사진 액자: 과거/가족/행복 — 침체 감정이 없다.
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness });

            var result = Resolve(PrototypeContent.GoodChild(Polarity), tags);

            Assert.IsFalse(result.Steps[0].Triggered);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Happiness));
        }

        [Test]
        public void ChildhoodFriend_PastFriend_BecomesLover()
        {
            // 꽃 한 송이: 과거/친구/사랑
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love });

            var result = Resolve(PrototypeContent.ChildhoodFriend(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsFalse(result.Final.HasPerson(PersonTag.Friend));
            Assert.IsTrue(result.Final.HasPerson(PersonTag.Lover));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Love), "감정은 건드리지 않는다.");
        }

        [Test]
        public void ChildhoodFriend_PresentFriend_DoesNotTrigger()
        {
            var tags = new TagSet(TimeTag.Present, new[] { PersonTag.Friend }, new[] { EmotionTag.Happiness });

            var result = Resolve(PrototypeContent.ChildhoodFriend(), tags);

            Assert.IsFalse(result.Steps[0].Triggered, "'과거' 조건이 깨지면 발동하지 않는다.");
            Assert.IsTrue(result.Final.HasPerson(PersonTag.Friend));
        }

        [Test]
        public void ChildhoodFriend_KeepsOtherPersons_AndObservesOnlyFriend()
        {
            // 아이들의 낙서: 과거/타인,친구/분노
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Other, PersonTag.Friend }, new[] { EmotionTag.Anger });

            var result = Resolve(PrototypeContent.ChildhoodFriend(), tags);

            CollectionAssert.AreEquivalent(new[] { PersonTag.Other, PersonTag.Lover }, result.Final.Persons);
            CollectionAssert.AreEqual(new[] { PersonTag.Friend }, result.Steps[0].ObservedPersons,
                "조건이 참조한 인물(친구)만 관찰로 남아야 한다.");
        }

        [TestCase(PersonTag.Friend)]
        [TestCase(PersonTag.Lover)]
        public void Othering_FriendOrLover_BecomesOther(PersonTag source)
        {
            var tags = new TagSet(TimeTag.Past, new[] { source }, new[] { EmotionTag.Sadness });

            var result = Resolve(PrototypeContent.Othering(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            CollectionAssert.AreEqual(new[] { PersonTag.Other }, result.Final.Persons);
        }

        [Test]
        public void Othering_OnlyFamilyOrOther_DoesNotTrigger()
        {
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Family, PersonTag.Other }, new[] { EmotionTag.Sadness });

            var result = Resolve(PrototypeContent.Othering(), tags);

            Assert.IsFalse(result.Steps[0].Triggered);
            CollectionAssert.AreEquivalent(new[] { PersonTag.Family, PersonTag.Other }, result.Final.Persons);
        }

        [Test]
        public void Othering_FriendAndOther_MergesIntoSingleOther()
        {
            // 찢어진 책가방: 과거/타인,친구/공포,분노
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Other, PersonTag.Friend },
                new[] { EmotionTag.Fear, EmotionTag.Anger });

            var result = Resolve(PrototypeContent.Othering(), tags);

            CollectionAssert.AreEqual(new[] { PersonTag.Other }, result.Final.Persons);
        }

        [Test]
        public void Optimism_OtherWithAnyEmotion_AddsHappiness()
        {
            // 개학 날짜 달력: 미래/타인/슬픔,혐오
            var tags = new TagSet(TimeTag.Future, new[] { PersonTag.Other },
                new[] { EmotionTag.Sadness, EmotionTag.Disgust });

            var result = Resolve(PrototypeContent.Optimism(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Happiness));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Disgust));
        }

        [Test]
        public void Optimism_WithoutOtherOrWithoutEmotion_DoesNotTrigger()
        {
            var noOther = new TagSet(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness });
            var noEmotion = new TagSet(TimeTag.Past, new[] { PersonTag.Other });

            Assert.IsFalse(Resolve(PrototypeContent.Optimism(), noOther).Steps[0].Triggered);
            Assert.IsFalse(Resolve(PrototypeContent.Optimism(), noEmotion).Steps[0].Triggered);
        }

        [Test]
        public void Rumination_Past_EachEmotionGainsOne()
        {
            // 찢어진 책가방: 과거/타인,친구/공포,분노
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Other, PersonTag.Friend },
                new[] { EmotionTag.Fear, EmotionTag.Anger });

            var result = Resolve(PrototypeContent.Rumination(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.AreEqual(2, result.Final.CountOf(EmotionTag.Fear));
            Assert.AreEqual(2, result.Final.CountOf(EmotionTag.Anger));
        }

        [Test]
        public void Rumination_NotPast_DoesNotTrigger()
        {
            var tags = new TagSet(TimeTag.Present, null, new[] { EmotionTag.Sadness });

            var result = Resolve(PrototypeContent.Rumination(), tags);

            Assert.IsFalse(result.Steps[0].Triggered);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness));
        }

        [Test]
        public void Guilt_FamilyHappiness_BecomesSadness()
        {
            // 가족사진 액자: 과거/가족/행복
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness });

            var result = Resolve(PrototypeContent.Guilt(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Happiness));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Sadness));
        }

        [Test]
        public void Guilt_HappinessWithoutFamily_DoesNotTrigger()
        {
            // 과거/(인물 없음)/행복 — 가족 태그가 없다.
            var tags = new TagSet(TimeTag.Past, null, new[] { EmotionTag.Happiness });

            Assert.IsFalse(Resolve(PrototypeContent.Guilt(), tags).Steps[0].Triggered);
        }

        [Test]
        public void Dependence_OtherSadness_BecomesLove()
        {
            // 개학 날짜 달력: 미래/타인/슬픔,혐오
            var tags = new TagSet(TimeTag.Future, new[] { PersonTag.Other },
                new[] { EmotionTag.Sadness, EmotionTag.Disgust });

            var result = Resolve(PrototypeContent.Dependence(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsFalse(result.Final.HasEmotion(EmotionTag.Sadness));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Love));
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Disgust), "슬픔이 아닌 감정은 그대로 남는다.");
        }

        [Test]
        public void Avoidance_PresentFear_ShiftsToPast()
        {
            // 공포 소설: 현재/(인물 없음)/공포
            var tags = new TagSet(TimeTag.Present, new PersonTag[0], new[] { EmotionTag.Fear });

            var result = Resolve(PrototypeContent.Avoidance(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.AreEqual(TimeTag.Past, result.Final.Time);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Fear), "감정은 바뀌지 않는다.");
        }

        [Test]
        public void Avoidance_FutureDisgust_ShiftsToPast()
        {
            // 개학 날짜 달력: 미래/타인/슬픔,혐오
            var tags = new TagSet(TimeTag.Future, new[] { PersonTag.Other },
                new[] { EmotionTag.Sadness, EmotionTag.Disgust });

            var result = Resolve(PrototypeContent.Avoidance(), tags);

            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.AreEqual(TimeTag.Past, result.Final.Time);
            Assert.AreEqual(1, result.Final.CountOf(EmotionTag.Disgust), "감정은 바뀌지 않는다.");
        }

        [Test]
        public void Avoidance_PastFearOrDisgust_DoesNotTrigger_PastIsNoLongerTheSource()
        {
            // 찢어진 책가방: 과거/타인,친구/공포,분노 — 회피는 이제 현재/미래에서만 발동한다.
            var tags = new TagSet(TimeTag.Past, new[] { PersonTag.Other, PersonTag.Friend },
                new[] { EmotionTag.Fear, EmotionTag.Anger });

            var result = Resolve(PrototypeContent.Avoidance(), tags);

            Assert.IsFalse(result.Steps[0].Triggered);
            Assert.AreEqual(TimeTag.Past, result.Final.Time);
        }

        [Test]
        public void Avoidance_PresentWithoutFearOrDisgust_DoesNotTrigger()
        {
            var tags = new TagSet(TimeTag.Present, new[] { PersonTag.Family }, new[] { EmotionTag.Sadness });

            var result = Resolve(PrototypeContent.Avoidance(), tags);

            Assert.IsFalse(result.Steps[0].Triggered);
            Assert.AreEqual(TimeTag.Present, result.Final.Time);
        }
    }
}
