using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>스테이지 종료 컷신 순서(번호)와 시퀀서(있으면 재생, 없으면 건너뜀).</summary>
    public class StageCutscenePlanTests
    {
        private static string Flat(IReadOnlyList<int[]> steps) => string.Join(",", steps.Select(s => string.Join("+", s)));

        [TestCase(StageOutcome.Cleared)]
        [TestCase(StageOutcome.Failed)]
        public void Stage1_PlaysOneToFour_Regardless(StageOutcome outcome) =>
            Assert.AreEqual("1,2,3,4", Flat(StageCutscenePlan.For(1, outcome)));

        [TestCase(StageOutcome.Cleared)]
        [TestCase(StageOutcome.Failed)]
        public void Stage2_PlaysFive_SixPlusSeven_Eight_Nine_Regardless(StageOutcome outcome) =>
            Assert.AreEqual("5,6+7,8,9", Flat(StageCutscenePlan.For(2, outcome)));

        [Test]
        public void Stage3_OnlyClearedHasCutscenes_AsTenPlusEleven()
        {
            Assert.AreEqual("10+11", Flat(StageCutscenePlan.For(3, StageOutcome.Cleared)));
            Assert.IsEmpty(StageCutscenePlan.For(3, StageOutcome.Failed));
        }

        [Test]
        public void UnknownStage_HasNoCutscenes() => Assert.IsEmpty(StageCutscenePlan.For(4, StageOutcome.Cleared));

        [Test]
        public void InProgress_HasNoCutscenes() =>
            Assert.Throws<System.ArgumentException>(() => StageCutscenePlan.For(1, StageOutcome.InProgress));

        /// <summary>유니티 코루틴처럼 중첩된 IEnumerator를 끝까지 따라가며, 한 번 양보할 때마다 onYield를 부른다.</summary>
        private static void Drain(IEnumerator e, System.Action onYield = null)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(e);
            while (stack.Count > 0)
            {
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                if (stack.Peek().Current is IEnumerator inner) stack.Push(inner);
                else onYield?.Invoke();
            }
        }

        [Test]
        public void Sequencer_CallsEveryNumberInOrder_AndSkipsMissingOnes()
        {
            var called = new List<int>();
            var log = new List<string>();
            IEnumerator Provider(int n)
            {
                called.Add(n);
                return n == 2 || n == 7 ? null : One(n);
            }
            IEnumerator One(int n) { yield return null; }

            Drain(CutsceneSequencer.Play(StageCutscenePlan.For(2, StageOutcome.Cleared), Provider, log.Add));
            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8, 9 }, called);
            Assert.IsTrue(log.Any(l => l.Contains("#7 건너뜀")));
            Assert.IsTrue(log.Any(l => l.Contains("#6 재생")));
        }

        [Test]
        public void Sequencer_WithNoProvider_SkipsEverythingWithoutStalling()
        {
            var log = new List<string>();
            Drain(CutsceneSequencer.Play(StageCutscenePlan.For(1, StageOutcome.Failed), null, log.Add));
            Assert.AreEqual(4, log.Count(l => l.Contains("건너뜀")));
        }

        [Test]
        public void Sequencer_WaitsForEachCutsceneToFinishBeforeTheNext()
        {
            var order = new List<string>();
            IEnumerator Provider(int n)
            {
                order.Add($"start{n}");
                yield return null;
                order.Add($"end{n}");
            }

            var steps = StageCutscenePlan.For(3, StageOutcome.Cleared); // 10+11
            Drain(CutsceneSequencer.Play(steps, Provider), () => order.Add("tick"));
            CollectionAssert.AreEqual(new[] { "start10", "tick", "end10", "start11", "tick", "end11" }, order);
        }
    }
}
