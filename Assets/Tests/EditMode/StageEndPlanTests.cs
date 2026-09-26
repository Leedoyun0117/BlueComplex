using System;
using NUnit.Framework;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>"스테이지 클리어 연출"의 자물쇠 계획 — 자물쇠 수·열린 수·이어지는 경로.</summary>
    public class StageEndPlanTests
    {
        [Test]
        public void Cleared_OpensEveryLock_AndGoesToTheNextStage()
        {
            var plan = StageEndPlan.Create(StageOutcome.Cleared, requiredKeys: 2, collectedKeys: 2, hasNextStage: true);
            Assert.AreEqual(2, plan.LockCount);
            Assert.AreEqual(2, plan.OpenedCount);
            Assert.IsTrue(plan.AllOpened);
            Assert.AreEqual(StageEndRoute.ClearToNextStage, plan.Route);
        }

        [Test]
        public void Cleared_WithoutANextStage_EndsAtTheResultPanel()
        {
            var plan = StageEndPlan.Create(StageOutcome.Cleared, 3, 3, hasNextStage: false);
            Assert.IsTrue(plan.AllOpened);
            Assert.AreEqual(StageEndRoute.ClearToEnd, plan.Route);
        }

        [Test]
        public void Failed_LeavesTheMissingLocksClosed_AndReturnsToStart()
        {
            var plan = StageEndPlan.Create(StageOutcome.Failed, 3, 1, hasNextStage: true);
            Assert.AreEqual(3, plan.LockCount);
            Assert.AreEqual(1, plan.OpenedCount);
            Assert.IsFalse(plan.AllOpened);
            Assert.AreEqual(StageEndRoute.FailToStart, plan.Route);
        }

        [Test]
        public void Failed_WithNoKeys_OpensNothing()
        {
            var plan = StageEndPlan.Create(StageOutcome.Failed, 2, 0, hasNextStage: false);
            Assert.AreEqual(0, plan.OpenedCount);
            Assert.AreEqual(StageEndRoute.FailToStart, plan.Route);
        }

        [Test]
        public void Failed_NeverShowsEveryLockOpen_EvenIfTheCountIsOff()
        {
            var plan = StageEndPlan.Create(StageOutcome.Failed, 2, 5, hasNextStage: true);
            Assert.IsFalse(plan.AllOpened);
            Assert.AreEqual(StageEndRoute.FailToStart, plan.Route);
        }

        [Test]
        public void InProgress_HasNoEndSequence() =>
            Assert.Throws<ArgumentException>(() => StageEndPlan.Create(StageOutcome.InProgress, 2, 0, false));

        [Test]
        public void ZeroRequiredKeys_IsInvalid() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => StageEndPlan.Create(StageOutcome.Cleared, 0, 0, false));

        /// <summary>실제 스테이지 결과(봇 실행)에서 계획이 코어 판정과 항상 일치하는가: Cleared ⇔ 모든 자물쇠 열림.</summary>
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void RealRuns_PlanAgreesWithTheCoreOutcome(int seed)
        {
            var polarity = new DefaultEmotionPolarityTable();
            foreach (var config in new[] { PrototypeContent.PrototypeStage(polarity), Stage2Content.Stage2(polarity) })
            {
                var result = QuarterBots.RunStage(config, seed, config.Id, QuarterBots.ChooseHeuristicCard, null);
                var plan = StageEndPlan.Create(result.Outcome, result.KeyRequired, result.KeysCollected, hasNextStage: true);

                Assert.AreEqual(config.RequiredKeys, plan.LockCount, config.Id);
                Assert.AreEqual(result.Outcome == StageOutcome.Cleared, plan.AllOpened, $"{config.Id} seed={seed}");
                Assert.AreEqual(result.KeysCollected, plan.OpenedCount, $"{config.Id} seed={seed}");
            }
        }
    }
}
