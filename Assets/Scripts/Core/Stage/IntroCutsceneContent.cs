using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 게임 시작 컷신(암흑 → 고래의 눈 → 암흑 → TV 뉴스 → 경찰서)의 글. 화면 구성과 시간은 UI(IntroCutsceneDirector)가 정하고,
    /// 여기는 뉴스 자막 문구만 가진다. <b>임시</b>: 노션 "게임 시작 연출"에는 뉴스가 "연회 살인 사건 보도"라고만 적혀 있어 문구가 없다 —
    /// 아래 3줄은 앞서 받은 PPT 애니마틱의 자막을 그대로 둔 것이라, 기획이 뉴스 원문을 주면 여기만 바꾸면 된다.
    /// 오프닝 몽타주의 포스트잇에 적히는 단서 이름(<see cref="ClueNames"/>)도 여기서 모은다.
    /// </summary>
    public static class IntroCutsceneContent
    {
        private static readonly string[] News =
        {
            "어제 새벽, 이그탈린 근방의 연회장에서 살인 사건이 발생했습니다.",
            "용의자는 10대 소녀로, 스테이크 나이프로 주최자 2인을 찔러 살해한 범인으로 지목되었습니다.",
            "목격자들의 증언과 현장 증거가 대부분 일치하는 것으로 보아 수사 종결 시기가 예상보다 앞당겨질 것으로 예상됩니다."
        };

        /// <summary>뉴스 자막 줄들(위에서 아래 순서 = 재생 순서).</summary>
        public static IReadOnlyList<string> NewsLines => News;

        /// <summary>오프닝 몽타주(어지럽게 쌓이는 포스트잇)에 적을 단서 이름 — 스테이지 1·2·3에 실제로 구현된 단서의 표시 이름을 겹침 없이 모은 것이다.
        /// 어떤 이름을 어떤 순서로 쓸지는 UI가 매번 무작위로 정한다.</summary>
        public static IReadOnlyList<string> ClueNames() =>
            PrototypeContent.Clues().Concat(Stage2Content.Clues()).Concat(Stage3Content.Clues())
                .Select(clue => clue.DisplayName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToArray();
    }
}
