using System;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>쿼터 손패에 그 쿼터 키 목표 구간 쪽 감정의 단서를 강제로 넣는 규칙(KeyZoneHandBiasRule / ClueHand 편향).</summary>
    public class KeyHandBiasTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();
        private static readonly HeartbeatZone Zone = new();

        private static ClueDefinition Clue(string id, params EmotionTag[] emotions) =>
            new(id, id, "", TimeTag.Past, Array.Empty<PersonTag>(), emotions);

        private static ClueDefinition Sad(int i) => Clue($"sad{i}", EmotionTag.Sadness);
        private static ClueDefinition Happy(int i) => Clue($"happy{i}", EmotionTag.Happiness);

        private static KeyZoneHandBiasRule Rule(int minClues = 3, int normal = 1, int very = 1) =>
            new(Zone, Polarity, new KeyHandBiasSettings(minClues, normal, very));

        // ── 구간 판정 ───────────────────────────────────────────────────────────

        [TestCase(10, HeartbeatState.VeryDepressed)]  // 구역 10~45, 한가운데 28
        [TestCase(34, HeartbeatState.Depressed)]      // 구역 34~69, 한가운데 52
        [TestCase(131, HeartbeatState.Excited)]       // 구역 131~166, 한가운데 149
        [TestCase(155, HeartbeatState.VeryExcited)]   // 구역 155~190, 한가운데 173
        public void Rule_ChoosesTheSideOfTheZonesCenterBand(int startSlot, HeartbeatState expectedBand)
        {
            var bias = Rule().For(new KeyZone(startSlot, 36));
            var depressedSide = expectedBand is HeartbeatState.VeryDepressed or HeartbeatState.Depressed;

            Assert.IsNotNull(bias);
            Assert.AreEqual(depressedSide, bias.Qualifies(Sad(0)));
            Assert.AreEqual(!depressedSide, bias.Qualifies(Happy(0)));
            Assert.AreEqual(3, bias.MinClues);
        }

        [Test]
        public void Rule_StableBand_HasNoBias()
        {
            Assert.IsNull(Rule().For(new KeyZone(62, 36)), "한가운데 80은 안정 구간이다");
        }

        [Test]
        public void Rule_VeryBands_CanDemandMoreTagsThanNormalBands()
        {
            var rule = Rule(normal: 1, very: 2);
            var oneTag = Sad(0);
            var twoTags = Clue("two", EmotionTag.Sadness, EmotionTag.Fear);

            var normal = rule.For(new KeyZone(34, 36));   // 침체
            var very = rule.For(new KeyZone(10, 36));     // 매우 침체

            Assert.IsTrue(normal.Qualifies(oneTag));
            Assert.IsFalse(very.Qualifies(oneTag));
            Assert.IsTrue(very.Qualifies(twoTags));
        }

        [Test]
        public void Rule_CountsOnlyTagsOnTheTargetSide()
        {
            var mixed = Clue("mixed", EmotionTag.Sadness, EmotionTag.Anger);
            var rule = Rule(normal: 2, very: 2);

            Assert.IsFalse(rule.For(new KeyZone(34, 36)).Qualifies(mixed), "침체 쪽 태그는 1개뿐");
            Assert.IsFalse(rule.For(new KeyZone(131, 36)).Qualifies(mixed), "흥분 쪽 태그도 1개뿐");
        }

        // ── 손패 채우기 ─────────────────────────────────────────────────────────

        private static ClueHand Hand(int seed, params ClueDefinition[] defs) =>
            new(new CluePool(defs, new SystemRandomSource(seed)));

        [Test]
        public void RefillForNewQuarter_ForcesTheMinimumNumberOfQualifyingClues()
        {
            var defs = Enumerable.Range(0, 8).Select(Sad).Concat(Enumerable.Range(0, 8).Select(Happy)).ToArray();
            var bias = Rule().For(new KeyZone(34, 36));

            for (var seed = 0; seed < 200; seed++)
            {
                var hand = Hand(seed, defs);
                hand.RefillForNewQuarter(bias);

                Assert.AreEqual(ClueHand.HandSize, hand.Cards.Count, $"seed={seed}");
                Assert.GreaterOrEqual(hand.Cards.Count(c => bias.Qualifies(c.Definition)), 3, $"seed={seed}");
            }
        }

        [Test]
        public void RefillForNewQuarter_WithFewerQualifyingThanRequired_TakesAllOfThem()
        {
            var defs = new[] { Sad(0), Sad(1), Happy(0), Happy(1), Happy(2), Happy(3) };
            var bias = Rule(minClues: 3).For(new KeyZone(34, 36));

            for (var seed = 0; seed < 50; seed++)
            {
                var hand = Hand(seed, defs);
                hand.RefillForNewQuarter(bias);

                Assert.AreEqual(ClueHand.HandSize, hand.Cards.Count);
                Assert.AreEqual(2, hand.Cards.Count(c => bias.Qualifies(c.Definition)), "있는 만큼(2장)은 다 들어온다");
            }
        }

        [Test]
        public void RedrawAll_ReusesSpentQualifyingClues_WhenThePoolHasNoneLeft()
        {
            var defs = new[] { Sad(0), Sad(1), Sad(2), Happy(0), Happy(1), Happy(2) };
            var bias = Rule(minClues: 3).For(new KeyZone(34, 36));
            var hand = Hand(7, defs);
            hand.RefillForNewQuarter(bias);

            foreach (var card in hand.Cards.Where(c => bias.Qualifies(c.Definition)).ToList()) hand.Use(card);

            hand.RedrawAll();

            Assert.AreEqual(3, hand.Cards.Count(c => bias.Qualifies(c.Definition)),
                "이번 쿼터에 소진된 침체 단서도 조건 충족이면 다시 쓴다");
        }

        [Test]
        public void WithoutBias_TheHandIsDrawnExactlyAsBefore()
        {
            var defs = Enumerable.Range(0, 8).Select(Sad).Concat(Enumerable.Range(0, 8).Select(Happy)).ToArray();

            for (var seed = 0; seed < 30; seed++)
            {
                var plain = Hand(seed, defs);
                plain.Refill();
                var viaQuarter = Hand(seed, defs);
                viaQuarter.RefillForNewQuarter();

                CollectionAssert.AreEqual(plain.Cards.Select(c => c.Definition.Id).ToList(),
                    viaQuarter.Cards.Select(c => c.Definition.Id).ToList(), $"seed={seed}: 편향이 없으면 난수도 그대로다");
            }
        }

        [Test]
        public void RefillForNewQuarter_ReusesClueSpentInEarlierQuarters_WhenNothingQualifyingIsLeftInThePool()
        {
            // 조건 충족 단서가 딱 3장뿐인 풀 — 1쿼터에 손패로 다 들어와 소진(무덤)된다. 2쿼터 손패에도 다시 3장이 들어와야 한다.
            var defs = new[] { Sad(0), Sad(1), Sad(2), Happy(0), Happy(1), Happy(2) };
            var bias = Rule(minClues: 3).For(new KeyZone(34, 36));

            for (var seed = 0; seed < 50; seed++)
            {
                var hand = Hand(seed, defs);
                hand.RefillForNewQuarter(bias);
                foreach (var card in hand.Cards.ToList()) hand.Use(card);

                hand.RefillForNewQuarter(bias);

                Assert.AreEqual(3, hand.Cards.Count(c => bias.Qualifies(c.Definition)), $"seed={seed}: 소진된 단서도 다시 풀에 올라 강제 배치된다");
            }
        }

        // ── 다섯 구간 ──────────────────────────────────────────────────────────

        // 구역 폭 36의 시작 칸(한가운데 칸이 속한 구간): 매우 침체 / 침체 / 안정 / 흥분 / 매우 흥분을 한 번씩 만든다.
        private static readonly (int StartSlot, HeartbeatState Band)[] AllBands =
        {
            (10, HeartbeatState.VeryDepressed), (34, HeartbeatState.Depressed), (62, HeartbeatState.Stable),
            (131, HeartbeatState.Excited), (155, HeartbeatState.VeryExcited),
        };

        [Test]
        public void EveryBandDemandsItsOwnMinimum_AcrossManySeeds()
        {
            // 기획 규칙 그대로: 매우 = 최소 2장, 일반 = 최소 1장, 안정 = 조건 없음(완전 랜덤). 풀에는 양쪽 감정이 넉넉히 있다.
            var rule = new KeyZoneHandBiasRule(Zone, Polarity, new KeyHandBiasSettings(minClues: 1, minTagsNormal: 1, minTagsVery: 1, minCluesVery: 2));
            var defs = Enumerable.Range(0, 6).Select(Sad).Concat(Enumerable.Range(0, 6).Select(Happy))
                .Concat(new[] { Clue("anger", EmotionTag.Anger), Clue("fear", EmotionTag.Fear) }).ToArray();

            foreach (var (startSlot, band) in AllBands)
            {
                var bias = rule.For(new KeyZone(startSlot, 36));
                if (band == HeartbeatState.Stable) { Assert.IsNull(bias, "안정 구간은 강제 조건이 없다"); continue; }

                var wanted = band is HeartbeatState.VeryDepressed or HeartbeatState.VeryExcited ? 2 : 1;
                Assert.AreEqual(wanted, bias.MinClues, band.ToString());

                for (var seed = 0; seed < 300; seed++)
                {
                    var hand = Hand(seed, defs);
                    hand.RefillForNewQuarter(bias);
                    Assert.GreaterOrEqual(hand.Cards.Count(c => bias.Qualifies(c.Definition)), wanted, $"{band} seed={seed}");
                }
            }
        }

        [Test]
        public void ExcitedSideIsHappinessLoveAnger_DepressedSideIsSadnessDisgustFear()
        {
            var depressed = Rule().For(new KeyZone(34, 36));
            var excited = Rule().For(new KeyZone(131, 36));

            foreach (var emotion in new[] { EmotionTag.Sadness, EmotionTag.Disgust, EmotionTag.Fear })
            {
                Assert.IsTrue(depressed.Qualifies(Clue("c", emotion)), emotion.ToString());
                Assert.IsFalse(excited.Qualifies(Clue("c", emotion)), emotion.ToString());
            }

            foreach (var emotion in new[] { EmotionTag.Happiness, EmotionTag.Love, EmotionTag.Anger })
            {
                Assert.IsTrue(excited.Qualifies(Clue("c", emotion)), emotion.ToString());
                Assert.IsFalse(depressed.Qualifies(Clue("c", emotion)), emotion.ToString());
            }
        }

        [Test]
        public void StableBand_LeavesTheRandomDrawUntouched()
        {
            var defs = Enumerable.Range(0, 8).Select(Sad).Concat(Enumerable.Range(0, 8).Select(Happy)).ToArray();
            var stable = Rule().For(new KeyZone(62, 36));

            for (var seed = 0; seed < 30; seed++)
            {
                var plain = Hand(seed, defs);
                plain.RefillForNewQuarter();
                var biased = Hand(seed, defs);
                biased.RefillForNewQuarter(stable);

                CollectionAssert.AreEqual(plain.Cards.Select(c => c.Definition.Id).ToList(),
                    biased.Cards.Select(c => c.Definition.Id).ToList(), $"seed={seed}: 안정 구간은 난수 소비도 그대로다");
            }
        }

        [Test]
        public void MinCluesVery_DefaultsToMinClues()
        {
            Assert.AreEqual(3, new KeyHandBiasSettings(3, 1, 1).MinCluesVery);
            Assert.AreEqual(1, new KeyHandBiasSettings(1, 1, 1, minCluesVery: 2).MinClues);
            Assert.AreEqual(2, new KeyHandBiasSettings(1, 1, 1, minCluesVery: 2).MinCluesVery);
        }

        // ── 스테이지 연결 ───────────────────────────────────────────────────────

        [Test]
        public void Stage1_TurnsTheBiasOn_Stage2LeavesItOff()
        {
            var stage1 = PrototypeContent.PrototypeStage(Polarity).KeyHandBias;

            Assert.IsNotNull(stage1);
            Assert.AreEqual(3, stage1.MinClues);
            Assert.IsNull(Stage2Content.Stage2(Polarity).KeyHandBias);
        }

        [Test]
        public void Stage1_EveryQuarterStartHand_HasTheClueOfTheTargetSide()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            var rule = new KeyZoneHandBiasRule(Zone, Polarity, config.KeyHandBias);

            for (var seed = 1000; seed < 1040; seed++)
            {
                var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), Polarity);
                session.Runner.StartStage();

                var checkedQuarters = 0;
                while (session.Runner.Outcome == StageOutcome.InProgress)
                {
                    if (session.Runner.CurrentTurnInQuarter == 1)
                    {
                        var quarter = session.Runner.CurrentQuarter;
                        var bias = rule.For(session.Keys.Zones[session.Runner.Schedule.LastTurnOf(quarter)]);
                        var qualifying = session.Hand.Cards.Count(c => bias.Qualifies(c.Definition));
                        var available = config.Clues.Count(bias.Qualifies);

                        Assert.GreaterOrEqual(qualifying, Math.Min(config.KeyHandBias.MinClues, available),
                            $"seed={seed} 쿼터 {quarter}: 목표 쪽 감정 단서가 손패에 부족하다");
                        checkedQuarters++;
                    }

                    session.Runner.PlayClue(session.Hand.Cards[0]);
                }

                Assert.GreaterOrEqual(checkedQuarters, 1);
            }
        }
    }
}
