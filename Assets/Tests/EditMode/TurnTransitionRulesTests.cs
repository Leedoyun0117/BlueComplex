using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>"연출 목록" 턴 진행 연출의 트리거: 키 턴(암전 + 나츠 독백)과 심박수 변화(카메라 확대). 심박수 변화의 "구간이 바뀐 턴만" 해석은
    /// 기획에 명시된 것이 아니다(<see cref="TurnTransitionRules"/> 문서 참고).</summary>
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

        [Test]
        public void HeartbeatChange_SameZone_IsNotAChange() =>
            Assert.IsFalse(TurnTransitionRules.HeartbeatChanged(HeartbeatState.Stable, HeartbeatState.Stable));

        [TestCase(HeartbeatState.Stable, HeartbeatState.Depressed)]
        [TestCase(HeartbeatState.Depressed, HeartbeatState.VeryDepressed)]
        [TestCase(HeartbeatState.Excited, HeartbeatState.Stable)]
        [TestCase(HeartbeatState.Stable, HeartbeatState.VeryExcited)]
        public void HeartbeatChange_ZoneCrossing_IsAChange(HeartbeatState before, HeartbeatState after) =>
            Assert.IsTrue(TurnTransitionRules.HeartbeatChanged(before, after));

        [Test]
        public void Plan_CombinesKeyTurnAndHeartbeatChange()
        {
            var both = TurnTransitionRules.Plan(4, 4, HeartbeatState.Stable, HeartbeatState.Excited);
            Assert.IsTrue(both.KeyTurn);
            Assert.IsTrue(both.HeartbeatChanged);

            var plain = TurnTransitionRules.Plan(2, 4, HeartbeatState.Stable, HeartbeatState.Stable);
            Assert.IsFalse(plain.KeyTurn);
            Assert.IsFalse(plain.HeartbeatChanged);
        }

        /// <summary>실제 스테이지를 끝까지 돌려서, 규칙이 코어의 실제 턴·심박수 흐름 위에서 기대대로 나오는지 본다:
        /// 키 턴 계획은 코어의 "쿼터 안 위치"와 항상 같고, 심박수 변화는 구간이 실제로 바뀐 턴에서만 나온다.</summary>
        [TestCase(20260916)]
        [TestCase(1)]
        [TestCase(777)]
        public void OverAFullStage_PlansMatchTheRealTurnFlow(int seed)
        {
            var polarity = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarity);
            var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), polarity);
            session.Runner.StartStage();

            var perQuarter = config.Quarters.TurnsPerQuarter;

            while (session.Runner.Outcome == StageOutcome.InProgress)
            {
                var stateBefore = session.Zone.StateOf(session.Heartbeat.Value);
                session.Runner.PlayClue(session.Hand.Cards[0]);
                if (session.Runner.Outcome != StageOutcome.InProgress) break;

                var stateAfter = session.Zone.StateOf(session.Heartbeat.Value);
                var plan = TurnTransitionRules.Plan(session.Runner.CurrentTurnInQuarter, perQuarter, stateBefore, stateAfter);

                Assert.AreEqual(session.Runner.CurrentTurnInQuarter == perQuarter, plan.KeyTurn, $"턴 {session.Runner.CurrentTurn}");
                Assert.AreEqual(stateBefore != stateAfter, plan.HeartbeatChanged, $"턴 {session.Runner.CurrentTurn}");
            }
        }
    }
}
