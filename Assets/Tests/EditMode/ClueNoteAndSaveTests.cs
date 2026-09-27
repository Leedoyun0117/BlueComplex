using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Save;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using UnityEngine;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 시작 화면의 단서 노트(본 단서만 목록에, 해금 안 된 칸은 "?")와 그 지식을 남기는 세이브(save.json).
    /// </summary>
    public class ClueNoteAndSaveTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static InterpretationResult FriendLoveViaChildhoodFriend()
        {
            var board = new ComplexBoard();
            board.TryAttach(new ComplexInstance(PrototypeContent.ChildhoodFriend(), priority: 0));
            return new ComplexResolver(board).Resolve(new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love }));
        }

        private static ClueDefinition Clue(string id) =>
            new(id, id, "story", TimeTag.Past, new[] { PersonTag.Friend, PersonTag.Family }, new[] { EmotionTag.Love, EmotionTag.Fear });

        // ── 본 단서 기록 ──────────────────────────────────────────────────────

        [Test]
        public void CardsDealtIntoTheHand_AreSeenImmediately_WithoutWaitingForCommit()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            var ledger = new ClueKnowledgeLedger();
            var session = StageFactory.Create(config, new SystemRandomSource(7), ledger, Polarity);

            CollectionAssert.IsEmpty(ledger.SeenClueIds, "스테이지를 시작하기 전(손패가 비었을 때)엔 본 단서가 없다");

            session.Runner.StartStage();
            var hand = session.Hand.Cards.Select(c => c.Definition.Id).ToList();
            Assert.IsNotEmpty(hand);
            CollectionAssert.AreEquivalent(hand, ledger.SeenClueIds, "손패에 들어온 단서가 곧바로 '본 단서'다(CommitRun 없이)");

            ledger.DiscardPending();
            CollectionAssert.AreEquivalent(hand, ledger.SeenClueIds, "버려진 런이라도 본 사실은 지워지지 않는다");
        }

        [Test]
        public void ReplacedCards_AreAlsoSeen()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            var ledger = new ClueKnowledgeLedger();
            var session = StageFactory.Create(config, new SystemRandomSource(11), ledger, Polarity);
            session.Runner.StartStage();

            Assume.That(session.Hand.TryReplaceRandom(session.Hand.Cards[0], out var replacement), "풀에 바꿔 올 단서가 있어야 한다");
            Assert.IsTrue(ledger.IsSeen(replacement.Definition.Id), "선택적 기억 등으로 새로 들어온 단서도 본 것이다");
        }

        [Test]
        public void PlayingAClue_MarksItSeen_EvenWithoutTheHandHook()
        {
            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("played", FriendLoveViaChildhoodFriend());
            Assert.IsTrue(ledger.IsSeen("played"));
        }

        // ── 노트 목록 필터 ──────────────────────────────────────────────────────

        [Test]
        public void TheNoteLists_OnlySeenClues_InCatalogOrder()
        {
            var catalog = new[] { Clue("a"), Clue("b"), Clue("c"), Clue("d") };
            var ledger = new ClueKnowledgeLedger();
            ledger.MarkSeen("d");
            ledger.MarkSeen("b");
            ledger.GetKnowledge("c"); // 카드 표시가 빈 지식 칸을 만들어도 본 것은 아니다.

            CollectionAssert.AreEqual(new[] { "b", "d" }, ClueNote.SeenClues(catalog, ledger).Select(d => d.Id).ToArray());
        }

        [Test]
        public void ANeverSeenClue_IsNotListed_EvenIfSomethingAboutItWasRevealed()
        {
            // 정상 흐름에선 낸 단서가 곧 본 단서지만, 목록 기준은 오직 "본 적 있는가"다.
            var catalog = new[] { Clue("a") };
            var ledger = new ClueKnowledgeLedger();
            ledger.GetKnowledge("a").RevealTime();
            CollectionAssert.IsEmpty(ClueNote.SeenClues(catalog, ledger));
        }

        [Test]
        public void TheMainStoryCatalog_HasEveryStageClueOnce_AndNoTutorialClues()
        {
            var catalog = ClueCatalog.MainStory(Polarity);
            var expected = PrototypeContent.PrototypeStage(Polarity).Clues
                .Concat(Stage2Content.Stage2(Polarity).Clues)
                .Concat(Stage3Content.Stage3(Polarity).Clues)
                .Select(d => d.Id).Distinct().ToList();

            CollectionAssert.AreEqual(expected, catalog.Select(d => d.Id).ToList());
            Assert.IsFalse(catalog.Any(d => d.Id.StartsWith("tutorial_")), "튜토리얼 단서는 본편 노트에 없다");
        }

        // ── 잠금 표시 ────────────────────────────────────────────────────────

        [Test]
        public void Reveal_KeepsEverySlot_AndOnlyOpensCommittedTags()
        {
            var def = Clue("a");
            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("a", FriendLoveViaChildhoodFriend()); // 소꿉친구가 본 것: 과거·친구.

            var before = ClueNote.Reveal(def, ledger);
            Assert.IsFalse(before.TimeRevealed, "커밋 전엔 전부 가려져 있다");
            Assert.IsTrue(before.Persons.All(p => !p.Revealed));

            ledger.CommitRun();
            var after = ClueNote.Reveal(def, ledger);
            Assert.IsTrue(after.TimeRevealed);
            CollectionAssert.AreEqual(new[] { (PersonTag.Friend, true), (PersonTag.Family, false) }, after.Persons.ToArray(),
                "칸 수·순서는 정의 그대로, 해금된 칸만 열린다");
            CollectionAssert.AreEqual(new[] { (EmotionTag.Love, false), (EmotionTag.Fear, false) }, after.Emotions.ToArray());
        }

        // ── 저장 시점 ────────────────────────────────────────────────────────

        [Test]
        public void CommitRun_RaisesCommitted_OncePerCommit()
        {
            var ledger = new ClueKnowledgeLedger();
            var count = 0;
            ledger.Committed += () => count++;
            ledger.CommitRun();
            ledger.DiscardPending();
            ledger.CommitRun();
            Assert.AreEqual(2, count);
        }

        // ── 세이브 왕복 ──────────────────────────────────────────────────────

        private string _dir;

        [SetUp]
        public void CreateTempDir()
        {
            _dir = Path.Combine(Path.GetTempPath(), "bc-save-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void DeleteTempDir()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        private SaveFileStore Store() => new(Path.Combine(_dir, "save.json"),
            data => JsonUtility.ToJson(data, true), JsonUtility.FromJson<SaveData>);

        [Test]
        public void SaveThenLoad_RestoresSeenAndCommittedKnowledge_ButNotPending()
        {
            var ledger = new ClueKnowledgeLedger();
            ledger.MarkSeen("only_seen");
            ledger.RecordInterpretation("revealed", FriendLoveViaChildhoodFriend());
            ledger.CommitRun();
            ledger.RecordInterpretation("pending", FriendLoveViaChildhoodFriend());
            ledger.GetKnowledge("empty");

            var store = Store();
            store.Save(new SaveData { Clues = ledger.ToSaveEntries() });

            Assert.IsTrue(store.TryLoad(out var data, out var error), error);
            CollectionAssert.AreEqual(new[] { "only_seen", "pending", "revealed" }, data.Clues.Select(c => c.Id).ToArray(),
                "빈 칸(empty)은 저장하지 않는다. pending은 '본 단서'로만 남는다");

            var restored = new ClueKnowledgeLedger();
            restored.Restore(data.Clues);

            CollectionAssert.AreEquivalent(new[] { "only_seen", "revealed", "pending" }, restored.SeenClueIds);
            Assert.IsTrue(restored.GetKnowledge("revealed").TimeRevealed);
            Assert.IsTrue(restored.GetKnowledge("revealed").IsPersonRevealed(PersonTag.Friend));
            Assert.IsFalse(restored.GetKnowledge("pending").TimeRevealed, "확정 안 된 관찰은 세이브에 없다");
            Assert.AreEqual(0, restored.PendingCount);
        }

        [Test]
        public void TagsAreSavedByName()
        {
            var ledger = new ClueKnowledgeLedger();
            ledger.RecordInterpretation("revealed", FriendLoveViaChildhoodFriend());
            ledger.CommitRun();

            var json = JsonUtility.ToJson(new SaveData { Clues = ledger.ToSaveEntries() });
            StringAssert.Contains("\"Friend\"", json, "enum 순서가 바뀌어도 옛 세이브가 깨지지 않게 이름으로 남긴다");
        }

        [Test]
        public void Restore_SkipsUnknownTagNames_AndAddsToExistingKnowledge()
        {
            var ledger = new ClueKnowledgeLedger();
            ledger.MarkSeen("kept");
            ledger.Restore(new List<ClueSaveEntry>
            {
                new() { Id = "a", Seen = true, Persons = new List<string> { "Friend", "NoSuchPerson" }, Emotions = new List<string> { "Love" } },
                null,
                new() { Id = "" }
            });

            CollectionAssert.AreEquivalent(new[] { "kept", "a" }, ledger.SeenClueIds);
            Assert.IsTrue(ledger.GetKnowledge("a").IsPersonRevealed(PersonTag.Friend));
            Assert.AreEqual(1, ledger.GetKnowledge("a").RevealedPersons.Count);
            Assert.IsTrue(ledger.GetKnowledge("a").IsEmotionRevealed(EmotionTag.Love));
        }

        [Test]
        public void NoFile_LoadsNothing_WithoutError()
        {
            Assert.IsFalse(Store().TryLoad(out var data, out var error));
            Assert.IsNull(data);
            Assert.IsNull(error);
        }

        [Test]
        public void ACorruptFile_IsMovedAside_AndLoadsNothing()
        {
            var store = Store();
            File.WriteAllText(store.Path, "{ this is not json");

            Assert.IsFalse(store.TryLoad(out var data, out var error));
            Assert.IsNull(data);
            Assert.IsNotNull(error);
            Assert.IsFalse(File.Exists(store.Path));
            Assert.IsTrue(File.Exists(store.Path + ".corrupt"), "깨진 원본은 지우지 않고 옆에 치워 둔다");
        }

        [Test]
        public void SavingTwice_Overwrites_AndLeavesNoTempFile()
        {
            var store = Store();
            store.Save(new SaveData { Clues = new List<ClueSaveEntry> { new() { Id = "first", Seen = true } } });
            store.Save(new SaveData { Clues = new List<ClueSaveEntry> { new() { Id = "second", Seen = true } } });

            Assert.IsTrue(store.TryLoad(out var data, out _));
            CollectionAssert.AreEqual(new[] { "second" }, data.Clues.Select(c => c.Id).ToArray());
            Assert.IsFalse(File.Exists(store.Path + ".tmp"));
        }
    }
}
