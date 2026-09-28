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
    /// 튜토리얼(기획서 '게임 시작 연출 / 튜토리얼', 2026-09-28 개정: 애정 공허 → 자아 부정, 아이템 자아비대 + 기억 공감)의 콘텐츠와 이음새를 검증한다: 단서·컴플렉스가 표와 같은지, 마지막 중첩 턴의 결과가 기획 지시 표와 같은지,
    /// 스크립트대로 4턴을 끝까지 통과할 수 있는지(키 범위 90~100 / 70~80에 맞는 심박수 경로), 오답 거절·결과를 보여 준 오답의 되돌리기가 상태를 원래대로 두는지, 코어 이음새(ReplaceWith·고정 키 구역·정해진 발현·게이트)가 약속대로 움직이는지.
    /// 기댓값은 TutorialContent를 거치지 않고 노션 표와 기획 지시를 직접 옮겨 적었다.
    /// </summary>
    public class TutorialContentTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private const string Ego = "item_ego_inflation";
        private const string Empathy = "item_empathy";

        private static StageSession NewSession(ClueKnowledgeLedger ledger = null) =>
            TutorialContent.CreateSession(Polarity, new SystemRandomSource(1), ledger ?? new ClueKnowledgeLedger());

        private static ClueInstance Held(StageSession session, string id) =>
            session.Hand.Cards.First(card => card.Definition.Id == id);

        /// <summary>정답으로 턴 1·3을 지나 턴 4가 시작된 세션(아이템 둘을 쥐고 있고 아직 안 썼다).</summary>
        private static StageSession SessionAtTurn4(ClueKnowledgeLedger ledger = null)
        {
            var session = NewSession(ledger);
            session.Runner.StartStage();
            session.Runner.PlayClue(Held(session, "tutorial_calendar"));
            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));
            Assert.AreEqual(4, session.Runner.CurrentTurn);
            return session;
        }

        private static void UseItem(StageSession session, string itemId) =>
            session.Runner.UseItem(session.Items.Held.First(item => item.Id == itemId));

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
                ("tutorial_souvenir", "기념품", TimeTag.Past, PersonTag.Friend, EmotionTag.Happiness),
                ("tutorial_phone_ring", "전화벨 소리", TimeTag.Present, PersonTag.Family, EmotionTag.Love),
                ("tutorial_fishing_rod", "낚싯대", TimeTag.Future, PersonTag.Friend, EmotionTag.Happiness),
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
        public void Table2_EmotionsAreHappinessLoveHappinessHappiness()
        {
            var emotions = TutorialContent.Quarter2Hand().Select(c => c.Emotions.Single()).ToArray();
            CollectionAssert.AreEqual(new[] { EmotionTag.Happiness, EmotionTag.Love, EmotionTag.Happiness, EmotionTag.Happiness }, emotions);
        }

        [Test]
        public void SelfDenial_AddsOneHappiness_WhenAnyDepressedEmotion()
        {
            var definition = TutorialContent.SelfDenial(Polarity);
            Assert.AreEqual("tutorial_self_denial", definition.Id);
            Assert.AreEqual("자아 부정 컴플렉스", definition.DisplayName);
            Assert.AreEqual("침체되는 감정을 느끼면 행복을 함께 느낍니다.", definition.Description);
            Assert.AreEqual(2, definition.DefaultDuration, "노션에 없어 반 과거와 같은 2턴으로 둔 값");

            var disgust = new TagSet(TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Disgust });
            Assert.IsTrue(definition.TryInterpret(new ComplexContext(disgust)));
            Assert.AreEqual(1, disgust.CountOf(EmotionTag.Disgust), "침체 감정은 그대로 남는다");
            Assert.AreEqual(1, disgust.CountOf(EmotionTag.Happiness));

            // 침체 감정이 여러 개(종류도 여러 개)여도 행복은 한 번만.
            var many = new TagSet(TimeTag.Past, new[] { PersonTag.Other }, new[] { EmotionTag.Sadness, EmotionTag.Sadness, EmotionTag.Fear });
            Assert.IsTrue(definition.TryInterpret(new ComplexContext(many)));
            Assert.AreEqual(1, many.CountOf(EmotionTag.Happiness));
        }

        [Test]
        public void SelfDenial_DoesNotReactWithoutDepressedEmotion()
        {
            var definition = TutorialContent.SelfDenial(Polarity);

            var excited = new TagSet(TimeTag.Present, new[] { PersonTag.Family }, new[] { EmotionTag.Love, EmotionTag.Happiness });
            Assert.IsFalse(definition.TryInterpret(new ComplexContext(excited)));
            Assert.AreEqual(1, excited.CountOf(EmotionTag.Happiness), "흥분 감정만 있으면 그대로");

            Assert.IsFalse(definition.TryInterpret(new ComplexContext(new TagSet(TimeTag.Present, new[] { PersonTag.Family }))));
        }

        [Test]
        public void SelfDenial_IsSeparateFromStage3SelfDenial()
        {
            var tutorial = TutorialContent.SelfDenial(Polarity);
            var stage3 = Stage3Content.SelfDenial(Polarity);

            Assert.AreNotEqual(stage3.Id, tutorial.Id, "규칙이 달라 id를 따로 쓴다");
            Assert.AreEqual(stage3.DisplayName, tutorial.DisplayName);

            // 흥분이 우세한 태그에 스테이지 3 것은 혐오를 더하지만 튜토리얼 것은 반응하지 않는다.
            var excited = new TagSet(TimeTag.Present, new[] { PersonTag.Family }, new[] { EmotionTag.Love });
            Assert.IsFalse(tutorial.TryInterpret(new ComplexContext(excited.Clone())));
            Assert.IsTrue(stage3.TryInterpret(new ComplexContext(excited.Clone())));
        }

        [Test]
        public void Items_AreEgoInflationAndMemoryEmpathy_TwoSlots_EmptyAtStart()
        {
            var session = NewSession();
            Assert.AreEqual(2, session.Items.Capacity, "튜토리얼 아이템 칸은 둘");
            Assert.AreEqual(Ego, TutorialContent.EgoInflation().Id);
            Assert.AreEqual(Empathy, TutorialContent.MemoryEmpathy().Id);
            Assert.AreEqual("trait_hallucination", TutorialContent.MemoryEmpathy().GrantedTraitId);
            Assert.IsNotNull(session.Traits.Find("trait_hallucination"), "기억 공감이 부여할 환각이 카탈로그에 있어야 한다");

            session.Runner.StartStage();
            Assert.IsEmpty(session.Items.Held, "시작 리필이 칸을 채우면 턴 4의 지급 자리가 사라진다 — 시작 때는 비어 있어야 한다");
        }

        // ── 설계 검증: 카드 × 컴플렉스 × 아이템 (기획 지시 표) ────────────────────

        [Test]
        public void ThirdTurn_AntiPastOnly_SouvenirAndPhotoDepressed_PhoneAndRodExcited()
        {
            // 턴 3에는 반 과거만 붙어 있다(자아 부정은 턴 3이 끝날 때 붙는다).
            var antiPast = TutorialContent.AntiPast(Polarity);
            var directions = TutorialContent.Quarter2Hand().ToDictionary(c => c.Id, c => Direction(Resolve(c, antiPast)));

            Assert.AreEqual(-1, directions["tutorial_souvenir"], "기념품 = 침체(정답)");
            Assert.AreEqual(-1, directions["tutorial_daughter_photo"], "어린 딸의 사진 = 침체(정답)");
            Assert.AreEqual(1, directions["tutorial_phone_ring"], "전화벨 소리 = 흥분(오답)");
            Assert.AreEqual(1, directions["tutorial_fishing_rod"], "낚싯대 = 흥분(오답)");
        }

        [Test]
        public void FinalTurn_BothItems_MatchesTheDesignTable()
        {
            // 기획 지시: 턴 4(반 과거 + 자아 부정 + 아이템 둘, 환각 + ×2) — 기념품·어린 딸의 사진 = 변화 없음, 전화벨 소리·낚싯대 = 침체.
            var expected = new Dictionary<string, (int Delta, string Tags)>
            {
                ["tutorial_souvenir"] = (0, "Disgust×2,Happiness×2"),
                ["tutorial_daughter_photo"] = (0, "Disgust×2,Happiness×2"),
                ["tutorial_phone_ring"] = (-12, "Love×2"),
                ["tutorial_fishing_rod"] = (-12, "Happiness×2"),
            };

            foreach (var pair in expected)
            {
                var session = SessionAtTurn4();
                UseItem(session, Ego);
                UseItem(session, Empathy);

                var report = session.Runner.PlayClue(Held(session, pair.Key));
                var tags = string.Join(",", report.FinalTags.Emotions.OrderBy(e => e.Key.ToString()).Select(e => $"{e.Key}×{e.Value}"));

                Assert.AreEqual(pair.Value.Delta, report.HeartbeatDelta, pair.Key);
                Assert.AreEqual(pair.Value.Tags, tags, pair.Key);
                Assert.AreEqual(pair.Value.Delta != 0, report.KeyResult.Value.Success, pair.Key + " — 침체 카드만 키를 얻는다");
            }
        }

        [Test]
        public void FinalTurn_EachCardFiresTheComplexesItsTagsMatch()
        {
            var board = new ComplexBoard();
            board.TryAttach(new ComplexInstance(TutorialContent.AntiPast(Polarity), 100));
            board.TryAttach(new ComplexInstance(TutorialContent.SelfDenial(Polarity), 101));
            var resolver = new ComplexResolver(board);

            var expected = new Dictionary<string, string[]>
            {
                ["tutorial_souvenir"] = new[] { "complex_anti_past", "tutorial_self_denial" },
                ["tutorial_phone_ring"] = new string[0],
                ["tutorial_fishing_rod"] = new string[0],
                ["tutorial_daughter_photo"] = new[] { "complex_anti_past", "tutorial_self_denial" },
            };

            foreach (var clue in TutorialContent.Quarter2Hand())
            {
                var triggered = resolver.Resolve(clue.CreateOriginalTagSet()).Steps
                    .Where(s => s.Triggered).Select(s => s.Complex.Definition.Id).ToArray();
                CollectionAssert.AreEqual(expected[clue.Id], triggered, clue.Id);
            }
        }

        [Test]
        public void FinalTurn_OneItemOrNone_NeverReachesTheKey()
        {
            // 아이템을 하나만(또는 하나도) 쓰면 어느 카드를 내도 70~80에 못 들어간다.
            var itemSets = new[] { new string[0], new[] { Ego }, new[] { Empathy } };
            foreach (var items in itemSets)
            foreach (var clue in TutorialContent.Quarter2Hand())
            {
                var session = SessionAtTurn4();
                foreach (var item in items) UseItem(session, item);

                var report = session.Runner.PlayClue(Held(session, clue.Id));
                var label = $"{clue.Id} + [{string.Join(",", items)}]";
                Assert.IsFalse(report.KeyResult.Value.Success, label);
                Assert.That(session.Heartbeat.Value, Is.Not.InRange(TutorialContent.Quarter2KeyMin, TutorialContent.Quarter2KeyMax), label);
            }
        }

        // ── 심박수 경로 ──────────────────────────────────────────────────────────

        [Test]
        public void HeartbeatPath_MatchesKeyRanges_ByArithmetic()
        {
            // 태그 수: 턴 1 +1(달력 행복), 턴 3 −1(딸: 혐오), 턴 4 전화벨/낚싯대: 흥분 1 → ×2 → 환각으로 −2. 상쇄 카드(기념품·딸)는 0. 턴 2는 넘김.
            var m = TutorialContent.TagMagnitude;
            var s = TutorialContent.StartHeartbeat;
            var afterTurn3 = s + m - m;

            Assert.That(s + m, Is.InRange(90, 100), "1쿼터 키 90~100");
            Assert.That(afterTurn3 - 2 * m, Is.InRange(70, 80), "2쿼터 키 70~80 (두 아이템)");
            Assert.That(afterTurn3 - m, Is.Not.InRange(70, 80), "기억 공감만: 환각 −1태그");
            Assert.That(afterTurn3 + 2 * m, Is.Not.InRange(70, 80), "자아비대만: 흥분 +2태그");
            Assert.That(afterTurn3 + m, Is.Not.InRange(70, 80), "아이템 없이: 흥분 +1태그");
            Assert.That(afterTurn3, Is.Not.InRange(70, 80), "상쇄 카드: 변화 없음");
            Assert.AreEqual(6, m);
            Assert.AreEqual(89, s);
        }

        [Test]
        public void TagMagnitudeCandidates_OnlySmallValuesFitTheNarrowRanges()
        {
            // 두 아이템이 모두 필요하면서(하나만/없이/상쇄 카드는 실패) 두 범위에 모두 들어오는 시작 심박수가 있는 영향력만 쓸 수 있다.
            bool Fits(int m, int s)
            {
                bool InKey2(int v) => v is >= 70 and <= 80;
                var h3 = s;
                return s + m is >= 90 and <= 100 && InKey2(h3 - 2 * m)
                       && !InKey2(h3 - m) && !InKey2(h3 + 2 * m) && !InKey2(h3 + m) && !InKey2(h3);
            }

            bool Feasible(int m) => Enumerable.Range(0, 201).Any(s => Fits(m, s));

            Assert.IsTrue(Fits(TutorialContent.TagMagnitude, TutorialContent.StartHeartbeat), "지금 값(6·89)이 그대로 맞는다");
            Assert.IsFalse(Feasible(10), "본편 기본값 10은 두 범위에 모두 들어오는 시작 심박수가 없다");
            Assert.IsFalse(Feasible(30), "30도 좁은 범위에 안 맞는다");

            // 영향력 6에서 가능한 시작값은 87~92 — 89는 모든 경계에서 여유 3 이상(턴 4의 77은 80까지 3, 기억 공감만의 83은 80까지 3).
            CollectionAssert.AreEqual(new[] { 87, 88, 89, 90, 91, 92 }, Enumerable.Range(0, 201).Where(s => Fits(6, s)).ToArray());
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
                Assert.IsFalse(verdict.IsTrial, "턴 1의 오답은 내기 전에 거절한다");
                Assert.AreEqual(PlayRejection.WrongDirection, verdict.Reason);
                CollectionAssert.AreEqual(card.Definition.Emotions, verdict.ObservedEmotions, "거절 메시지가 말할 감정 = 그 카드의 감정");
            }
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_calendar")).Allowed);

            session.Runner.PlayClue(Held(session, "tutorial_calendar"));

            // 턴 2는 손패가 비어 시간만 흐르고, 그 턴 끝에 첫 키(90~100)가 판정된다. 2쿼터 시작에 반 과거가 붙고 손패는 표 2로 바뀐다.
            Assert.AreEqual(2, reports.Count);
            Assert.IsFalse(reports[0].IsPass);
            Assert.AreEqual(6, reports[0].HeartbeatDelta);
            Assert.IsTrue(reports[1].IsPass, "턴 2는 넘어간 턴(필러)");
            Assert.IsTrue(reports[1].KeyResult.Value.Success);
            Assert.AreEqual(95, reports[1].KeyResult.Value.Position);
            Assert.AreEqual(1, session.Keys.Collected);
            Assert.AreEqual("complex_anti_past", reports[1].SpawnedComplex.Definition.Id);
            Assert.AreEqual(95, session.Heartbeat.Value);
            Assert.AreEqual(3, session.Runner.CurrentTurn);
            CollectionAssert.AreEquivalent(TutorialContent.Quarter2Hand().Select(c => c.Id), HandIds(session));
            Assert.IsEmpty(session.Items.Held, "아이템은 턴 4 전에는 손에 없다");

            // 턴 3: 반 과거 하나 — 기념품·어린 딸의 사진 둘 다 통과, 전화벨/낚싯대는 내기 전에 거절.
            foreach (var card in session.Hand.Cards.Where(c => c.Definition.Id != "tutorial_souvenir" && c.Definition.Id != "tutorial_daughter_photo"))
            {
                var verdict = session.CheckPlay(card);
                Assert.IsFalse(verdict.Allowed, card.Definition.Id);
                Assert.IsFalse(verdict.IsTrial, "턴 3의 오답은 내기 전에 거절한다");
                Assert.AreEqual(PlayRejection.WrongDirection, verdict.Reason);
            }
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_souvenir")).Allowed);
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_daughter_photo")).Allowed);

            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));

            Assert.AreEqual(-6, reports[2].HeartbeatDelta);
            Assert.AreEqual(89, session.Heartbeat.Value);
            Assert.AreEqual(4, session.Runner.CurrentTurn);
            Assert.AreEqual("tutorial_self_denial", reports[2].SpawnedComplex.Definition.Id);
            CollectionAssert.AreEqual(new[] { "complex_anti_past", "tutorial_self_denial" },
                session.Complexes.InPriorityOrder().Select(c => c.Definition.Id).ToArray(), "반 과거가 먼저, 자아 부정이 나중");
            CollectionAssert.AreEquivalent(TutorialContent.Quarter2Hand().Select(c => c.Id), HandIds(session), "턴 4 손패도 표 2의 4장");

            // 턴 4: 아이템 둘(자아비대·기억 공감)이 이 턴에 처음 손에 들어온다. 둘 다 켜기 전에는 어느 카드든 아이템 안내.
            CollectionAssert.AreEqual(new[] { Ego, Empathy }, session.Items.Held.Select(i => i.Id).ToArray());
            foreach (var card in session.Hand.Cards)
                Assert.AreEqual(PlayRejection.ItemNeeded, session.CheckPlay(card).Reason, card.Definition.Id);

            UseItem(session, Ego);
            foreach (var card in session.Hand.Cards)
                Assert.AreEqual(PlayRejection.ItemNeeded, session.CheckPlay(card).Reason, card.Definition.Id + " — 하나만 켜서는 부족");

            UseItem(session, Empathy);
            Assert.IsTrue(session.Traits.Has("trait_hallucination"), "기억 공감이 환각을 붙인다");

            // 둘 다 켜면: 전화벨 소리·낚싯대는 허용, 기념품·딸의 사진은 "보여 준 뒤 되돌리는" 오답.
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_phone_ring")).Allowed);
            Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_fishing_rod")).Allowed);
            foreach (var id in new[] { "tutorial_souvenir", "tutorial_daughter_photo" })
            {
                var verdict = session.CheckPlay(Held(session, id));
                Assert.IsFalse(verdict.Allowed, id);
                Assert.IsTrue(verdict.IsTrial, id + " — 턴 4의 오답은 결과를 보여 준 뒤 되돌린다");
                Assert.AreEqual(PlayRejection.KeyMissed, verdict.Reason, id);
            }

            var last = session.Runner.PlayClue(Held(session, "tutorial_phone_ring"));

            Assert.AreEqual(-12, last.HeartbeatDelta, "사랑 하나 ×2 → 환각으로 침체 2태그 × 6");
            Assert.AreEqual(77, session.Heartbeat.Value);
            Assert.IsTrue(last.KeyResult.Value.Success);
            Assert.AreEqual(2, session.Keys.Collected);
            Assert.AreEqual(StageOutcome.Cleared, last.Outcome);
            CollectionAssert.AreEqual(new[] { "trait_hallucination" }, last.TraitsManifested.Select(t => t.Id).ToArray(), "결과에 환각 특성 태그가 뜬다");
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
            Assert.AreEqual(2, remaining["tutorial_self_denial"]);
        }

        [Test]
        public void TheAllowedPaths_ClearTheStage()
        {
            // 턴 1은 정답이 하나(달력), 턴 3은 둘(기념품·딸의 사진), 턴 4는 둘(전화벨 소리·낚싯대) — 어느 정답을 골라도 스테이지를 깰 수 있다.
            foreach (var turn3 in new[] { "tutorial_souvenir", "tutorial_daughter_photo" })
            foreach (var turn4 in new[] { "tutorial_phone_ring", "tutorial_fishing_rod" })
            {
                var session = NewSession();
                session.Runner.StartStage();

                var turn1 = session.Hand.Cards.Where(c => session.CheckPlay(c).Allowed).ToList();
                Assert.AreEqual(1, turn1.Count);
                session.Runner.PlayClue(turn1[0]);
                Assert.AreEqual(1, session.Keys.Collected);

                var allowed3 = session.Hand.Cards.Where(c => session.CheckPlay(c).Allowed).Select(c => c.Definition.Id).ToArray();
                CollectionAssert.AreEquivalent(new[] { "tutorial_souvenir", "tutorial_daughter_photo" }, allowed3);
                session.Runner.PlayClue(Held(session, turn3));

                UseItem(session, Ego);
                UseItem(session, Empathy);
                var allowed4 = session.Hand.Cards.Where(c => session.CheckPlay(c).Allowed).Select(c => c.Definition.Id).ToArray();
                CollectionAssert.AreEquivalent(new[] { "tutorial_phone_ring", "tutorial_fishing_rod" }, allowed4);

                var report = session.Runner.PlayClue(Held(session, turn4));
                Assert.AreEqual(StageOutcome.Cleared, report.Outcome, $"{turn3} → {turn4}");
                Assert.AreEqual(77, session.Heartbeat.Value);
            }
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

            // 컴플렉스가 발동하는 미리보기(턴 4에서 아이템을 켠 뒤 오답 카드)도 관찰 기록·심박수·손패·지속 턴을 건드리지 않는다.
            session.Runner.PlayClue(Held(session, "tutorial_calendar"));
            session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));
            UseItem(session, Ego);
            UseItem(session, Empathy);
            var pendingBefore = ledger.PendingCount;
            var heartbeatBefore = session.Heartbeat.Value;
            var handBeforeTurn4 = HandIds(session);
            var remaining = session.Complexes.Slots.Select(c => c.RemainingTurns).ToArray();
            var reportCount = reports.Count;

            for (var i = 0; i < 3; i++)
                Assert.IsTrue(session.CheckPlay(Held(session, "tutorial_daughter_photo")).IsTrial);

            Assert.AreEqual(pendingBefore, ledger.PendingCount, "판정은 관찰 기록을 남기지 않는다");
            Assert.AreEqual(heartbeatBefore, session.Heartbeat.Value);
            CollectionAssert.AreEqual(handBeforeTurn4, HandIds(session));
            CollectionAssert.AreEqual(remaining, session.Complexes.Slots.Select(c => c.RemainingTurns).ToArray());
            Assert.AreEqual(4, session.Runner.CurrentTurn);
            Assert.AreEqual(reportCount, reports.Count);
        }

        // ── 턴 4 오답: 결과를 보여 준 뒤 되돌리기 ───────────────────────────────────

        [Test]
        public void PreviewPlay_MatchesTheRealPlay_WithoutChangingState()
        {
            foreach (var clue in TutorialContent.Quarter2Hand())
            {
                var session = SessionAtTurn4();
                UseItem(session, Ego);
                UseItem(session, Empathy);
                var heartbeat = session.Heartbeat.Value;
                var pending = session.Ledger.PendingCount;

                var preview = session.Runner.PreviewPlay(Held(session, clue.Id));
                Assert.AreEqual(heartbeat, session.Heartbeat.Value, clue.Id);
                Assert.AreEqual(pending, session.Ledger.PendingCount, clue.Id);
                Assert.AreEqual(1, session.Keys.Collected, clue.Id);
                Assert.AreEqual(StageOutcome.InProgress, preview.Outcome, "미리보기는 판을 끝내지 않는다");

                var real = session.Runner.PlayClue(Held(session, clue.Id));
                Assert.AreEqual(real.HeartbeatDelta, preview.HeartbeatDelta, clue.Id);
                Assert.AreEqual(real.HeartbeatValue, preview.HeartbeatValue, clue.Id);
                CollectionAssert.AreEquivalent(real.FinalTags.Emotions, preview.FinalTags.Emotions, clue.Id);
                Assert.AreEqual(real.KeyResult.Value.Success, preview.KeyResult.Value.Success, clue.Id);
                Assert.AreEqual(real.KeyResult.Value.Quarter, preview.KeyResult.Value.Quarter, clue.Id);
                CollectionAssert.AreEqual(real.TraitsManifested, preview.TraitsManifested, clue.Id);
                CollectionAssert.AreEqual(
                    real.Interpretation.Steps.Where(s => s.Triggered).Select(s => s.Complex.Definition.Id),
                    preview.Interpretation.Steps.Where(s => s.Triggered).Select(s => s.Complex.Definition.Id), clue.Id);
            }
        }

        [Test]
        public void Turn4Trial_RollBack_RestoresTheTurnStartExactly()
        {
            var ledger = new ClueKnowledgeLedger();
            var session = SessionAtTurn4(ledger);
            var reports = new List<TurnReport>();
            var ended = 0;
            session.Runner.TurnResolved += reports.Add;
            session.Runner.StageEnded += _ => ended++;

            // 턴 4가 시작된 상태(카드를 내기 전, 아이템을 쓰기 전)를 떠 둔다.
            var cards = session.Hand.Cards.ToList();
            var held = session.Items.Held.ToList();
            var complexes = session.Complexes.InPriorityOrder().Select(c => (c, c.RemainingTurns)).ToList();
            var heartbeat = session.Heartbeat.Value;
            var pending = ledger.PendingCount;
            var keyResults = session.Keys.Results.ToList();
            var activeZone = session.Keys.ActiveZone;

            // 두 아이템을 쓰고 오답 카드(딸의 사진)를 낸다 → 결과를 보여 줄 거절.
            UseItem(session, Ego);
            UseItem(session, Empathy);
            var card = Held(session, "tutorial_daughter_photo");
            Assert.IsTrue(session.CheckPlay(card).IsTrial);
            var shown = session.Runner.PreviewPlay(card);
            Assert.AreEqual(0, shown.HeartbeatDelta, "변화 없음");
            Assert.IsFalse(shown.KeyResult.Value.Success, "키 미획득");
            CollectionAssert.AreEqual(new[] { "complex_anti_past", "tutorial_self_denial" },
                shown.Interpretation.Steps.Where(s => s.Triggered).Select(s => s.Complex.Definition.Id).ToArray(), "연출할 컴플렉스 반응");

            session.RollBackTrial();

            CollectionAssert.AreEqual(cards, session.Hand.Cards, "손패: 같은 카드 인스턴스가 같은 순서로");
            CollectionAssert.AreEqual(held, session.Items.Held, "사용한 아이템 둘이 원래 칸 순서대로 돌아온다");
            Assert.IsEmpty(session.ActiveItems.Active, "켜 둔 아이템 효과가 거둬진다");
            Assert.IsEmpty(session.Traits.Traits, "부여된 환각이 떨어진다");
            CollectionAssert.AreEqual(complexes, session.Complexes.InPriorityOrder().Select(c => (c, c.RemainingTurns)).ToList(), "컴플렉스와 남은 턴");
            Assert.AreEqual(heartbeat, session.Heartbeat.Value);
            Assert.AreEqual(4, session.Runner.CurrentTurn);
            Assert.AreEqual(StageOutcome.InProgress, session.Runner.Outcome);
            Assert.AreEqual(1, session.Keys.Collected);
            CollectionAssert.AreEqual(keyResults, session.Keys.Results);
            Assert.AreEqual(activeZone, session.Keys.ActiveZone, "2쿼터 구역이 열린 채");
            Assert.AreEqual(pending, ledger.PendingCount, "튜토리얼 장부에 관찰이 남지 않는다");
            Assert.IsEmpty(reports, "턴 결산이 일어나지 않는다");
            Assert.AreEqual(0, ended);

            // 되돌린 뒤 다시: 아이템을 또 쓸 수 있고, 정답을 내면 깨진다 — 환각은 이번 결과에 한 번만 뜬다.
            foreach (var verdictCard in session.Hand.Cards)
                Assert.AreEqual(PlayRejection.ItemNeeded, session.CheckPlay(verdictCard).Reason, "되돌린 뒤엔 다시 아이템부터");
            UseItem(session, Ego);
            UseItem(session, Empathy);
            var report = session.Runner.PlayClue(Held(session, "tutorial_phone_ring"));

            Assert.AreEqual(77, session.Heartbeat.Value);
            Assert.AreEqual(StageOutcome.Cleared, report.Outcome);
            CollectionAssert.AreEqual(new[] { "trait_hallucination" }, report.TraitsManifested.Select(t => t.Id).ToArray());
            Assert.AreEqual(1, reports.Count);
        }

        [Test]
        public void Turn4Trial_RollBack_WorksRepeatedly()
        {
            var session = SessionAtTurn4();
            var held = session.Items.Held.ToList();

            for (var i = 0; i < 3; i++)
            {
                UseItem(session, Empathy);
                UseItem(session, Ego);
                Assert.IsTrue(session.CheckPlay(Held(session, i % 2 == 0 ? "tutorial_souvenir" : "tutorial_daughter_photo")).IsTrial);
                session.RollBackTrial();

                CollectionAssert.AreEqual(held, session.Items.Held, $"{i}회째: 쓴 순서와 무관하게 원래 칸 순서");
                Assert.IsEmpty(session.ActiveItems.Active);
                Assert.IsEmpty(session.Traits.Traits);
                Assert.AreEqual(89, session.Heartbeat.Value);
            }
        }

        [Test]
        public void Turn4Trial_RollBack_ReturnsItemsThroughGainedEvents()
        {
            var session = SessionAtTurn4();
            var gained = new List<string>();
            var removedTraits = new List<string>();
            session.Items.Gained += item => gained.Add(item.Id);
            session.Traits.Removed += trait => removedTraits.Add(trait.Definition.Id);

            UseItem(session, Ego);
            UseItem(session, Empathy);
            session.RollBackTrial();

            CollectionAssert.AreEqual(new[] { Ego, Empathy }, gained, "화면(ItemController)이 칸에 다시 끼울 수 있게 획득 이벤트로 알린다");
            CollectionAssert.AreEqual(new[] { "trait_hallucination" }, removedTraits);
        }

        // ── 장부·발현 ────────────────────────────────────────────────────────────

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
                Assert.AreEqual(89, session.Heartbeat.Value, $"seed {seed}");
                session.Runner.PlayClue(Held(session, "tutorial_calendar"));
                Assert.AreEqual(new[] { "complex_anti_past" }, session.Complexes.Slots.Select(c => c.Definition.Id).ToArray(), $"seed {seed}");
                session.Runner.PlayClue(Held(session, "tutorial_daughter_photo"));
                Assert.AreEqual(new[] { "complex_anti_past", "tutorial_self_denial" }, session.Complexes.Slots.Select(c => c.Definition.Id).ToArray(), $"seed {seed}");
                CollectionAssert.AreEqual(new[] { Ego, Empathy }, session.Items.Held.Select(i => i.Id).ToArray(), $"seed {seed}");
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
            Assert.DoesNotThrow(session.RollBackTrial, "게이트가 없으면 되돌리기는 아무 일도 없다");
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

            Assert.IsTrue(session.Items.TryGrant(TutorialContent.EgoInflation()));
            Assert.IsTrue(session.Items.TryGrant(TutorialContent.MemoryEmpathy()));
            Assert.IsFalse(session.Items.TryGrant(TutorialContent.EgoInflation()), "칸이 둘이라 세 번째는 못 받는다");
            Assert.AreEqual(2, session.Items.Held.Count);
        }
    }
}
