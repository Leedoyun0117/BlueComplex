using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 튜토리얼(기획서 '게임 시작 연출 / 튜토리얼')의 콘텐츠와 이음새를 검증한다: 단서·컴플렉스가 표와 같은지, 마지막 중첩 턴(반 과거 → 자아 비판)에 정답 카드가 실제로 있는지,
    /// 스크립트대로 4턴을 끝까지 통과할 수 있는지(키 범위 90~100 / 70~80에 맞는 심박수 경로), 오답 거절이 상태를 건드리지 않는지, 코어 이음새(ReplaceWith·고정 키 구역·정해진 발현·게이트)가 약속대로 움직이는지.
    /// 기댓값은 TutorialContent를 거치지 않고 노션 표와 기획 지시를 직접 옮겨 적었다.
    /// </summary>
    public class TutorialContentTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static StageSession NewSession(ClueKnowledgeLedger ledger = null) =>
            TutorialContent.CreateSession(Polarity, new SystemRandomSource(1), ledger ?? new ClueKnowledgeLedger());

        private static ClueInstance Held(StageSession session, string id) =>
            session.Hand.Cards.First(card => card.Definition.Id == id);

        private static string[] HandIds(StageSession session) => session.Hand.Cards.Select(c => c.Definition.Id).ToArray();

        private static TagSet Resolve(ClueDefinition clue, params ComplexDefinition[] complexes)
        {
            var board = new ComplexBoard();
            var priority = 100;
            foreach (var definition in complexes) board.TryAttach(new ComplexInstance(definition, priority++));
            return new ComplexResolver(board).Resolve(clue.CreateOriginalTagSet()).Final;
        }

        private static int Direction(TagSet tags) => tags.EnumerateEmotionsFlat().Sum(e => (int)Polarity.GetPolarity(e));

        private static string Ids(IEnumerable<ClueDefinition> clues) => string.Join(",", clues.Select(c => c.Id));

        // ── 데이터 ───────────────────────────────────────────────────────────────

        [Test]
        public void ClueTable_MatchesNotion()
        {
            var expected = new (string Id, string Name, TimeTag Time, PersonTag Person, EmotionTag Emotion)[]
            {
                ("tutorial_stale_donut", "상한 도넛", TimeTag.Past, PersonTag.Other, EmotionTag.Disgust),
                ("tutorial_horror_poster", "공포 영화 포스터", TimeTag.Future, PersonTag.Friend, EmotionTag.Fear),
                ("tutorial_calendar", "달력", TimeTag.Future, PersonTag.Family, EmotionTag.Happiness),
                ("tutorial_empty_fishbowl", "빈 어항", TimeTag.Past, PersonTag.Other, EmotionTag.Sadness),
                ("tutorial_paper_pile", "산더미처럼 쌓인 서류", TimeTag.Past, PersonTag.Other, EmotionTag.Anger),
                ("tutorial_phone_ring", "전화벨 소리", TimeTag.Present, PersonTag.Family, EmotionTag.Love),
                ("tutorial_fishing_rod", "낙싯대", TimeTag.Future, PersonTag.Friend, EmotionTag.Happiness),
                ("tutorial_daughter_photo", "어린 딸의 사진", TimeTag.Past, PersonTag.Family, EmotionTag.Happiness),
            };

            var actual = TutorialContent.AllClues();
            Assert.AreEqual(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Id, actual[i].Id);
                Assert.AreEqual(expected[i].Name, actual[i].DisplayName);
                CollectionAssert.AreEqual(new[] { expected[i].Time }, actual[i].Times, expected[i].Id);
                CollectionAssert.AreEqual(new[] { expected[i].Person }, actual[i].Persons, expected[i].Id);
                CollectionAssert.AreEqual(new[] { expected[i].Emotion }, actual[i].Emotions, expected[i].Id);
                Assert.IsTrue(actual[i].Id.StartsWith("tutorial_"), "튜토리얼 단서 id는 본편과 섞이지 않는다");
                Assert.IsTrue(actual[i].Story.StartsWith(actual[i].DisplayName + "\n\n"), expected[i].Id);
            }
        }

        [Test]
        public void ClueIds_DoNotCollideWithOtherStages()
        {
            // 스테이지 3의 단서는 "s3_" 접두어라 "tutorial_"과 겹칠 수 없다 — 접두어 검사(ClueTable_MatchesNotion)와 스테이지 1·2 대조로 충분하다.
            var others = PrototypeContent.PrototypeStage(Polarity).Clues
                .Concat(Stage2Content.Stage2(Polarity).Clues)
                .Select(c => c.Id).ToHashSet();

            Assert.IsFalse(TutorialContent.AllClues().Any(c => others.Contains(c.Id)));
        }

        [Test]
        public void Table1_IsThreeDepressedAndOneExcited()
        {
            var directions = TutorialContent.Quarter1Hand().Select(c => Direction(c.CreateOriginalTagSet())).ToArray();
            CollectionAssert.AreEqual(new[] { -1, -1, 1, -1 }, directions, "표 1: 침체, 침체, 흥분, 침체 (흥분 하나 = 달력)");
        }

        [Test]
        public void Table2_EmotionsAreAngerLoveHappinessHappiness()
        {
            var emotions = TutorialContent.Quarter2Hand().Select(c => c.Emotions.Single()).ToArray();
            CollectionAssert.AreEqual(new[] { EmotionTag.Anger, EmotionTag.Love, EmotionTag.Happiness, EmotionTag.Happiness }, emotions);
        }

        [Test]
        public void SelfCriticism_AddsOneSadnessWhenAnyDepressedEmotion()
        {
            var definition = TutorialContent.SelfCriticism(Polarity);
            Assert.AreEqual("tutorial_self_criticism", definition.Id);
            Assert.AreEqual("자아 비판 컴플렉스", definition.DisplayName);
            Assert.AreNotEqual("stage3_self_denial", definition.Id, "스테이지 3의 자아 부정과 별개");

            // 침체 감정이 하나라도 있으면 슬픔 +1 — 겹친 개수만큼이 아니라 한 번만.
            var depressed = new TagSet(TimeTag.Present, new[] { PersonTag.Other }, new[] { EmotionTag.Disgust, EmotionTag.Disgust, EmotionTag.Fear });
            Assert.IsTrue(definition.TryInterpret(new ComplexContext(depressed)));
            Assert.AreEqual(1, depressed.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(2, depressed.CountOf(EmotionTag.Disgust), "기존 감정은 그대로");
            Assert.AreEqual(1, depressed.CountOf(EmotionTag.Fear));

            // 슬픔이 이미 있어도 하나 더.
            var sad = new TagSet(TimeTag.Present, new[] { PersonTag.Other }, new[] { EmotionTag.Sadness });
            Assert.IsTrue(definition.TryInterpret(new ComplexContext(sad)));
            Assert.AreEqual(2, sad.CountOf(EmotionTag.Sadness));

            // 침체와 흥분이 섞여 있어도 침체가 있으면 발동, 흥분은 손대지 않는다.
            var mixed = new TagSet(TimeTag.Present, new[] { PersonTag.Other }, new[] { EmotionTag.Fear, EmotionTag.Anger });
            Assert.IsTrue(definition.TryInterpret(new ComplexContext(mixed)));
            Assert.AreEqual(1, mixed.CountOf(EmotionTag.Anger));
            Assert.AreEqual(1, mixed.CountOf(EmotionTag.Sadness));
        }

        [Test]
        public void SelfCriticism_DoesNotReactToExcitedEmotionsOrNoEmotions()
        {
            var definition = TutorialContent.SelfCriticism(Polarity);

            foreach (var excited in new[] { EmotionTag.Happiness, EmotionTag.Love, EmotionTag.Anger })
            {
                var tags = new TagSet(TimeTag.Present, new[] { PersonTag.Other }, new[] { excited });
                Assert.IsFalse(definition.TryInterpret(new ComplexContext(tags)), excited.ToString());
                Assert.AreEqual(0, tags.CountOf(EmotionTag.Sadness));
            }

            Assert.IsFalse(definition.TryInterpret(new ComplexContext(new TagSet(TimeTag.Present, new[] { PersonTag.Other }))));
        }

        // ── 설계 검증: 카드 × 컴플렉스 ────────────────────────────────────────────

        [Test]
        public void FinalTurn_EachOfFourCards_AntiPastThenSelfCriticism()
        {
            var antiPast = TutorialContent.AntiPast(Polarity);
            var selfCriticism = TutorialContent.SelfCriticism(Polarity);

            // 카드별 최종 감정(반 과거 → 자아 비판 순서).
            var single = new (ClueDefinition Clue, EmotionTag Emotion)[]
            {
                (TutorialContent.PaperPile, EmotionTag.Anger),      // 반 과거 조건 아님(분노), 자아 비판 조건 아님(흥분) → 분노 그대로
                (TutorialContent.PhoneRing, EmotionTag.Love),       // 현재라 반 과거 아님, 흥분이라 자아 비판 아님
                (TutorialContent.FishingRod, EmotionTag.Happiness), // 미래라 반 과거 아님, 흥분이라 자아 비판 아님
            };
            foreach (var (clue, emotion) in single)
            {
                var final = Resolve(clue, antiPast, selfCriticism);
                Assert.AreEqual(1, final.Emotions.Count, clue.Id);
                Assert.AreEqual(1, final.CountOf(emotion), clue.Id);
                Assert.AreEqual(1, Direction(final), $"{clue.Id}: 흥분 → 오답");
            }

            // 어린 딸의 사진: 반 과거로 행복 → 혐오, 자아 비판이 침체(혐오)를 보고 슬픔 +1 → 혐오 + 슬픔. 두 컴플렉스가 모두 발동하고 결과는 침체다.
            var daughter = Resolve(TutorialContent.DaughterPhoto, antiPast, selfCriticism);
            Assert.AreEqual(2, daughter.Emotions.Count);
            Assert.AreEqual(1, daughter.CountOf(EmotionTag.Disgust));
            Assert.AreEqual(1, daughter.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(-2, Direction(daughter));

            // 목표(침체)를 만족하는 카드는 어린 딸의 사진 하나.
            var depressed = TutorialContent.Quarter2Hand()
                .Where(c => Direction(Resolve(c, antiPast, selfCriticism)) < 0).ToArray();
            Assert.AreEqual("tutorial_daughter_photo", Ids(depressed));
        }

        [Test]
        public void FinalTurn_DaughterPhoto_FiresBothComplexesInOrder()
        {
            var board = new ComplexBoard();
            board.TryAttach(new ComplexInstance(TutorialContent.AntiPast(Polarity), 100));
            board.TryAttach(new ComplexInstance(TutorialContent.SelfCriticism(Polarity), 101));

            var result = new ComplexResolver(board).Resolve(TutorialContent.DaughterPhoto.CreateOriginalTagSet());

            CollectionAssert.AreEqual(new[] { "complex_anti_past", "tutorial_self_criticism" },
                result.Steps.Where(s => s.Triggered).Select(s => s.Complex.Definition.Id).ToArray(), "중첩이 화면에 보이려면 둘 다 발동해야 한다");
        }

        [Test]
        public void FinalTurn_OnlyDaughterPhotoFiresBothComplexes_OthersFireNone()
        {
            var board = new ComplexBoard();
            board.TryAttach(new ComplexInstance(TutorialContent.AntiPast(Polarity), 100));
            board.TryAttach(new ComplexInstance(TutorialContent.SelfCriticism(Polarity), 101));
            var resolver = new ComplexResolver(board);

            foreach (var clue in TutorialContent.Quarter2Hand().Where(c => c.Id != "tutorial_daughter_photo"))
                Assert.AreEqual(0, resolver.Resolve(clue.CreateOriginalTagSet()).Steps.Count(s => s.Triggered), clue.Id);
        }

        [Test]
        public void ThirdTurn_OnlyDaughterPhotoIsDepressedUnderAntiPast()
        {
            // 턴 3에는 반 과거만 붙어 있다(자아 비판은 턴 3이 끝날 때 붙는다) — 자아 비판 교체와 무관하게 정답은 그대로.
            var antiPast = TutorialContent.AntiPast(Polarity);
            var depressed = TutorialContent.Quarter2Hand()
                .Where(c => Direction(Resolve(c, antiPast)) < 0).ToArray();
            Assert.AreEqual("tutorial_daughter_photo", Ids(depressed), "턴 3(반 과거만)의 정답은 어린 딸의 사진 하나");
        }

        // ── 심박수 경로 ──────────────────────────────────────────────────────────

        [Test]
        public void HeartbeatPath_MatchesKeyRanges_ByArithmetic()
        {
            // 태그 수: 턴 1 +1(달력 행복), 턴 3 −1(딸: 혐오), 턴 4 −2(딸: 혐오+슬픔) → 자아비대로 ×2 = −4. 턴 2는 넘김.
            var m = TutorialContent.TagMagnitude;
            var afterTurn2 = TutorialContent.StartHeartbeat + m;
            var afterTurn3 = afterTurn2 - m;
            var afterTurn4WithItem = afterTurn3 - 4 * m;
            var afterTurn4WithoutItem = afterTurn3 - 2 * m;

            Assert.That(afterTurn2, Is.InRange(90, 100), "1쿼터 키 90~100");
            Assert.That(afterTurn4WithItem, Is.InRange(70, 80), "2쿼터 키 70~80 (자아비대 사용)");
            Assert.That(afterTurn4WithoutItem, Is.Not.InRange(70, 80), "아이템 없이는 키를 못 딴다 — 자아비대가 필요하다");
            Assert.AreEqual(5, m);
            Assert.AreEqual(93, TutorialContent.StartHeartbeat);
        }

        [Test]
        public void TagMagnitudeCandidates_OnlySmallValuesFitTheNarrowRanges()
        {
            // 자아비대가 필수이면서(없으면 실패) 두 범위에 모두 들어오는 시작 심박수가 있는 영향력만 쓸 수 있다.
            bool Feasible(int m) => Enumerable.Range(0, 201).Any(s =>
            {
                var h2 = s + m;
                var h3 = h2 - m;
                var withItem = h3 - 4 * m;
                var without = h3 - 2 * m;
                return h2 is >= 90 and <= 100 && withItem is >= 70 and <= 80 && !(without is >= 70 and <= 80);
            });

            Assert.IsTrue(Feasible(TutorialContent.TagMagnitude));
            Assert.IsFalse(Feasible(10), "본편 기본값 10은 자아비대를 켠 턴 4가 범위를 지나친다");
            Assert.IsFalse(Feasible(30), "지난번 값 30은 좁은 범위에 안 맞는다");
        }

        // ── 스크립트 진행 ────────────────────────────────────────────────────────

        [Test]
        public void SessionShape_TwoQuartersOfTwoTurnsAndTwoKeys()
        {
            var session = NewSession();
            Assert.AreEqual(TutorialContent.StageId, session.Config.Id);
            Assert.AreEqual(2, session.Config.Quarters.QuarterCount);
            Assert.AreEqual(2, session.Config.Quarters.TurnsPerQuarter);
            Assert.AreEqual(4, session.Config.TotalTurns);
            Assert.AreEqual(2, session.Config.RequiredKeys);
            Assert.AreEqual(TutorialContent.StartHeartbeat, session.Heartbeat.Value);
        }

        [Test]
        public void StartStage_KeyZones_AreTheExactRanges()
        {
            var session = NewSession();
            session.Runner.StartStage();

            Assert.AreEqual(new[] { 2, 4 }, session.Keys.Zones.Keys.OrderBy(t => t).ToArray());
            var first = session.Keys.Zones[2];
            var second = session.Keys.Zones[4];

            Assert.AreEqual(90, first.StartSlot);
            Assert.AreEqual(11, first.Width);
            Assert.IsTrue(first.Contains(90) && first.Contains(100), "양 끝 포함");
            Assert.IsFalse(first.Contains(89) || first.Contains(101));

            Assert.AreEqual(70, second.StartSlot);
            Assert.AreEqual(11, second.Width);
            Assert.IsTrue(second.Contains(70) && second.Contains(80), "양 끝 포함");
            Assert.IsFalse(second.Contains(69) || second.Contains(81));
        }

        [Test]
        public void FullRun_FollowsTheScript()
        {
            var session = NewSession();
            var reports = new List<TurnReport>();
            session.Runner.TurnResolved += reports.Add;
            session.Runner.StartStage();

            // 턴 1: 표 1의 4장, 흥분 카드(달력)만 통과.
            CollectionAssert.AreEquivalent(TutorialContent.Quarter1Hand().Select(c => c.Id), HandIds(session));
            foreach (var card in session.Hand.Cards.Where(c => c.Definition.Id != "tutorial_calendar"))
            {
                var verdict = session.CheckPlay(card);
                Assert.IsFalse(verdict.Allowed, card.Definition.Id);
                Assert.AreEqual(PlayRejection.WrongDirection, verdict.Reason);
                CollectionAssert.AreEqual(card.Definition.Emotions, verdict.ObservedEmotions, "거절 메시지가 말할 감정 = 그 카드의 감정");
            }
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_calendar")).Allowed);

            session.Runner.PlayClue(Held(session, "tutorial_calendar"));

            // 턴 2는 손패가 비어 시간만 흐르고, 그 턴 끝에 첫 키(90~100)가 판정된다. 2쿼터 시작에 반 과거가 붙고 손패는 표 2로 바뀐다.
            Assert.AreEqual(2, reports.Count);
            Assert.IsFalse(reports[0].IsPass);
            Assert.AreEqual(5, reports[0].HeartbeatDelta);
            Assert.IsTrue(reports[1].IsPass, "턴 2는 넘어간 턴(필러)");
            Assert.IsTrue(reports[1].KeyResult.Value.Success);
            Assert.AreEqual(98, reports[1].KeyResult.Value.Position);
            Assert.AreEqual(1, session.Keys.Collected);
            Assert.AreEqual("complex_anti_past", reports[1].SpawnedComplex.Definition.Id);
            Assert.AreEqual(98, session.Heartbeat.Value);
            Assert.AreEqual(3, session.Runner.CurrentTurn);
            CollectionAssert.AreEquivalent(TutorialContent.Quarter2Hand().Select(c => c.Id), HandIds(session));
            Assert.IsEmpty(session.Items.Held, "아이템은 턴 4 전에는 손에 없다");

            // 턴 3: 반 과거 하나 — 어린 딸의 사진만 통과.
            foreach (var card in session.Hand.Cards.Where(c => c.Definition.Id != "tutorial_daughter_photo"))
                Assert.IsFalse(session.CheckPlay(card).Allowed, card.Definition.Id);
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_daughter_photo")).Allowed);

            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));

            Assert.AreEqual(-5, reports[2].HeartbeatDelta);
            Assert.AreEqual(93, session.Heartbeat.Value);
            Assert.AreEqual(4, session.Runner.CurrentTurn);
            CollectionAssert.AreEqual(new[] { "complex_anti_past", "tutorial_self_criticism" },
                session.Complexes.InPriorityOrder().Select(c => c.Definition.Id).ToArray(), "반 과거가 먼저, 자아 비판이 나중");
            CollectionAssert.AreEquivalent(TutorialContent.Quarter2Hand().Select(c => c.Id), HandIds(session), "턴 4 손패도 표 2의 4장");

            // 턴 4: 아이템(자아비대)이 이 턴에 처음 손에 들어온다. 서류/전화벨/낙싯대는 아이템과 무관하게 오답, 딸의 사진은 아이템을 켜야 통과.
            Assert.AreEqual(new[] { TutorialContent.EgoInflationItemId }, session.Items.Held.Select(i => i.Id).ToArray());
            var expectedEmotion = new Dictionary<string, EmotionTag>
            {
                ["tutorial_paper_pile"] = EmotionTag.Anger,
                ["tutorial_phone_ring"] = EmotionTag.Love,
                ["tutorial_fishing_rod"] = EmotionTag.Happiness,
            };
            foreach (var pair in expectedEmotion)
            {
                var verdict = session.CheckPlay(Held(session, pair.Key));
                Assert.AreEqual(PlayRejection.WrongDirection, verdict.Reason, pair.Key);
                CollectionAssert.AreEqual(new[] { pair.Value }, verdict.ObservedEmotions, pair.Key);
            }
            Assert.AreEqual(PlayRejection.ItemNeeded, session.CheckPlay(Held(session, "tutorial_daughter_photo")).Reason);

            session.Runner.UseItem(session.Items.Held[0]);
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_daughter_photo")).Allowed);
            foreach (var id in expectedEmotion.Keys)
                Assert.AreEqual(PlayRejection.WrongDirection, session.CheckPlay(Held(session, id)).Reason, id + " (아이템을 켜도 오답)");

            var last = session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));

            Assert.AreEqual(-20, last.HeartbeatDelta, "혐오 + 슬픔, 자아비대로 ×2 → 4태그 × 5");
            Assert.AreEqual(73, session.Heartbeat.Value);
            Assert.IsTrue(last.KeyResult.Value.Success);
            Assert.AreEqual(2, session.Keys.Collected);
            Assert.AreEqual(StageOutcome.Cleared, last.Outcome);
            CollectionAssert.AreEqual(new[] { "complex_anti_past", "tutorial_self_criticism" },
                last.Interpretation.Steps.Where(s => s.Triggered).Select(s => s.Complex.Definition.Id).ToArray(), "턴 4의 실제 판정에서 중첩이 둘 다 발동");
            Assert.AreEqual(2, last.FinalTags.CountOf(EmotionTag.Disgust));
            Assert.AreEqual(2, last.FinalTags.CountOf(EmotionTag.Sadness));
        }

        [Test]
        public void ComplexDurations_BothAliveAtTurn4()
        {
            var session = NewSession();
            session.Runner.StartStage();
            session.Runner.PlayClue(Held(session, "tutorial_calendar"));
            Assert.AreEqual(2, session.Complexes.Slots.Single().RemainingTurns, "발현은 지속 감소 뒤라 붙은 턴에는 소모되지 않는다");

            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));
            var remaining = session.Complexes.Slots.ToDictionary(c => c.Definition.Id, c => c.RemainingTurns);
            Assert.AreEqual(1, remaining["complex_anti_past"], "턴 4 판정 시점에도 반 과거가 살아 있다(턴 4가 끝날 때 만료)");
            Assert.AreEqual(2, remaining["tutorial_self_criticism"]);
        }

        [Test]
        public void ItemIsRequiredForTheSecondKey()
        {
            // 게이트를 우회해 아이템 없이 딸의 사진을 내면 심박수가 83에 머물러 70~80을 놓친다 — 자아비대가 정말 필요하다.
            var session = NewSession();
            session.Runner.StartStage();
            session.Runner.PlayClue(Held(session, "tutorial_calendar"));
            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));

            var report = session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));

            Assert.AreEqual(-10, report.HeartbeatDelta);
            Assert.AreEqual(83, session.Heartbeat.Value);
            Assert.IsFalse(report.KeyResult.Value.Success);
            Assert.AreEqual(StageOutcome.Failed, report.Outcome);
        }

        [Test]
        public void TheOnlyAllowedPath_ClearsTheStage_AndEveryTurnHasExactlyOneAnswer()
        {
            var session = NewSession();
            session.Runner.StartStage();

            var turn1 = session.Hand.Cards.Where(c => session.CheckPlay(c).Allowed).ToList();
            Assert.AreEqual(1, turn1.Count);
            session.Runner.PlayClue(turn1[0]);
            Assert.AreEqual(1, session.Keys.Collected);

            var turn3 = session.Hand.Cards.Where(c => session.CheckPlay(c).Allowed).ToList();
            Assert.AreEqual(1, turn3.Count);
            session.Runner.PlayClue(turn3[0]);

            session.Runner.UseItem(session.Items.Held[0]);
            var turn4 = session.Hand.Cards.Where(c => session.CheckPlay(c).Allowed).ToList();
            Assert.AreEqual(1, turn4.Count, "턴 4의 정답도 하나");
            Assert.AreEqual("tutorial_daughter_photo", turn4[0].Definition.Id);

            var report = session.Runner.PlayClue(turn4[0]);
            Assert.AreEqual(StageOutcome.Cleared, report.Outcome);
            Assert.AreEqual(2, session.Keys.Collected);
        }

        [Test]
        public void RejectedPlay_ChangesNothing()
        {
            var session = NewSession();
            var ledger = session.Ledger;
            var reports = new List<TurnReport>();
            session.Runner.TurnResolved += reports.Add;
            session.Runner.StartStage();
            var handBefore = HandIds(session);

            for (var i = 0; i < 3; i++)
                foreach (var card in session.Hand.Cards.Where(c => c.Definition.Id != "tutorial_calendar").ToList())
                    Assert.IsFalse(session.CheckPlay(card).Allowed);

            CollectionAssert.AreEqual(handBefore, HandIds(session));
            Assert.AreEqual(TutorialContent.StartHeartbeat, session.Heartbeat.Value);
            Assert.AreEqual(1, session.Runner.CurrentTurn);
            Assert.IsEmpty(reports);
            Assert.IsEmpty(session.Complexes.Slots);
            Assert.AreEqual(0, ledger.PendingCount);

            // 컴플렉스가 발동하는 미리보기(턴 4에서 아이템 없이 딸의 사진 → ItemNeeded)도 관찰 기록·심박수·손패·지속 턴을 건드리지 않는다.
            session.Runner.PlayClue(Held(session, "tutorial_calendar"));
            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));
            var pendingBefore = ledger.PendingCount;
            var heartbeatBefore = session.Heartbeat.Value;
            var handBeforeTurn4 = HandIds(session);
            var remaining = session.Complexes.Slots.Select(c => c.RemainingTurns).ToArray();

            for (var i = 0; i < 3; i++)
                Assert.AreEqual(PlayRejection.ItemNeeded, session.CheckPlay(Held(session, "tutorial_daughter_photo")).Reason);

            Assert.AreEqual(pendingBefore, ledger.PendingCount, "거절은 관찰 기록을 남기지 않는다");
            Assert.AreEqual(heartbeatBefore, session.Heartbeat.Value);
            CollectionAssert.AreEqual(handBeforeTurn4, HandIds(session));
            CollectionAssert.AreEqual(remaining, session.Complexes.Slots.Select(c => c.RemainingTurns).ToArray());
            Assert.AreEqual(4, session.Runner.CurrentTurn);
        }

        [Test]
        public void Ledger_IsIsolatedFromTheMainLedger()
        {
            var main = new ClueKnowledgeLedger();
            var tutorial = new ClueKnowledgeLedger();
            var session = NewSession(tutorial);
            session.Runner.StartStage();
            session.Runner.PlayClue(Held(session, "tutorial_calendar"));
            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo")); // 반 과거가 발동해 이 단서의 속성이 관찰된다
            tutorial.CommitRun();

            Assert.IsTrue(tutorial.GetKnowledge("tutorial_daughter_photo").IsEmotionRevealed(EmotionTag.Happiness));
            Assert.IsTrue(tutorial.GetKnowledge("tutorial_daughter_photo").TimeRevealed);
            Assert.IsFalse(main.GetKnowledge("tutorial_daughter_photo").IsEmotionRevealed(EmotionTag.Happiness), "본편 장부는 튜토리얼 관찰을 모른다");
            Assert.IsFalse(main.GetKnowledge("tutorial_daughter_photo").TimeRevealed);
            Assert.AreSame(tutorial, session.Ledger);
        }

        [Test]
        public void ComplexesAreScriptedNotRandom_AcrossSeeds()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var session = TutorialContent.CreateSession(Polarity, new SystemRandomSource(seed), new ClueKnowledgeLedger());
                session.Runner.StartStage();
                Assert.AreEqual(93, session.Heartbeat.Value, $"seed {seed}");
                session.Runner.PlayClue(Held(session, "tutorial_calendar"));
                Assert.AreEqual(new[] { "complex_anti_past" }, session.Complexes.Slots.Select(c => c.Definition.Id).ToArray(), $"seed {seed}");
                session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));
                Assert.AreEqual(new[] { "complex_anti_past", "tutorial_self_criticism" }, session.Complexes.Slots.Select(c => c.Definition.Id).ToArray(), $"seed {seed}");
            }
        }

        // ── 코어 이음새 ──────────────────────────────────────────────────────────

        [Test]
        public void ReplaceWith_SwapsHandWithoutPoolOrGraveyard()
        {
            var pool = new CluePool(new[] { TutorialContent.Calendar }, new SystemRandomSource(1));
            var hand = new ClueHand(pool);
            var added = new List<ClueInstance>();
            var destroyed = new List<ClueInstance>();
            hand.CardAdded += added.Add;
            hand.CardDestroyed += destroyed.Add;

            hand.ReplaceWith(TutorialContent.Quarter1Hand());
            var first = hand.Cards.ToList();
            Assert.AreEqual(4, hand.Cards.Count);
            Assert.AreEqual(4, added.Count);

            hand.ReplaceWith(TutorialContent.Quarter2Hand());
            CollectionAssert.AreEqual(first, destroyed, "이전 카드는 버려진다");
            CollectionAssert.AreEqual(TutorialContent.Quarter2Hand().Select(c => c.Id), hand.Cards.Select(c => c.Definition.Id));
            Assert.AreEqual(1, pool.Count, "풀은 건드리지 않는다");

            hand.ReplaceWith(new ClueDefinition[0]);
            Assert.IsEmpty(hand.Cards);
        }

        [Test]
        public void ReplaceWith_SameDefinitionAgain_MakesFreshInstances()
        {
            var hand = new ClueHand(new CluePool(new ClueDefinition[0], new SystemRandomSource(1)));
            hand.ReplaceWith(new[] { TutorialContent.DaughterPhoto });
            var before = hand.Cards[0];
            hand.ReplaceWith(new[] { TutorialContent.DaughterPhoto });

            Assert.AreNotSame(before, hand.Cards[0]);
            Assert.AreSame(TutorialContent.DaughterPhoto, hand.Cards[0].Definition);
        }

        [Test]
        public void ReplaceWith_MoreThanHandSize_Throws()
        {
            var hand = new ClueHand(new CluePool(new ClueDefinition[0], new SystemRandomSource(1)));
            var five = TutorialContent.AllClues().Take(5).ToList();
            Assert.Throws<ArgumentException>(() => hand.ReplaceWith(five));
        }

        [Test]
        public void ScriptedKeyPlacer_FromRanges_MapsExactRangesToKeyTurnsInOrder()
        {
            var placer = ScriptedKeyPlacer.FromRanges((90, 100), (70, 80), (10, 20));

            var zones = placer.PlaceAll(80, new[] { 9, 3, 6 });

            Assert.AreEqual(90, zones[3].StartSlot);
            Assert.AreEqual(100, zones[3].StartSlot + zones[3].Width - 1, "양 끝 포함");
            Assert.AreEqual(70, zones[6].StartSlot);
            Assert.AreEqual(80, zones[6].StartSlot + zones[6].Width - 1);
            Assert.AreEqual(10, zones[9].StartSlot);
            Assert.AreEqual(zones[3].StartSlot, placer.PlaceAll(0, new[] { 3, 6, 9 })[3].StartSlot, "난수도 시작 위치도 쓰지 않는다");
        }

        [Test]
        public void ScriptedKeyPlacer_SingleValueRange_IsOneSlotWide()
        {
            var zone = ScriptedKeyPlacer.FromRanges((75, 75)).Place(0, 1);
            Assert.AreEqual(1, zone.Width);
            Assert.IsTrue(zone.Contains(75));
            Assert.IsFalse(zone.Contains(76));
        }

        [Test]
        public void ScriptedKeyPlacer_InvalidInput_Throws()
        {
            Assert.Throws<ArgumentException>(() => ScriptedKeyPlacer.FromRanges((80, 70)));
            Assert.Throws<ArgumentException>(() => ScriptedKeyPlacer.FromRanges());
        }

        [Test]
        public void ScriptedKeyPlacer_CountMismatch_Throws()
        {
            var placer = ScriptedKeyPlacer.FromRanges((90, 100), (70, 80));
            Assert.Throws<InvalidOperationException>(() => placer.PlaceAll(80, new[] { 3, 6, 9 }));
        }

        [Test]
        public void ScriptedComplexSchedule_SpawnsOnlyOnScheduledTurns()
        {
            var antiPast = TutorialContent.AntiPast(Polarity);
            var schedule = new ScriptedComplexSchedule(new Dictionary<int, ComplexDefinition> { [2] = antiPast });
            var turn = 1;
            schedule.BindTurn(() => turn);
            var board = new ComplexBoard();

            Assert.IsFalse(schedule.ShouldSpawn(150), "확률·심박수와 무관");
            turn = 2;
            Assert.IsTrue(schedule.ShouldSpawn(80));
            Assert.IsTrue(schedule.TrySpawn(board, out var spawned));
            Assert.AreSame(antiPast, spawned.Definition);
            Assert.AreEqual(1, board.Slots.Count);

            turn = 3;
            Assert.IsFalse(schedule.ShouldSpawn(150));
        }

        [Test]
        public void ScriptedComplexSchedule_WithoutBoundTurn_Throws()
        {
            var schedule = new ScriptedComplexSchedule(new Dictionary<int, ComplexDefinition>());
            Assert.Throws<InvalidOperationException>(() => schedule.ShouldSpawn(80));
        }

        [Test]
        public void Stage1Session_HasNoGate_AndAllowsEverything()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);
            var session = StageFactory.Create(config, new SystemRandomSource(7), new ClueKnowledgeLedger(), Polarity);
            session.Runner.StartStage();

            Assert.IsNull(session.PlayGate);
            foreach (var card in session.Hand.Cards)
                Assert.IsTrue(session.CheckPlay(card).Allowed);
        }

        [Test]
        public void DefaultFactoryParameters_KeepRandomBehaviourUnchanged()
        {
            // 새 주입 인자(spawner·spawnPolicy·tagMagnitude)를 안 주면 예전과 같은 무작위 조립이다 — 같은 시드는 같은 결과, 태그 영향력은 본편 기본 10.
            var config = PrototypeContent.PrototypeStage(Polarity);
            var a = StageFactory.Create(config, new SystemRandomSource(11), new ClueKnowledgeLedger(), Polarity);
            var b = StageFactory.Create(config, new SystemRandomSource(11), new ClueKnowledgeLedger(), Polarity);
            a.Runner.StartStage();
            b.Runner.StartStage();

            CollectionAssert.AreEqual(HandIds(a), HandIds(b));
            a.Runner.PlayClue(a.Hand.Cards[0]);
            b.Runner.PlayClue(b.Hand.Cards[0]);
            Assert.AreEqual(a.Heartbeat.Value, b.Heartbeat.Value);
            Assert.That(Math.Abs(a.Heartbeat.Value - Heartbeat.DefaultStartValue) % EmotionEvaluator.DefaultTagMagnitude, Is.EqualTo(0), "기본 영향력 10 단위로 움직인다");
            Assert.AreNotEqual(TutorialContent.TagMagnitude, EmotionEvaluator.DefaultTagMagnitude, "튜토리얼 값이 본편 상수를 바꾸지 않는다");
        }

        [Test]
        public void ItemInventory_TryGrant_RespectsCapacity()
        {
            var session = NewSession();
            session.Runner.StartStage();
            var ego = TutorialContent.EgoInflation();

            Assert.IsTrue(session.Items.TryGrant(ego));
            Assert.IsFalse(session.Items.TryGrant(ego), "칸이 하나라 두 번째는 못 받는다");
            Assert.AreEqual(1, session.Items.Held.Count);
        }
    }
}
