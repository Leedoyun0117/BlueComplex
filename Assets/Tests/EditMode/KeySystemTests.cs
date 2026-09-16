using System.Collections.Generic;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Tests
{
    /// <summary>D. 키 — 등장 턴, 구역 범위, 획득/실패 판정, 클리어 조건.</summary>
    public class KeySystemTests
    {
        [Test]
        public void IsKeyTurn_OnlyTrueOn3579()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3, 5, 7, 9 });

            for (var turn = 1; turn <= 10; turn++)
            {
                var expected = turn == 3 || turn == 5 || turn == 7 || turn == 9;
                Assert.AreEqual(expected, keys.IsKeyTurn(turn), $"턴 {turn}");
            }
        }

        [Test]
        public void RandomKeyZonePlacer_AlwaysLandsInLeftOrRightEdge_AcrossManySeeds()
        {
            var layout = new KeyZoneLayout(slots: 10, edgeWidth: 3, keyWidth: 2);

            for (var seed = 0; seed < 200; seed++)
            {
                var random = new SystemRandomSource(seed);
                var placer = new RandomKeyZonePlacer(random, layout);

                for (var i = 0; i < 20; i++)
                {
                    var zone = placer.Place(5, System.Array.Empty<ClueInstance>());
                    var lastSlot = zone.StartSlot + zone.Width - 1;

                    var withinLeft = zone.StartSlot >= 0 && lastSlot <= 2;
                    var withinRight = zone.StartSlot >= 7 && lastSlot <= 9;

                    Assert.IsTrue(withinLeft || withinRight,
                        $"seed={seed} 반복={i}: 구역 [{zone.StartSlot}, {lastSlot}] 이 좌측 0~2 또는 우측 7~9 범위를 벗어났다.");
                }
            }
        }

        [Test]
        public void Judge_IndicatorInsideZone_CollectsKey()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3 });
            var collectedCounts = new List<int>();
            keys.KeyCollected += c => collectedCounts.Add(c);
            var missed = false;
            keys.ZoneMissed += () => missed = true;

            keys.OpenZone(new KeyZone(0, 2)); // 0,1 구역
            keys.Judge(indicatorPosition: 1);

            Assert.AreEqual(1, keys.Collected);
            CollectionAssert.AreEqual(new[] { 1 }, collectedCounts);
            Assert.IsFalse(missed);
            Assert.IsNull(keys.ActiveZone, "판정 후 구역은 닫혀야 한다.");
        }

        [Test]
        public void Judge_IndicatorOutsideZone_Misses()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3 });
            var missed = false;
            keys.ZoneMissed += () => missed = true;

            keys.OpenZone(new KeyZone(0, 2)); // 0,1 구역
            keys.Judge(indicatorPosition: 5);

            Assert.AreEqual(0, keys.Collected);
            Assert.IsTrue(missed);
        }

        [Test]
        public void Judge_WithNoActiveZone_IsNoOp()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3 });
            var missed = false;
            keys.ZoneMissed += () => missed = true;
            var collected = false;
            keys.KeyCollected += _ => collected = true;

            keys.Judge(indicatorPosition: 5); // 열린 구역이 없음

            Assert.IsFalse(missed);
            Assert.IsFalse(collected);
        }

        [Test]
        public void TwoKeysCollected_MarksComplete()
        {
            var keys = new KeyProgress(required: 2, keyTurns: new[] { 3, 5 });

            keys.OpenZone(new KeyZone(0, 2));
            keys.Judge(0);
            Assert.IsFalse(keys.IsComplete, "키 1개로는 아직 완료되지 않아야 한다.");

            keys.OpenZone(new KeyZone(7, 2));
            keys.Judge(7);
            Assert.IsTrue(keys.IsComplete, "키 2개를 모으면 완료되어야 한다.");
        }
    }
}
