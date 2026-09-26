using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 튜토리얼 3단계: 가이드 진행(어떤 단계가 무엇을 가리키고 무엇이 일어나면 넘어가는지), 게이트 안내 문구, 선택지 진행 규칙, 시작 대화 에셋(청장 화자·선택지 3곳).
    /// 화면(말풍선·강조·클릭 막)은 UI 어셈블리라 여기서 못 다룬다 — 그 논리는 코어에 두고 테스트하고, 화면은 Play 모드 프로브로 확인한다.
    /// 이 테스트 어셈블리는 UI 어셈블리를 참조하지 못해 시작 대화는 에셋 YAML을 직접 읽는다(StageDialogueLinesTests와 같은 방식).
    /// </summary>
    public class TutorialGuideTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static string Emotion(EmotionTag tag) => tag switch
        {
            EmotionTag.Sadness => "슬픔",
            EmotionTag.Disgust => "혐오",
            EmotionTag.Fear => "공포",
            EmotionTag.Happiness => "행복",
            EmotionTag.Love => "사랑",
            _ => "분노"
        };

        private static string[] Ids(TutorialGuideFlow flow) => flow.Steps.Select(s => s.Id).ToArray();

        // ── 단계 표 ──────────────────────────────────────────────────────────────

        [Test]
        public void Steps_AreInScriptOrder()
        {
            CollectionAssert.AreEqual(new[]
            {
                "intro_device", "intro_connect", "intro_key", "intro_key_count",
                "click_clue", "open_manual", "read_manual", "close_book",
                "find_excited", "drag_clue",
                "clear_1", "new_branch", "complex_intro", "read_xray",
                "find_depressed", "clear_3",
                "stack_intro", "use_item", "branch_reminder"
            }, Ids(TutorialGuideContent.CreateFlow()));
        }

        [Test]
        public void HighlightTargets_MatchTheSpec()
        {
            var steps = TutorialGuideContent.Steps().ToDictionary(s => s.Id);

            Assert.AreEqual(GuideTarget.Cards, steps["click_clue"].Target, "\"단서 하나를 눌러보게\" → 단서 카드");
            Assert.AreEqual(GuideTarget.ManualTab, steps["open_manual"].Target, "\"메뉴얼을 누르면…\" → 메뉴얼 버튼");
            Assert.AreEqual(GuideTarget.Xray, steps["read_xray"].Target, "\"아래의 정보를 읽어 보게\" → 엑스레이 판넬");
            Assert.AreEqual(GuideTarget.Items, steps["use_item"].Target, "\"함께 활용해 봐\" → 아이템 슬롯");

            // 흥분 단서를 찾는 단계는 특정 카드를 가리키지 않는다 — 텍스트만.
            Assert.AreEqual(GuideTarget.None, steps["find_excited"].Target);
            Assert.AreEqual(GuideTarget.None, steps["drag_clue"].Target);

            // 위 넷을 뺀 나머지는 강조가 없다.
            var highlighted = steps.Values.Where(s => s.Target != GuideTarget.None).Select(s => s.Id).ToArray();
            CollectionAssert.AreEquivalent(new[] { "click_clue", "open_manual", "read_xray", "use_item" }, highlighted);
        }

        [Test]
        public void Advance_Conditions_MatchTheScript()
        {
            var steps = TutorialGuideContent.Steps().ToDictionary(s => s.Id);

            Assert.AreEqual(GuideAdvance.BookOpened, steps["click_clue"].Advance);
            Assert.AreEqual(GuideAdvance.ManualTabShown, steps["open_manual"].Advance);
            Assert.AreEqual(GuideAdvance.BookClosed, steps["close_book"].Advance);
            Assert.AreEqual(GuideAdvance.XrayOpened, steps["read_xray"].Advance);
            Assert.AreEqual(GuideAdvance.ItemUsed, steps["use_item"].Advance);

            // 카드 제시는 턴 결산으로 안다: 턴 1의 정답은 턴 2(넘김 턴, 첫 키 판정)까지 끝나야, 턴 3, 턴 4는 각자 그 턴.
            Assert.AreEqual((GuideAdvance.TurnResolved, 2), (steps["drag_clue"].Advance, steps["drag_clue"].WaitTurn));
            Assert.AreEqual((GuideAdvance.TurnResolved, 3), (steps["find_depressed"].Advance, steps["find_depressed"].WaitTurn));
            Assert.AreEqual((GuideAdvance.TurnResolved, 4), (steps["branch_reminder"].Advance, steps["branch_reminder"].WaitTurn));
        }

        [Test]
        public void Lines_AreTheNotionText()
        {
            var steps = TutorialGuideContent.Steps().ToDictionary(s => s.Id);

            Assert.AreEqual("먼저, 단서 하나를 눌러보게나.", steps["click_clue"].Line);
            Assert.AreEqual("그럼 그 안에 단서와 관련된 시간, 인물, 감정이 빈 칸으로 보일걸세. ‘메뉴얼’을 누르면 그 종류를 볼 수 있어.", steps["open_manual"].Line);
            Assert.AreEqual("다시 돌아오게.", steps["close_book"].Line);
            Assert.AreEqual("찾았나? 그렇다면 단서를 끌어서 사용해 보게.", steps["drag_clue"].Line);
            Assert.AreEqual("아래의 정보를 읽어 보게.", steps["read_xray"].Line);
            Assert.AreEqual("잘했어! 이제 마지막이네.", steps["clear_3"].Line);
            Assert.AreEqual("2번의 변형을 고려해서 최종 결과가 침체 쪽으로 기울게 해보게. 도움이 될 아이템을 하나 넣어 두었으니, 설명을 읽고 함께 활용해 봐.", steps["use_item"].Line);
            StringAssert.Contains("‘컴플렉스 중첩’", steps["stack_intro"].Line);
            StringAssert.Contains("열쇠 2개가 필요하네", steps["intro_key_count"].Line);

            // 원문의 오탈자("상태방")도 그대로 옮겼다 — 기획 확인 대상.
            StringAssert.Contains("상태방을 흥분시킬 수 있는 단서", steps["find_excited"].Line);
            Assert.AreEqual("내가 전문 최면 수사관을 너무 무시했나 보군. 완벽해. 당장 수사에 투입될 수 있도록 절차를 밟겠네.", TutorialGuideContent.ClearLine);
            Assert.AreEqual("청장", TutorialGuideContent.GuideName);
        }

        // ── 진행 ─────────────────────────────────────────────────────────────────

        [Test]
        public void Begin_AnnouncesFirstStepOnce()
        {
            var flow = TutorialGuideContent.CreateFlow();
            var seen = new List<string>();
            flow.StepChanged += s => seen.Add(s.Id);

            Assert.IsNull(flow.Current);
            Assert.IsFalse(flow.IsStarted);

            flow.Begin();
            flow.Begin(); // 두 번째는 아무 일도 없다

            CollectionAssert.AreEqual(new[] { "intro_device" }, seen);
            Assert.IsTrue(flow.IsStarted);
        }

        [Test]
        public void Notify_IgnoresEventsTheStepIsNotWaitingFor()
        {
            var flow = TutorialGuideContent.CreateFlow();
            flow.Begin();
            foreach (var _ in Enumerable.Range(0, 4)) flow.Notify(GuideAdvance.Read); // → click_clue (책이 열려야 넘어간다)
            Assert.AreEqual("click_clue", flow.Current.Id);

            Assert.IsFalse(flow.Notify(GuideAdvance.ManualTabShown), "책이 열리기 전의 메뉴얼 탭 알림은 넘기지 않는다");
            Assert.IsFalse(flow.Notify(GuideAdvance.Read), "행동을 기다리는 단계는 읽기 알림으로 안 넘어간다");
            Assert.IsFalse(flow.Notify(GuideAdvance.TurnResolved, 4));
            Assert.IsFalse(flow.Notify(GuideAdvance.ItemUsed));
            Assert.AreEqual("click_clue", flow.Current.Id);

            Assert.IsTrue(flow.Notify(GuideAdvance.BookOpened));
            Assert.AreEqual("open_manual", flow.Current.Id);
        }

        [Test]
        public void TurnResolved_NeedsTheWaitedTurn()
        {
            var flow = TutorialGuideContent.CreateFlow();
            flow.Begin();
            FastForward(flow, "drag_clue");

            Assert.IsFalse(flow.Notify(GuideAdvance.TurnResolved, 1), "턴 1 결산만으로는 안 끝난다(턴 2가 첫 키 판정)");
            Assert.AreEqual("drag_clue", flow.Current.Id);
            Assert.IsTrue(flow.Notify(GuideAdvance.TurnResolved, 2));
            Assert.AreEqual("clear_1", flow.Current.Id);
        }

        [Test]
        public void Finished_FiresAfterTheLastStep()
        {
            var flow = TutorialGuideContent.CreateFlow();
            var finished = 0;
            flow.Finished += () => finished++;
            flow.Begin();
            FastForward(flow, "branch_reminder");

            Assert.IsFalse(flow.IsFinished);
            Assert.IsTrue(flow.Notify(GuideAdvance.TurnResolved, 4));

            Assert.IsTrue(flow.IsFinished);
            Assert.IsNull(flow.Current);
            Assert.AreEqual(1, finished);
            Assert.IsFalse(flow.Notify(GuideAdvance.TurnResolved, 5), "끝난 뒤의 알림은 무시");
        }

        /// <summary>실제 튜토리얼 세션을 끝까지 플레이하며, 화면이 알릴 사건(턴 결산·아이템 사용)을 코어 이벤트로 흐름에 넣는다 — 카드 제시는 TurnRunner.TurnResolved로 감지된다.</summary>
        [Test]
        public void RealSession_DrivesTheFlowThroughTheWholeTutorial()
        {
            var session = TutorialContent.CreateSession(Polarity, new SystemRandomSource(1), new ClueKnowledgeLedger());
            var flow = TutorialGuideContent.CreateFlow();
            session.Runner.TurnBegan += turn => { if (turn == 1) flow.Begin(); };
            session.Runner.TurnResolved += report => flow.Notify(GuideAdvance.TurnResolved, report.Turn);
            session.Items.Used += _ => flow.Notify(GuideAdvance.ItemUsed);

            session.Runner.StartStage();
            Assert.AreEqual("intro_device", flow.Current.Id, "첫 턴이 시작될 때 안내가 시작된다");

            // 시작 안내 → 책
            GoTo(flow, "click_clue");
            Assert.IsTrue(flow.Notify(GuideAdvance.BookOpened));
            Assert.IsTrue(flow.Notify(GuideAdvance.ManualTabShown));
            Assert.IsTrue(flow.Notify(GuideAdvance.Read));
            Assert.IsTrue(flow.Notify(GuideAdvance.BookClosed));
            Assert.IsTrue(flow.Notify(GuideAdvance.Read));
            Assert.AreEqual("drag_clue", flow.Current.Id);

            // 턴 1: 달력. 턴 1·2가 연달아 결산되고 턴 2에서 넘어간다.
            session.Runner.PlayClue(session.Hand.Cards.First(c => c.Definition.Id == "tutorial_calendar"));
            Assert.AreEqual("clear_1", flow.Current.Id);
            GoTo(flow, "read_xray");
            Assert.IsTrue(flow.Notify(GuideAdvance.XrayOpened));
            Assert.AreEqual("find_depressed", flow.Current.Id);

            // 턴 3
            session.Runner.PlayClue(session.Hand.Cards.First(c => c.Definition.Id == "tutorial_daughter_photo"));
            Assert.AreEqual("clear_3", flow.Current.Id);
            GoTo(flow, "use_item");

            // 턴 4: 아이템을 쓰면 넘어가고, 카드를 내면 끝난다.
            session.Runner.UseItem(session.Items.Held[0]);
            Assert.AreEqual("branch_reminder", flow.Current.Id);
            session.Runner.PlayClue(session.Hand.Cards.First(c => c.Definition.Id == "tutorial_daughter_photo"));

            Assert.IsTrue(flow.IsFinished);
            Assert.AreEqual(StageOutcome.Cleared, session.Runner.Outcome);
        }

        [Test]
        public void RealSession_RejectedCardsDoNotAdvanceTheGuide()
        {
            var session = TutorialContent.CreateSession(Polarity, new SystemRandomSource(1), new ClueKnowledgeLedger());
            var flow = TutorialGuideContent.CreateFlow();
            session.Runner.TurnBegan += turn => { if (turn == 1) flow.Begin(); };
            session.Runner.TurnResolved += report => flow.Notify(GuideAdvance.TurnResolved, report.Turn);
            session.Runner.StartStage();
            FastForward(flow, "drag_clue");

            // 오답(게이트가 막아 PlayClue까지 가지 않는다) — 턴 결산이 없으니 안내는 그대로.
            var donut = session.Hand.Cards.First(c => c.Definition.Id == "tutorial_stale_donut");
            Assert.IsFalse(session.CheckPlay(donut).Allowed);
            Assert.AreEqual("drag_clue", flow.Current.Id);
        }

        /// <summary>단계가 기다리는 사건을 하나씩 알려 그 단계까지 간다(행동 단계도 알림으로 채운다).</summary>
        private static void FastForward(TutorialGuideFlow flow, string stepId)
        {
            var guard = 0;
            while (flow.Current != null && flow.Current.Id != stepId)
            {
                flow.Notify(flow.Current.Advance, flow.Current.WaitTurn);
                Assert.Less(guard++, 50);
            }

            Assert.IsNotNull(flow.Current);
            Assert.AreEqual(stepId, flow.Current.Id);
        }

        private static void GoTo(TutorialGuideFlow flow, string stepId)
        {
            // 읽기 단계만 건너뛴다 — 행동 단계는 호출자가 직접 알린다(멈추면 테스트가 잘못된 자리에 있는 것).
            var guard = 0;
            while (flow.Current != null && flow.Current.Id != stepId)
            {
                Assert.AreEqual(GuideAdvance.Read, flow.Current.Advance, $"{stepId}로 가는 길에 행동 단계 {flow.Current.Id}가 있다");
                flow.Notify(GuideAdvance.Read);
                Assert.Less(guard++, 50);
            }

            Assert.IsNotNull(flow.Current);
            Assert.AreEqual(stepId, flow.Current.Id);
        }

        // ── 게이트 안내 문구 ─────────────────────────────────────────────────────

        [Test]
        public void RejectionLine_Turn1_TellsTheEmotion_WithTheRightParticle()
        {
            string Line(params EmotionTag[] emotions) =>
                TutorialGuideContent.RejectionLine(PlayVerdict.Reject(PlayRejection.WrongDirection, emotions), 1, Emotion);

            Assert.AreEqual("이런, 이 감정은 ‘혐오’였네. 다시 한 번 생각해 봐.", Line(EmotionTag.Disgust));
            Assert.AreEqual("이런, 이 감정은 ‘공포’였네. 다시 한 번 생각해 봐.", Line(EmotionTag.Fear));
            Assert.AreEqual("이런, 이 감정은 ‘슬픔’이었네. 다시 한 번 생각해 봐.", Line(EmotionTag.Sadness), "받침이 있으면 이었네");
            Assert.AreEqual("이런, 이 감정은 ‘행복’이었네. 다시 한 번 생각해 봐.", Line(EmotionTag.Happiness));
            Assert.AreEqual("이런, 이 감정은 ‘사랑’이었네. 다시 한 번 생각해 봐.", Line(EmotionTag.Love));
            Assert.AreEqual("이런, 이 감정은 ‘분노’였네. 다시 한 번 생각해 봐.", Line(EmotionTag.Anger));
            Assert.AreEqual("이런, 이 감정은 ‘혐오·슬픔’이었네. 다시 한 번 생각해 봐.", Line(EmotionTag.Disgust, EmotionTag.Sadness), "여럿이면 마지막 글자로");
        }

        [Test]
        public void RejectionLine_LaterTurns_UseTheGenericLine()
        {
            var wrong = PlayVerdict.Reject(PlayRejection.WrongDirection, new[] { EmotionTag.Anger });

            Assert.AreEqual("이게 아니네. 다시 한 번 생각해 봐.", TutorialGuideContent.RejectionLine(wrong, 3, Emotion));
            Assert.AreEqual("이게 아니네. 다시 한 번 생각해 봐.", TutorialGuideContent.RejectionLine(wrong, 4, Emotion));
        }

        [Test]
        public void RejectionLine_ItemNeeded_AndAllowed()
        {
            Assert.AreEqual("도움이 될 아이템을 하나 넣어 두었으니, 설명을 읽고 함께 활용해 봐.",
                TutorialGuideContent.RejectionLine(PlayVerdict.Reject(PlayRejection.ItemNeeded), 4, Emotion));
            Assert.IsNull(TutorialGuideContent.RejectionLine(PlayVerdict.Allow, 1, Emotion));
        }

        [Test]
        public void RejectionLine_MatchesTheRealGateVerdicts()
        {
            var session = TutorialContent.CreateSession(Polarity, new SystemRandomSource(1), new ClueKnowledgeLedger());
            session.Runner.StartStage();

            var donut = session.CheckPlay(session.Hand.Cards.First(c => c.Definition.Id == "tutorial_stale_donut"));
            Assert.AreEqual("이런, 이 감정은 ‘혐오’였네. 다시 한 번 생각해 봐.",
                TutorialGuideContent.RejectionLine(donut, session.Runner.CurrentTurn, Emotion));

            session.Runner.PlayClue(session.Hand.Cards.First(c => c.Definition.Id == "tutorial_calendar"));
            var paper = session.CheckPlay(session.Hand.Cards.First(c => c.Definition.Id == "tutorial_paper_pile"));
            Assert.AreEqual("이게 아니네. 다시 한 번 생각해 봐.",
                TutorialGuideContent.RejectionLine(paper, session.Runner.CurrentTurn, Emotion), "턴 3의 오답");
        }

        [Test]
        public void EndsWithBatchim_HandlesEdgeCases()
        {
            Assert.IsTrue(TutorialGuideContent.EndsWithBatchim("행복"));
            Assert.IsFalse(TutorialGuideContent.EndsWithBatchim("공포"));
            Assert.IsFalse(TutorialGuideContent.EndsWithBatchim(""));
            Assert.IsFalse(TutorialGuideContent.EndsWithBatchim(null));
            Assert.IsFalse(TutorialGuideContent.EndsWithBatchim("ABC"), "한글이 아니면 받침 없음");
        }

        // ── 선택지 진행 규칙 ─────────────────────────────────────────────────────

        [Test]
        public void DialogueAdvanceRule_ChoiceLinesNeedTheChoiceClick()
        {
            Assert.IsTrue(DialogueAdvanceRule.Accepts(lineIsChoice: false, clickedTheChoice: false), "일반 줄은 아무 곳이나 눌러도");
            Assert.IsTrue(DialogueAdvanceRule.Accepts(lineIsChoice: false, clickedTheChoice: true));
            Assert.IsFalse(DialogueAdvanceRule.Accepts(lineIsChoice: true, clickedTheChoice: false), "선택지 줄은 배경 클릭으로 안 넘어간다");
            Assert.IsTrue(DialogueAdvanceRule.Accepts(lineIsChoice: true, clickedTheChoice: true));
        }

        // ── 시작 대화 에셋 / 화자 enum ───────────────────────────────────────────

        private const int Player = 0;
        private const int Chief = 2;

        private sealed class AssetLine
        {
            public int Speaker;
            public string Text;
            public bool IsChoice;
        }

        private static string AssetText() =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Resources", "StageDialogueLines.asset"), Encoding.UTF8);

        /// <summary>stageId가 tutorial인 항목의 stageStartFirst 줄들과 나머지 풀이 비어 있는지.</summary>
        private static (List<AssetLine> Lines, string EntryText) ParseTutorialEntry()
        {
            var text = AssetText().Replace("\r\n", "\n");
            var start = text.IndexOf("  - stageId: tutorial\n", StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, "stageId: tutorial 항목이 있어야 한다");
            var next = text.IndexOf("\n  - stageId:", start + 5, StringComparison.Ordinal);
            var entry = next < 0 ? text.Substring(start) : text.Substring(start, next - start);

            var lines = new List<AssetLine>();
            AssetLine current = null;
            foreach (var raw in entry.Split('\n'))
            {
                var trimmed = raw.Trim();
                var speaker = Regex.Match(trimmed, @"^- speaker: (\d+)$");
                if (speaker.Success)
                {
                    current = new AssetLine { Speaker = int.Parse(speaker.Groups[1].Value) };
                    lines.Add(current);
                    continue;
                }

                if (current == null) continue;
                var body = Regex.Match(trimmed, @"^text: '(.*)'$");
                if (body.Success) current.Text = body.Groups[1].Value.Replace("''", "'");
                if (trimmed == "isChoice: 1") current.IsChoice = true;
            }

            return (lines, entry);
        }

        [Test]
        public void SpeakerEnum_KeepsExistingValuesAndAddsChiefAtTheEnd()
        {
            // 에셋에는 화자가 정수로 저장된다 — Player 0, Yuki 1은 그대로, Chief는 끝(2).
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "UI", "Presentation", "StageDialogueLines.cs"), Encoding.UTF8);
            var body = Regex.Match(source, @"public enum DialogueSpeaker\s*\{(?<body>.*?)\}", RegexOptions.Singleline).Groups["body"].Value;
            var names = Regex.Matches(Regex.Replace(body, @"///.*", string.Empty), @"[A-Za-z]+").Select(m => m.Value).ToArray();

            CollectionAssert.AreEqual(new[] { "Player", "Yuki", "Chief" }, names);
        }

        [Test]
        public void OpeningDialogue_IsTheNotionConversation_WithThreeChoices()
        {
            var (lines, _) = ParseTutorialEntry();

            var expected = new (int Speaker, bool Choice, string Text)[]
            {
                (Chief, false, "유키는 무언가를 숨기고 있어. 그런데, 자신이 숨기고 있다는 사실 조차 모르는 느낌이랄까. 유키의 부모님은 바닷가에서 익사했고, 그 뒤에 ‘B’라는 남성이 입양해서 보호하던 것으로 파악되었네."),
                (Player, true, "B는 누군데 갑자기 유키를 입양한거죠?"),
                (Chief, false, "유키의 부모님이 사망하기 몇 달 전 부터 가족에게 수표로 금전적 지원을 해주고 있었거든. B는 거기에 더해서 매 주말마다 유키를 데리고 놀러가는 등, 거의 가족처럼 지냈던 모양이야."),
                (Player, true, "B에 대한 더 자세한 정보가 필요합니다."),
                (Chief, false, "음, 우리가 개인적으로 조사한 결과와 유키와 나눈 대화를 바탕으로 재구성한 정보가 있어. 확인하고 싶나?"),
                (Player, true, "네."),
                (Chief, false, "좋아. 그럼 잠시만 기다리게. 자네의 전문 분야인 최면 기법을 활용한 장치가 있거든. 이걸 구매하느라 경찰서의 한 달 운용 자금이 동나버렸으니, 최선을 다해 주게."),
            };

            Assert.AreEqual(expected.Length, lines.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Speaker, lines[i].Speaker, $"줄 {i} 화자");
                Assert.AreEqual(expected[i].Choice, lines[i].IsChoice, $"줄 {i} 선택지 여부");
                Assert.AreEqual(expected[i].Text, lines[i].Text, $"줄 {i} 본문");
            }

            Assert.AreEqual(3, lines.Count(l => l.IsChoice), "선택지 3곳");
            Assert.IsTrue(lines.Where(l => l.IsChoice).All(l => l.Speaker == Player), "선택지는 플레이어의 대답");
            Assert.IsTrue(lines.Where(l => !l.IsChoice).All(l => l.Speaker == Chief), "나머지는 전부 청장 — 유키나 임시 화자가 아니다");
            Assert.IsFalse(lines.Any(l => l.Text.StartsWith("/")), "원문의 '/'는 표식이라 본문에 넣지 않는다(화면이 붙인다)");
        }

        [Test]
        public void OpeningDialogue_HasNoReplayOrQuarterOrClearLines()
        {
            var (_, entry) = ParseTutorialEntry();

            StringAssert.Contains("stageStartReplay: []", entry, "다시 시작하면 시작 대화는 건너뛴다");
            StringAssert.Contains("quarterEnd:\n      excited: []\n      stable: []\n      depressed: []", entry);
            StringAssert.Contains("stageClear:\n      excited: []\n      stable: []\n      depressed: []", entry);
        }

        [Test]
        public void ExistingDialogueEntries_AreUntouchedByTheChoiceField()
        {
            // 선택지 필드가 없는 기존 스테이지 줄은 그대로 읽힌다 — 튜토리얼 밖의 항목에는 isChoice가 하나도 없다.
            var text = AssetText().Replace("\r\n", "\n");
            var tutorialStart = text.IndexOf("  - stageId: tutorial\n", StringComparison.Ordinal);
            Assert.Greater(tutorialStart, 0);
            var others = text.Substring(0, tutorialStart);
            StringAssert.DoesNotContain("isChoice", others);
        }
    }
}
