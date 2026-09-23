using NUnit.Framework;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>"표정과 반응" 기획표의 유키/나츠 판정 로직. 우선순위(분노 → 슬픔 → 기쁨)는 기획표 순서를 그대로 따른 해석이다
    /// (PortraitReactionRules.ClassifyYuki 문서 참고) — 스펙에 명시된 순서는 아니다.</summary>
    public class PortraitReactionRulesTests
    {
        private static TagSet Tags(params EmotionTag[] emotions) => new(TimeTag.None, null, emotions);

        [Test]
        public void NoClue_IsNormal() =>
            Assert.AreEqual(YukiReaction.Normal, PortraitReactionRules.ClassifyYuki(null, HeartbeatState.Stable));

        [Test]
        public void AngerPlurality_IsAnger() =>
            Assert.AreEqual(YukiReaction.Anger,
                PortraitReactionRules.ClassifyYuki(Tags(EmotionTag.Anger, EmotionTag.Anger, EmotionTag.Sadness), HeartbeatState.Stable));

        [Test]
        public void AngerTiedWithAnother_IsNotAnger() =>
            Assert.AreNotEqual(YukiReaction.Anger,
                PortraitReactionRules.ClassifyYuki(Tags(EmotionTag.Anger, EmotionTag.Sadness), HeartbeatState.Stable));

        [Test]
        public void DepressedHeartbeat_IsSadness_EvenWithHappyTag() =>
            Assert.AreEqual(YukiReaction.Sadness,
                PortraitReactionRules.ClassifyYuki(Tags(EmotionTag.Happiness), HeartbeatState.VeryDepressed));

        [Test]
        public void HappinessAndLoveCombined_BeatsSingleOtherTag_IsJoy() =>
            Assert.AreEqual(YukiReaction.Joy,
                PortraitReactionRules.ClassifyYuki(Tags(EmotionTag.Happiness, EmotionTag.Love), HeartbeatState.Stable));

        [Test]
        public void JoyCombinedTiedWithAnother_IsNormal() =>
            Assert.AreEqual(YukiReaction.Normal,
                PortraitReactionRules.ClassifyYuki(Tags(EmotionTag.Happiness, EmotionTag.Fear, EmotionTag.Fear), HeartbeatState.Stable));

        [Test]
        public void AngerPlurality_OutranksJoy_WhenBothPresent() =>
            Assert.AreEqual(YukiReaction.Anger,
                PortraitReactionRules.ClassifyYuki(Tags(EmotionTag.Anger, EmotionTag.Anger, EmotionTag.Happiness), HeartbeatState.Stable));

        [Test]
        public void Stable_IsNotFlustered() => Assert.IsFalse(PortraitReactionRules.IsNatsuFlustered(HeartbeatState.Stable));

        [Test]
        public void Depressed_IsNotFlustered() => Assert.IsFalse(PortraitReactionRules.IsNatsuFlustered(HeartbeatState.Depressed));

        [Test]
        public void VeryDepressed_IsFlustered() => Assert.IsTrue(PortraitReactionRules.IsNatsuFlustered(HeartbeatState.VeryDepressed));

        [Test]
        public void VeryExcited_IsFlustered() => Assert.IsTrue(PortraitReactionRules.IsNatsuFlustered(HeartbeatState.VeryExcited));
    }
}
