using System.Collections.Generic;
using NUnit.Framework;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Tests
{
    /// <summary>극단 패널티 — 단서 검열 상태. 진입(극단)과 해제(정확히 중앙) 조건이 비대칭임을 검증한다.</summary>
    public class CensorshipStateTests
    {
        private static (StabilityIndicator Indicator, CensorshipState State) Build()
        {
            var indicator = new StabilityIndicator(10); // 0~9, 중앙 5
            var state = new CensorshipState(indicator, new[] { 0, 9 }, releasePosition: 5);
            return (indicator, state);
        }

        [Test]
        public void ReachingLowerExtreme_EntersCensorship()
        {
            var (indicator, state) = Build();
            Assert.IsFalse(state.IsCensored);

            indicator.Move(-5); // 5 -> 0

            Assert.IsTrue(state.IsCensored);
        }

        [Test]
        public void ReachingUpperExtreme_EntersCensorship()
        {
            var (indicator, state) = Build();

            indicator.Move(4); // 5 -> 9

            Assert.IsTrue(state.IsCensored);
        }

        [Test]
        public void MovingToMiddleValue_KeepsCensorshipUnchanged()
        {
            var (indicator, state) = Build();
            indicator.Move(-5); // -> 0, 검열 진입
            Assert.IsTrue(state.IsCensored);

            indicator.Move(3); // 0 -> 3, 극단도 중앙도 아님

            Assert.IsTrue(state.IsCensored, "1~4 구간에서는 직전 상태가 유지되어야 한다.");
        }

        [Test]
        public void ReturningExactlyToCenter_ReleasesCensorship()
        {
            var (indicator, state) = Build();
            indicator.Move(-5); // -> 0
            Assert.IsTrue(state.IsCensored);

            indicator.Move(5); // 0 -> 5

            Assert.IsFalse(state.IsCensored);
        }

        [Test]
        public void AfterRelease_ReturningToExtreme_ReEntersCensorship()
        {
            var (indicator, state) = Build();
            indicator.Move(-5); // -> 0, 진입
            indicator.Move(5);  // -> 5, 해제
            Assert.IsFalse(state.IsCensored);

            indicator.Move(-5); // -> 0, 재진입

            Assert.IsTrue(state.IsCensored);
        }

        [Test]
        public void CensorshipChanged_FiresOnlyOnActualStateTransitions()
        {
            var (indicator, state) = Build();
            var changes = new List<bool>();
            state.CensorshipChanged += changes.Add;

            indicator.Move(-5); // 5 -> 0   : 진입 (변화)
            indicator.Move(2);  // 0 -> 2   : 유지 (변화 없음)
            indicator.Move(-2); // 2 -> 0   : 이미 검열 중, 다시 0 (변화 없음)
            indicator.Move(5);  // 0 -> 5   : 해제 (변화)
            indicator.Move(-5); // 5 -> 0   : 재진입 (변화)

            CollectionAssert.AreEqual(new[] { true, false, true }, changes,
                "실제로 상태가 바뀔 때만, 그리고 그 순서대로 이벤트가 발생해야 한다.");
        }
    }
}
