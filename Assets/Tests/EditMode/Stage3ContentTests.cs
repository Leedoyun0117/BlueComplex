using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 기획서 '스테이지 기획 - 스테이지 3_"무제"' 표와 코드 데이터가 어긋나지 않는지, 컴플렉스 12종이 상세 정보대로 움직이는지,
    /// 4개짜리 set(랜덤 2개만 제공)이 실제 스테이지 3 데이터로 지켜지는지 확인한다.
    /// 기댓값은 Stage3Content를 거치지 않고 노션 표를 직접 옮겨 적었다 — 표가 바뀌면 여기부터 고친다.
    /// </summary>
    public class Stage3ContentTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private const TimeTag Past = TimeTag.Past;
        private const TimeTag Present = TimeTag.Present;
        private const TimeTag Future = TimeTag.Future;
        private const PersonTag Family = PersonTag.Family;
        private const PersonTag Other = PersonTag.Other;
        private const PersonTag Friend = PersonTag.Friend;
        private const EmotionTag Sad = EmotionTag.Sadness;
        private const EmotionTag Disgust = EmotionTag.Disgust;
        private const EmotionTag Fear = EmotionTag.Fear;
        private const EmotionTag Happy = EmotionTag.Happiness;
        private const EmotionTag Love = EmotionTag.Love;
        private const EmotionTag Anger = EmotionTag.Anger;

        // ── 단서 표 ─────────────────────────────────────────────────────────────

        private sealed class ClueRow
        {
            public string Name;
            public string SetId;
            public TimeTag[] Times;
            public PersonTag[] Persons;
            public EmotionTag[] Emotions;
            /// <summary>노션 UI상 표시 칸 원문(&lt;br&gt;와 "//" 그대로).</summary>
            public string RawUi;
        }

        private static readonly ClueRow[] ClueTable =
        {
            new ClueRow { Name = "피 묻은 나이프", SetId = "set1", Times = new[] { Present }, Persons = new[] { Other }, Emotions = new[] { Anger },
                RawUi = "피 묻은 나이프<br><br>//<br><br>방금 사용했어. 보기만 해도 그 악마들이 떠올라서 이마가 뜨거워져." },
            new ClueRow { Name = "낙서가 가득한 공책", SetId = null, Times = new[] { Past }, Persons = new[] { Other }, Emotions = new[] { Disgust, Anger },
                RawUi = "낙서가 가득한 공책<br><br>//<br><br>어릴 때 미친 녀석들이 매일 괴롭힌 기록이야. 화내고 혐오해봤자 바뀌는건 없었어. " },
            new ClueRow { Name = "낡은 코트", SetId = null, Times = new[] { Present }, Persons = new[] { Other }, Emotions = new[] { Sad },
                RawUi = "비에 젖은 코트<br><br>//<br><br>B씨의 코트야. " },
            new ClueRow { Name = "시계", SetId = null, Times = new[] { Past, Present, Future }, Persons = new[] { Other }, Emotions = new[] { Happy, Love },
                RawUi = "시계<br><br>//<br><br>알람이 울리면 액자를 깨뜨리거나 마네킹을 찌르는거야. 그러면, B씨의 웃음을 볼 수 있어. 어제도, 오늘도, 그리고 내일도 그러겠지. " },
            new ClueRow { Name = "마네킹", SetId = "set1", Times = new[] { Present }, Persons = new[] { Other }, Emotions = new[] { Anger },
                RawUi = "마네킹<br><br>//<br><br>액자 안의 여자와 닮았어. 가끔 이것 때문에 화를 주체할 수 없을 때가 있어. 지금도 안에서 무언가가 부글거리는  느낌이 들어." },
            new ClueRow { Name = "장소가 표시된 약도", SetId = "set1", Times = new[] { Future }, Persons = new[] { Other }, Emotions = new[] { Disgust },
                RawUi = "저택이 표시된 약도<br><br>//<br><br>내일이면 B씨와 이 곳에 방문 할 거야. 그 악마들이 여기에 모인데. 그것들이 서로 이야기하며 B씨를 괴롭힌다니,   생각만 해도 역겨워." },
            new ClueRow { Name = "꽃다발", SetId = null, Times = new[] { Present }, Persons = new[] { Other }, Emotions = new[] { Love },
                RawUi = "꽃다발<br><br>//<br><br>B씨의 선물이야. 내가 뭘 해낸건지 모르겠지만, 칭찬과 함께 웃으며 건내주었어. 시들지 않도록 오래 간직해야지." },
            new ClueRow { Name = "소프트 아이스크림", SetId = null, Times = new[] { Past }, Persons = new[] { Other }, Emotions = new[] { Happy },
                RawUi = "소프트 아이스크림<br><br>//<br><br>오랜만에 먹는 아이스크림이야. 처음 B씨를 만났을 때가 떠올라. 그 때는 나도 정말 어렸는데." },
            new ClueRow { Name = "낡은 편지", SetId = null, Times = new[] { Past }, Persons = new[] { Family }, Emotions = new[] { Happy },
                RawUi = "낡은 편지<br><br>//<br><br>부모님이 써주신 마지막 생일 편지야. 주머니에 구겨져 있었는데, 오랜만에 보니까 기분이 좋아져." },
            new ClueRow { Name = "액자", SetId = "set1", Times = new[] { Future }, Persons = new[] { Other }, Emotions = new[] { Anger, Fear },
                RawUi = "두 사람의 얼굴이 그려진 액자<br><br>//<br><br>이들은 악마야. B씨가 슬퍼하는 이유일지도 몰라. 내일 그들을 만난다는데, 나도 모르게 화가 날까봐 걱정돼." },
            new ClueRow { Name = "유골함", SetId = null, Times = new[] { Past }, Persons = new[] { Family }, Emotions = new[] { Sad },
                RawUi = "유골함<br><br>//<br><br>..그리워." },
            new ClueRow { Name = "코트", SetId = null, Times = new[] { Past }, Persons = new[] { Family }, Emotions = new[] { Sad, Disgust },
                RawUi = "물기가 남은 코트<br><br>//<br><br>차라리 장례식에 가지 않았으면 좋았을텐데. 매일 밤 그 장면이 나타나는데,  식은 땀에 이불이 젖을 때 까지 꿈 속에서 아무 말도 하지 못한 내가 싫어." }
        };

        private static ClueDefinition ClueNamed(string name) => Stage3Content.Clues().Single(c => c.DisplayName == name);

        [Test]
        public void Clues_AreExactlyTheTwelveFromTheStageTable()
        {
            var clues = Stage3Content.Clues();

            Assert.AreEqual(12, ClueTable.Length);
            Assert.AreEqual(ClueTable.Length, clues.Count);
            CollectionAssert.AreEquivalent(ClueTable.Select(r => r.Name).ToList(), clues.Select(c => c.DisplayName).ToList());
            Assert.AreEqual(clues.Count, clues.Select(c => c.Id).Distinct().Count(), "단서 id는 서로 달라야 한다.");
        }

        [Test]
        public void Clue_MatchesItsTableRow_TagsSetAndUiText()
        {
            foreach (var row in ClueTable)
            {
                var clue = ClueNamed(row.Name);

                CollectionAssert.AreEqual(row.Times, clue.Times, $"{row.Name}: 시간 태그");
                CollectionAssert.AreEquivalent(row.Persons, clue.Persons, $"{row.Name}: 인물 태그");
                CollectionAssert.AreEquivalent(row.Emotions, clue.Emotions, $"{row.Name}: 감정 태그");
                Assert.AreEqual(row.SetId, clue.SetId == null ? null : clue.SetId.Replace("stage3_", ""), $"{row.Name}: set");
                Assert.AreEqual(Stage2ContentTests.FromNotion(row.RawUi), clue.Story, $"{row.Name}: UI상 표시");
            }
        }

        [Test]
        public void Clues_NoneUseTheNonePlaceholderTags_AndIdsDoNotCollideWithOtherStages()
        {
            foreach (var clue in Stage3Content.Clues())
            {
                CollectionAssert.DoesNotContain(clue.Times, TimeTag.None, $"{clue.DisplayName}: 시간");
                CollectionAssert.DoesNotContain(clue.Persons, PersonTag.None, $"{clue.DisplayName}: 인물");
            }

            var others = PrototypeContent.Clues().Concat(Stage2Content.Clues()).Select(c => c.Id).ToList();
            CollectionAssert.IsEmpty(others.Intersect(Stage3Content.Clues().Select(c => c.Id)), "단서 id는 스테이지끼리 겹치지 않는다(해금 장부가 id로 기억한다).");
        }

        [Test]
        public void TheClueWhoseUiNameDiffersFromItsTableName_ShowsTheUiNameInTheStory()
        {
            Assert.IsTrue(ClueNamed("낡은 코트").Story.StartsWith("비에 젖은 코트\n\n"));
            Assert.IsTrue(ClueNamed("코트").Story.StartsWith("물기가 남은 코트\n\n"));
            Assert.AreNotEqual(ClueNamed("낡은 코트").Id, ClueNamed("코트").Id, "낡은 코트와 코트는 서로 다른 단서다.");
            CollectionAssert.IsEmpty(Stage3Content.Clues().Where(c => c.DisplayName == "우산"), "우산은 코트로 바뀌었다(09/25).");
        }

        // ── set ───────────────────────────────────────────────────────────────

        private static readonly string[] Set1Members = { "피 묻은 나이프", "장소가 표시된 약도", "마네킹", "액자" };

        [Test]
        public void Set1_IsTheFourClueSetFromTheTable_AndTheRestHaveNoSet()
        {
            var clues = Stage3Content.Clues();
            var inSet = clues.Where(c => c.SetId != null).ToList();

            CollectionAssert.AreEquivalent(Set1Members, inSet.Select(c => c.DisplayName).ToList());
            Assert.AreEqual(1, inSet.Select(c => c.SetId).Distinct().Count());
            Assert.AreEqual(8, clues.Count(c => c.SetId == null));
        }

        [Test]
        public void Set1_FourCluesInThePool_EveryDrawGivesExactlyTwoOfThem_AndAllFourEventuallyAppear()
        {
            var config = Stage3Content.Stage3(Polarity);
            var seenMembers = new HashSet<string>();
            var seenPairs = new HashSet<string>();
            var setQuarters = 0;

            for (var seed = 1000; seed < 2000; seed++)
            {
                var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), Polarity);
                var added = new List<ClueInstance>();
                session.Hand.CardAdded += added.Add;
                session.Runner.StartStage();

                var guard = 0;
                while (true)
                {
                    if (added.Any(c => c.Definition.SetId != null))
                    {
                        var inHand = session.Hand.Cards.Where(c => c.Definition.SetId != null).ToList();
                        setQuarters++;
                        Assert.AreEqual(2, inHand.Count, $"seed {seed}: 4개짜리 set에서 손패에 2장이 아니라 {inHand.Count}장이 있다.");
                        foreach (var card in inHand) seenMembers.Add(card.Definition.DisplayName);
                        seenPairs.Add(string.Join("+", inHand.Select(c => c.Definition.DisplayName).OrderBy(x => x)));
                    }

                    added.Clear();
                    if (session.Runner.Outcome != StageOutcome.InProgress || guard++ >= config.TotalTurns) break;
                    session.Runner.PlayClue(QuarterBots.ChooseHeuristicCard(session));
                }
            }

            Assert.Greater(setQuarters, 300, "set가 실제로 자주 뽑혀야 검증이 의미 있다.");
            CollectionAssert.AreEquivalent(Set1Members, seenMembers, "네 단서가 모두(어느 시드에선가) 나온다 — 항상 같은 둘이 나오는 게 아니다.");
            Assert.GreaterOrEqual(seenPairs.Count, 4, "짝 조합도 여러 가지로 나온다.");
        }

        // ── 컴플렉스 표 ─────────────────────────────────────────────────────────

        private static readonly (string Name, string Id, string Ui, int Duration)[] ComplexTable =
        {
            ("허망 컴플렉스", "stage3_futility", "과거의 행복한 기억을 전부 슬픔으로 해석합니다.", 5),
            ("무기력 컴플렉스", "stage3_lethargy", "슬픔 감정을 느끼고 있다면 그것이 배가 됩니다.", 3),
            ("스톡홀름 컴플렉스", "stage3_stockholm", "공포를 주는 사람에게 사랑의 감정을 느낍니다.", 3),
            ("밀착 컴플렉스", "stage3_clinging", "타인을 가족으로 받아들입니다.", 4),
            ("반사 컴플렉스", "stage3_reflection", "오랜 시간 받아들인 감정을 더 깊게 느끼지만, 그런 자신의 모습에 혐오감을 가집니다.", 3),
            ("낭떠러지 컴플렉스", "stage3_precipice", "침체 감정을 깊게 느끼고 있다면, 그 감정이 미래까지 지속됩니다.", 3),
            ("기대 불안 컴플렉스", "stage3_expectation_anxiety", "타인이 자신에 대한 기대로 행복해한다면, 미래의 일에 대해 혐오를 느낍니다.", 4),
            ("자아 부정 컴플렉스", "stage3_self_denial", "지금 느끼는 감정과 반대되는 감정을 함께 느낍니다.", 5),
            ("부모 애착 컴플렉스", "stage3_parental_attachment", "가족에 관한 행복한 기억이 나면, 지금 느끼는 침체되는 감정을 모두 무시하고 행복을 느낍니다.", 4),
            ("시간 혼합 컴플렉스", "stage3_time_mixing", "과거의 기억을 현재로 착각합니다.", 4),
            ("과거 회피 컴플렉스", "stage3_past_avoidance", "과거의 감정에 슬픔과 혐오감을 느낍니다.", 4),
            ("시간 해석 컴플렉스", "stage3_time_interpretation", "현재나 미래의 우울한 감정에 분노를 느낍니다.", 3)
        };

        [Test]
        public void Complexes_AreExactlyTheTwelveFromTheStageTable_WithUiTextAndDurations_AndNoBlankComplex()
        {
            var complexes = Stage3Content.Complexes(Polarity);

            Assert.AreEqual(ComplexTable.Length, complexes.Count);
            foreach (var (name, id, ui, duration) in ComplexTable)
            {
                var complex = complexes.Single(c => c.DisplayName == name);
                Assert.AreEqual(id, complex.Id, $"{name}: id");
                Assert.AreEqual(ui, complex.Description, $"{name}: UI상 표시");
                Assert.AreEqual(duration, complex.DefaultDuration, $"{name}: 지속 시간(턴)");
            }

            Assert.AreEqual(complexes.Count, complexes.Select(c => c.Id).Distinct().Count(), "컴플렉스 id는 서로 달라야 한다.");
            Assert.IsFalse(complexes.Any(c => c.DisplayName.Contains("백지")), "백지 컴플렉스는 노션이 비어 있어 아직 없다.");
        }

        [Test]
        public void ComplexIds_NeverCollideWithStage1Or2_EspeciallyTheThreeStockholms()
        {
            var earlier = PrototypeContent.Complexes(Polarity).Concat(Stage2Content.Complexes(Polarity)).Select(c => c.Id).ToList();
            var stage3 = Stage3Content.Complexes(Polarity).Select(c => c.Id).ToList();

            CollectionAssert.IsEmpty(earlier.Intersect(stage3));
            CollectionAssert.Contains(earlier, "complex_stockholm");
            CollectionAssert.Contains(stage3, "stage3_stockholm");
        }

        [Test]
        public void ReactionLines_AreInTheAsset_ReactionLineFirstThenVariations_VerbatimFromTheTable()
        {
            var expected = new Dictionary<string, string[]>
            {
                ["stage3_futility"] = new[] { "옛날의 행복이 무슨 소용이겠어?", "그때 행복했다고 해서 뭐가 달라져? 지금은 이렇게 됐는데.", "좋은 기억이면 뭐 해. 떠올릴수록 더 허전하기만 한데.", "차라리 행복하지 않았으면 덜 아팠을까?", "웃고 있었네, 저때는. …그래서 더 슬픈 건가.", "돌아갈 수도 없는 행복을 계속 기억해서 뭐 하겠어." },
                ["stage3_lethargy"] = new[] { "더 깊이 가라앉는 기분이야.", "이제 올라갈 힘도 없어.", "슬픈 건 알겠는데, 뭘 해야 할지도 모르겠어. 그냥 이대로 있고 싶어.", "조금 괜찮아지는가 싶었는데… 역시 아니었나 봐.", "몸까지 무거워진 기분이야. 아무것도 하고 싶지 않아.", "그만 울고 싶은데… 그것조차 귀찮아." },
                ["stage3_stockholm"] = new[] { "조금 무서웠지만 내 마음 속에 계속 남아있는 건, 좋은 감정이 아닐까?", "새장 속의 새는 날개를 다쳤어. 처음이 어떠했건, 결국엔 새장이 새를 위한 공간이 된거야." },
                ["stage3_clinging"] = new[] { "갑자기 친근하게 느껴져.", "평범한 기억이 쌓여서 발목까지 덮었어. 나아가려 할 때, 계속 한 켠에서 느껴지는 걸." },
                ["stage3_reflection"] = new[] { "왜 사라지지 않는걸까? 별로 좋은 감정도 아닌데 말이야." },
                ["stage3_precipice"] = new[] { "지금의 우울함이 미래까지 이어질 것만 같아.", "이 기분… 앞으로도 계속 이러면 어떡하지?", "오늘만 그런 게 아니야. 내일도, 그다음 날도 계속 이럴 것 같아.", "끝이 안 보여. 계속 아래로 떨어지는 것 같아.", "언제까지 버텨야 하는 거지? 나아질 거라는 느낌이 전혀 없어.", "잠깐만. 잠시 우울한 게 아니라면? 정말 평생 이렇게 살아야 한다면? 그럼 어떡하지?" },
                ["stage3_expectation_anxiety"] = new[] { "왜 나에게 기대를 품는 걸까? 내일이 오지 않았으면 좋겠어." },
                ["stage3_self_denial"] = new[] { "이 감정은 진짜 내가 아니야.", "내가 느끼는 진짜 감정은 정 반대라고. 이건.. 내가 아니야." },
                ["stage3_parental_attachment"] = new[] { "다른 건 중요하지 않아. 그냥, 옛날에 부모님과 보냈던 추억이 그리워.", "다른 건 잘 기억도 안 나. 그냥… 함께  있었던 때가 제일 선명해.", "그때로 돌아가고 싶어. 식탁도, 목소리도, 전부 그대로였던 때로.", "이상하게 그 기억만 떠올리면 마음이 놓여. 집에 돌아온 것처럼.", "잠깐만 더 떠올리고 있을래. 여기서 나오고 싶지 않아." },
                ["stage3_time_mixing"] = new[] { "확실히 지나간 일이지?", "이미 지나간 일인데, 방금 겪은 것 처럼 생생하게 남아있어.", "지금 눈 앞에서 일어나고 있어. 그 때 나는 왜 가만히 있었지? 내가 왜 지금 그대로인 거지? 아니야. 내 눈 앞에서, 지금, 보인다고." },
                ["stage3_past_avoidance"] = new[] { "지금의 나와 과거의 나는 완전히 다른 사람 같아. 어색하고, 혐오스러워." },
                ["stage3_time_interpretation"] = new[] { "슬픈 개가 나를 물고 놓아주지 않아. 발로 차버릴 수 있다면 좋을 텐데." }
            };

            var actual = Stage2ContentTests.ParseReactionAsset();
            foreach (var pair in expected)
            {
                Assert.IsTrue(actual.ContainsKey(pair.Key), $"{pair.Key}: 에셋에 항목이 없다.");
                CollectionAssert.AreEqual(pair.Value, actual[pair.Key], pair.Key);
            }

            Assert.AreEqual(ComplexTable.Length, actual.Keys.Count(k => k.StartsWith("stage3_")));
            foreach (var (_, id, _, _) in ComplexTable)
                Assert.IsTrue(actual.ContainsKey(id) && actual[id].Count >= 1, $"{id}: 반응 대사(첫 줄)는 표에 모두 있다.");
        }

        // ── 컴플렉스 상세 정보 ──────────────────────────────────────────────────

        private static TagSet Make(TimeTag[] times, PersonTag[] persons, params EmotionTag[] emotions) => new(times, persons, emotions);

        private static bool Apply(ComplexDefinition complex, TagSet tags) => complex.TryInterpret(new ComplexContext(tags));

        private static ComplexDefinition Complex(string id) => Stage3Content.Complexes(Polarity).Single(c => c.Id == id);

        private static void AssertEmotions(TagSet tags, string message, params (EmotionTag Emotion, int Count)[] expected)
        {
            var actual = tags.Emotions.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)).ToList();
            var want = expected.Where(e => e.Count > 0).OrderBy(e => e.Emotion).ToList();
            CollectionAssert.AreEqual(want, actual, message);
        }

        private static readonly PersonTag[] Nobody = new PersonTag[0];

        [Test]
        public void Futility_PastAndHappinessOrLove_TurnsThemIntoSadness()
        {
            var complex = Complex("stage3_futility");

            var both = Make(new[] { Past }, new[] { Other }, Happy, Love, Fear);
            Assert.IsTrue(Apply(complex, both));
            AssertEmotions(both, "행복·사랑 → 슬픔(공포는 그대로)", (Sad, 2), (Fear, 1));

            var stacked = Make(new[] { Past }, Nobody, Happy, Happy, Sad);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "겹친 행복도 전부 슬픔으로, 이미 있는 슬픔과 합쳐진다", (Sad, 3));

            Assert.IsFalse(Apply(complex, Make(new[] { Present }, Nobody, Happy)), "과거가 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody, Sad, Anger)), "행복/사랑이 없으면 발동하지 않는다");
            Assert.IsTrue(Apply(complex, Make(new[] { Past, Present }, Nobody, Love)), "과거+현재 단서도 과거 태그가 있으면 발동한다");
        }

        [Test]
        public void Lethargy_WithSadness_AddsTwoSadness()
        {
            var complex = Complex("stage3_lethargy");

            var tags = Make(new[] { Present }, new[] { Other }, Sad, Fear);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "슬픔 1 → 3", (Sad, 3), (Fear, 1));

            var stacked = Make(new[] { Past }, Nobody, Sad, Sad);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "슬픔 2 → 4", (Sad, 4));

            var noSadness = Make(new[] { Past }, Nobody, Fear);
            Assert.IsFalse(Apply(complex, noSadness), "슬픔이 없으면 발동하지 않는다");
            AssertEmotions(noSadness, "슬픔을 새로 만들지 않는다", (Fear, 1));
        }

        [Test]
        public void Stockholm_OtherAndFear_AddsLoveAndKeepsFear()
        {
            var complex = Complex("stage3_stockholm");

            var tags = Make(new[] { Future }, new[] { Other }, Anger, Fear);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "사랑 +1 — 공포는 그대로", (Anger, 1), (Fear, 1), (Love, 1));

            Assert.IsFalse(Apply(complex, Make(new[] { Future }, new[] { Family }, Fear)), "타인이 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Future }, new[] { Other }, Anger)), "공포가 없으면 발동하지 않는다");
        }

        [Test]
        public void Clinging_Other_OtherMinusOneFamilyPlusOne()
        {
            var complex = Complex("stage3_clinging");

            var tags = Make(new[] { Present }, new[] { Other }, Sad);
            Assert.IsTrue(Apply(complex, tags));
            CollectionAssert.AreEquivalent(new[] { Family }, tags.Persons, "타인 1개 → 0개(사라짐), 가족 +1");
            Assert.AreEqual(1, tags.CountOfPerson(Family));
            AssertEmotions(tags, "감정은 그대로", (Sad, 1));

            var stackedOther = Make(new[] { Present }, new[] { Other });
            stackedOther.AddPerson(Other);
            Assert.IsTrue(Apply(complex, stackedOther));
            Assert.AreEqual(1, stackedOther.CountOfPerson(Other), "타인 2개 → 1개");
            Assert.AreEqual(1, stackedOther.CountOfPerson(Family));

            var withFamily = Make(new[] { Present }, new[] { Other, Family });
            Assert.IsTrue(Apply(complex, withFamily));
            Assert.IsFalse(withFamily.HasPerson(Other));
            Assert.AreEqual(2, withFamily.CountOfPerson(Family), "이미 있는 가족에는 개수가 쌓인다");

            Assert.IsTrue(Apply(complex, Make(new[] { Present }, new[] { Other })), "감정이 없어도 조건은 타인 태그뿐이다");
            Assert.IsFalse(Apply(complex, Make(new[] { Present }, new[] { Family, Friend }, Sad)), "타인이 없으면 발동하지 않는다");
        }

        [Test]
        public void Reflection_PastAndEmotion_DoublesEmotionsThenAddsOneDisgust()
        {
            var complex = Complex("stage3_reflection");

            var tags = Make(new[] { Past }, new[] { Other }, Sad, Happy);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "감정 ×2 뒤에 혐오 +1", (Sad, 2), (Happy, 2), (Disgust, 1));

            var withDisgust = Make(new[] { Past }, Nobody, Disgust);
            Assert.IsTrue(Apply(complex, withDisgust));
            AssertEmotions(withDisgust, "혐오 1 → 2(×2)로 끝나고 +1 → 3: 곱하기가 먼저다", (Disgust, 3));

            var stacked = Make(new[] { Past }, Nobody, Sad, Sad);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "중첩 포함 ×2", (Sad, 4), (Disgust, 1));

            Assert.IsFalse(Apply(complex, Make(new[] { Present }, Nobody, Sad)), "과거가 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody)), "감정이 없으면 발동하지 않는다");
        }

        [Test]
        public void Precipice_StackedDepressedEmotion_AddsFutureAndKeepsTheOtherTimes()
        {
            var complex = Complex("stage3_precipice");

            var tags = Make(new[] { Past }, new[] { Family }, Sad, Sad);
            Assert.IsTrue(Apply(complex, tags));
            CollectionAssert.AreEqual(new[] { Past, Future }, tags.Times, "미래가 '추가'된다 — 과거는 그대로");
            AssertEmotions(tags, "감정은 그대로", (Sad, 2));

            var mixed = Make(new[] { Present }, Nobody, Disgust, Disgust, Disgust, Happy);
            Assert.IsTrue(Apply(complex, mixed), "혐오 3중첩도 침체 중첩이다");
            CollectionAssert.AreEqual(new[] { Present, Future }, mixed.Times);

            var alreadyFuture = Make(new[] { Future }, Nobody, Fear, Fear);
            Assert.IsTrue(Apply(complex, alreadyFuture));
            CollectionAssert.AreEqual(new[] { Future }, alreadyFuture.Times, "이미 있는 미래는 중복되지 않는다");

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody, Sad, Disgust)), "서로 다른 침체 감정이 하나씩 있는 것은 '중첩'이 아니다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody, Happy, Happy)), "흥분 감정의 중첩은 해당하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody, Sad)), "침체 감정 1개");
        }

        [Test]
        public void ExpectationAnxiety_OtherAndHappiness_AddsFutureAndDisgust()
        {
            var complex = Complex("stage3_expectation_anxiety");

            var tags = Make(new[] { Past }, new[] { Other }, Happy);
            Assert.IsTrue(Apply(complex, tags));
            CollectionAssert.AreEqual(new[] { Past, Future }, tags.Times, "미래 +1(시간 태그 추가)");
            AssertEmotions(tags, "혐오 +1, 행복은 그대로", (Happy, 1), (Disgust, 1));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family }, Happy)), "타인이 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Other }, Love)), "행복이 아니라 사랑이면 발동하지 않는다");
        }

        [Test]
        public void SelfDenial_MoreDepressed_AddsHappiness_MoreExcited_AddsDisgust_TieDoesNothing()
        {
            var complex = Complex("stage3_self_denial");

            var depressed = Make(new[] { Past }, Nobody, Sad, Fear, Happy);
            Assert.IsTrue(Apply(complex, depressed));
            AssertEmotions(depressed, "침체 2 > 흥분 1 → 행복 +1", (Sad, 1), (Fear, 1), (Happy, 2));

            var excited = Make(new[] { Past }, Nobody, Anger, Love, Sad);
            Assert.IsTrue(Apply(complex, excited));
            AssertEmotions(excited, "흥분 2 > 침체 1 → 혐오 +1", (Anger, 1), (Love, 1), (Sad, 1), (Disgust, 1));

            var onlyDepressed = Make(new[] { Past }, Nobody, Sad);
            Assert.IsTrue(Apply(complex, onlyDepressed));
            AssertEmotions(onlyDepressed, "침체만 있어도 행복 +1", (Sad, 1), (Happy, 1));
        }

        [Test]
        public void SelfDenial_Tie_DoesNotFire_AndCountsStackedTags()
        {
            var complex = Complex("stage3_self_denial");

            var tie = Make(new[] { Past }, Nobody, Sad, Happy);
            Assert.IsFalse(Apply(complex, tie), "침체 1 = 흥분 1 — 어느 쪽도 많지 않다");
            AssertEmotions(tie, "그대로", (Sad, 1), (Happy, 1));

            var stackedExcited = Make(new[] { Past }, Nobody, Happy, Happy, Sad);
            Assert.IsTrue(Apply(complex, stackedExcited));
            AssertEmotions(stackedExcited, "행복 2개(중첩) > 슬픔 1개 → 혐오 +1", (Happy, 2), (Sad, 1), (Disgust, 1));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody)), "감정이 없으면 발동하지 않는다");
        }

        [Test]
        public void ParentalAttachment_FamilyAndHappiness_RemovesEveryDepressedEmotionAndAddsHappiness()
        {
            var complex = Complex("stage3_parental_attachment");

            var tags = Make(new[] { Past }, new[] { Family }, Happy, Sad, Sad, Fear, Anger);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "슬픔·공포(중첩 포함) 제거, 행복 +1, 분노는 그대로", (Happy, 2), (Anger, 1));

            var plain = Make(new[] { Past }, new[] { Family }, Happy);
            Assert.IsTrue(Apply(complex, plain));
            AssertEmotions(plain, "침체가 없어도 행복 +1", (Happy, 2));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Other }, Happy, Sad)), "가족이 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family }, Love, Sad)), "행복이 없으면 발동하지 않는다");
        }

        [Test]
        public void TimeMixing_Past_RemovesPastAndAddsPresent()
        {
            var complex = Complex("stage3_time_mixing");

            var past = Make(new[] { Past }, new[] { Family }, Sad);
            Assert.IsTrue(Apply(complex, past));
            CollectionAssert.AreEqual(new[] { Present }, past.Times);
            AssertEmotions(past, "감정은 그대로", (Sad, 1));

            var clock = Make(new[] { Past, Present, Future }, new[] { Other }, Happy);
            Assert.IsTrue(Apply(complex, clock));
            CollectionAssert.AreEqual(new[] { Present, Future }, clock.Times, "과거만 떨어지고 이미 있는 현재는 중복되지 않는다");

            var noEmotion = Make(new[] { Past }, Nobody);
            Assert.IsTrue(Apply(complex, noEmotion), "조건은 과거 태그뿐이다");

            Assert.IsFalse(Apply(complex, Make(new[] { Present, Future }, Nobody, Sad)), "과거가 아니면 발동하지 않는다");
        }

        [Test]
        public void PastAvoidance_PastAndEmotion_AddsSadnessAndDisgust()
        {
            var complex = Complex("stage3_past_avoidance");

            var tags = Make(new[] { Past }, new[] { Other }, Happy);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "슬픔 +1, 혐오 +1", (Happy, 1), (Sad, 1), (Disgust, 1));

            var stacked = Make(new[] { Past }, Nobody, Sad, Disgust);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "이미 있는 감정에 쌓인다", (Sad, 2), (Disgust, 2));

            Assert.IsFalse(Apply(complex, Make(new[] { Present }, Nobody, Happy)), "과거가 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody)), "감정이 없으면 발동하지 않는다");
        }

        [Test]
        public void TimeInterpretation_PresentOrFutureAndDepressed_AddsAnger()
        {
            var complex = Complex("stage3_time_interpretation");

            var present = Make(new[] { Present }, new[] { Other }, Sad);
            Assert.IsTrue(Apply(complex, present));
            AssertEmotions(present, "분노 +1", (Sad, 1), (Anger, 1));

            var future = Make(new[] { Future }, new[] { Other }, Disgust, Happy);
            Assert.IsTrue(Apply(complex, future));
            AssertEmotions(future, "미래 + 혐오", (Disgust, 1), (Happy, 1), (Anger, 1));

            var clock = Make(new[] { Past, Present, Future }, new[] { Other }, Fear);
            Assert.IsTrue(Apply(complex, clock), "과거+현재+미래 단서도 현재/미래가 있으면 발동한다");

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, Nobody, Sad)), "과거만이면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Present }, Nobody, Happy, Anger)), "침체 감정이 없으면 발동하지 않는다");
        }

        [Test]
        public void EveryComplexOnEveryClue_NeverLeavesAnEmptyOrNonPositiveTag()
        {
            foreach (var complex in Stage3Content.Complexes(Polarity))
            foreach (var clue in Stage3Content.Clues())
            {
                var tags = clue.CreateOriginalTagSet();
                Apply(complex, tags);

                foreach (var pair in tags.Emotions) Assert.Greater(pair.Value, 0, $"{complex.DisplayName} × {clue.DisplayName}: 감정 {pair.Key}");
                foreach (var person in tags.Persons) Assert.Greater(tags.CountOfPerson(person), 0, $"{complex.DisplayName} × {clue.DisplayName}: 인물 {person}");
                Assert.IsTrue(tags.Times.Count > 0, $"{complex.DisplayName} × {clue.DisplayName}: 시간 태그가 사라졌다");
            }
        }

        [Test]
        public void TagSet_RemovePersonAndRemoveTime_BehaveAsCounters()
        {
            var tags = Make(new[] { Past, Present }, new[] { Other }, Sad);
            tags.AddPerson(Other, 2);

            tags.RemovePerson(Other);
            Assert.AreEqual(2, tags.CountOfPerson(Other));
            tags.RemovePerson(Other, 5);
            Assert.IsFalse(tags.HasPerson(Other), "0 이하가 되면 종류째 사라진다");
            tags.RemovePerson(Friend);
            tags.RemovePerson(Other, 0);

            tags.RemoveTime(Past);
            tags.RemoveTime(Future);
            CollectionAssert.AreEqual(new[] { Present }, tags.Times);

            var clone = Make(new[] { Past }, new[] { Family }).Clone();
            clone.RemovePerson(Family);
            clone.RemoveTime(Past);
            Assert.IsFalse(clone.HasPerson(Family));
            Assert.IsFalse(clone.HasTime(Past));
        }

        // ── 스테이지 구성 ───────────────────────────────────────────────────────

        [Test]
        public void Stage_Is15TurnsAsFiveQuartersOfThreeTurns_With3Keys_AndTheStage3Pools()
        {
            var config = Stage3Content.Stage3(Polarity);

            Assert.AreEqual("stage_3", config.Id);
            Assert.AreEqual("무제", config.DisplayName);
            Assert.AreEqual(15, config.TotalTurns);
            Assert.AreEqual(5, config.Quarters.QuarterCount);
            Assert.AreEqual(3, config.Quarters.TurnsPerQuarter);
            Assert.AreEqual(3, config.RequiredKeys);
            Assert.AreEqual(12, config.Clues.Count);
            Assert.AreEqual(12, config.ComplexPool.Count);
            Assert.AreEqual(12, config.ItemPool.Count);
            Assert.IsTrue(config.RandomStartingComplex);
            Assert.LessOrEqual(config.Quarters.TurnsPerQuarter, ClueHand.HandSize,
                "손패는 쿼터 시작에만 채워지므로 쿼터당 턴 수가 손패 크기를 넘으면 마지막 턴에 낼 카드가 없다.");
        }

        [Test]
        public void KeyZones_AreJudgedAtTheFiveQuarterEnds_LikeStage2_AcrossManySeeds()
        {
            var config = Stage3Content.Stage3(Polarity);
            var stage2 = Stage2Content.Stage2(Polarity);
            CollectionAssert.AreEqual(stage2.Quarters.QuarterEndTurns().ToList(), config.Quarters.QuarterEndTurns().ToList(), "스테이지 2와 같은 분기점 구조");
            CollectionAssert.AreEqual(new[] { 3, 6, 9, 12, 15 }, config.Quarters.QuarterEndTurns().ToList());

            for (var seed = 0; seed < 200; seed++)
            {
                var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), Polarity);
                session.Runner.StartStage();
                CollectionAssert.AreEquivalent(new[] { 3, 6, 9, 12, 15 }, session.Keys.Zones.Keys.ToList(), $"seed {seed}: 키 구역은 쿼터의 마지막 턴마다 하나씩");
            }
        }

        [Test]
        public void PersuasionTargets_AreAllComplexesOfThisStage_AndOnlyThoseThatChangeDepressedEmotions()
        {
            var config = Stage3Content.Stage3(Polarity);
            var ids = config.ComplexPool.Select(c => c.Id).ToList();
            var targets = config.ItemParameters[PrototypeContent.PersuasionTargetsKey];

            foreach (var id in targets) CollectionAssert.Contains(ids, id);
            CollectionAssert.AreEquivalent(new[]
            {
                "stage3_futility", "stage3_lethargy", "stage3_reflection", "stage3_expectation_anxiety",
                "stage3_self_denial", "stage3_parental_attachment", "stage3_past_avoidance"
            }, targets);
        }

        [Test]
        public void Stage1And2Configs_AreUntouched()
        {
            var stage1 = PrototypeContent.PrototypeStage(Polarity);
            Assert.AreEqual(12, stage1.TotalTurns);
            Assert.AreEqual(10, stage1.ComplexPool.Count);

            var stage2 = Stage2Content.Stage2(Polarity);
            Assert.AreEqual(15, stage2.TotalTurns);
            Assert.AreEqual(14, stage2.ComplexPool.Count);
        }

        [Test]
        public void EveryRunOfTheStageFinishes_WithoutExceptions_AcrossManySeeds()
        {
            var config = Stage3Content.Stage3(Polarity);
            for (var seed = 0; seed < 300; seed++)
            {
                var result = QuarterBots.RunStage(config, seed, "휴리스틱", QuarterBots.ChooseHeuristicCard, null);
                Assert.AreNotEqual(StageOutcome.InProgress, result.Outcome, $"seed {seed}");
            }
        }

        // ── 밸런스 측정(참고용) ─────────────────────────────────────────────────

        [Test]
        public void Balance_ThousandSeeds_IsMeasuredAndEveryRunFinishes()
        {
            var config = Stage3Content.Stage3(Polarity);
            var seeds = Enumerable.Range(1000, 1000).ToArray();

            var naive = seeds.Select(s => QuarterBots.RunStage(config, s, "무전략(첫 장)", QuarterBots.ChooseFirstCard, null)).ToList();
            var heuristic = seeds.Select(s => QuarterBots.RunStage(config, s, "휴리스틱(쿼터 인지)", QuarterBots.ChooseHeuristicCard, null)).ToList();

            var fullInfo = seeds.Select(s => KnowledgeBots.RunOnce(config, s, KnowledgeBots.FullyRevealedLedger(config.Clues), QuarterBots.ChooseHeuristicCard)).ToList();
            var fullInfoItems = seeds.Select(s => KnowledgeBots.RunOnce(config, s, KnowledgeBots.FullyRevealedLedger(config.Clues), QuarterBots.ChooseHeuristicCard, useItems: true)).ToList();

            Debug.Log($"[스테이지 3 밸런스] {config.TotalTurns}턴 = {config.Quarters.QuarterCount}쿼터 × {config.Quarters.TurnsPerQuarter}턴, 키 {config.RequiredKeys}개, 폭 {config.KeyWidth}, {seeds.Length}시드\n" +
                      $"  무전략          : {Describe(naive)}\n  휴리스틱        : {Describe(heuristic)}\n" +
                      $"  완전 정보       : 클리어율 {fullInfo.Count(r => r.Cleared) * 100.0 / seeds.Length:F1}% · 평균 키 {fullInfo.Average(r => r.KeysCollected):F2}개\n" +
                      $"  완전 정보+아이템: 클리어율 {fullInfoItems.Count(r => r.Cleared) * 100.0 / seeds.Length:F1}% · 평균 키 {fullInfoItems.Average(r => r.KeysCollected):F2}개 · 아이템 평균 {fullInfoItems.Average(r => r.ItemsUsed):F1}개");

            Assert.IsTrue(naive.Concat(heuristic).All(r => r.Outcome != StageOutcome.InProgress));
        }

        private static string Describe(IReadOnlyList<BotRunResult> results)
        {
            var judged = results.Sum(r => r.KeyAttempts);
            var hits = results.Sum(r => r.KeyHits);
            return $"클리어율 {results.Count(r => r.Outcome == StageOutcome.Cleared) * 100.0 / results.Count:F1}% · " +
                   $"키 성공률 {hits * 100.0 / System.Math.Max(1, judged):F1}% ({hits}/{judged}) · 평균 {results.Average(r => r.TurnsPlayed):F1}턴 · " +
                   $"평균 키 {results.Average(r => r.KeysCollected):F2}개";
        }
    }
}
