using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
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
                "이 장치에 대해서는 이미 잘 알겠지만, 장치를 사용한 지 오래되었다고 하니 내가 보조를 해주겠네. 이 기억은 경찰서의 한 순경과 협조해 구성한 훈련용 샘플이니 마음 편히 실력을 발휘해 주게나."),
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
            // ▣ 이미지 자리 ⑦ "…설명을 읽고 함께 활용해 봐." 뒤: 아이템(자아 비대·기억 공감) 슬롯/설명 그림.
            // 원문은 "아이템을 하나 넣어 두었으니"지만 2026-09-28 개정으로 아이템이 둘(자아비대·기억 공감)이다 — 문구는 원문 그대로 두고, 두 칸을 모두 강조하고 둘 다 써야 넘어간다(기획 확인 대상).
            new TutorialGuideStep("use_item",
                "2번의 변형을 고려해서 최종 결과가 한 번 더 침체 쪽으로 기울게 해보게. 도움이 될 아이템을 하나 넣어 두었으니, 설명을 읽고 함께 활용해 봐.",
                GuideTarget.Items, GuideAdvance.ItemUsed, requiredCount: 2),
            new TutorialGuideStep("trait_intro",
                "아이템을 사용하면 ‘특성’이 나타나기도 한다네. 특성 위에 마우스를 올리면 정보를 확인할 수 있으니 참고하게나."),
            new TutorialGuideStep("branch_reminder",
                "모든 대화는 ‘분기점’으로 나뉜다네. 각 분기점에서 열쇠를 얻어야 다음 기억으로 넘어갈 수 있다는 사실을 잊지 말게나.",
                GuideTarget.None, GuideAdvance.TurnResolved, waitTurn: 4),
        };

        /// <summary>
        /// 게이트가 카드를 돌려보냈을 때 청장이 하는 말. 턴 1은 원문의 "이 감정은 ‘○○’였네"(카드의 감정을 알려 준다), 이후 턴은 "이게 아니네", 아이템이 필요하면 원문의 아이템 안내.
        /// 결과를 끝까지 보여 준 오답(턴 4, <see cref="PlayRejection.KeyMissed"/>)도 "이게 아니네" — 연출이 끝난 뒤, 되돌리기 전에 나온다. 사유가 안내할 게 없으면 null.
        /// </summary>
        /// <param name="emotionName">감정의 한글 이름(화면 라벨).</param>
        public static string RejectionLine(PlayVerdict verdict, int turn, Func<EmotionTag, string> emotionName)
        {
            switch (verdict.Reason)
            {
                case PlayRejection.ItemNeeded:
                    return "도움이 될 아이템을 하나 넣어 두었으니, 설명을 읽고 함께 활용해 봐.";

                case PlayRejection.KeyMissed:
                    return RetryLine;

                case PlayRejection.WrongDirection:
                    if (turn > 1) return RetryLine;

                    var emotions = string.Join("·", verdict.ObservedEmotions.Select(emotionName));
                    return emotions.Length == 0
                        ? "이런, 이 단서로는 안 되겠어. 다시 한 번 생각해 봐."
                        : $"이런, 이 감정은 ‘{emotions}’{(EndsWithBatchim(emotions) ? "이었네" : "였네")}. 다시 한 번 생각해 봐.";

                default:
                    return null;
            }
        }

        private const string RetryLine = "이게 아니네. 다시 한 번 생각해 봐.";

        /// <summary>마지막 글자에 받침이 있는가 — "행복이었네 / 공포였네"처럼 서술격 조사 형태를 고른다(한글 음절이 아니면 받침 없음으로 본다).</summary>
        public static bool EndsWithBatchim(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var last = text[text.Length - 1];
            return last >= '가' && last <= '힣' && (last - '가') % 28 != 0;
        }
    }
}
