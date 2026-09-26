using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>가이드 말풍선이 가리키는 화면 요소. <see cref="None"/>이면 강조 없이 글만 띄운다.</summary>
    public enum GuideTarget
    {
        None,

        /// <summary>손패의 단서 카드들.</summary>
        Cards,

        /// <summary>단서 정보 책의 "메뉴얼" 탭 버튼.</summary>
        ManualTab,

        /// <summary>컴플렉스 엑스레이 판넬.</summary>
        Xray,

        /// <summary>아이템 슬롯.</summary>
        Items
    }

    /// <summary>가이드 단계가 나오는 순간 함께 터지는 연출. 원문의 "(효과음)" 표시 지점 — 소리 자체는 UI가 훅(UiSoundCue)으로 낸다.</summary>
    public enum GuideEffect
    {
        None,

        /// <summary>최면 접속이 시작되는 효과음(원문 "그럼, 최면 접속을 시작할테니 준비하게." 뒤의 "(효과음)").</summary>
        HypnosisConnect
    }

    /// <summary>가이드 한 단계가 끝나는 조건. 화면(UI)이 일어난 일을 <see cref="TutorialGuideFlow.Notify"/>로 알린다.</summary>
    public enum GuideAdvance
    {
        /// <summary>다 읽으면(화면이 시간을 재거나 말풍선을 누르면) 넘어간다.</summary>
        Read,

        /// <summary>단서 정보 책이 열렸다.</summary>
        BookOpened,

        /// <summary>책의 "메뉴얼" 탭이 펼쳐졌다.</summary>
        ManualTabShown,

        /// <summary>책이 닫혔다.</summary>
        BookClosed,

        /// <summary>컴플렉스 엑스레이 판넬이 열렸다.</summary>
        XrayOpened,

        /// <summary>아이템을 썼다.</summary>
        ItemUsed,

        /// <summary>턴이 결산됐다(<see cref="TutorialGuideStep.WaitTurn"/>번째 이상). 카드를 낸 것은 <see cref="TurnRunner.TurnResolved"/>로 안다.</summary>
        TurnResolved
    }

    /// <summary>청장의 안내 한 줄과 그 줄이 무엇을 가리키고 언제 끝나는지.</summary>
    public sealed class TutorialGuideStep
    {
        public string Id { get; }
        public string Line { get; }
        public GuideTarget Target { get; }
        public GuideAdvance Advance { get; }

        /// <summary><see cref="GuideAdvance.TurnResolved"/>일 때 몇 번째 턴이 결산되면 끝나는지.</summary>
        public int WaitTurn { get; }

        /// <summary>이 줄이 나오는 순간 함께 터지는 효과. 대부분 없음.</summary>
        public GuideEffect Effect { get; }

        public TutorialGuideStep(string id, string line, GuideTarget target = GuideTarget.None,
                                 GuideAdvance advance = GuideAdvance.Read, int waitTurn = 0, GuideEffect effect = GuideEffect.None)
        {
            Effect = effect;
            Id = id;
            Line = line;
            Target = target;
            Advance = advance;
            WaitTurn = waitTurn;
        }
    }

    /// <summary>
    /// 튜토리얼 가이드의 진행 상태. 지금 어느 단계이고 무엇이 일어나면 다음 단계로 넘어가는지만 안다 — 말풍선·강조·클릭 막은 UI(TutorialGuide)가 이 상태를 읽어 그린다.
    /// 화면과 무관해서 EditMode로 검증할 수 있다. 단계에 맞지 않는 사건은 무시한다(예: 책이 열리기 전에 메뉴얼 탭이 알려져도 넘어가지 않는다).
    /// </summary>
    public sealed class TutorialGuideFlow
    {
        private readonly IReadOnlyList<TutorialGuideStep> _steps;
        private int _index = -1;

        public TutorialGuideFlow(IReadOnlyList<TutorialGuideStep> steps)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }

        public IReadOnlyList<TutorialGuideStep> Steps => _steps;

        /// <summary>지금 단계. 시작 전이거나 끝났으면 null.</summary>
        public TutorialGuideStep Current => _index >= 0 && _index < _steps.Count ? _steps[_index] : null;

        public bool IsStarted => _index >= 0;
        public bool IsFinished => _index >= _steps.Count;

        /// <summary>단계가 바뀔 때마다(시작 포함) 새 단계로 알린다.</summary>
        public event Action<TutorialGuideStep> StepChanged;

        /// <summary>마지막 단계가 끝났을 때.</summary>
        public event Action Finished;

        /// <summary>처음 단계로 시작한다. 이미 시작했으면 아무 일도 없다.</summary>
        public void Begin()
        {
            if (IsStarted) return;
            _index = 0;
            Announce();
        }

        /// <summary>일어난 일을 알린다. 지금 단계가 기다리던 것이면 다음 단계로 넘어가고 true. <paramref name="turn"/>은 <see cref="GuideAdvance.TurnResolved"/>일 때 결산된 턴 번호.</summary>
        public bool Notify(GuideAdvance happened, int turn = 0)
        {
            var current = Current;
            if (current == null || current.Advance != happened) return false;
            if (happened == GuideAdvance.TurnResolved && turn < current.WaitTurn) return false;

            _index++;
            Announce();
            return true;
        }

        private void Announce()
        {
            var current = Current;
            if (current != null) StepChanged?.Invoke(current);
            else Finished?.Invoke();
        }
    }

    /// <summary>
    /// 기획서 '게임 시작 연출 / 튜토리얼'의 청장 안내 대사와 단계 표. 문장은 노션 원문 그대로다(원문의 오탈자 "상태방"도 그대로 — 기획 확인 필요).
    /// 턴 번호는 4턴 구조(<see cref="TutorialContent"/>): 턴 1 흥분 실전, 턴 2 자동 넘김(첫 키 판정), 턴 3 반 과거, 턴 4 중첩 + 아이템.
    /// </summary>
    public static class TutorialGuideContent
    {
        public const string GuideName = "청장";

        /// <summary>튜토리얼 클리어 뒤 청장의 마지막 대사(노션 원문).</summary>
        public const string ClearLine = "내가 전문 최면 수사관을 너무 무시했나 보군. 완벽해. 당장 수사에 투입될 수 있도록 절차를 밟겠네.";

        public static TutorialGuideFlow CreateFlow() => new(Steps());

        public static IReadOnlyList<TutorialGuideStep> Steps() => new[]
        {
            // ▣ 이미지 자리 ② 오프닝 대화 끝 "→ 튜토리얼 시작" 직후: 장치 사진(원문에 이미지가 있고 아직 못 가져옴) — 이 줄이 나올 때 함께 보여 줄 자리.
            // 시작 안내(읽기)
            new TutorialGuideStep("intro_device",
                "이 장치에 대해서는 이미 잘 알겠지만, 장치를 사용한 지 오래되었다고 하니 내가 보조를 해주겠네. 이 기억은 유키와의 면담으로 구성한 훈련용 샘플이니 마음 편히 실력을 발휘해 주게나."),
            // 원문: 이 줄 다음에 "(효과음)" — 줄이 나오는 순간이 아니라 줄이 끝난 뒤에 울려야 하지만, 말풍선이 한 줄씩이라 다음 줄(intro_key) 시작에 건다.
            new TutorialGuideStep("intro_connect", "그럼, 최면 접속을 시작할테니 준비하게."),
            new TutorialGuideStep("intro_key",
                "지금 보다 더 깊은 기억으로 들어가려면, 장치 작동을 위한 ‘열쇠’가 필요해. 열쇠를 얻기 위해선 최면 대상을 장치가 원하는 심리 상태로 만들어야 한다네.",
                effect: GuideEffect.HypnosisConnect),
            new TutorialGuideStep("intro_key_count", "이번 기억에서는 열쇠 2개가 필요하네. 이번 분기에서 하나, 다음 분기에서 하나를 얻어야겠지."),

            // 단서 정보 책
            // ▣ 이미지 자리 ③ "먼저, 단서 하나를 눌러보게나." 뒤: 단서 카드를 눌러 정보 책이 열린 모습.
            new TutorialGuideStep("click_clue", "먼저, 단서 하나를 눌러보게나.", GuideTarget.Cards, GuideAdvance.BookOpened),
            // ▣ 이미지 자리 ④ "‘메뉴얼’을 누르면 그 종류를 볼 수 있어." 뒤: 메뉴얼 탭이 펼쳐진 모습.
            new TutorialGuideStep("open_manual",
                "그럼 그 안에 단서와 관련된 시간, 인물, 감정이 빈 칸으로 보일걸세. ‘메뉴얼’을 누르면 그 종류를 볼 수 있어.",
                GuideTarget.ManualTab, GuideAdvance.ManualTabShown),
            new TutorialGuideStep("read_manual", "빈 칸에 들어가는 요소들은 모두 메뉴얼 안에 있으니 참고하게나."),
            new TutorialGuideStep("close_book", "다시 돌아오게.", GuideTarget.None, GuideAdvance.BookClosed),

            // 턴 1 — 흥분 (특정 카드는 가리키지 않는다)
            new TutorialGuideStep("find_excited",
                "지금은 목표 심박수가 높으니까, 상태방을 흥분시킬 수 있는 단서를 사용해야 해. 흥분 감정과 관련된 단서를 찾아보게. 단서의 설명은 단서의 정보 말고도, 대상 인물이 이 단서에 대해 어떻게 생각하는지 엿볼 수 있는 좋은 수단이지."),
            // ▣ 이미지 자리 ⑤ 이 줄 뒤: 흥분 단서/심박수 변화를 보여 주는 그림(감정 하나 = 심박수 10).
            new TutorialGuideStep("emotion_ten", "감정 하나는 심박수를 10 변화시킬 수 있으니 참고하게."),
            new TutorialGuideStep("drag_clue", "찾았나? 그렇다면 단서를 끌어서 사용해 보게.", GuideTarget.None, GuideAdvance.TurnResolved, waitTurn: 2),

            // 첫 키 → 2분기, 컴플렉스
            new TutorialGuideStep("clear_1", "잘했어. 역시 아직 감이 녹슬지 않았구만."),
            new TutorialGuideStep("new_branch", "이제 다음 열쇠를 얻을 수 있는 새 분기로 넘어왔네. 여기서 열쇠를 하나 더 얻어야 해."),
            // ▣ 이미지 자리 ⑥ "…컴플렉스는 단서의 의미를 변형시켜." 뒤: 컴플렉스 설명 그림.
            new TutorialGuideStep("complex_intro",
                "대상에게 ‘컴플렉스’가 발현되었네. 컴플렉스는 심리적 필터라고 불리지. 그 이름에 맞게 컴플렉스는 단서의 의미를 변형시켜."),
            new TutorialGuideStep("read_xray", "아래의 정보를 읽어 보게.", GuideTarget.Xray, GuideAdvance.XrayOpened),

            // 턴 3 — 반 과거
            new TutorialGuideStep("find_depressed",
                "살펴봤나? 좋아, 그렇다면 컴플렉스의 변형을 고려해서 최종 결과가 침체 쪽으로 기울도록 하는 단서를 찾아보게.",
                GuideTarget.None, GuideAdvance.TurnResolved, waitTurn: 3),
            new TutorialGuideStep("clear_3", "잘했어! 이제 마지막이네."),

            // 턴 4 — 중첩 + 아이템
            new TutorialGuideStep("stack_intro",
                "대상에게 컴플렉스가 한 번 더 발현되었네. 이런 경우 변형된 결과가 다시 변형되게 돼. 우리는 이 상황을 ‘컴플렉스 중첩’이라고 부르지."),
            // ▣ 이미지 자리 ⑦ "…설명을 읽고 함께 활용해 봐." 뒤: 아이템(자아 비대) 슬롯/설명 그림.
            new TutorialGuideStep("use_item",
                "2번의 변형을 고려해서 최종 결과가 침체 쪽으로 기울게 해보게. 도움이 될 아이템을 하나 넣어 두었으니, 설명을 읽고 함께 활용해 봐.",
                GuideTarget.Items, GuideAdvance.ItemUsed),
            new TutorialGuideStep("branch_reminder",
                "모든 대화는 ‘분기점’으로 나뉜다네. 각 분기점에서 열쇠를 얻어야 다음 기억으로 넘어갈 수 있다는 사실을 잊지 말게나.",
                GuideTarget.None, GuideAdvance.TurnResolved, waitTurn: 4),
        };

        /// <summary>
        /// 게이트가 카드를 돌려보냈을 때 청장이 하는 말. 턴 1은 원문의 "이 감정은 ‘○○’였네"(카드의 감정을 알려 준다), 이후 턴은 "이게 아니네", 아이템이 필요하면 원문의 아이템 안내.
        /// 사유가 안내할 게 없으면 null.
        /// </summary>
        /// <param name="emotionName">감정의 한글 이름(화면 라벨).</param>
        public static string RejectionLine(PlayVerdict verdict, int turn, Func<EmotionTag, string> emotionName)
        {
            switch (verdict.Reason)
            {
                case PlayRejection.ItemNeeded:
                    return "도움이 될 아이템을 하나 넣어 두었으니, 설명을 읽고 함께 활용해 봐.";

                case PlayRejection.WrongDirection:
                    if (turn > 1) return "이게 아니네. 다시 한 번 생각해 봐.";

                    var emotions = string.Join("·", verdict.ObservedEmotions.Select(emotionName));
                    return emotions.Length == 0
                        ? "이런, 이 단서로는 안 되겠어. 다시 한 번 생각해 봐."
                        : $"이런, 이 감정은 ‘{emotions}’{(EndsWithBatchim(emotions) ? "이었네" : "였네")}. 다시 한 번 생각해 봐.";

                default:
                    return null;
            }
        }

        /// <summary>마지막 글자에 받침이 있는가 — "행복이었네 / 공포였네"처럼 서술격 조사 형태를 고른다(한글 음절이 아니면 받침 없음으로 본다).</summary>
        public static bool EndsWithBatchim(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var last = text[text.Length - 1];
            return last >= '가' && last <= '힣' && (last - '가') % 28 != 0;
        }
    }

    /// <summary>
    /// 대화 한 줄이 어떤 클릭으로 넘어가는지. 일반 줄은 (타이핑이 끝난 뒤) 아무 곳이나 눌러도 넘어가고, 선택지 줄은 선택지 자체를 눌러야만 넘어간다
    /// (배경을 눌러서 플레이어의 대답을 건너뛰지 못한다).
    /// </summary>
    public static class DialogueAdvanceRule
    {
        public static bool Accepts(bool lineIsChoice, bool clickedTheChoice) => !lineIsChoice || clickedTheChoice;
    }
}
