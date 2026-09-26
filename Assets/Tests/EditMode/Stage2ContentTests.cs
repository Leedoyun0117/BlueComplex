using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    /// 기획서 '스테이지 기획 - 스테이지 2_"천사"' 표와 코드 데이터가 어긋나지 않는지, 컴플렉스 14종이 상세 정보대로 움직이는지,
    /// set(같은 set 단서 동반 등장)이 실제 스테이지 2 데이터로 지켜지는지 확인한다.
    /// 기댓값은 Stage2Content를 거치지 않고 노션 표를 직접 옮겨 적었다 — 표가 바뀌면 여기부터 고친다.
    /// </summary>
    public class Stage2ContentTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private const TimeTag Past = TimeTag.Past;
        private const TimeTag Present = TimeTag.Present;
        private const TimeTag Future = TimeTag.Future;
        private const PersonTag Family = PersonTag.Family;
        private const PersonTag Other = PersonTag.Other;
        private const PersonTag Friend = PersonTag.Friend;
        private const PersonTag Lover = PersonTag.Lover;
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
            new ClueRow { Name = "지원 신청서", SetId = "set1", Times = new[] { Past }, Persons = new[] { Other }, Emotions = new[] { Happy },
                RawUi = "아동 지원 신청서 - 날짜<br><br>19XX년 1월 19일.<br><br>본인은 사회적 약자 아동 후원 및 보호를 신청합니다.<br><br>서명 B.<br><br>//<br><br>B씨 덕분에 밀린 집세를 내고 집에 머무를 수 있게 되었어. 정말 고마운 분이야. 가족은 아니지만 그 만큼 친절하게 대해주시니까." },
            new ClueRow { Name = "사망 통지서", SetId = null, Times = new[] { Past }, Persons = new[] { Family }, Emotions = new[] { Sad },
                RawUi = "사망 통지서 <br><br>호시노 유키(모)와 하시모토 유키(부)의 사망을 통지합니다.<br><br>사인 : 익사<br><br>센 브람스 병원 19XX년 3월 20일<br><br>//<br><br>…" },
            new ClueRow { Name = "소프트 아이스크림", SetId = null, Times = new[] { Past, Present }, Persons = new[] { Other }, Emotions = new[] { Happy },
                RawUi = "소프트 아이스크림. <br><br>//<br><br>B씨가 방금 놓고 간 아이스크림이야. 이 아이스크림이 없었다면 B씨의 도움을 받지 못했겠지." },
            new ClueRow { Name = "B의 편지", SetId = "set1", Times = new[] { Past }, Persons = new[] { Other, Family }, Emotions = new[] { Sad },
                RawUi = "유키, 이번 주말에 가족들과 바닷가로 놀러가는 거 어떠니? 어제 가봤는데 날씨가 정말 좋더구나.<br>…<br>네가 하고싶어했던 ‘숨바꼭질’ 놀이를 해보렴. 바닷가에서 놀다가 아무도 모르게 바위 뒤에 숨는거지. <br>네가 가족과의 추억을 더 만들면 좋겠어. 그게 내 기쁨이란다.<br><br>B씨가. 19XX.3.18<br><br>//<br><br>B씨는 날씨를 몰랐던 거겠지. 그래도.. 내 잘못은 아닐거야." },
            new ClueRow { Name = "물이 담긴 컵", SetId = null, Times = new[] { Past }, Persons = new[] { Family }, Emotions = new[] { Sad, Fear },
                RawUi = "물이 담긴 컵<br><br>//<br>뭔가 두려워. 마실 수 없어.. " },
            new ClueRow { Name = "TV 뉴스", SetId = "set3", Times = new[] { Past }, Persons = new[] { Family, Other }, Emotions = new[] { Sad, Fear, Disgust },
                RawUi = "TV 재난 보도 채널<br><br>두 달전, 지역을 강타한 폭풍의 여파가 아직도 가시지 않고 있습니다. 폭풍의 징조는 몇 주 전부터 예고되었지만, 충분한 준비에도 불구하고 치명적인 피해를 피할 수 없었습니다.<br>—…<br>19XX년 4월 19일 뉴스를 마칩니다.<br><br>//<br><br>그 날 바닷가에 가는게 아니었는데. 내가 정말 멍청했어.. 이제 되돌릴수도 없지만.. 겁쟁이 같이 구하러 뛰어들지도 못했으면서 뭘 후회 하는 걸까?" },
            new ClueRow { Name = "식탁 맡의 쪽지", SetId = "set2", Times = new[] { Past, Present }, Persons = new[] { Other }, Emotions = new[] { Happy },
                RawUi = "구겨진 쪽지. <br> <br> 1. 식사 전에 이 액자를 포크로 3번 찔러 깨뜨린다.<br>  2. “시계”가 울리면 액자 속 종이를 꺼내 완전히 찢는다.<br><br>유키, 이 규칙은 너를 위한것이지만, 나를 위한 것이기도 해. 언제나 지켜주면 좋겠구나.<br><br>//<br> 이 쪽지가 눈에 보이면 식사 시간이란 뜻이지! <br>B씨의 규칙이 뭔가 이상하지만, 그의 요리는 정말 맛있어. 내일은 어떤 요리를 만들어주실까?" },
            new ClueRow { Name = "깨진 액자", SetId = "set2", Times = new[] { Past, Future }, Persons = new[] { Other }, Emotions = new[] { Disgust, Fear },
                RawUi = "두 인물의 사진이 담긴 액자. 겉면이 깨지고 사진이 나뒹굴고있다.<br><br>//<br><br>B씨는 이 사람들이 악마와 같다고 매일 말하는데, 악마가 뭘까?<br><br>B씨를 화나게 했다면 좋은 사람들은 아니겠지. 이 사람은 왜 이렇게 무섭게 생긴거야?  으, 나중에 만날 일이 없으면 좋겠어.<br>" },
            new ClueRow { Name = "시든 꽃", SetId = null, Times = new[] { Past }, Persons = new[] { Friend }, Emotions = new[] { Love, Sad },
                RawUi = "시든 꽃<br><br>//<br><br>마지막으로 본 지 한 달이 넘은 것 같아. 그 학교에서도 잘 지내고 있을까? 다시 만나고 싶어." },
            new ClueRow { Name = "시계", SetId = null, Times = new[] { Present }, Persons = new[] { Other }, Emotions = new[] { Happy, Love },
                RawUi = "시계<br><br>//<br><br>항상 움직이는 시곗 바늘을 보면, B씨와 연결된 느낌이 들어. 조금의 애정이 느껴지기도 해." },
            new ClueRow { Name = "빗물이 고인 그릇", SetId = "set3", Times = new[] { Present }, Persons = new PersonTag[0], Emotions = new[] { Disgust },
                RawUi = "빗물이 고인 그릇<br><br>//<br><br>지금도 조금씩 차오르고 있어. 비 따위는 평생 안 와도 돼. " },
            new ClueRow { Name = "흰 꽃", SetId = null, Times = new[] { Past }, Persons = new[] { Family }, Emotions = new[] { Sad },
                RawUi = "흰 꽃<br><br>//<br><br>부모님이 받아주실까? " },
            new ClueRow { Name = "바게트", SetId = null, Times = new[] { Present }, Persons = new[] { Other }, Emotions = new[] { Happy },
                RawUi = "바게트<br><br>//<br><br>B씨가 저녁 식사를 위해 만들어 주셨어. 고소한 냄새가 기분을 좋게 만들어." },
            new ClueRow { Name = "만년필", SetId = null, Times = new[] { Past }, Persons = new[] { Other }, Emotions = new[] { Happy },
                RawUi = "만년필<br><br>//<br><br>B씨가 지원서에 서명한 만년필이야. 오래된 흔적이 보여." },
            new ClueRow { Name = "우산", SetId = "set3", Times = new[] { Past }, Persons = new[] { Family }, Emotions = new[] { Happy, Love },
                RawUi = "우산<br><br>예전에, 가족들과 비 오는 날 처음으로 갓 구운 빵을 먹었어. 그 때도 부모님은 내게 빵 조각을 더 나누어 주지 못해서 아쉬워 하셨지. 그 순간 만큼은 누구보다 행복했어." }
        };

        /// <summary>노션 UI 칸 → 게임 텍스트. &lt;br&gt;&lt;br&gt; = 빈 줄, &lt;br&gt; = 줄바꿈, "//"는 사물 설명과 혼잣말 사이 칸(스테이지 1은 빈 줄)이라 사라지고 양옆이 이어진다.</summary>
        internal static string FromNotion(string raw)
        {
            var tokens = raw.Split(new[] { "<br>" }, System.StringSplitOptions.None).Select(t => t.Trim()).Where(t => t != "//").ToList();
            var collapsed = new List<string>();
            foreach (var token in tokens)
            {
                if (token.Length == 0 && collapsed.Count > 0 && collapsed[collapsed.Count - 1].Length == 0) continue;
                collapsed.Add(token);
            }

            return string.Join("\n", collapsed).Trim('\n');
        }

        private static ClueDefinition ClueNamed(string name) => Stage2Content.Clues().Single(c => c.DisplayName == name);

        [Test]
        public void Clues_AreExactlyTheFifteenFromTheStageTable()
        {
            var clues = Stage2Content.Clues();

            Assert.AreEqual(15, ClueTable.Length);
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
                Assert.AreEqual(row.SetId, clue.SetId == null ? null : clue.SetId.Replace("stage2_", ""), $"{row.Name}: set");
                Assert.AreEqual(FromNotion(row.RawUi), clue.Story, $"{row.Name}: UI상 표시");
            }
        }

        [Test]
        public void NewClues_BaguetteFountainPenAndClock_AreOrdinaryCluesWithoutASet()
        {
            foreach (var name in new[] { "바게트", "만년필", "시계" })
            {
                var clue = ClueNamed(name);
                Assert.IsNull(clue.SetId, $"{name}: set에 속하지 않는다.");
                CollectionAssert.AreEquivalent(new[] { Other }, clue.Persons, name);
            }

            CollectionAssert.AreEquivalent(new[] { Happy }, ClueNamed("바게트").Emotions);
            CollectionAssert.AreEquivalent(new[] { Happy }, ClueNamed("만년필").Emotions);
            CollectionAssert.AreEquivalent(new[] { Happy, Love }, ClueNamed("시계").Emotions, "09/25 09:19: 현재 + 타인 + 행복 + 사랑");

            CollectionAssert.AreEqual(new[] { Present }, ClueNamed("바게트").Times);
            CollectionAssert.AreEqual(new[] { Past }, ClueNamed("만년필").Times);
            CollectionAssert.AreEqual(new[] { Present }, ClueNamed("시계").Times);
            Assert.AreEqual(1, Stage2Content.Clues().Count(c => c.DisplayName == "시계"), "시계는 새로 추가가 아니라 기존 항목을 채운 것이다.");
        }

        [Test]
        public void Clues_NoneUseTheNonePlaceholderTags()
        {
            foreach (var clue in Stage2Content.Clues())
            {
                CollectionAssert.DoesNotContain(clue.Times, TimeTag.None, $"{clue.DisplayName}: 시간이 없으면 TimeTag.None이 아니라 빈 목록이어야 한다.");
                CollectionAssert.DoesNotContain(clue.Persons, PersonTag.None, $"{clue.DisplayName}: 인물이 없으면 빈 목록이어야 한다.");
            }
        }

        [Test]
        public void MultiTimeClue_CarriesAllItsTimeTagsIntoTheTagSet()
        {
            var tags = ClueNamed("깨진 액자").CreateOriginalTagSet();

            CollectionAssert.AreEqual(new[] { Past, Future }, tags.Times);
            Assert.IsTrue(tags.HasTime(Past));
            Assert.IsTrue(tags.HasTime(Future));
            Assert.IsFalse(tags.HasTime(Present));

            var clone = tags.Clone();
            CollectionAssert.AreEqual(tags.Times, clone.Times);
            clone.SetTime(Present);
            CollectionAssert.AreEqual(new[] { Past, Future }, tags.Times, "복제본을 바꿔도 원본은 그대로다.");
            CollectionAssert.AreEqual(new[] { Present }, clone.Times, "SetTime은 시간 태그를 전부 그 하나로 바꾼다(스테이지 1 회피).");
        }

        // ── set ───────────────────────────────────────────────────────────────

        private static readonly (string SetId, string[] Members)[] SetTable =
        {
            ("set1", new[] { "지원 신청서", "B의 편지" }),
            ("set2", new[] { "식탁 맡의 쪽지", "깨진 액자" }),
            ("set3", new[] { "TV 뉴스", "빗물이 고인 그릇", "우산" })
        };

        [Test]
        public void Sets_AreTheThreeSetsFromTheTable_Set3HavingThreeClues()
        {
            var clues = Stage2Content.Clues();
            var bySet = clues.Where(c => c.SetId != null).GroupBy(c => c.SetId).ToList();

            Assert.AreEqual(SetTable.Length, bySet.Count);
            foreach (var (setId, expected) in SetTable)
            {
                var members = bySet.Single(g => g.Key.EndsWith(setId)).Select(c => c.DisplayName).ToList();
                CollectionAssert.AreEquivalent(expected, members, setId);
            }

            Assert.AreEqual(8, clues.Count(c => c.SetId == null), "set에 속하지 않는 단서는 8개다.");
        }

        [Test]
        public void SetClue_WhenOneIsDrawn_TheOtherComesAlong_InTheRealStage_AcrossManySeeds()
        {
            var config = Stage2Content.Stage2(Polarity);
            var newSetCards = 0;
            var quarterStarts = 0;

            for (var seed = 1000; seed < 2000; seed++)
            {
                var session = StageFactory.Create(config, new SystemRandomSource(seed), new ClueKnowledgeLedger(), Polarity);
                var added = new List<ClueInstance>();
                session.Hand.CardAdded += added.Add;

                void CheckNewCards()
                {
                    foreach (var fresh in added.Where(c => c.Definition.SetId != null))
                    {
                        newSetCards++;
                        Assert.IsTrue(session.Hand.Cards.Any(c => c != fresh && c.Definition.SetId == fresh.Definition.SetId),
                            $"seed {seed}: {fresh.Definition.DisplayName} 만 들어왔고 같은 set 동료가 손패에 없다.");
                    }
                    added.Clear();
                }

                session.Runner.StartStage();
                CheckNewCards();
                Assert.AreEqual(ClueHand.HandSize, session.Hand.Cards.Count, $"seed {seed}: 1쿼터 시작 손패");
                quarterStarts++;

                var guard = 0;
                while (session.Runner.Outcome == StageOutcome.InProgress && guard++ < config.TotalTurns)
                {
                    var quarter = session.Runner.CurrentQuarter;
                    session.Runner.PlayClue(QuarterBots.ChooseHeuristicCard(session));
                    CheckNewCards();

                    if (session.Runner.Outcome == StageOutcome.InProgress && session.Runner.CurrentQuarter != quarter)
                    {
                        Assert.AreEqual(ClueHand.HandSize, session.Hand.Cards.Count,
                            $"seed {seed}: {session.Runner.CurrentQuarter}쿼터 시작 손패는 set가 있어도 4장으로 가득 차야 한다.");
                        quarterStarts++;
                    }
                }
            }

            Assert.Greater(newSetCards, 1000, "set 단서가 실제로 자주 뽑혀야 검증이 의미 있다.");
            Assert.Greater(quarterStarts, 3000);
        }

        [Test]
        public void EverySetPairIsPlayedAsAPair_NeverOnlyHalfOfASetAtQuarterStart_WhenBothAreInThePool()
        {
            // 쿼터 시작마다 낸 단서가 풀로 돌아오고 손패가 다시 채워진다 — 그때 set의 한쪽만 새로 들어오면 안 된다.
            var clues = Stage2Content.Clues();
            for (var seed = 0; seed < 1000; seed++)
            {
                var hand = new ClueHand(new CluePool(clues, new SystemRandomSource(seed)));
                hand.RefillForNewQuarter();
                for (var quarter = 0; quarter < 5; quarter++)
                {
                    foreach (var card in hand.Cards.Take(3).ToList()) hand.Use(card);
                    var before = new HashSet<ClueInstance>(hand.Cards);
                    hand.RefillForNewQuarter();

                    Assert.AreEqual(ClueHand.HandSize, hand.Cards.Count, $"seed {seed} 쿼터 {quarter}");
                    foreach (var fresh in hand.Cards.Where(c => !before.Contains(c) && c.Definition.SetId != null))
                        Assert.IsTrue(hand.Cards.Any(c => c != fresh && c.Definition.SetId == fresh.Definition.SetId),
                            $"seed {seed}: {fresh.Definition.DisplayName} 의 set 동료가 손패에 없다.");
                }
            }
        }

        // ── 컴플렉스 표 ─────────────────────────────────────────────────────────

        private static readonly (string Name, string Id, string Ui, int Duration)[] ComplexTable =
        {
            ("과거 부정 컴플렉스", "stage2_past_denial", "과거의 슬픔을 부정하여, 느끼지 않는다.", 5),
            ("착한아이 컴플렉스", "stage2_kind_child", "타인의 슬픔에 자신 또한 동화되어 깊은 슬픔을 느낍니다.", 3),
            ("복합 감정 컴플렉스", "stage2_mixed_emotion", "한 번에 여러 감정이 들어오면, 깊은 분노를 느낍니다.", 2),
            ("합리화 컴플렉스", "stage2_rationalization", "가족과 관련된 감정의 주체에 타인을 더해 합리화합니다.", 5),
            ("자책 컴플렉스", "stage2_self_blame", "가족과 관련된 침체되는 감정을 자책하며 더 깊게 느낍니다.", 3),
            ("과한 기대 컴플렉스", "stage2_over_expectation", "현재의 행복이 미래까지 이어질 것이라고 기대하며, 강한 행복을 느낀다.", 3),
            ("의식 분산 컴플렉스", "stage2_scattered_mind", "두 명 이상의 인물에 대한 감정을 느끼면, 의식이 분산되어 침체됩니다.", 3),
            ("자기 분노 컴플렉스", "stage2_self_anger", "침체와 흥분 감정을 동시에 느끼고 있다면 흥분 감정을 더 깊게 느낀다.", 4),
            ("전이 컴플렉스", "stage2_transference", "타인에게 가족의 모습을 겹쳐 본다.", 4),
            ("불신 컴플렉스", "stage2_distrust", "가까운 사람의 애정을 두려움으로 받아들인다.", 5),
            ("과대 해석 컴플렉스", "stage2_over_interpretation", "가까운 사람의 사랑을 느끼면, 다른 감정은 모두 무시하고, 사랑만 받아들입니다.", 3),
            ("피해 망상 컴플렉스", "stage2_persecution", "가까운 사람에게 슬픔을 느끼면 행복한 감정은 잊고, 슬픔을 더 깊게 느낍니다.", 3),
            ("사고 과다 컴플렉스", "stage2_overthinking", "과거의 감정을 배로 느낍니다.", 4),
            ("가족애 컴플렉스", "stage2_family_love", "기억에 가족이 강하게 남아있다면, 깊은 행복을 느낍니다.", 3)
        };

        [Test]
        public void Complexes_AreExactlyTheFourteenFromTheStageTable_WithUiTextAndDurations()
        {
            var complexes = Stage2Content.Complexes(Polarity);

            Assert.AreEqual(ComplexTable.Length, complexes.Count);
            foreach (var (name, id, ui, duration) in ComplexTable)
            {
                var complex = complexes.Single(c => c.DisplayName == name);
                Assert.AreEqual(id, complex.Id, $"{name}: id");
                Assert.AreEqual(ui, complex.Description, $"{name}: UI상 표시");
                Assert.AreEqual(duration, complex.DefaultDuration, $"{name}: 지속 턴 수");
            }

            Assert.AreEqual(complexes.Count, complexes.Select(c => c.Id).Distinct().Count(), "컴플렉스 id는 서로 달라야 한다.");
        }

        [Test]
        public void ComplexIds_NeverCollideWithStage1_EspeciallyTheTwoKindChildComplexes()
        {
            var stage1 = PrototypeContent.Complexes(Polarity).Select(c => c.Id).ToList();
            var stage2 = Stage2Content.Complexes(Polarity).Select(c => c.Id).ToList();

            CollectionAssert.IsEmpty(stage1.Intersect(stage2));
            CollectionAssert.Contains(stage1, "complex_good_child");
            CollectionAssert.Contains(stage2, "stage2_kind_child");
        }

        [Test]
        public void ReactionLines_AreInTheAsset_ReactionLineFirstThenVariations_VerbatimFromTheTable()
        {
            var expected = new Dictionary<string, string[]>
            {
                ["stage2_past_denial"] = new[] { "과거의 감정은 진짜가 아니야.", "뭔가 착각했을 거야.", "내가 그런 감정을 느꼈을 리가 없어.", "녹슬어서 어긋난 시곗 바늘을 보며 일하러 나가는 사람은 없어." },
                ["stage2_kind_child"] = new[] { "이렇게 행복했던 건, 내가 착했기 때문이야.", "내가 착하지 않다는 걸 알았다면, 행복하지 못했을까?", "행복했어, 내가 친절했던 만큼 말이야." },
                ["stage2_mixed_emotion"] = new[] { "왜 이렇게 복잡한거야? 혼란스러워.", "이건 무슨 감정인거야?", "검정색의 처음을 종이가 이해할 수 있을 거라 생각하지 마.", "나조차 이해하지 못하는거야?" },
                ["stage2_rationalization"] = new[] { "그렇게 깊은 관계도 아니었어. 이렇게 신경 쓸 필요가 없다고.", "내 삶은 나 혼자서 살아가는 거야. 아무리 가까워도 결국 다른 사람인 걸.", "차라리 다른 사람이라고 생각할래.", "이런다고 나한테 좋은 일이 생길까?" },
                ["stage2_self_blame"] = new[] { "왜 하필 가족이었을까? 차라리 다른 사람이나, 하다 못해 친구였다면? 아니. 분명 나였어야 했는데.", "내가 바꿀 수 있었을까? 내가 좀 더 노력했다면, 결말이 달랐을까?", "내가 바꿀 수 없었다는 사실을 인정하고 싶지 않아. 전부 정해진 운명이었던 걸까?" },
                ["stage2_over_expectation"] = new[] { "너무 행복해. 내일도, 그 다음 날도, 그리고 몇 년 뒤 까지도 계속 행복 할게 분명해.", "확실히 나는 계속 행복할거야. 무슨 일이 있어도, 계속.", "이 행복이 끝난다고? 그건 전부 질투하는 사람들의 거짓말이야.", "지금부터 우울한 날들은 전부 끝이야. 이제 난 평생 행복에 잠겨 환상적인 삶을 보낼 거거든!" },
                ["stage2_scattered_mind"] = new[] { "이건 누구를 향한 감정이지?", "나 자신? 가족? 아니면.. 다른 사람?", "왜 이러는지 모르겠어.. 혼란스러워." },
                ["stage2_self_anger"] = new[] { "왜 이렇게 우울한거지? 나는 이정도도 견디지 못하는 건가?", "고작 우울감 하나도 견디지 못한 내가 무슨 쓸모가 있다고.", "산책을 나가거나 친구를 만날 용기조차 없으면서.", "아무 것도 할 수 없고, 그냥 우울해 하기. 나 같은 건 차라리 죽는게 나아." },
                ["stage2_transference"] = new[] { "그 사람이 가깝게 느껴져.", "내가 그 사람을 잘 모르고 있던 것 같아. 돌이켜 보면 꽤 좋았을 지도.", "마음 한 곳에 계속 남아있어." },
                ["stage2_distrust"] = new[] { "정말 나를 위해 주는 게 맞을까? 그게 맞다고 해도, 내가 뭔가를 해야 할 것 같은 느낌이 들어.", "전부를 내어주기엔 뭔가 꺼려져.", "바늘 끝의 달콤함을 이미 삼켜버렸어. 그저 당기지 않길 바랄 뿐이야.", "내가 그 사람이라면, 나를 좋아할까? 아닐텐데." },
                ["stage2_over_interpretation"] = new[] { "지금 감정이 제일 중요해. 다른 건 필요 없어.", "오직 지금 순간이 의미 있는 거야. 과거나 미래는 잡을 수 없는 걸.", "내 눈에 보이고, 내가 느끼는 건 지금 밖에 없어.", "존재 하는 것이 그 의미 보다 중요해." },
                ["stage2_persecution"] = new[] { "어떻게 그럴 수 있지? 우리가 함께 한 시간이 아무것도 아니었던 건가?", "역시 아무런 의미도 없던 거야. 전부 내 망상, 내 손으로 이끈 인형극에 불과한 걸.", "처음부터 의지 하지 않았다면, 그랬다면 덜 고통스러울 텐데." },
                ["stage2_overthinking"] = new[] { "생각이 너무 많아서 어지러워.", "머릿속이 너무 복잡해.", "생각이 전부 얽혀서 꼬인 실타래가 되어 버렸어." }
            };

            var actual = ParseReactionAsset();
            foreach (var pair in expected)
            {
                Assert.IsTrue(actual.ContainsKey(pair.Key), $"{pair.Key}: 에셋에 항목이 없다.");
                CollectionAssert.AreEqual(pair.Value, actual[pair.Key], pair.Key);
            }

            // 가족애(09/25 09:40에 신설)는 노션 반응 대사 표에 아직 없다 — 에셋에 넣지 않고 공통 문구로 대체된다.
            Assert.AreEqual(ComplexTable.Length - 1, actual.Keys.Count(k => k.StartsWith("stage2_")));
            Assert.IsFalse(actual.ContainsKey("stage2_family_love"), "노션에 가족애 반응 대사가 생기기 전에는 만들어 넣지 않는다.");
        }

        /// <summary>Assets/Resources/ComplexReactionLines.asset(YAML)에서 컴플렉스 id → 대사 목록을 읽는다.</summary>
        internal static Dictionary<string, List<string>> ParseReactionAsset()
        {
            var path = Path.Combine(Application.dataPath, "Resources", "ComplexReactionLines.asset");
            var result = new Dictionary<string, List<string>>();
            List<string> current = null;

            foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                var line = raw.TrimEnd();
                if (line.StartsWith("  - complexId: "))
                {
                    current = new List<string>();
                    result[line.Substring("  - complexId: ".Length)] = current;
                }
                else if (current != null && line.StartsWith("    - '") && line.EndsWith("'"))
                {
                    current.Add(line.Substring("    - '".Length, line.Length - "    - '".Length - 1).Replace("''", "'"));
                }
            }

            return result;
        }

        // ── 컴플렉스 상세 정보 ──────────────────────────────────────────────────

        private static TagSet Make(TimeTag[] times, PersonTag[] persons, params EmotionTag[] emotions) => new(times, persons, emotions);

        private static bool Apply(ComplexDefinition complex, TagSet tags) => complex.TryInterpret(new ComplexContext(tags));

        private static ComplexDefinition Complex(string id) => Stage2Content.Complexes(Polarity).Single(c => c.Id == id);

        private static void AssertEmotions(TagSet tags, string message, params (EmotionTag Emotion, int Count)[] expected)
        {
            var actual = tags.Emotions.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)).ToList();
            var want = expected.Where(e => e.Count > 0).OrderBy(e => e.Emotion).ToList();
            CollectionAssert.AreEqual(want, actual, message);
        }

        [Test]
        public void PastDenial_PastAndSadness_RemovesOneSadness()
        {
            var complex = Complex("stage2_past_denial");

            var stacked = Make(new[] { Past }, new PersonTag[0], Sad, Sad, Happy);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "슬픔 2 → 1", (Sad, 1), (Happy, 1));

            var single = Make(new[] { Past }, new[] { Family }, Sad);
            Assert.IsTrue(Apply(complex, single));
            AssertEmotions(single, "슬픔 1 → 사라짐");

            Assert.IsFalse(Apply(complex, Make(new[] { Present }, new PersonTag[0], Sad)), "과거가 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0], Happy)), "슬픔이 없으면 발동하지 않는다");
            Assert.IsTrue(Apply(complex, Make(new[] { Past, Present }, new PersonTag[0], Sad)), "과거+현재 단서도 과거 태그가 있으면 발동한다");
        }

        [Test]
        public void KindChild_OtherAndSadness_AddsThreeSadness()
        {
            var complex = Complex("stage2_kind_child");

            var tags = Make(new[] { Past }, new[] { Other }, Sad, Fear);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "슬픔 1 → 4", (Sad, 4), (Fear, 1));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family }, Sad)), "타인이 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Other }, Fear)), "슬픔이 없으면 발동하지 않는다");
        }

        [Test]
        public void MixedEmotion_TwoOrMoreEmotionKinds_AddsAnger()
        {
            var complex = Complex("stage2_mixed_emotion");

            var tags = Make(new[] { Past }, new PersonTag[0], Sad, Fear);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "분노 +3 (09/25 09:40)", (Sad, 1), (Fear, 1), (Anger, 3));

            var withAnger = Make(new[] { Past }, new PersonTag[0], Sad, Anger);
            Assert.IsTrue(Apply(complex, withAnger));
            AssertEmotions(withAnger, "이미 있는 분노에 +3", (Sad, 1), (Anger, 4));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0], Sad)), "감정 1종류");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0], Sad, Sad)), "같은 감정이 겹친 것은 1종류로 센다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0])), "감정 없음");
        }

        [Test]
        public void Rationalization_FamilyAndEmotion_AddsOtherAndKeepsFamily()
        {
            var complex = Complex("stage2_rationalization");

            // 09/25 09:40 — 변형(가족 → 타인)이 추가(타인 +1)로 바뀌었다: 가족 태그는 그대로 남는다.
            var tags = Make(new[] { Past }, new[] { Family }, Sad);
            Assert.IsTrue(Apply(complex, tags));
            CollectionAssert.AreEquivalent(new[] { Family, Other }, tags.Persons);
            Assert.AreEqual(1, tags.CountOfPerson(Family), "가족은 그대로");
            Assert.AreEqual(1, tags.CountOfPerson(Other), "타인 +1");
            AssertEmotions(tags, "감정은 그대로", (Sad, 1));

            var both = Make(new[] { Past }, new[] { Family, Other }, Sad, Fear);
            Assert.IsTrue(Apply(complex, both));
            CollectionAssert.AreEquivalent(new[] { Family, Other }, both.Persons);
            Assert.AreEqual(1, both.CountOfPerson(Family));
            Assert.AreEqual(2, both.CountOfPerson(Other), "이미 있는 타인에는 개수가 쌓인다(2)");

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family })), "감정 태그가 없으면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Other }, Sad)), "가족이 아니면 발동하지 않는다");
        }

        [Test]
        public void SelfBlame_FamilyAndDepressed_EveryDepressedTagGetsOneMore_DuplicatesIncluded()
        {
            var complex = Complex("stage2_self_blame");

            var tags = Make(new[] { Past }, new[] { Family }, Sad, Fear, Happy);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "슬픔 1→2, 공포 1→2, 행복은 그대로", (Sad, 2), (Fear, 2), (Happy, 1));

            var stacked = Make(new[] { Past }, new[] { Family }, Sad, Sad, Disgust);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "중복 포함: 슬픔 2→4, 혐오 1→2", (Sad, 4), (Disgust, 2));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family }, Happy, Love)), "침체 감정이 없으면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Other }, Sad)), "가족이 아니면 발동하지 않는다");
        }

        [Test]
        public void OverExpectation_PresentAndHappinessOrLove_AddsFutureKeepingPresent_AndHappinessPlusTwo()
        {
            var complex = Complex("stage2_over_expectation");

            var tags = Make(new[] { Present }, new PersonTag[0], Love);
            Assert.IsTrue(Apply(complex, tags));
            CollectionAssert.AreEqual(new[] { Present, Future }, tags.Times, "미래가 '추가'된다 — 현재는 그대로");
            AssertEmotions(tags, "사랑만 있어도 행복이 +2 (없던 행복이 생긴다)", (Love, 1), (Happy, 2));

            var pastPresent = Make(new[] { Past, Present }, new[] { Other }, Happy);
            Assert.IsTrue(Apply(complex, pastPresent));
            CollectionAssert.AreEqual(new[] { Past, Present, Future }, pastPresent.Times);
            AssertEmotions(pastPresent, "행복 1 → 3", (Happy, 3));

            var alreadyFuture = Make(new[] { Present, Future }, new PersonTag[0], Happy);
            Assert.IsTrue(Apply(complex, alreadyFuture));
            CollectionAssert.AreEqual(new[] { Present, Future }, alreadyFuture.Times, "이미 있는 태그는 중복되지 않는다");
            AssertEmotions(alreadyFuture, "시간 태그가 이미 있어도 행복은 +2", (Happy, 3));

            Assert.IsFalse(Apply(complex, Make(new[] { Present }, new PersonTag[0], Sad)), "행복/사랑이 없으면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0], Happy)), "현재가 아니면 발동하지 않는다");
        }

        [Test]
        public void ScatteredMind_TwoOrMorePersonsAndEmotion_AddsSadness()
        {
            var complex = Complex("stage2_scattered_mind");

            var tags = Make(new[] { Past }, new[] { Family, Other }, Happy);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "슬픔 +1", (Happy, 1), (Sad, 1));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Other }, Happy)), "인물 1명");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family, Other })), "감정 태그 없음");
        }

        [Test]
        public void SelfAnger_DepressedAndExcitedTogether_AddsHappinessTriceTheExcitedTagCount()
        {
            var complex = Complex("stage2_self_anger");

            var oneEach = Make(new[] { Past }, new PersonTag[0], Sad, Happy);
            Assert.IsTrue(Apply(complex, oneEach));
            AssertEmotions(oneEach, "흥분 1개 × 3 → 행복 +3", (Sad, 1), (Happy, 4));

            var twoKinds = Make(new[] { Past }, new PersonTag[0], Fear, Happy, Love);
            Assert.IsTrue(Apply(complex, twoKinds));
            AssertEmotions(twoKinds, "흥분 2개(행복·사랑) × 3 → 행복 +6", (Fear, 1), (Happy, 7), (Love, 1));

            var stacked = Make(new[] { Past }, new PersonTag[0], Disgust, Love, Love);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "겹친 흥분 태그도 하나씩 센다: 사랑 2개 × 3 → 행복 +6", (Disgust, 1), (Love, 2), (Happy, 6));

            var withAnger = Make(new[] { Past }, new PersonTag[0], Sad, Anger);
            Assert.IsTrue(Apply(complex, withAnger));
            AssertEmotions(withAnger, "분노도 흥분 감정이다: 분노 1개 × 3 → 행복 +3", (Sad, 1), (Anger, 1), (Happy, 3));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0], Sad, Fear)), "침체만");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0], Happy, Love)), "흥분만");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0])), "감정이 하나도 없으면 발동하지 않는다");
        }

        [Test]
        public void Transference_AnyOtherTag_AddsFamilyAndKeepsOther()
        {
            var complex = Complex("stage2_transference");

            // 09/25 09:40 — 변형(타인 → 가족)이 추가(가족 +1)로 바뀌었다: 타인 태그는 그대로 남는다.
            var tags = Make(new[] { Past }, new[] { Other }, Happy);
            Assert.IsTrue(Apply(complex, tags));
            CollectionAssert.AreEquivalent(new[] { Other, Family }, tags.Persons);
            Assert.AreEqual(1, tags.CountOfPerson(Other), "타인은 그대로");
            Assert.AreEqual(1, tags.CountOfPerson(Family), "가족 +1");
            AssertEmotions(tags, "감정은 그대로", (Happy, 1));

            var both = Make(new[] { Past }, new[] { Family, Other }, Sad);
            Assert.IsTrue(Apply(complex, both));
            Assert.AreEqual(2, both.CountOfPerson(Family), "이미 있는 가족에는 개수가 쌓인다(2)");
            Assert.AreEqual(1, both.CountOfPerson(Other));

            var noEmotion = Make(new[] { Past }, new[] { Other });
            Assert.IsTrue(Apply(complex, noEmotion), "조건은 타인 태그뿐이다 — 감정이 없어도 발동한다");

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Friend }, Love)), "타인이 아니면 발동하지 않는다");
        }

        [Test]
        public void TagSet_PersonTagsAreCounted_ButStillListedOncePerKind()
        {
            var tags = Make(new[] { Past }, new[] { Family, Family, Other }); // 원본 목록의 중복은 한 종류 1개로 본다
            Assert.AreEqual(1, tags.CountOfPerson(Family));
            Assert.AreEqual(0, tags.CountOfPerson(Friend));

            tags.AddPerson(Family, 2);
            tags.AddPerson(Friend);
            tags.AddPerson(PersonTag.None);
            tags.AddPerson(Lover, 0);
            Assert.AreEqual(3, tags.CountOfPerson(Family));
            Assert.AreEqual(1, tags.CountOfPerson(Friend), "없던 종류는 새로 붙는다");
            CollectionAssert.AreEquivalent(new[] { Family, Other, Friend }, tags.Persons, "None·0개는 붙지 않고, 종류는 개수와 무관하게 한 번씩만 나온다");

            var clone = tags.Clone();
            clone.AddPerson(Family);
            Assert.AreEqual(3, tags.CountOfPerson(Family), "복제본을 바꿔도 원본은 그대로");
            Assert.AreEqual(4, clone.CountOfPerson(Family), "복제본은 개수까지 가져간다");

            tags.ReplacePerson(Family, Other);
            Assert.IsFalse(tags.HasPerson(Family), "변형은 원래 태그를 없앤다");
            Assert.AreEqual(3, tags.CountOfPerson(Other), "합쳐질 때는 개수가 큰 쪽을 따른다");
        }

        [Test]
        public void FamilyLove_TwoOrMoreFamilyTags_AddsHappinessThree()
        {
            var complex = Complex("stage2_family_love");

            var stacked = new TagSet(new[] { Past }, new[] { Family }, new[] { Sad });
            stacked.AddPerson(Family);
            Assert.IsTrue(Apply(complex, stacked));
            AssertEmotions(stacked, "행복 +3", (Sad, 1), (Happy, 3));

            var threeFamily = new TagSet(new[] { Past }, new[] { Family }, new[] { Happy });
            threeFamily.AddPerson(Family, 2);
            Assert.IsTrue(Apply(complex, threeFamily));
            AssertEmotions(threeFamily, "2개 이상이면 몇 개든 +3 한 번", (Happy, 4));

            var noEmotion = new TagSet(new[] { Past }, new[] { Family });
            noEmotion.AddPerson(Family);
            Assert.IsTrue(Apply(complex, noEmotion), "감정이 없어도 발동한다 — 조건은 가족 태그 개수뿐이다");
            AssertEmotions(noEmotion, "감정이 없던 단서에 행복 +3", (Happy, 3));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family }, Sad)), "가족 태그 1개(원본 단서는 늘 1개)로는 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family, Other }, Sad)), "서로 다른 인물이 둘이어도 가족이 1개면 발동하지 않는다");
        }

        [Test]
        public void FamilyLove_NoOriginalClueHasTwoFamilyTags_SoAloneItNeverFires()
        {
            foreach (var clue in Stage2Content.Clues())
                Assert.LessOrEqual(clue.Persons.Count(p => p == Family), 1, $"{clue.DisplayName}: 원본 가족 태그는 늘 0~1개다.");

            var board = new ComplexBoard();
            board.TryAttach(new ComplexInstance(Complex("stage2_family_love"), 100));

            // 가족애만 붙어 있으면 어느 단서로도 발동하지 않는다.
            foreach (var clue in Stage2Content.Clues())
                Assert.IsFalse(new ComplexResolver(board).Resolve(clue.CreateOriginalTagSet()).Steps.Any(step => step.Triggered), clue.DisplayName);
        }

        [Test]
        public void FamilyLove_IsAlwaysEvaluatedRightAfterTransference_WhateverTheAttachOrder()
        {
            var bLetter = Stage2Content.Clues().Single(c => c.Id == "s2_b_letter"); // 타인 + 가족, 슬픔

            InterpretationResult Resolve(int transferencePriority, int familyLovePriority)
            {
                var board = new ComplexBoard();
                board.TryAttach(new ComplexInstance(Complex("stage2_transference"), transferencePriority));
                board.TryAttach(new ComplexInstance(Complex("stage2_family_love"), familyLovePriority));
                return new ComplexResolver(board).Resolve(bLetter.CreateOriginalTagSet());
            }

            // 가족애가 우선순위상 뒤(붙은 순서상 나중)여도, 앞이어도 결과는 같다 — 전이 바로 뒤에서 평가된다.
            foreach (var (transferencePriority, familyLovePriority) in new[] { (100, 101), (101, 100) })
            {
                var result = Resolve(transferencePriority, familyLovePriority);
                var final = result.Final;

                Assert.AreEqual(2, final.CountOfPerson(Family), $"전이 {transferencePriority} / 가족애 {familyLovePriority}: 전이가 가족 +1 → 가족 2");
                Assert.AreEqual(1, final.CountOfPerson(Other));
                Assert.AreEqual(3, final.CountOf(Happy), "이어서 가족애가 발동해 행복 +3");
                CollectionAssert.AreEqual(new[] { "stage2_transference", "stage2_family_love" }, result.Steps.Select(s => s.Complex.Definition.Id).ToArray(), "평가 순서는 전이 → 가족애");
                Assert.IsTrue(result.Steps.All(s => s.Triggered));
            }
        }

        [Test]
        public void FamilyLove_MovesOnlyItself_OtherComplexesKeepTheirRelativeOrder_AndDisplayOrderIsUntouched()
        {
            var board = new ComplexBoard(4);
            var familyLove = new ComplexInstance(Complex("stage2_family_love"), 100);
            var selfBlame = new ComplexInstance(Complex("stage2_self_blame"), 101);
            var transference = new ComplexInstance(Complex("stage2_transference"), 102);
            var distrust = new ComplexInstance(Complex("stage2_distrust"), 103);
            foreach (var instance in new[] { familyLove, selfBlame, transference, distrust }) board.TryAttach(instance);

            CollectionAssert.AreEqual(new[] { familyLove, selfBlame, transference, distrust }, board.InPriorityOrder().ToArray(),
                "화면에 보이는 순서(우선순위)는 그대로다 — 뇌 영역 배치가 바뀌면 안 된다");
            CollectionAssert.AreEqual(new[] { selfBlame, transference, familyLove, distrust }, board.InEvaluationOrder().ToArray(),
                "가족애만 전이 바로 뒤로 옮겨진다");

            var result = new ComplexResolver(board).Resolve(ClueNamed("B의 편지").CreateOriginalTagSet());
            CollectionAssert.AreEqual(new[] { selfBlame, transference, familyLove, distrust }, result.Steps.Select(s => s.Complex).ToArray(), "해석 단계는 평가 순서");
            CollectionAssert.AreEqual(new[] { familyLove, selfBlame, transference, distrust }, result.DisplayOrder.ToArray(), "DisplayOrder는 화면 순서 그대로");
        }

        [Test]
        public void FamilyLove_WithoutTransferenceOnTheBoard_StaysWhereItIs_AndDoesNotFire()
        {
            var board = new ComplexBoard();
            var familyLove = new ComplexInstance(Complex("stage2_family_love"), 100);
            var selfBlame = new ComplexInstance(Complex("stage2_self_blame"), 101);
            board.TryAttach(familyLove);
            board.TryAttach(selfBlame);

            CollectionAssert.AreEqual(new[] { familyLove, selfBlame }, board.InEvaluationOrder().ToArray(), "앵커(전이)가 없으면 평가 순서도 우선순위 그대로");
            var result = new ComplexResolver(board).Resolve(ClueNamed("B의 편지").CreateOriginalTagSet());
            Assert.IsFalse(result.Steps.Single(s => s.Complex == familyLove).Triggered, "가족 태그가 1개뿐이라 발동하지 않는다");
        }

        [Test]
        public void EvaluationOrder_OfStage1AndOtherStage2Complexes_IsStillPurelyByPriority()
        {
            foreach (var pool in new[] { PrototypeContent.Complexes(Polarity), Stage2Content.Complexes(Polarity).Where(c => c.Id != "stage2_family_love").ToList() })
            {
                var board = new ComplexBoard(pool.Count);
                for (var i = 0; i < pool.Count; i++) board.TryAttach(new ComplexInstance(pool[pool.Count - 1 - i], 100 + i));

                CollectionAssert.AreEqual(board.InPriorityOrder().ToArray(), board.InEvaluationOrder().ToArray(), "고정 규칙이 없는 컴플렉스끼리는 순서가 바뀌지 않는다");
            }
        }

        [Test]
        public void RationalizationThenTransference_ChainsIntoFamilyLove_OnAFamilyOnlyClue()
        {
            var deathNotice = Stage2Content.Clues().Single(c => c.Id == "s2_death_notice"); // 가족, 슬픔

            InterpretationResult Resolve(params (string Id, int Priority)[] attached)
            {
                var board = new ComplexBoard();
                foreach (var (id, priority) in attached) board.TryAttach(new ComplexInstance(Complex(id), priority));
                return new ComplexResolver(board).Resolve(deathNotice.CreateOriginalTagSet());
            }

            // 합리화(가족 → 타인 +1) → 전이(타인 → 가족 +1) → 가족애(가족 2 → 행복 +3). 가족애가 어디에 붙었든 전이 바로 뒤에서 평가된다.
            foreach (var familyLovePriority in new[] { 99, 102 })
            {
                var final = Resolve(("stage2_rationalization", 100), ("stage2_transference", 101), ("stage2_family_love", familyLovePriority)).Final;

                Assert.AreEqual(2, final.CountOfPerson(Family), $"가족애 우선순위 {familyLovePriority}");
                Assert.AreEqual(1, final.CountOfPerson(Other));
                Assert.AreEqual(3, final.CountOf(Happy));
                Assert.AreEqual(1, final.CountOf(Sad));
            }

            // 전이가 합리화보다 먼저면 전이가 평가될 때 타인이 아직 없어(가족 단서) 연쇄가 이어지지 않는다 — 가족애는 전이 바로 뒤라 여전히 발동하지 않는다.
            var transferenceFirst = Resolve(("stage2_transference", 100), ("stage2_rationalization", 101), ("stage2_family_love", 102)).Final;
            Assert.AreEqual(1, transferenceFirst.CountOfPerson(Family));
            Assert.AreEqual(0, transferenceFirst.CountOf(Happy));
        }

        [Test]
        public void ConversionComplexes_OfBothStages_StillConvertPersonsAndEmotionsAsBefore()
        {
            // "변형" 계열 중 합리화·전이만 추가로 바뀌었다. 스테이지 1의 인물 변형(소꿉친구·타자화)과 감정 변형(불신 등)은 그대로다.
            var friend = Make(new[] { Past }, new[] { Friend }, Happy);
            Assert.IsTrue(Apply(PrototypeContent.ChildhoodFriend(), friend));
            CollectionAssert.AreEqual(new[] { Lover }, friend.Persons, "소꿉친구: 친구 → 연인(변형 — 친구는 사라진다)");

            var lover = Make(new[] { Past }, new[] { Lover, Other }, Happy);
            Assert.IsTrue(Apply(PrototypeContent.Othering(), lover));
            CollectionAssert.AreEqual(new[] { Other }, lover.Persons, "타자화: 이미 타인이 있으면 하나로 합쳐진다");
            Assert.AreEqual(1, lover.CountOfPerson(Other));

            var distrust = Make(new[] { Past }, new[] { Lover }, Love);
            Assert.IsTrue(Apply(Complex("stage2_distrust"), distrust));
            AssertEmotions(distrust, "불신: 사랑 → 공포", (Fear, 1));

            var over = Make(new[] { Past }, new[] { Family, Lover }, Love, Sad, Happy);
            Assert.IsTrue(Apply(Complex("stage2_over_interpretation"), over));
            AssertEmotions(over, "과대 해석: 사랑만 남고 +1", (Love, 2));
            CollectionAssert.AreEquivalent(new[] { Family, Lover }, over.Persons, "인물 태그는 건드리지 않는다");
        }

        [Test]
        public void Distrust_LoverAndLove_TurnsLoveIntoFear()
        {
            var complex = Complex("stage2_distrust");

            var tags = Make(new[] { Past }, new[] { Lover }, Love, Happy);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "사랑 → 공포", (Fear, 1), (Happy, 1));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Friend }, Love)), "연인이 아니면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Lover }, Happy)), "사랑이 없으면 발동하지 않는다");
        }

        [Test]
        public void OverInterpretation_FamilyLoverLove_KeepsOnlyLovePlusOne()
        {
            var complex = Complex("stage2_over_interpretation");

            var tags = Make(new[] { Past }, new[] { Family, Lover }, Love, Sad, Fear, Fear);
            Assert.IsTrue(Apply(complex, tags));
            AssertEmotions(tags, "다른 감정은 모두 제거, 사랑 1 → 2", (Love, 2));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Lover }, Love, Sad)), "가족이 없다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family }, Love, Sad)), "연인이 없다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family, Lover }, Sad)), "사랑이 없다");
        }

        [Test]
        public void Persecution_FamilyOrLoverAndSadness_HappinessMinusOneSadnessPlusOne()
        {
            var complex = Complex("stage2_persecution");

            var family = Make(new[] { Past }, new[] { Family }, Sad, Happy, Happy);
            Assert.IsTrue(Apply(complex, family));
            AssertEmotions(family, "행복 2 → 1, 슬픔 1 → 2", (Sad, 2), (Happy, 1));

            var lover = Make(new[] { Past }, new[] { Lover }, Sad, Happy);
            Assert.IsTrue(Apply(complex, lover));
            AssertEmotions(lover, "행복 1 → 0, 슬픔 1 → 2", (Sad, 2));

            var noHappy = Make(new[] { Past }, new[] { Family }, Sad);
            Assert.IsTrue(Apply(complex, noHappy));
            AssertEmotions(noHappy, "행복이 없어도 슬픔은 +1", (Sad, 2));

            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Other }, Sad, Happy)), "타인은 가까운 사람이 아니다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new[] { Family }, Happy)), "슬픔이 없다");
        }

        [Test]
        public void Overthinking_PastDoublesEmotions_AndNothingElse()
        {
            var complex = Complex("stage2_overthinking");

            var past = Make(new[] { Past }, new[] { Family }, Sad, Sad, Happy);
            Assert.IsTrue(Apply(complex, past));
            AssertEmotions(past, "과거: 감정 * 2", (Sad, 4), (Happy, 2));

            var pastAndPresent = Make(new[] { Past, Present }, new[] { Other }, Happy);
            Assert.IsTrue(Apply(complex, pastAndPresent));
            AssertEmotions(pastAndPresent, "과거+현재: 과거의 감정이므로 배로", (Happy, 2));

            // 09/25 09:19 — "과거가 아니면 슬픔 +1" 분기가 빠졌다.
            var present = Make(new[] { Present }, new PersonTag[0], Disgust);
            Assert.IsFalse(Apply(complex, present), "현재 단서에는 발동하지 않는다");
            AssertEmotions(present, "현재: 그대로", (Disgust, 1));

            var future = Make(new[] { Future }, new[] { Other }, Happy);
            Assert.IsFalse(Apply(complex, future), "미래 단서에는 발동하지 않는다");
            AssertEmotions(future, "미래: 그대로", (Happy, 1));

            Assert.IsFalse(Apply(complex, Make(new TimeTag[0], new PersonTag[0], Sad)), "시간 태그가 없으면 발동하지 않는다");
            Assert.IsFalse(Apply(complex, Make(new[] { Past }, new PersonTag[0])), "감정 태그가 없으면 발동하지 않는다");
        }

        [Test]
        public void Stage1Complexes_StillBehaveTheSame_OnSingleTimeClues()
        {
            // 시간 태그를 여러 개로 넓힌 뒤에도 스테이지 1 회피(현재/미래 → 과거)와 반 과거·되새김의 시간 조건은 그대로다.
            var avoidance = PrototypeContent.Avoidance();
            var future = Make(new[] { Future }, new[] { Other }, Sad, Disgust);
            Assert.IsTrue(Apply(avoidance, future));
            CollectionAssert.AreEqual(new[] { Past }, future.Times);

            var rumination = PrototypeContent.Rumination();
            var past = Make(new[] { Past }, new[] { Family }, Happy);
            Assert.IsTrue(Apply(rumination, past));
            AssertEmotions(past, "되새김", (Happy, 2));
            Assert.IsFalse(Apply(rumination, Make(new[] { Present }, new PersonTag[0], Sad)));
        }

        // ── 스테이지 구성 ───────────────────────────────────────────────────────

        [Test]
        public void Stage_Is15TurnsAsFiveQuartersOfThreeTurns_With3Keys_AndTheStage2Pools()
        {
            var config = Stage2Content.Stage2(Polarity);

            Assert.AreEqual("천사", config.DisplayName);
            Assert.AreEqual(15, config.TotalTurns);
            Assert.AreEqual(5, config.Quarters.QuarterCount);
            Assert.AreEqual(3, config.Quarters.TurnsPerQuarter);
            Assert.AreEqual(3, config.RequiredKeys);
            Assert.AreEqual(15, config.Clues.Count);
            Assert.AreEqual(14, config.ComplexPool.Count);
            Assert.LessOrEqual(config.Quarters.TurnsPerQuarter, ClueHand.HandSize,
                "손패는 쿼터 시작에만 채워지므로 쿼터당 턴 수가 손패 크기를 넘으면 마지막 턴에 낼 카드가 없다.");
        }

        [Test]
        public void PersuasionTargets_AreAllComplexesOfThisStage()
        {
            var config = Stage2Content.Stage2(Polarity);
            var ids = config.ComplexPool.Select(c => c.Id).ToList();

            foreach (var id in config.ItemParameters[PrototypeContent.PersuasionTargetsKey])
                CollectionAssert.Contains(ids, id);
        }

        [Test]
        public void Stage1Config_IsUntouched()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);

            Assert.AreEqual(12, config.TotalTurns);
            Assert.AreEqual(3, config.Quarters.QuarterCount);
            Assert.AreEqual(2, config.RequiredKeys);
            Assert.AreEqual(12, config.Clues.Count);
            Assert.AreEqual(10, config.ComplexPool.Count);
            Assert.IsTrue(config.Clues.All(c => c.SetId == null && c.Times.Count == 1));
        }

        // ── 밸런스 측정(참고용) ─────────────────────────────────────────────────

        [Test]
        public void Balance_ThousandSeeds_IsMeasuredAndEveryRunFinishes()
        {
            var config = Stage2Content.Stage2(Polarity);
            var seeds = Enumerable.Range(1000, 1000).ToArray();

            var naive = seeds.Select(s => QuarterBots.RunStage(config, s, "무전략(첫 장)", QuarterBots.ChooseFirstCard, null)).ToList();
            var heuristic = seeds.Select(s => QuarterBots.RunStage(config, s, "휴리스틱(쿼터 인지)", QuarterBots.ChooseHeuristicCard, null)).ToList();

            Debug.Log($"[스테이지 2 밸런스] {config.TotalTurns}턴 = {config.Quarters.QuarterCount}쿼터 × {config.Quarters.TurnsPerQuarter}턴, 키 {config.RequiredKeys}개, 폭 {config.KeyWidth}, {seeds.Length}시드\n" +
                      $"  무전략   : {Describe(naive)}\n  휴리스틱 : {Describe(heuristic)}");

            Assert.IsTrue(naive.Concat(heuristic).All(r => r.Outcome != StageOutcome.InProgress));
            Assert.Greater(heuristic.Count(r => r.Outcome == StageOutcome.Cleared), naive.Count(r => r.Outcome == StageOutcome.Cleared),
                "쿼터를 아는 봇이 무전략보다 더 많이 클리어해야 실력이 작동하는 스테이지다.");
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
