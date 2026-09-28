using NUnit.Framework;
using BlueComplex.Core.Save;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// "취조시작"을 누르면 저장된 진행도(<see cref="StageProgress.ResumeStage"/>)로 어디부터 이어할지 고르는 순수 분기 로직.
    /// 실패나 중도 이탈은 진행도에 반영되지 않으므로 여기 입력값에도 나타나지 않는다 — 클리어에서만 갱신된 값을 받는다는 전제.
    /// </summary>
    public class StageProgressTests
    {
        private const int LastStage = 3;

        [Test]
        public void TutorialNotCleared_ResumesAtTutorial_RegardlessOfStageProgress()
        {
            Assert.IsNull(StageProgress.ResumeStage(tutorialCleared: false, highestStageCleared: 0, LastStage));
            Assert.IsNull(StageProgress.ResumeStage(tutorialCleared: false, highestStageCleared: 2, LastStage),
                "튜토리얼 세이브가 없으면(예: 옛 세이브) 스테이지 진행이 있어도 튜토리얼부터다");
        }

        [Test]
        public void TutorialClearedWithNoStageProgress_ResumesAtStageOne()
        {
            Assert.AreEqual(1, StageProgress.ResumeStage(tutorialCleared: true, highestStageCleared: 0, LastStage));
        }

        [Test]
        public void TutorialClearedWithStageProgress_ResumesAtNextStage()
        {
            Assert.AreEqual(2, StageProgress.ResumeStage(tutorialCleared: true, highestStageCleared: 1, LastStage));
            Assert.AreEqual(3, StageProgress.ResumeStage(tutorialCleared: true, highestStageCleared: 2, LastStage));
        }

        [Test]
        public void AllStagesCleared_StaysOnTheLastStage_InsteadOfGoingPastIt()
        {
            Assert.AreEqual(LastStage, StageProgress.ResumeStage(tutorialCleared: true, highestStageCleared: LastStage, LastStage));
        }
    }
}
