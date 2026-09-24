using System;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;
using UnityEngine;

namespace BlueComplex.Core.Tests
{
    /// <summary>해금 상태를 아는 만큼만 보는 봇과, 런을 이어 붙여 해금을 쌓는 실행기(KnowledgeBots) 검증.</summary>
    public class KnowledgeBotsTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static ClueDefinition Clue() => new("c", "c", "", TimeTag.Past,
            new[] { PersonTag.Other, PersonTag.Friend }, new[] { EmotionTag.Happiness, EmotionTag.Anger });

        [Test]
        public void FullyRevealedLedger_KnowsEveryTag_AndEmptyLedgerKnowsNone()
        {
            var clues = PrototypeContent.Clues();

            Assert.AreEqual(1.0, KnowledgeBots.RevealedFraction(KnowledgeBots.FullyRevealedLedger(clues), clues), 1e-9);
            Assert.AreEqual(0.0, KnowledgeBots.RevealedFraction(new ClueKnowledgeLedger(), clues), 1e-9);
        }

        [Test]
        public void BelievedTags_TrustsOnlyRevealedSlots_WhenNotGuessing()
        {
            var clue = Clue();
            var knowledge = new ClueKnowledge();
            knowledge.RevealPerson(PersonTag.Friend);
            knowledge.RevealEmotion(EmotionTag.Anger);

            var believed = KnowledgeBots.BelievedTags(clue, knowledge, 0.0, new System.Random(1));

            Assert.AreEqual(TimeTag.None, believed.Time, "시간이 안 밝혀졌으면 빠진다");
            CollectionAssert.AreEqual(new[] { PersonTag.Friend }, believed.Persons.ToArray());
            Assert.AreEqual(1, believed.CountOf(EmotionTag.Anger));
            Assert.AreEqual(0, believed.CountOf(EmotionTag.Happiness));
        }

        [Test]
        public void BelievedTags_WithPerfectGuess_EqualsTheTrueTags()
        {
            var clue = Clue();

            var believed = KnowledgeBots.BelievedTags(clue, new ClueKnowledge(), 1.0, new System.Random(1));

            Assert.AreEqual(TimeTag.Past, believed.Time);
            CollectionAssert.AreEquivalent(clue.Persons, believed.Persons.ToArray());
            Assert.AreEqual(1, believed.CountOf(EmotionTag.Happiness));
            Assert.AreEqual(1, believed.CountOf(EmotionTag.Anger));
        }

        [Test]
        public void TagsUnknownBot_OnAFullyRevealedLedger_PlaysLikeTheFullInformationHeuristic()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);

            for (var seed = 1000; seed < 1040; seed++)
            {
                var ledger = KnowledgeBots.FullyRevealedLedger(config.Clues);
                var believing = KnowledgeBots.RunOnce(config, seed, ledger, new KnowledgeBots.BeliefChooser(ledger, 0.0, seed).Choose);
                var knowing = KnowledgeBots.RunOnce(config, seed, KnowledgeBots.FullyRevealedLedger(config.Clues), QuarterBots.ChooseHeuristicCard);

                Assert.AreEqual(knowing.Outcome, believing.Outcome, $"seed={seed}");
                Assert.AreEqual(knowing.KeysCollected, believing.KeysCollected, $"seed={seed}");
            }
        }

        [Test]
        public void RunOnce_CommitsObservations_SoRevealedFractionOnlyGrowsAcrossRuns()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            var ledger = new ClueKnowledgeLedger();
            var previous = KnowledgeBots.RevealedFraction(ledger, config.Clues);

            for (var run = 1; run <= 8; run++)
            {
                var seed = 500 + run;
                var result = KnowledgeBots.RunOnce(config, seed, ledger, new KnowledgeBots.BeliefChooser(ledger, 0.0, seed).Choose);
                Assert.AreNotEqual(StageOutcome.InProgress, result.Outcome, $"run={run}");

                var now = KnowledgeBots.RevealedFraction(ledger, config.Clues);
                Assert.GreaterOrEqual(now, previous, $"run={run}: 해금은 줄지 않는다");
                previous = now;
            }

            Assert.Greater(previous, 0.0, "여러 런 동안 컴플렉스가 한 번도 발동하지 않았을 리 없다");
        }

        // ── 측정(참고용) — 작은 표본. 1000시드 전체는 별도 하니스로 돌린다. ─────────────────────

        [Test]
        public void Curve_SmallSample_IsMeasuredAndEveryRunFinishes()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            const int players = 60;
            const int runs = 6;
            var cleared = new int[runs + 1];

            for (var player = 1; player <= players; player++)
            {
                var ledger = new ClueKnowledgeLedger();
                for (var run = 1; run <= runs; run++)
                {
                    var seed = player * 1000 + run;
                    var result = KnowledgeBots.RunOnce(config, seed, ledger, new KnowledgeBots.BeliefChooser(ledger, 0.0, seed).Choose);
                    if (result.Cleared) cleared[run]++;
                }
            }

            Debug.Log($"[해금 곡선 표본] 스테이지 1, 태그 모름 봇, {players}명 × {runs}런 — 런별 클리어 수: " +
                      string.Join(", ", Enumerable.Range(1, runs).Select(r => $"{r}런째 {cleared[r]}")));
        }
    }
}
