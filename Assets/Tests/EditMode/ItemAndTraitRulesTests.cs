using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 기획서 "아이템과 특성": 아이템 보유/충전 규칙(스테이지 시작 때 4개 지급, 이후 쿼터가 바뀔 때마다 쓴 칸만 다른 아이템으로
    /// 채워지고 안 쓴 아이템은 그대로 유지됨 — 매 쿼터 전체를 새로 뽑는 손패와는 "쓴 칸만" 채운다는 점이 다르다),
    /// 아이템 7종 효과, 특성 6종 효과와 계산 순서, 컴플렉스 최대 중첩 초과 → 특수 특성 발현·치유.
    /// </summary>
    public class ItemAndTraitRulesTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private sealed class AnyZonePlacer : IKeyZonePlacer
        {
            public KeyZone Place(int startPosition, int turnsUntilKey) => new(150, 10);
        }

        private static ClueDefinition Clue(string id, params EmotionTag[] emotions) =>
            new(id, id, "", TimeTag.None, Array.Empty<PersonTag>(), emotions);

        private static ItemDefinition Item(string id) => PrototypeContent.Items().First(i => i.Id == id);

        private static StageConfig Config(IReadOnlyList<ItemDefinition> items,
                                          IReadOnlyList<ClueDefinition> clues = null,
                                          ComplexDefinition starting = null,
                                          IReadOnlyList<ComplexDefinition> complexPool = null,
                                          int maxSlots = 3,
                                          double complexWeight = 0.0,
                                          IReadOnlyDictionary<string, IReadOnlyList<string>> itemParameters = null) =>
            new("test", "test", quarterCount: 3, turnsPerQuarter: 4, requiredKeys: 2, complexWeight: complexWeight,
                clues: clues ?? Enumerable.Range(0, 12).Select(i => Clue($"c{i}", EmotionTag.Happiness)).ToList(),
                complexPool: complexPool ?? Array.Empty<ComplexDefinition>(),
                startingComplex: starting,
                itemPool: items,
                keyWidth: KeyZoneLayout.DefaultKeyWidth,
                maxComplexSlots: maxSlots,
                traits: PrototypeContent.Traits(),
                itemSlots: items.Count,
                itemParameters: itemParameters ?? PrototypeContent.PrototypeStage(Polarity).ItemParameters);

        private static StageSession Start(StageConfig config, int seed = 1, int heartbeat = Heartbeat.DefaultStartValue)
        {
            var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), Polarity,
                heartbeatStartValue: heartbeat, keyPlacer: new AnyZonePlacer());
            session.Runner.StartStage();
            return session;
        }

        // ------------------------------------------------------------------
        // 보유 / 충전
        // ------------------------------------------------------------------

        [Test]
        public void Stage_StartsWithFourDistinctItems()
        {
            var session = Start(PrototypeContent.PrototypeStage(Polarity));

            Assert.AreEqual(4, session.Items.Capacity);
            Assert.AreEqual(4, session.Items.Held.Count, "스테이지 시작 시 아이템 4개가 주어진다.");
            Assert.AreEqual(4, session.Items.Held.Select(i => i.Id).Distinct().Count(), "처음 4개는 서로 다른 종류다.");
        }

        [Test]
        public void ItemPool_HasTheEightItemsOfTheDesignDoc()
        {
            var ids = PrototypeContent.Items().Select(i => i.DisplayName).ToList();

            CollectionAssert.AreEquivalent(
                new[] { "극복", "감정적 설득", "기억 공감", "회상", "논리적 설득", "착한 사마리아인", "선택적 기억", "무관심" }, ids);
        }

        [Test]
        public void UsedSlot_IsRefilledAtNextQuarterStart_AndUnusedItemsStay()
        {
            // 쓴 칸은 쿼터가 바뀌는 순간(다음 쿼터 시작) 다른 아이템으로 채워진다 — 안 쓴 아이템은 그대로 유지된다.
            // 매 쿼터 전체를 새로 뽑는 손패(ClueHand.RefillForNewQuarter)와 달리, 여기선 "쓴 칸만" 채운다.
            var session = Start(PrototypeContent.PrototypeStage(Polarity), seed: 5);
            var runner = session.Runner;

            var used = session.Items.Held.First(i => i.TargetKind == ItemTargetKind.None && runner.CanUseItem(i));
            var unused = session.Items.Held.Where(i => i != used).ToList();
            runner.UseItem(used);
            Assert.AreEqual(3, session.Items.Held.Count);

            var gained = new List<ItemDefinition>();
            session.Items.Gained += gained.Add;

            for (var turn = 1; turn <= 3; turn++) // 1쿼터 남은 턴 진행 — 4번째(마지막) 카드가 2쿼터 경계를 넘긴다
            {
                runner.PlayClue(session.Hand.Cards[0]);
                Assert.AreEqual(3, session.Items.Held.Count, $"턴 {turn} 뒤에는 아직 같은 쿼터라 채워지지 않는다.");
            }

            runner.PlayClue(session.Hand.Cards[0]); // 1쿼터 마지막 턴 → 2쿼터 시작으로 넘어가며 쓴 칸이 채워진다
            Assert.AreEqual(2, runner.CurrentQuarter);
            Assert.AreEqual(4, session.Items.Held.Count, "쿼터가 바뀌면 쓴 칸이 다시 채워진다.");
            Assert.AreEqual(1, gained.Count, "쓴 칸 하나만큼만 채워진다.");
            Assert.AreNotEqual(used, gained[0], "직전에 쓴 종류는 되도록 피한다.");
            CollectionAssert.IsSubsetOf(unused, session.Items.Held.ToList());

            gained.Clear();
            for (var turn = 5; turn <= 8; turn++) session.Runner.PlayClue(session.Hand.Cards[0]); // 2쿼터 전체(아무것도 안 씀)

            Assert.AreEqual(3, runner.CurrentQuarter);
            Assert.AreEqual(4, session.Items.Held.Count);
            Assert.AreEqual(0, gained.Count, "이미 가득 찬 칸은 쿼터가 바뀌어도 다시 채워지지 않는다.");
        }

        [Test]
        public void NothingUsed_MeansNothingIsRefilled_AtQuarterStart()
        {
            var session = Start(PrototypeContent.PrototypeStage(Polarity), seed: 9);
            var before = session.Items.Held.ToList();

            for (var turn = 1; turn <= 4; turn++) session.Runner.PlayClue(session.Hand.Cards[0]);

            Assert.AreEqual(2, session.Runner.CurrentQuarter, "4턴 뒤에는 2쿼터가 시작되어 있어야 한다.");
            CollectionAssert.AreEqual(before, session.Items.Held.ToList(), "칸이 이미 가득 차 있으면 쿼터가 바뀌어도 바뀌는 게 없다.");
        }

        // ------------------------------------------------------------------
        // 아이템 7종
        // ------------------------------------------------------------------

        [Test]
        public void Overcome_HalvesTheChosenComplexDuration_RoundedUp()
        {
            var overcome = Item("item_overcome");
            var session = Start(Config(new[] { overcome }, starting: PrototypeContent.AntiPast(Polarity)));
            var complex = session.Complexes.Slots[0];
            Assert.AreEqual(3, complex.RemainingTurns);

            var changed = 0;
            session.Complexes.DurationChanged += _ => changed++;

            session.Runner.UseItem(overcome, ItemTarget.Of(complex));

            Assert.AreEqual(2, complex.RemainingTurns, "3턴의 절반은 올림해 2턴.");
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Overcome_NeedsAComplexWithMoreThanOneTurnLeft()
        {
            var overcome = Item("item_overcome");
            var noComplex = Start(Config(new[] { overcome }));
            Assert.IsFalse(noComplex.Runner.CanUseItem(overcome), "붙은 컴플렉스가 없으면 쓸 수 없다.");
            Assert.AreEqual(0, noComplex.Runner.GetItemTargets(overcome).Count);

            var session = Start(Config(new[] { overcome }, starting: PrototypeContent.AntiPast(Polarity)));
            var complex = session.Complexes.Slots[0];
            session.Complexes.HalveRemainingTurns(complex); // 3 → 2
            session.Complexes.HalveRemainingTurns(complex); // 2 → 1
            Assert.AreEqual(1, complex.RemainingTurns);

            Assert.IsFalse(session.Runner.CanUseItem(overcome), "남은 턴이 1이면 줄일 게 없어 대상이 아니다.");
            Assert.Throws<ArgumentException>(() => session.Runner.UseItem(overcome, ItemTarget.Of(complex)));
        }

        [Test]
        public void EmotionalPersuasion_IgnoresTheComplexesTheStageNames_AndNoOthers()
        {
            var persuasion = Item("item_persuasion");
            var config = Config(new[] { persuasion }, starting: PrototypeContent.Stockholm());
            var session = Start(config);

            session.Runner.UseItem(persuasion);

            ComplexInstance Make(ComplexDefinition d) => new(d, priority: 1);
            Assert.IsTrue(session.ActiveItems.ShouldIgnore(Make(PrototypeContent.Stockholm())), "스톡홀름");
            Assert.IsTrue(session.ActiveItems.ShouldIgnore(Make(PrototypeContent.Dependence())), "의존");
            Assert.IsTrue(session.ActiveItems.ShouldIgnore(Make(PrototypeContent.Avoidance())), "회피");
            Assert.IsFalse(session.ActiveItems.ShouldIgnore(Make(PrototypeContent.AntiPast(Polarity))), "반 과거는 침체 감정 컴플렉스가 아니다.");
        }

        [Test]
        public void EmotionalPersuasion_ReadsItsTargetsFromTheStageConfig()
        {
            var persuasion = Item("item_persuasion");
            var otherStage = new Dictionary<string, IReadOnlyList<string>>
            {
                [PrototypeContent.PersuasionTargetsKey] = new[] { "complex_anti_past" }
            };
            var session = Start(Config(new[] { persuasion }, itemParameters: otherStage));

            session.Runner.UseItem(persuasion);

            Assert.IsTrue(session.ActiveItems.ShouldIgnore(new ComplexInstance(PrototypeContent.AntiPast(Polarity), 0)));
            Assert.IsFalse(session.ActiveItems.ShouldIgnore(new ComplexInstance(PrototypeContent.Stockholm(), 0)),
                "스테이지가 다르면 대상 컴플렉스도 다르다.");
        }

        [Test]
        public void MemoryEmpathy_RemovesOnlySadnessOneEach_AndGrantsHallucination()
        {
            var empathy = Item("item_empathy");
            var session = Start(Config(new[] { empathy }));

            session.Runner.UseItem(empathy);

            var tags = new TagSet(emotions: new[] { EmotionTag.Fear, EmotionTag.Fear, EmotionTag.Sadness, EmotionTag.Anger });
            session.ActiveItems.Modify(tags);

            Assert.AreEqual(2, tags.CountOf(EmotionTag.Fear), "공포는 건드리지 않는다(기획: 슬픔 감정만)");
            Assert.AreEqual(0, tags.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(1, tags.CountOf(EmotionTag.Anger));
            Assert.IsTrue(session.Traits.Has(PrototypeContent.TraitHallucination));

            var stacked = new TagSet(emotions: new[] { EmotionTag.Sadness, EmotionTag.Sadness });
            session.ActiveItems.Modify(stacked);
            Assert.AreEqual(1, stacked.CountOf(EmotionTag.Sadness), "슬픔은 1씩 제거된다");
        }

        [Test]
        public void Indifference_IgnoresDepressedEmotionsOfAResultWithOtherPerson_AndGrantsLethargy()
        {
            var indifference = Item("item_indifference");
            var session = Start(Config(new[] { indifference }));

            session.Runner.UseItem(indifference);

            var toOther = new TagSet(TimeTag.Past, new[] { PersonTag.Other, PersonTag.Family },
                new[] { EmotionTag.Sadness, EmotionTag.Sadness, EmotionTag.Disgust, EmotionTag.Fear, EmotionTag.Happiness, EmotionTag.Anger });
            session.ActiveItems.Modify(toOther);
            Assert.AreEqual(0, toOther.CountOf(EmotionTag.Sadness), "겹친 개수까지 전부 무시");
            Assert.AreEqual(0, toOther.CountOf(EmotionTag.Disgust));
            Assert.AreEqual(0, toOther.CountOf(EmotionTag.Fear));
            Assert.AreEqual(1, toOther.CountOf(EmotionTag.Happiness), "흥분 감정은 그대로");
            Assert.AreEqual(1, toOther.CountOf(EmotionTag.Anger));

            var notOther = new TagSet(TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Sadness });
            session.ActiveItems.Modify(notOther);
            Assert.AreEqual(1, notOther.CountOf(EmotionTag.Sadness), "타인이 아닌 결과는 그대로");

            Assert.IsTrue(session.Traits.Has(PrototypeContent.TraitLethargy));
        }

        [Test]
        public void Recollection_RedrawsTheHand_AndGrantsGrandiosity()
        {
            var recollection = Item("item_recollection");
            var session = Start(Config(new[] { recollection }));
            var before = session.Hand.Cards.ToList();

            session.Runner.UseItem(recollection);

            Assert.AreEqual(4, session.Hand.Cards.Count);
            CollectionAssert.AreNotEquivalent(before, session.Hand.Cards.ToList(), "손패 전체가 풀로 돌아갔다 다시 뽑힌다.");
            Assert.IsTrue(session.Traits.Has(PrototypeContent.TraitGrandiosity));
        }

        [Test]
        public void LogicalPersuasion_KeepsOneOfEachDuplicateEmotion()
        {
            var logic = Item("item_logic");
            var session = Start(Config(new[] { logic }));

            session.Runner.UseItem(logic);

            var tags = new TagSet(emotions: new[] { EmotionTag.Sadness, EmotionTag.Sadness, EmotionTag.Sadness, EmotionTag.Anger, EmotionTag.Anger });
            session.ActiveItems.Modify(tags);

            Assert.AreEqual(1, tags.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(1, tags.CountOf(EmotionTag.Anger));
            Assert.AreEqual(0, session.Traits.Traits.Count, "부여하는 특성이 없다.");
        }

        [Test]
        public void GoodSamaritan_RaisesTenOnlyWhileDepressed_AndAlwaysGrantsLethargy()
        {
            var samaritan = Item("item_samaritan");

            var depressed = Start(Config(new[] { samaritan }), heartbeat: 50);
            depressed.Runner.UseItem(samaritan);
            Assert.AreEqual(60, depressed.Heartbeat.Value, "침체 구간(40~70)이면 10 오른다.");
            Assert.IsTrue(depressed.Traits.Has(PrototypeContent.TraitLethargy));

            var stable = Start(Config(new[] { samaritan }), heartbeat: 80);
            stable.Runner.UseItem(samaritan);
            Assert.AreEqual(80, stable.Heartbeat.Value, "침체가 아니면 심박수는 그대로다.");
            Assert.IsTrue(stable.Traits.Has(PrototypeContent.TraitLethargy), "특성 부여는 조건과 무관하다(기획서 문구 그대로).");

            var excited = Start(Config(new[] { samaritan }), heartbeat: 120);
            excited.Runner.UseItem(samaritan);
            Assert.AreEqual(120, excited.Heartbeat.Value, "흥분 구간에서도 오르지 않는다.");
        }

        [Test]
        public void SelectiveMemory_SwapsTheChosenClueForARandomOne_InPlace()
        {
            var selective = Item("item_selective_memory");
            var session = Start(Config(new[] { selective }));
            var hand = session.Hand;
            var chosen = hand.Cards[2];
            var destroyed = new List<ClueInstance>();
            var added = new List<ClueInstance>();
            hand.CardDestroyed += destroyed.Add;
            hand.CardAdded += added.Add;

            Assert.AreEqual(4, session.Runner.GetItemTargets(selective).Count, "보유 단서 4장이 모두 대상이다.");

            session.Runner.UseItem(selective, ItemTarget.Of(chosen));

            Assert.AreEqual(4, hand.Cards.Count);
            Assert.AreNotSame(chosen, hand.Cards[2], "같은 자리에서 교체된다.");
            Assert.AreNotEqual(chosen.Definition.Id, hand.Cards[2].Definition.Id, "방금 고른 단서가 그대로 다시 나오지 않는다.");
            CollectionAssert.AreEqual(new[] { chosen }, destroyed);
            CollectionAssert.AreEqual(new[] { hand.Cards[2] }, added);
        }

        [Test]
        public void SelectiveMemory_CannotBeUsedWhenThePoolIsEmpty()
        {
            var selective = Item("item_selective_memory");
            var fourClues = Enumerable.Range(0, 4).Select(i => Clue($"only{i}", EmotionTag.Love)).ToList();
            var session = Start(Config(new[] { selective }, clues: fourClues));

            Assert.AreEqual(4, session.Hand.Cards.Count);
            Assert.IsFalse(session.Runner.CanUseItem(selective), "손패가 풀을 다 써서 바꿔 올 단서가 없다.");
        }

        [Test]
        public void UseItem_ValidatesTargets()
        {
            var overcome = Item("item_overcome");
            var logic = Item("item_logic");
            var session = Start(Config(new[] { overcome, logic }, starting: PrototypeContent.AntiPast(Polarity)));
            var complex = session.Complexes.Slots[0];

            Assert.Throws<ArgumentException>(() => session.Runner.UseItem(logic, ItemTarget.Of(complex)),
                "대상이 필요 없는 아이템에 대상을 넘기면 오류");
            Assert.Throws<ArgumentException>(() => session.Runner.UseItem(overcome), "대상이 필요한 아이템에 대상이 없으면 오류");
            Assert.Throws<ArgumentException>(() => session.Runner.UseItem(overcome, ItemTarget.Of(session.Hand.Cards[0])),
                "종류가 다른 대상은 오류");

            session.Runner.UseItem(logic);
            Assert.Throws<InvalidOperationException>(() => session.Runner.UseItem(logic), "이미 쓴 아이템은 다시 쓸 수 없다");
        }

        // ------------------------------------------------------------------
        // 특성 6종 + 계산 순서
        // ------------------------------------------------------------------

        private static (TraitBoard Traits, TraitAwareEmotionEvaluator Evaluator) Board(params string[] ids)
        {
            var traits = new TraitBoard(PrototypeContent.Traits());
            foreach (var id in ids) traits.Grant(id);
            return (traits, new TraitAwareEmotionEvaluator(Polarity, traits));
        }

        private static TagSet Tags(params EmotionTag[] emotions) => new(emotions: emotions);

        [Test]
        public void Sensitive_TriplesAndLethargy_Halves()
        {
            Assert.AreEqual(30, Board(PrototypeContent.TraitSensitive).Evaluator.Evaluate(Tags(EmotionTag.Anger)));
            Assert.AreEqual(-30, Board(PrototypeContent.TraitSensitive).Evaluator.Evaluate(Tags(EmotionTag.Fear)));
            Assert.AreEqual(5, Board(PrototypeContent.TraitLethargy).Evaluator.Evaluate(Tags(EmotionTag.Anger)));
            Assert.AreEqual(-5, Board(PrototypeContent.TraitLethargy).Evaluator.Evaluate(Tags(EmotionTag.Fear)), "0.5는 0에서 먼 쪽으로 반올림");
            Assert.AreEqual(15, Board(PrototypeContent.TraitSensitive, PrototypeContent.TraitLethargy).Evaluator.Evaluate(Tags(EmotionTag.Anger)), "×3 × ×1/2");
        }

        [Test]
        public void Hallucination_InvertsTheDirectionOfTheHeartbeatChange()
        {
            var (_, evaluator) = Board(PrototypeContent.TraitHallucination);

            Assert.AreEqual(-10, evaluator.Evaluate(Tags(EmotionTag.Happiness)), "흥분 감정을 침체로 받아들인다");
            Assert.AreEqual(20, evaluator.Evaluate(Tags(EmotionTag.Fear, EmotionTag.Sadness)), "침체 감정을 흥분으로 받아들인다");
        }

        [Test]
        public void Grandiosity_DoublesTheOriginalEmotionCount_BeforeComplexes()
        {
            var traits = new TraitBoard(PrototypeContent.Traits());
            traits.Grant(PrototypeContent.TraitGrandiosity);
            var original = Tags(EmotionTag.Sadness, EmotionTag.Anger);

            var doubled = traits.ApplyToOriginal(original);

            Assert.AreEqual(2, doubled.CountOf(EmotionTag.Sadness));
            Assert.AreEqual(2, doubled.CountOf(EmotionTag.Anger));
            Assert.AreEqual(1, original.CountOf(EmotionTag.Sadness), "원본 태그는 건드리지 않는다.");
        }

        [Test]
        public void SpecialTraits_HalveTheirSideOnly()
        {
            var (_, highFunctioning) = Board(PrototypeContent.TraitHighFunctioningDepression);
            Assert.AreEqual(5, highFunctioning.Evaluate(Tags(EmotionTag.Happiness)), "고기능 우울증: 흥분 영향 ×1/2");
            Assert.AreEqual(-10, highFunctioning.Evaluate(Tags(EmotionTag.Sadness)), "침체 감정은 그대로");

            var (_, hyper) = Board(PrototypeContent.TraitHyperexcitement);
            Assert.AreEqual(-5, hyper.Evaluate(Tags(EmotionTag.Sadness)), "과흥분: 침체 영향 ×1/2");
            Assert.AreEqual(10, hyper.Evaluate(Tags(EmotionTag.Anger)), "흥분 감정은 그대로");
        }

        [Test]
        public void Order_HallucinationRunsBeforeTheSpecialTrait_SoSpecialHalvesWhatIsPerceived()
        {
            // 환각 → 특수 특성 순서: 환각이 극성을 뒤집은 '뒤'의 극성에 특수 특성이 붙는다.
            var (_, evaluator) = Board(PrototypeContent.TraitHallucination, PrototypeContent.TraitHighFunctioningDepression);

            // 행복(흥분) → 환각으로 침체로 받아들임 → 고기능 우울증(흥분 ×1/2)은 해당 없음 → -10
            Assert.AreEqual(-10, evaluator.Evaluate(Tags(EmotionTag.Happiness)));
            // 공포(침체) → 환각으로 흥분으로 받아들임 → 고기능 우울증이 그 흥분을 절반으로 → +5
            Assert.AreEqual(5, evaluator.Evaluate(Tags(EmotionTag.Fear)));
        }

        [Test]
        public void Order_AllNormalTraitsTogether_GrandiosityThenHallucinationThenMultipliers()
        {
            // 원래 감정: 분노 1개. 과대 망상 → 분노 2개, 환각 → 침체 2개(-20), 예민 ×3 → -60, 무력 ×1/2 → -30.
            var traits = new TraitBoard(PrototypeContent.Traits());
            foreach (var id in new[] { PrototypeContent.TraitGrandiosity, PrototypeContent.TraitHallucination,
                         PrototypeContent.TraitSensitive, PrototypeContent.TraitLethargy })
                traits.Grant(id);
            var evaluator = new TraitAwareEmotionEvaluator(Polarity, traits);

            var delta = evaluator.Evaluate(traits.ApplyToOriginal(Tags(EmotionTag.Anger)));

            Assert.AreEqual(-30, delta);
        }

        [Test]
        public void NormalTraits_LastOneTurn_SpecialTraitsDoNotExpireByTurns()
        {
            var traits = new TraitBoard(PrototypeContent.Traits());
            var expired = new List<string>();
            traits.Expired += t => expired.Add(t.Definition.Id);

            traits.Grant(PrototypeContent.TraitSensitive);
            traits.Grant(PrototypeContent.TraitHighFunctioningDepression);
            Assert.AreEqual(1, traits.Traits.First(t => t.Definition.Id == PrototypeContent.TraitSensitive).RemainingTurns);

            traits.TickDurations();
            traits.TickDurations();
            traits.TickDurations();

            CollectionAssert.AreEqual(new[] { PrototypeContent.TraitSensitive }, expired, "일반 특성만 1턴 뒤 만료");
            Assert.IsTrue(traits.Has(PrototypeContent.TraitHighFunctioningDepression));
            Assert.AreEqual(TraitBoard.UntilCured, traits.Traits.Single().RemainingTurns);
        }

        [Test]
        public void ItemTrait_AppliesToTheNextClueOnly()
        {
            var samaritan = Item("item_samaritan");
            var clues = new[] { Clue("a", EmotionTag.Anger), Clue("b", EmotionTag.Anger), Clue("c", EmotionTag.Anger), Clue("d", EmotionTag.Anger) };
            var session = Start(Config(new[] { samaritan }, clues: clues), heartbeat: 50);

            session.Runner.UseItem(samaritan); // 50 → 60, 무력 1턴
            var first = session.Runner.PlayClue(session.Hand.Cards[0]);
            var second = session.Runner.PlayClue(session.Hand.Cards[0]);

            Assert.AreEqual(5, first.HeartbeatDelta, "무력: 분노 +10이 절반");
            Assert.AreEqual(10, second.HeartbeatDelta, "특성은 한 턴만 간다");
            Assert.IsFalse(session.Traits.Has(PrototypeContent.TraitLethargy));
        }

        // ------------------------------------------------------------------
        // 컴플렉스 최대 중첩 초과 → 특수 특성
        // ------------------------------------------------------------------

        private static StageSession FullBoardSession(int heartbeat, IReadOnlyList<ClueDefinition> clues = null) =>
            Start(Config(Array.Empty<ItemDefinition>(), clues: clues ?? Enumerable.Range(0, 8).Select(i => Clue($"n{i}")).ToList(),
                    starting: PrototypeContent.AntiPast(Polarity), complexPool: PrototypeContent.Complexes(Polarity),
                    maxSlots: 1, complexWeight: 100.0),
                heartbeat: heartbeat);

        [Test]
        public void Overflow_InDepressedZone_GrantsHighFunctioningDepression_AndAttachesNothing()
        {
            var session = FullBoardSession(heartbeat: 55);
            var before = session.Complexes.Slots.ToList();

            var report = session.Runner.PlayClue(session.Hand.Cards[0]);

            Assert.IsTrue(report.ComplexOverflowed);
            Assert.IsNull(report.SpawnedComplex, "넘친 컴플렉스는 붙지 않는다(기존 컴플렉스도 밀어내지 않는다).");
            CollectionAssert.AreEqual(before, session.Complexes.Slots.ToList());
            Assert.AreEqual(PrototypeContent.TraitHighFunctioningDepression, report.SpecialTraitGranted?.Id);
            Assert.IsTrue(session.Traits.Has(PrototypeContent.TraitHighFunctioningDepression));
        }

        [Test]
        public void Overflow_InExcitedZone_GrantsHyperexcitement()
        {
            var session = FullBoardSession(heartbeat: 120);

            var report = session.Runner.PlayClue(session.Hand.Cards[0]);

            Assert.IsTrue(report.ComplexOverflowed);
            Assert.AreEqual(PrototypeContent.TraitHyperexcitement, report.SpecialTraitGranted?.Id);
        }

        [Test]
        public void SpecialTrait_IsCured_TheMomentTheHeartbeatEntersTheStableZone()
        {
            var clues = new List<ClueDefinition>
            {
                Clue("n0"), Clue("n1"), Clue("n2"), Clue("n3"),
                // 고기능 우울증이 흥분 영향을 절반으로 줄이므로 +40 → +20: 55에서 75(안정)로 들어선다.
                Clue("boost", EmotionTag.Happiness, EmotionTag.Love, EmotionTag.Anger, EmotionTag.Happiness),
                Clue("n4"), Clue("n5"), Clue("n6"),
            };
            var session = FullBoardSession(heartbeat: 55, clues);
            var cured = new List<string>();
            session.Traits.Expired += t => cured.Add(t.Definition.Id);

            session.Runner.PlayClue(session.Hand.Cards.First(c => c.Definition.Id != "boost"));
            Assert.IsTrue(session.Traits.HasSpecial);
            Assert.AreEqual(55, session.Heartbeat.Value);

            // 부스트 카드가 손패에 없으면 다른 카드를 내며 기다린다.
            var guard = 0;
            while (session.Hand.Cards.All(c => c.Definition.Id != "boost") && guard++ < 3)
                session.Runner.PlayClue(session.Hand.Cards[0]);
            Assume.That(session.Hand.Cards.Any(c => c.Definition.Id == "boost"), "부스트 카드가 손패에 들어와야 한다(시드 의존).");

            var report = session.Runner.PlayClue(session.Hand.Cards.First(c => c.Definition.Id == "boost"));

            Assert.AreEqual(HeartbeatState.Stable, session.Zone.StateOf(session.Heartbeat.Value));
            Assert.IsFalse(session.Traits.HasSpecial, "안정 구간에 들어서는 즉시 치유된다.");
            Assert.AreEqual(1, cured.Count(id => id == PrototypeContent.TraitHighFunctioningDepression));
            Assert.IsFalse(report.ComplexOverflowed, "안정 구간은 컴플렉스 발현 확률이 0이라 다시 넘치지 않는다.");
        }

        [Test]
        public void SpecialTrait_StaysWhileNotStable_AndRefreshesOnEachOverflow()
        {
            var session = FullBoardSession(heartbeat: 55);

            for (var turn = 1; turn <= 3; turn++)
            {
                session.Runner.PlayClue(session.Hand.Cards[0]);
                Assert.IsTrue(session.Traits.Has(PrototypeContent.TraitHighFunctioningDepression), $"턴 {turn} 뒤에도 붙어 있다.");
                Assert.AreEqual(1, session.Traits.Traits.Count, "특성은 겹쳐 쌓이지 않고 갱신된다.");
            }
        }

        [Test]
        public void StableZone_NeverOverflows()
        {
            var session = FullBoardSession(heartbeat: 80);

            for (var turn = 1; turn <= 4; turn++)
            {
                var report = session.Runner.PlayClue(session.Hand.Cards[0]);
                Assert.IsFalse(report.ComplexOverflowed);
            }

            Assert.IsFalse(session.Traits.HasSpecial);
        }

        [Test]
        public void MaxSlots_ComesFromStageConfig_AndDefaultsToThree()
        {
            Assert.AreEqual(3, PrototypeContent.PrototypeStage(Polarity).MaxComplexSlots);
            Assert.AreEqual(3, Start(PrototypeContent.PrototypeStage(Polarity)).Complexes.MaxSlots);
            Assert.AreEqual(1, FullBoardSession(80).Complexes.MaxSlots);
        }
    }
}
