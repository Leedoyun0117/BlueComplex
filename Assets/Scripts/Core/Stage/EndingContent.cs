using System.Collections.Generic;

namespace BlueComplex.Core.Stage
{
    /// <summary>크레딧 한 묶음: 역할 이름표 아래에 이름들이 줄줄이 나온다.</summary>
    public readonly struct CreditSection
    {
        public CreditSection(string role, params string[] names)
        {
            Role = role;
            Names = names;
        }

        public string Role { get; }
        public IReadOnlyList<string> Names { get; }
    }

    /// <summary>
    /// 엔딩(컷신 9 → 에필로그 → 크레딧)의 글. 화면 구성과 시간은 UI(EndingCutsceneDirector)가 정하고, 여기는 문구만 가진다.
    /// 에필로그는 노션 "엔딩" 문서 원문을 문단(빈 줄) 단위로 나눈 것이다 — 원문에 화자 태그가 없어 전부 플레이어 내레이션으로 흘리며,
    /// 오탈자·따옴표 방향도 원문 그대로 둔다(고치는 것은 기획 쪽 결정).
    /// </summary>
    public static class EndingContent
    {
        private const string Capture =
            "아수라장이 된 연회장 안에서 내가 마지막으로 본 것은, 창문 너머로 사라지는 B씨를 태운 자동차의 매연이었다. " +
            "얼마 지나지 않아 흰 연기를 뚫고 경찰이 들이닥치는 광경이 보였다.";

        // 노션 원문에서 빈 블록과 문단 안의 빈 줄(<br><br>)로 나뉜 7개. 문단 안의 한 줄 바꿈은 그대로 둔다.
        private static readonly string[] Epilogue =
        {
            "나는 마침내 진범 B를 찾을 수 있었다. 기억의 깊은 곳에서, B는 자신의 흔적을 지우지 못했다. 심문을 마치고 유키에게 모든 진실을 전해주었다. 처음 그녀는 무슨 말을 하는지 모르겠다며, 완강하게 부정했지만 우리가 여러 증거를 보여주자 마침내 받아들였다.",

            "“내 망가진 인생의.. 구원자가 되고 싶은 건가요? 뭐하러요? 뭐하러 그래요. 그냥 맛있는 저녁 식사와 함께 잊어버릴, 그리 특별하지 않은 기억. 그 이상도 이하도 아닐텐데. 구해준다느니, 지켜준다느니 하는 것들은 오래 전에 질렸어요.”",

            "”그래도 당신은 그 좀팽이들보단 조금 위로를 주는군요. 적어도, 오만하진 않으니까.”",

            "우리는 B의 심리 조작에 대한 증거를 모아갔고, 마침내 객관적인 사실로 인정받을 수준의 자료가 모이게 되었다. 유키는 유죄 선고를 피할 수 없었지만, 상당한 감형을 받아 원래 형량의 절반 정도로 줄었다.\n" +
            "연회가 열리던 날 밤, B가 타고 온 자동차가 어디로 향했는지 목격했다는 사람이 등장했다. 수사를 시작한 지 3개월 만에. \n" +
            "B를 태운 자동차의 기사를 만날 수 있었는데, 그는 B가 다른 승객들과는 다르게 아무것도 없는 한적한 바닷가에 내려 달라고 했다며, 지금도 의아하다고 말했다.",

            "몇 주간의 추가적인 수색 끝에, B의 신발 한 쪽을 몇 킬로 미터 떨어진 해안선에서 발견할 수 있었다. 이제 며칠 뒤면 전문 다이버들이 바다 깊은 곳을 뒤지기 시작할 것이다.\n" +
            "솔직하게, 나는 B의 시신이 발견되지 않기를 바라고 있다. B의 죽음은 유키의 삶 전체를 허망하게 만들지 않을까? ",

            "저번부터 유키에게 몇 차례 면회를 요청했지만 계속 거부했다는 답변만 돌아왔다. 사실 형사로서의 의무는 진즉에 끝난 것이나 마찬가지였지만, 알 수 없는 무언가가 계속 발목을 잡았다. ",

            "그런데 바로 오늘, 면회가 수락 되었다는 소식이 들려왔다. 유키에게 해줄 몇 마디 말을 종이에 적어갈 까 생각해봤지만, 곳 이어 그만두었다. 노을에 비친 흰 종이가 너무나 깨끗해 보였다.\n" +
            "나는 커피 한 잔과 함께 시간을 죽였다."
        };

        // 제작진 명단은 노션에 없다 — 구조만 두고 자리를 채운다. 실제 명단은 여기 이름만 바꾸면 된다.
        private static readonly CreditSection[] CreditRoll =
        {
            new CreditSection("기획", "(이름)"),
            new CreditSection("시나리오", "(이름)"),
            new CreditSection("프로그래밍", "(이름)", "(이름)"),
            new CreditSection("아트", "(이름)", "(이름)"),
            new CreditSection("사운드", "(이름)"),
        };

        /// <summary>컷신 9 "붙잡히는 유키" — 그림 없이 검은 화면에서 타이핑되는 한 덩어리 글.</summary>
        public static string CaptureText => Capture;

        /// <summary>에필로그 문단들(읽는 순서 = 재생 순서). 클릭 한 번에 한 문단씩 넘어간다.</summary>
        public static IReadOnlyList<string> EpilogueParagraphs => Epilogue;

        public const string CreditsTitle = "Blue Complex";
        public const string CreditsClosing = "플레이해 주셔서 감사합니다";

        public static IReadOnlyList<CreditSection> Credits => CreditRoll;
    }
}
