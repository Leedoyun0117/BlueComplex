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
    /// <summary>E. 단서 수명 — 2회 사용 후 소멸, 영구 삭제, 손패 4장 유지, 회상.</summary>
    public class ClueLifecycleTests
    {
        private static ClueDefinition NeutralClue(string id) =>
            new(id, id, "", TimeTag.None, Array.Empty<PersonTag>(), Array.Empty<EmotionTag>());

        [Test]
        public void ClueInstance_ExhaustsAfterTwoUses_AndThrowsOnFurtherUse()
        {
            var def = NeutralClue("clue_a");
            var instance = new ClueInstance(def);

            Assert.IsFalse(instance.IsExhausted);
            instance.ConsumeUse();
            Assert.IsFalse(instance.IsExhausted, "1회 사용으로는 소멸하지 않아야 한다.");
            instance.ConsumeUse();
            Assert.IsTrue(instance.IsExhausted, "2회 사용 후 소멸해야 한다.");

            Assert.Throws<InvalidOperationException>(() => instance.ConsumeUse());
        }

        [Test]
        public void Hand_ExhaustedCard_IsRemovedFromHand_AndRefilledFromPool()
        {
            var defs = Enumerable.Range(0, 5).Select(i => NeutralClue($"c{i}")).ToList();
            var pool = new CluePool(defs, new SystemRandomSource(1));
            var hand = new ClueHand(pool);

            var destroyed = new List<ClueInstance>();
            hand.CardDestroyed += c => destroyed.Add(c);

            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count);

            var target = hand.Cards[0];
            hand.Use(target); // 1회
            Assert.AreEqual(4, hand.Cards.Count, "1회 사용으로는 손패에서 빠지지 않는다.");

            hand.Use(target); // 2회 - 소멸
            Assert.AreEqual(1, destroyed.Count);
            Assert.AreSame(target, destroyed[0]);
            CollectionAssert.DoesNotContain(hand.Cards, target, "소멸한 카드는 손패에서 사라져야 한다.");

            // Use() 자체는 손패를 자동으로 채우지 않으므로, 명시적으로 Refill 해야 4장이 된다.
            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count, "풀에 남은 단서가 있다면 4장으로 채워져야 한다.");
        }

        [Test]
        public void ExhaustedClue_NeverReappearsFromPool()
        {
            var defs = Enumerable.Range(0, 6).Select(i => NeutralClue($"c{i}")).ToList();
            var pool = new CluePool(defs, new SystemRandomSource(42));
            var hand = new ClueHand(pool);
            hand.Refill();

            var target = hand.Cards[0];
            var exhaustedId = target.Definition.Id;
            hand.Use(target);
            hand.Use(target); // 소멸, 풀로 되돌아가지 않음

            // 남은 풀을 모두 뽑아서 소멸한 단서가 다시 나오는지 확인한다.
            var drawnIds = new List<string>();
            while (pool.TryDraw(out var drawn)) drawnIds.Add(drawn.Id);

            CollectionAssert.DoesNotContain(drawnIds, exhaustedId, "소멸한 단서는 풀에서 영구히 사라져야 한다.");
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

            // 손에 있는 카드를 순서대로 소멸시킨다. 풀이 비어 있으므로 손패는 점점 줄어야 한다.
            var expectedCount = 4;
            while (hand.Cards.Count > 0)
            {
                var card = hand.Cards[0];
                Assert.DoesNotThrow(() => hand.Use(card));
                if (card.IsExhausted) expectedCount--;
                hand.Refill();
                Assert.AreEqual(expectedCount, hand.Cards.Count);
            }

            Assert.AreEqual(0, pool.Count);
        }

        [Test]
        public void RecollectionItem_RefillsHandBackToFour()
        {
            var clueDefs = PrototypeContent.Clues();
            var cluePool = new CluePool(clueDefs, new SystemRandomSource(3));
            var hand = new ClueHand(cluePool);
            hand.Refill();
            Assert.AreEqual(4, hand.Cards.Count);

            // 카드 하나를 1회 사용해 상태를 변화시켜 둔다 — 회상 후에는 새 카드로 리셋되어야 한다.
            hand.Use(hand.Cards[0]);

            var traits = new TraitBoard();
            var activeItems = new ActiveItemBoard();
            var itemPool = new[]
            {
                new ItemDefinition("item_recollection", "회상", "", duration: 0, new Recollection())
            };
            var inventory = new ItemInventory(itemPool, new SystemRandomSource(9));

            Assert.IsTrue(inventory.TryGainRandom(out var gained), "풀에 아이템이 하나뿐이므로 반드시 획득해야 한다.");
            Assert.AreEqual("item_recollection", gained.Id);

            inventory.Use(gained, new ItemActivationContext(hand, traits, activeItems));

            Assert.AreEqual(4, hand.Cards.Count, "회상 후 손패는 다시 4장이 되어야 한다.");
            Assert.IsTrue(hand.Cards.All(c => c.RemainingUses == ClueInstance.MaxUses),
                "회상으로 다시 뽑힌 카드는 사용 횟수가 초기화되어야 한다.");
        }
    }
}
