using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>스테이지 종료 컷신 진행: 실패는 시도마다 하나씩(<see cref="StageCutsceneProgress.Take"/>),
    /// 클리어해서 다음 단계로 넘어가는 시점은 못 본 것을 전부 몰아서(<see cref="StageCutsceneProgress.TakeRemaining"/>).</summary>
    public class StageCutsceneProgressTests
    {
        private static string Flat(IReadOnlyList<int[]> steps) => string.Join(",", steps.Select(s => string.Join("+", s)));

        [Test]
        public void Take_EachFailedAttemptPlaysTheNextStepOnly()
        {
            var progress = new StageCutsceneProgress();
            Assert.AreEqual("1", Flat(progress.Take(1, StageOutcome.Failed)));
            Assert.AreEqual("2", Flat(progress.Take(1, StageOutcome.Failed)));
            Assert.AreEqual(2, progress.Shown(1));
        }

        [Test]
        public void TakeRemaining_FromScratch_PlaysEveryStepAtOnce()
        {
            var progress = new StageCutsceneProgress();
            var steps = progress.TakeRemaining(1, StageOutcome.Cleared);
            Assert.AreEqual("1,2,3,4", Flat(steps));
            Assert.AreEqual(4, progress.Shown(1));
        }

        [Test]
        public void TakeRemaining_AfterSomeFailedAttempts_PlaysOnlyTheUnseenOnes()
        {
            var progress = new StageCutsceneProgress();
            progress.Take(1, StageOutcome.Failed); // 1
            progress.Take(1, StageOutcome.Failed); // 2

            var steps = progress.TakeRemaining(1, StageOutcome.Cleared);
            Assert.AreEqual("3,4", Flat(steps));
            Assert.AreEqual(4, progress.Shown(1));
        }

        [Test]
        public void TakeRemaining_AfterEverythingAlreadyShown_ReturnsEmpty()
        {
            var progress = new StageCutsceneProgress();
            progress.TakeRemaining(1, StageOutcome.Cleared);

            Assert.IsEmpty(progress.TakeRemaining(1, StageOutcome.Cleared));
            Assert.AreEqual(4, progress.Shown(1));
        }

        [Test]
        public void AfterEveryStepWasShown_LaterFailedAttemptsHaveNoCutscene()
        {
            var progress = new StageCutsceneProgress();
            for (var i = 0; i < 4; i++) progress.Take(1, StageOutcome.Failed);

            Assert.IsEmpty(progress.Take(1, StageOutcome.Failed));
            Assert.IsEmpty(progress.TakeRemaining(1, StageOutcome.Cleared));
            Assert.AreEqual(4, progress.Shown(1));
        }

        [Test]
        public void StagesKeepTheirOwnCount_AndStage2UsesTheMergedStep()
        {
            var progress = new StageCutsceneProgress();
            progress.Take(1, StageOutcome.Failed); // stage 1: 1

            Assert.AreEqual("5", Flat(progress.Take(2, StageOutcome.Failed)));
            Assert.AreEqual("6+7,8,9", Flat(progress.TakeRemaining(2, StageOutcome.Cleared)));
            Assert.AreEqual("2", Flat(progress.Take(1, StageOutcome.Failed))); // stage 1 count untouched by stage 2 activity
        }

        [Test]
        public void NextStageStartsItsOwnCountFromZero()
        {
            var progress = new StageCutsceneProgress();
            progress.TakeRemaining(1, StageOutcome.Cleared); // stage 1 fully shown

            Assert.AreEqual(0, progress.Shown(2));
            Assert.AreEqual("5", Flat(progress.Take(2, StageOutcome.Failed)));
        }

        [Test]
        public void Stage3_FailurePlaysNothingAndIsNotCounted_ClearBingesTheMergedStep()
        {
            var progress = new StageCutsceneProgress();
            Assert.IsEmpty(progress.Take(3, StageOutcome.Failed));
            Assert.IsEmpty(progress.Take(3, StageOutcome.Failed));
            Assert.AreEqual(0, progress.Shown(3));

            Assert.AreEqual("10+11", Flat(progress.TakeRemaining(3, StageOutcome.Cleared)));
            Assert.IsEmpty(progress.TakeRemaining(3, StageOutcome.Cleared));
        }

        [Test]
        public void UnknownStage_HasNothing()
        {
            var progress = new StageCutsceneProgress();
            Assert.IsEmpty(progress.Take(9, StageOutcome.Cleared));
            Assert.IsEmpty(progress.TakeRemaining(9, StageOutcome.Cleared));
        }
    }
}
