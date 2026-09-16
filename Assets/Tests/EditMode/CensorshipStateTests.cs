using System.Collections.Generic;
using NUnit.Framework;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 검열 수준 — 심박수 구간에 완전히 종속된다(상태 기억 없음). 구간을 벗어나는 즉시 수준이 바뀐다.
    /// </summary>
    public class CensorshipStateTests
    {
        private static (Heartbeat Heartbeat, CensorshipState State) Build()
        {
            var heartbeat = new Heartbeat(); // 시작 80(안정)
            var state = new CensorshipState(heartbeat, new HeartbeatZone());
            return (heartbeat, state);
        }

        [Test]
        public void AtStart_StableZone_LevelIsNone()
        {
            var (_, state) = Build();

            Assert.AreEqual(CensorshipLevel.None, state.Level);
        }

        [Test]
        public void MovingIntoDepressed_SetsPartial()
        {
            var (heartbeat, state) = Build();

            heartbeat.Change(-25); // 80 -> 55, 침체(40~70)

            Assert.AreEqual(CensorshipLevel.Partial, state.Level);
        }

        [Test]
        public void MovingIntoVeryDepressed_SetsFull()
        {
            var (heartbeat, state) = Build();

            heartbeat.Change(-60); // 80 -> 20, 매우 침체(10~39)

            Assert.AreEqual(CensorshipLevel.Full, state.Level);
        }

        [Test]
        public void MovingIntoExcited_SetsPartial()
        {
            var (heartbeat, state) = Build();

            heartbeat.Change(25); // 80 -> 105, 흥분(101~150)

            Assert.AreEqual(CensorshipLevel.Partial, state.Level);
        }

        [Test]
        public void MovingIntoVeryExcited_SetsFull()
        {
            var (heartbeat, state) = Build();

            heartbeat.Change(75); // 80 -> 155, 매우 흥분(151~190)

            Assert.AreEqual(CensorshipLevel.Full, state.Level);
        }

        [Test]
        public void LeavingZone_ReleasesImmediately_WithoutReturningToCenter()
        {
            var (heartbeat, state) = Build();
            heartbeat.Change(-60); // 80 -> 20, 매우 침체 → Full
            Assert.AreEqual(CensorshipLevel.Full, state.Level);

            heartbeat.Change(30); // 20 -> 50, 침체(40~70) — 중앙으로 돌아오지 않아도 구간만 바뀌면 즉시 바뀐다.

            Assert.AreEqual(CensorshipLevel.Partial, state.Level, "중앙 복귀 없이도 구간을 벗어나면 즉시 수준이 바뀌어야 한다.");
        }

        [Test]
        public void ReturningToStable_ReleasesToNone()
        {
            var (heartbeat, state) = Build();
            heartbeat.Change(-60); // 80 -> 20, Full
            heartbeat.Change(51);  // 20 -> 71, 안정 진입

            Assert.AreEqual(CensorshipLevel.None, state.Level);
        }

        [Test]
        public void LevelChanged_FiresOnlyOnActualTransitions()
        {
            var (heartbeat, state) = Build();
            var changes = new List<CensorshipLevel>();
            state.LevelChanged += changes.Add;

            heartbeat.Change(-25); // 80 -> 55  Partial(변화)
            heartbeat.Change(-5);  // 55 -> 50  Partial 유지(변화 없음)
            heartbeat.Change(-15); // 50 -> 35  Full(변화)
            heartbeat.Change(15);  // 35 -> 50  Partial(변화)
            heartbeat.Change(21);  // 50 -> 71  None(변화)

            CollectionAssert.AreEqual(
                new[] { CensorshipLevel.Partial, CensorshipLevel.Full, CensorshipLevel.Partial, CensorshipLevel.None },
                changes,
                "실제로 수준이 바뀔 때만, 그리고 그 순서대로 이벤트가 발생해야 한다.");
        }
    }
}
