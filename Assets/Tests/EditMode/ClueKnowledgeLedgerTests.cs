using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 단서 속성 해금(ClueKnowledgeLedger) — 첫 번째로 실제 발동한 컴플렉스가 참조한 태그만 관찰로 인정하고,
    /// 관찰은 런이 끝나 CommitRun이 불릴 때에야 노트(GetKnowledge)에 반영된다.
    /// </summary>
    public class ClueKnowledgeLedgerTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static InterpretationResult Resolve(TagSet tags, params ComplexDefinition[] inPriorityOrder)
        {
            var board = new ComplexBoard();
            for (var i = 0; i < inPriorityOrder.Length; i++)
                board.TryAttach(new ComplexInstance(inPriorityOrder[i], priority: i));
            return new ComplexResolver(board).Resolve(tags);
        }

        // ── 첫 발동 컴플렉스의 참조만 기록 ──────────────────────────────────────

        [Test]
        public void OnlyTheFirstTriggeredComplexsReferencedTagsAreRecorded()
        {
            // 과거/친구/사랑: 소꿉친구(친구→연인)가 먼저 발동하고, 타자화(연인→타인)가 그다음에 발동한다.
            var result = Resolve(new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love }),
                PrototypeContent.ChildhoodFriend(), PrototypeContent.Othering());
            Assert.IsTrue(result.Steps[0].Triggered);
            Assert.IsTrue(result.Steps[1].Triggered, "이 시나리오는 두 컴플렉스가 모두 발동해야 의미가 있다");

            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("clue", result);
            ledger.CommitRun();

            var knowledge = ledger.GetKnowledge("clue");
            Assert.IsTrue(knowledge.IsPersonRevealed(PersonTag.Friend), "첫 컴플렉스가 본 것은 원본 태그(친구)다");
            Assert.IsTrue(knowledge.TimeRevealed);
            Assert.IsFalse(knowledge.IsPersonRevealed(PersonTag.Lover), "두 번째 컴플렉스(타자화)가 본 '연인'은 기록되지 않는다");
            Assert.IsFalse(knowledge.IsPersonRevealed(PersonTag.Other));
            CollectionAssert.IsEmpty(knowledge.RevealedEmotions);
        }

        [Test]
        public void AComplexThatDidNotTrigger_IsSkipped_AndItsPartialMatchesDoNotLeak()
        {
            // 반 과거(과거 ✓ · 인물 ✓ · 행복/사랑 ✗)는 시간과 인물을 훑고도 발동하지 못한다 — 그 훑은 흔적이 남으면 안 된다.
            // 그다음 소꿉친구(과거+친구)가 첫 발동이므로 시간과 친구만 기록된다.
            var result = Resolve(new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Fear }),
                PrototypeContent.AntiPast(Polarity), PrototypeContent.ChildhoodFriend());
            Assert.IsFalse(result.Steps[0].Triggered);
            Assert.IsTrue(result.Steps[1].Triggered);

            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("clue", result);
            ledger.CommitRun();

            var knowledge = ledger.GetKnowledge("clue");
            Assert.IsTrue(knowledge.TimeRevealed);
            Assert.IsTrue(knowledge.IsPersonRevealed(PersonTag.Friend));
            CollectionAssert.IsEmpty(knowledge.RevealedEmotions, "발동하지 못한 컴플렉스가 본 감정은 관찰이 아니다");
        }

        [Test]
        public void WhenNothingTriggers_NothingIsRecorded()
        {
            var result = Resolve(new TagSet(TimeTag.Present, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness }),
                PrototypeContent.ChildhoodFriend());
            Assert.IsFalse(result.Steps[0].Triggered);

            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("clue", result);
            ledger.CommitRun();

            var knowledge = ledger.GetKnowledge("clue");
            Assert.IsFalse(knowledge.TimeRevealed);
            CollectionAssert.IsEmpty(knowledge.RevealedPersons);
            CollectionAssert.IsEmpty(knowledge.RevealedEmotions);
        }

        [Test]
        public void WithNoComplexAttached_NothingIsRecorded()
        {
            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("clue", Resolve(new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love })));
            ledger.CommitRun();

            Assert.IsFalse(ledger.GetKnowledge("clue").TimeRevealed);
        }

        // ── 런 종료 시점에 커밋 ────────────────────────────────────────────────

        [Test]
        public void ObservationsStayHidden_UntilTheRunIsCommitted_AndThenStayForNextRuns()
        {
            var result = Resolve(new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love }),
                PrototypeContent.ChildhoodFriend());
            var ledger = new ClueKnowledgeLedger();

            ledger.RecordInterpretation("clue", result);
            Assert.IsFalse(ledger.GetKnowledge("clue").TimeRevealed, "같은 런 안에서는 노트에 반영되지 않는다");
            Assert.IsFalse(ledger.GetKnowledge("clue").IsPersonRevealed(PersonTag.Friend));

            ledger.CommitRun();
            Assert.IsTrue(ledger.GetKnowledge("clue").TimeRevealed);
            Assert.IsTrue(ledger.GetKnowledge("clue").IsPersonRevealed(PersonTag.Friend));

            ledger.CommitRun();
            Assert.IsTrue(ledger.GetKnowledge("clue").IsPersonRevealed(PersonTag.Friend), "한 번 해금된 정보는 사라지지 않는다");
        }

        [Test]
        public void ARecordAfterTheCommit_BelongsToTheNextRun()
        {
            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("a", Resolve(new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love }),
                PrototypeContent.ChildhoodFriend()));
            ledger.CommitRun();

            ledger.RecordInterpretation("b", Resolve(new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love }),
                PrototypeContent.ChildhoodFriend()));

            Assert.IsTrue(ledger.GetKnowledge("a").TimeRevealed);
            Assert.IsFalse(ledger.GetKnowledge("b").TimeRevealed, "두 번째 런의 관찰은 두 번째 CommitRun 전까지 보이지 않는다");
            ledger.CommitRun();
            Assert.IsTrue(ledger.GetKnowledge("b").TimeRevealed);
        }

        // ── 실제 스테이지 진행 ─────────────────────────────────────────────────

        [Test]
        public void PlayingAStage_NothingIsRevealedDuringTheRun_AndOnlyRealTagsAreRevealedAfterCommit()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            var revealedAnything = 0;

            for (var seed = 1000; seed < 1060; seed++)
            {
                var ledger = new ClueKnowledgeLedger();
                var session = StageFactory.Create(config, new SystemRandomSource(seed), ledger, Polarity);
                session.Runner.StartStage();

                var guard = 0;
                while (session.Runner.Outcome == StageOutcome.InProgress && guard++ < config.TotalTurns)
                {
                    if (session.Hand.Cards.Count == 0) break;
                    session.Runner.PlayClue(session.Hand.Cards[0]);
                    Assert.IsFalse(config.Clues.Any(c => IsAnythingRevealed(ledger, c)), $"seed={seed}: 런 도중에 노트가 채워졌다");
                }

                session.Ledger.CommitRun();

                foreach (var clue in config.Clues)
                {
                    var knowledge = ledger.GetKnowledge(clue.Id);
                    foreach (var person in knowledge.RevealedPersons)
                        CollectionAssert.Contains(clue.Persons, person, $"seed={seed} {clue.Id}: 원본에 없는 인물이 해금됐다");
                    foreach (var emotion in knowledge.RevealedEmotions)
                        CollectionAssert.Contains(clue.Emotions, emotion, $"seed={seed} {clue.Id}: 원본에 없는 감정이 해금됐다");
                    if (knowledge.TimeRevealed) Assert.Greater(clue.Times.Count, 0, $"seed={seed} {clue.Id}");
                    if (IsAnythingRevealed(ledger, clue)) revealedAnything++;
                }
            }

            Assert.Greater(revealedAnything, 0, "60번의 런에서 하나도 해금되지 않는다면 기록 경로가 끊긴 것이다");
        }

        private static bool IsAnythingRevealed(ClueKnowledgeLedger ledger, ClueDefinition clue)
        {
            var knowledge = ledger.GetKnowledge(clue.Id);
            return knowledge.TimeRevealed || knowledge.RevealedPersons.Count > 0 || knowledge.RevealedEmotions.Count > 0;
        }
    }
}
