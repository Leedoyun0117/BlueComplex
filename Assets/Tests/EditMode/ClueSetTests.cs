using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>set 시스템 — 같은 set 단서는 함께 손패에 들어오고, 3개 이상 set은 랜덤 2개만 제공되며,
    /// 손패 크기(4)에 안 들어가는 set는 쪼개지지 않고 건너뛴다. 전부 가짜 더미 단서로 검증한다.</summary>
    public class ClueSetTests
    {
        private static ClueDefinition Clue(string id, string setId = null) =>
            new(id, id, "", TimeTag.None, Array.Empty<PersonTag>(), Array.Empty<EmotionTag>(), setId);

        private static List<ClueDefinition> Solos(int count, string prefix = "s") =>
            Enumerable.Range(0, count).Select(i => Clue($"{prefix}{i}")).ToList();

        private static string Ids(IEnumerable<ClueInstance> cards) =>
            string.Join(",", cards.Select(c => c.Definition.Id).OrderBy(x => x));

        /// <summary>이번 Refill로 새로 들어온 카드 중 set 단서마다: 같은 set의 동료가 (이미 손에 있었거나) 이번에 함께 들어왔는가.</summary>
        private static void AssertNewSetCardsCameWithMate(IReadOnlyList<ClueInstance> before, IReadOnlyList<ClueInstance> after, int seed)
        {
            var beforeSet = new HashSet<ClueInstance>(before);
            foreach (var fresh in after.Where(c => !beforeSet.Contains(c) && c.Definition.SetId != null))
            {
                var mates = after.Where(c => c != fresh && c.Definition.SetId == fresh.Definition.SetId);
                Assert.IsTrue(mates.Any(), $"seed {seed}: {fresh.Definition.Id} 만 들어왔고 같은 set 동료가 손패에 없다.");
            }
        }

        [Test]
        public void TwoClueSet_WhenOneIsDrawn_TheOtherComesAlong_AcrossManySeeds()
        {
            var defs = new List<ClueDefinition>
            {
                Clue("a1", "A"), Clue("a2", "A"), Clue("b1", "B"), Clue("b2", "B"), Clue("c1", "C"), Clue("c2", "C"),
                Clue("s0"), Clue("s1"), Clue("s2"), Clue("s3"), Clue("s4"), Clue("s5")
            };

            var sawSetInHand = 0;
            for (var seed = 0; seed < 500; seed++)
            {
                var hand = new ClueHand(new CluePool(defs, new SystemRandomSource(seed)));
                hand.Refill();

                Assert.AreEqual(ClueHand.HandSize, hand.Cards.Count, $"seed {seed}");
                foreach (var setId in new[] { "A", "B", "C" })
                {
                    var inHand = hand.Cards.Count(c => c.Definition.SetId == setId);
                    Assert.IsTrue(inHand == 0 || inHand == 2, $"seed {seed}: set {setId} 가 {inHand}장만 들어왔다.");
                    if (inHand == 2) sawSetInHand++;
                }
            }

            Assert.Greater(sawSetInHand, 100, "set가 실제로 손패에 자주 들어와야 검증이 의미 있다.");
        }

        [Test]
        public void SetCardsInHand_AreAdjacent_AndBothFireCardAdded()
        {
            // 단서가 정확히 4개라 어떤 순서로 뽑혀도 set가 손패에 들어온다.
            var defs = new List<ClueDefinition> { Clue("a1", "A"), Clue("a2", "A"), Clue("s0"), Clue("s1") };
            var hand = new ClueHand(new CluePool(defs, new SystemRandomSource(3)));
            var added = new List<ClueInstance>();
            hand.CardAdded += added.Add;

            hand.Refill();

            Assert.AreEqual(hand.Cards.Count, added.Count);
            var indexes = hand.Cards.Select((c, i) => (c, i)).Where(t => t.c.Definition.SetId == "A").Select(t => t.i).ToList();
            Assert.AreEqual(2, indexes.Count);
            Assert.AreEqual(1, indexes[1] - indexes[0], "같은 set 카드는 손패에서 나란히 놓인다.");
        }

        [Test]
        public void ThreeClueSet_ProvidesExactlyTwoRandomMembers_TheRestStayInPool()
        {
            var pairsSeen = new Dictionary<string, int>();
            for (var seed = 0; seed < 600; seed++)
            {
                var pool = new CluePool(new[] { Clue("x1", "X"), Clue("x2", "X"), Clue("x3", "X") }, new SystemRandomSource(seed));
                var hand = new ClueHand(pool);
                hand.Refill();

                Assert.AreEqual(ClueDefinition.SetPickCount, hand.Cards.Count, $"seed {seed}: set 크기 3이어도 2개만 제공된다.");
                Assert.AreEqual(1, pool.Count, $"seed {seed}: 나머지 하나는 풀에 남는다.");

                var key = Ids(hand.Cards);
                pairsSeen[key] = pairsSeen.GetValueOrDefault(key) + 1;
            }

            Assert.AreEqual(3, pairsSeen.Count, "세 가지 쌍이 모두 나와야 한다: " + string.Join(" / ", pairsSeen));
            foreach (var pair in pairsSeen)
                Assert.That(pair.Value, Is.InRange(140, 260), $"쌍 {pair.Key} 가 고르게(600회 중 약 200) 나와야 한다.");
        }

        [Test]
        public void FourClueSet_AlsoProvidesTwo()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var pool = new CluePool(Enumerable.Range(0, 4).Select(i => Clue($"y{i}", "Y")), new SystemRandomSource(seed));
                var hand = new ClueHand(pool);
                hand.Refill();

                Assert.AreEqual(2, hand.Cards.Count, $"seed {seed}");
                Assert.AreEqual(2, pool.Count);
            }
        }

        [Test]
        public void SetThatDoesNotFitTheFreeSlots_IsSkippedNotSplit_AndSoloFillsTheGap()
        {
            var pool = new CluePool(Solos(3), new SystemRandomSource(1));
            var hand = new ClueHand(pool);
            hand.Refill();
            Assert.AreEqual(3, hand.Cards.Count);

            // 빈 칸이 1개인데 풀에는 set(2개)과 낱개 하나가 있다 — 낱개만 들어와야 한다.
            pool.Return(Clue("a1", "A"));
            pool.Return(Clue("a2", "A"));
            pool.Return(Clue("solo"));
            hand.Refill();

            Assert.AreEqual(4, hand.Cards.Count);
            Assert.IsTrue(hand.Cards.Any(c => c.Definition.Id == "solo"));
            Assert.IsFalse(hand.Cards.Any(c => c.Definition.SetId == "A"), "set는 쪼개져 들어오면 안 된다.");
            Assert.AreEqual(2, pool.Count, "set 두 장은 풀에 그대로 남는다.");
        }

        [Test]
        public void OnlySetLeftAndOneFreeSlot_SlotStaysEmpty_ThenSetArrivesOnceThereIsRoom()
        {
            var pool = new CluePool(Solos(3), new SystemRandomSource(1));
            var hand = new ClueHand(pool);
            hand.Refill();

            pool.Return(Clue("a1", "A"));
            pool.Return(Clue("a2", "A"));
            hand.Refill();
            Assert.AreEqual(3, hand.Cards.Count, "빈 칸 1개에 set(2)는 못 들어온다 — 칸이 빈 채로 둔다.");
            Assert.AreEqual(2, pool.Count);

            hand.Use(hand.Cards[0]);
            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count);
            Assert.AreEqual(2, hand.Cards.Count(c => c.Definition.SetId == "A"), "칸이 2개 되면 set가 함께 들어온다.");
        }

        [Test]
        public void MateAlreadyInHand_LetsTheOtherComeAloneIntoOneSlot()
        {
            var pool = new CluePool(new[] { Clue("a1", "A"), Clue("a2", "A") }, new SystemRandomSource(5));
            var hand = new ClueHand(pool);
            hand.RefillForNewQuarter();
            Assert.AreEqual(2, hand.Cards.Count);

            var used = hand.Cards.First(c => c.Definition.Id == "a2");
            hand.Use(used);
            hand.RefillForNewQuarter(); // 낸 a2가 풀로 돌아온다. a1이 손에 있으므로 a2는 혼자 들어와도 set는 온전하다.

            Assert.AreEqual("a1,a2", Ids(hand.Cards));
        }

        [Test]
        public void SetUsedInOneQuarter_ReturnsTogetherAtNextQuarter()
        {
            var defs = new List<ClueDefinition> { Clue("a1", "A"), Clue("a2", "A"), Clue("s0"), Clue("s1"), Clue("s2"), Clue("s3") };
            for (var seed = 0; seed < 200; seed++)
            {
                var hand = new ClueHand(new CluePool(defs, new SystemRandomSource(seed)));
                hand.RefillForNewQuarter();
                for (var quarter = 0; quarter < 4; quarter++)
                {
                    foreach (var card in hand.Cards.ToList()) hand.Use(card);
                    var before = hand.Cards.ToList();
                    hand.RefillForNewQuarter();
                    AssertNewSetCardsCameWithMate(before, hand.Cards, seed);
                    Assert.AreEqual(ClueHand.HandSize, hand.Cards.Count, $"seed {seed} 쿼터 {quarter}");
                }
            }
        }

        [Test]
        public void RedrawAll_KeepsSetsWhole_AndNeverExceedsHandSize()
        {
            var defs = new List<ClueDefinition>
            {
                Clue("a1", "A"), Clue("a2", "A"), Clue("b1", "B"), Clue("b2", "B"), Clue("x1", "X"), Clue("x2", "X"), Clue("x3", "X"),
                Clue("s0"), Clue("s1")
            };
            for (var seed = 0; seed < 300; seed++)
            {
                var hand = new ClueHand(new CluePool(defs, new SystemRandomSource(seed)));
                hand.Refill();
                for (var round = 0; round < 3; round++)
                {
                    hand.RedrawAll();
                    Assert.LessOrEqual(hand.Cards.Count, ClueHand.HandSize, $"seed {seed}");
                    AssertNewSetCardsCameWithMate(Array.Empty<ClueInstance>(), hand.Cards, seed);
                    foreach (var group in hand.Cards.Where(c => c.Definition.SetId != null).GroupBy(c => c.Definition.SetId))
                        Assert.AreEqual(ClueDefinition.SetPickCount, group.Count(), $"seed {seed}: set {group.Key} 는 정확히 2장만 나온다.");
                }
            }
        }

        [Test]
        public void ReplaceRandom_BringsSetMateWhenThereIsRoom_AndRefusesWhenTheSetWouldOverflow()
        {
            var pool = new CluePool(Solos(4), new SystemRandomSource(2));
            var hand = new ClueHand(pool);
            hand.Refill();
            pool.Return(Clue("a1", "A"));
            pool.Return(Clue("a2", "A"));

            // 손패 4장 가득 — 한 장을 빼도 빈 칸 1개라 set(2)는 못 들어온다.
            var target = hand.Cards[1];
            Assert.IsFalse(hand.CanReplace(target));
            Assert.IsFalse(hand.TryReplaceRandom(target, out _));
            Assert.AreEqual(4, hand.Cards.Count);
            CollectionAssert.Contains(hand.Cards, target, "거절되면 아무것도 바뀌지 않는다.");
            Assert.AreEqual(2, pool.Count);

            // 한 장 내서 빈 칸이 생기면 set가 함께 들어온다.
            hand.Use(hand.Cards[0]);
            var events = new List<string>();
            hand.CardAdded += c => events.Add("+" + c.Definition.Id);
            hand.CardDestroyed += c => events.Add("-" + c.Definition.Id);
            var replaceTarget = hand.Cards[0];
            Assert.IsTrue(hand.CanReplace(replaceTarget));
            Assert.IsTrue(hand.TryReplaceRandom(replaceTarget, out var replacement));

            Assert.AreEqual("A", replacement.Definition.SetId);
            Assert.AreEqual(4, hand.Cards.Count);
            Assert.AreEqual(2, hand.Cards.Count(c => c.Definition.SetId == "A"));
            CollectionAssert.DoesNotContain(hand.Cards, replaceTarget);
            Assert.AreEqual(3, events.Count, "파괴 1 + 추가 2.");
        }

        [Test]
        public void ReplaceRandom_FallsBackToSoloThatFits_WhenSetWouldOverflow()
        {
            var pool = new CluePool(Solos(4), new SystemRandomSource(2));
            var hand = new ClueHand(pool);
            hand.Refill();
            pool.Return(Clue("a1", "A"));
            pool.Return(Clue("a2", "A"));
            pool.Return(Clue("only"));

            Assert.IsTrue(hand.TryReplaceRandom(hand.Cards[0], out var replacement));
            Assert.AreEqual("only", replacement.Definition.Id, "set가 못 들어가면 들어가는 낱개가 뽑힌다.");
            Assert.AreEqual(4, hand.Cards.Count);
        }

        [Test]
        public void PoolWithoutSets_DrawsTheSameSequenceAsTheOriginalTryDraw()
        {
            // 스테이지 1처럼 set가 없으면 뽑기 결과와 난수 소비가 예전과 완전히 같아야 한다.
            var defs = PrototypeContent.Clues();
            for (var seed = 0; seed < 50; seed++)
            {
                var plainPool = new CluePool(defs, new SystemRandomSource(seed));
                var groupPool = new CluePool(defs, new SystemRandomSource(seed));
                var group = new List<ClueDefinition>();

                while (plainPool.TryDraw(out var expected))
                {
                    Assert.IsTrue(groupPool.TryDrawGroup(ClueHand.HandSize, Array.Empty<ClueInstance>(), group));
                    Assert.AreEqual(1, group.Count);
                    Assert.AreSame(expected, group[0], $"seed {seed}");
                }
                Assert.IsFalse(groupPool.TryDrawGroup(ClueHand.HandSize, Array.Empty<ClueInstance>(), group));
            }
        }

        [Test]
        public void StageConfig_RejectsASetWithASingleClue()
        {
            var clues = new List<ClueDefinition> { Clue("a1", "A"), Clue("s0") };
            Assert.Throws<ArgumentException>(() => new StageConfig("s", "s", quarterCount: 3, turnsPerQuarter: 4,
                requiredKeys: 1, complexWeight: 1.0, clues: clues,
                complexPool: Array.Empty<BlueComplex.Core.Complexes.ComplexDefinition>(), startingComplex: null,
                itemPool: Array.Empty<ItemDefinition>()));

            clues.Add(Clue("a2", "A"));
            Assert.DoesNotThrow(() => new StageConfig("s", "s", quarterCount: 3, turnsPerQuarter: 4,
                requiredKeys: 1, complexWeight: 1.0, clues: clues,
                complexPool: Array.Empty<BlueComplex.Core.Complexes.ComplexDefinition>(), startingComplex: null,
                itemPool: Array.Empty<ItemDefinition>()));
        }
    }
}
