using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Items;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Tests
{
    /// <summary>E. 단서 수명 — 한 번 내면 영구 소멸, 손패는 Refill을 부를 때만 채워짐, 회상.</summary>
    public class ClueLifecycleTests
    {
        private static ClueDefinition NeutralClue(string id) =>
            new(id, id, "", TimeTag.None, Array.Empty<PersonTag>(), Array.Empty<EmotionTag>());

        [Test]
        public void Hand_UsedCard_IsRemovedImmediately_AndNotRefilledUntilAsked()
        {
            var defs = Enumerable.Range(0, 8).Select(i => NeutralClue($"c{i}")).ToList();
            var pool = new CluePool(defs, new SystemRandomSource(1));
            var hand = new ClueHand(pool);

            var destroyed = new List<ClueInstance>();
            hand.CardDestroyed += c => destroyed.Add(c);

            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count);

            var target = hand.Cards[0];
            hand.Use(target);

            Assert.AreEqual(1, destroyed.Count);
            Assert.AreSame(target, destroyed[0]);
            CollectionAssert.DoesNotContain(hand.Cards, target, "낸 카드는 한 번 만에 손패에서 사라져야 한다.");
            Assert.AreEqual(3, hand.Cards.Count, "Use()는 손패를 자동으로 채우지 않는다 — 쿼터 중에는 손패가 줄어든 채로 진행된다.");
            Assert.AreEqual(4, pool.Count, "풀에 카드가 남아 있어도 Refill 전에는 뽑히지 않는다.");

            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count, "Refill 하면 풀에서 손패 크기까지 채워진다.");
        }

        [Test]
        public void Hand_UsingCardNotInHand_Throws()
        {
            var pool = new CluePool(Enumerable.Range(0, 5).Select(i => NeutralClue($"c{i}")), new SystemRandomSource(1));
            var hand = new ClueHand(pool);
            hand.Refill();

            var target = hand.Cards[0];
            hand.Use(target);

            Assert.Throws<InvalidOperationException>(() => hand.Use(target), "이미 낸 카드를 다시 낼 수 없다.");
        }

        [Test]
        public void UsedClue_NeverReappearsFromPool()
        {
            var defs = Enumerable.Range(0, 6).Select(i => NeutralClue($"c{i}")).ToList();
            var pool = new CluePool(defs, new SystemRandomSource(42));
            var hand = new ClueHand(pool);
            hand.Refill();

            var target = hand.Cards[0];
            var usedId = target.Definition.Id;
            hand.Use(target); // 영구 소멸, 풀로 되돌아가지 않음

            // 남은 풀을 모두 뽑아서 낸 단서가 다시 나오는지 확인한다.
            var drawnIds = new List<string>();
            while (pool.TryDraw(out var drawn)) drawnIds.Add(drawn.Id);

            CollectionAssert.DoesNotContain(drawnIds, usedId, "낸 단서는 풀에서 영구히 사라져야 한다.");
        }

        [Test]
        public void Hand_ShrinksGracefully_OncePoolIsDry()
        {
            // 풀 크기를 손패 크기와 동일하게 맞춰 풀이 바로 마르는 상황을 만든다.
            var defs = Enumerable.Range(0, ClueHand.HandSize).Select(i => NeutralClue($"c{i}")).ToList();
            var pool = new CluePool(defs, new SystemRandomSource(7));
            var hand = new ClueHand(pool);

            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count);
            Assert.AreEqual(0, pool.Count, "초기 4장을 뽑으면 풀이 빈다.");

            // 한 장씩 낸다. 풀이 비어 있으므로 Refill을 불러도 손패는 점점 줄어야 한다.
            for (var expected = 3; expected >= 0; expected--)
            {
                var card = hand.Cards[0];
                Assert.DoesNotThrow(() => hand.Use(card));
                hand.Refill();
                Assert.AreEqual(expected, hand.Cards.Count);
            }

            Assert.AreEqual(0, pool.Count);
        }

        [Test]
        public void RecollectionItem_RefillsHandBackToFour_WithoutBringingBackUsedClues()
        {
            var clueDefs = PrototypeContent.Clues();
            var cluePool = new CluePool(clueDefs, new SystemRandomSource(3));
            var hand = new ClueHand(cluePool);
            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count);

            // 카드 하나를 내서 영구 소멸시킨다 — 회상으로 다시 뽑아도 이 단서는 돌아오지 않아야 한다.
            var usedId = hand.Cards[0].Definition.Id;
            hand.Use(hand.Cards[0]);
            Assert.AreEqual(3, hand.Cards.Count);

            var traits = new TraitBoard();
            var activeItems = new ActiveItemBoard();
            var itemPool = new[]
            {
                new ItemDefinition("item_recollection", "회상", "", duration: 0, new RedrawHand())
            };
            var inventory = new ItemInventory(itemPool, new SystemRandomSource(9), capacity: 1);

            Assert.AreEqual(1, inventory.Refill(), "풀에 아이템이 하나뿐이므로 반드시 채워야 한다.");
            var gained = inventory.Held[0];
            Assert.AreEqual("item_recollection", gained.Id);

            inventory.Use(gained, new ItemActivationContext(hand, traits, activeItems));

            Assert.AreEqual(4, hand.Cards.Count, "회상 후 손패는 다시 4장이 되어야 한다.");
            CollectionAssert.DoesNotContain(hand.Cards.Select(c => c.Definition.Id).ToList(), usedId,
                "이미 낸 단서는 회상으로도 돌아오지 않는다.");
        }
    }
}
