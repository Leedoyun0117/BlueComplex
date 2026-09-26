using System.Collections.Generic;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 게임 시작 컷신(암전+엔진 소리 → 차 주행 → 암전 → 시계 → 암전 → TV 뉴스 → 경찰서)의 글. 화면 구성과 시간은 UI(IntroCutsceneDirector)가 정하고,
    /// 여기는 뉴스 자막 문구만 가진다 — PPT 애니마틱 기준 3줄.
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
    }
}
