using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>"연출 목록" 턴 진행 연출의 트리거: 키 턴(암전 + 나츠 독백).</summary>
    public class TurnTransitionRulesTests
    {
        [TestCase(1, 4, false)]
        [TestCase(2, 4, false)]
        [TestCase(3, 4, false)]
        [TestCase(4, 4, true)]
        public void KeyTurn_IsOnlyTheLastTurnOfTheQuarter(int turnInQuarter, int turnsPerQuarter, bool expected) =>
            Assert.AreEqual(expected, TurnTransitionRules.IsKeyTurn(turnInQuarter, turnsPerQuarter));

        [Test]
        public void KeyTurn_IsNeverTrueWithoutAQuarterLength() =>
            Assert.IsFalse(TurnTransitionRules.IsKeyTurn(0, 0));

        /// <summary>실제 스테이지를 끝까지 돌려서, 규칙이 코어의 실제 턴 흐름 위에서 기대대로 나오는지 본다:
        /// 키 턴 판정은 코어의 "쿼터 안 위치"와 항상 같다.</summary>
        [TestCase(20260916)]
        [TestCase(1)]
        [TestCase(777)]
        public void OverAFullStage_KeyTurnMatchesTheRealTurnFlow(int seed)
        {
            var polarity = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarity);
            var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), polarity);
            session.Runner.StartStage();

            var perQuarter = config.Quarters.TurnsPerQuarter;

            while (session.Runner.Outcome == StageOutcome.InProgress)
            {
                session.Runner.PlayClue(session.Hand.Cards[0]);
                if (session.Runner.Outcome != StageOutcome.InProgress) break;

                Assert.AreEqual(session.Runner.CurrentTurnInQuarter == perQuarter,
                    TurnTransitionRules.IsKeyTurn(session.Runner.CurrentTurnInQuarter, perQuarter), $"턴 {session.Runner.CurrentTurn}");
            }
        }
    }
}
