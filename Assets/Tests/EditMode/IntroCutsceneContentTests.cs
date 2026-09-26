using System.Linq;
using BlueComplex.Core.Stage;
using NUnit.Framework;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 시작 컷신(엔진 소리 → 차 → 시계 → 뉴스 → 경찰서)에서 Core에 있는 것은 뉴스 자막뿐이다: PPT 애니마틱 기준 3줄(줄당 2초 = 6초)이고, 빈 줄이 없어야 화면에 빈 자막 띠가 뜨지 않는다.
    /// 나머지 순서·시간은 UI 어셈블리(IntroCutsceneDirector)에 있어 Play 모드 프로브로 확인한다.
    /// </summary>
    public class IntroCutsceneContentTests
    {
        [Test]
        public void News_IsThreeNonEmptyLines()
        {
            Assert.AreEqual(3, IntroCutsceneContent.NewsLines.Count);
            Assert.IsTrue(IntroCutsceneContent.NewsLines.All(line => !string.IsNullOrWhiteSpace(line)));
        }

        [Test]
        public void News_FirstLineNamesTheIncident_LastLineNamesTheInvestigation()
        {
            Assert.That(IntroCutsceneContent.NewsLines[0], Does.Contain("살인 사건"));
            Assert.That(IntroCutsceneContent.NewsLines[2], Does.Contain("수사 종결"));
        }

        [Test]
        public void News_MatchesTheAnimaticScriptExactly()
        {
            CollectionAssert.AreEqual(new[]
            {
                "어제 새벽, 이그탈린 근방의 연회장에서 살인 사건이 발생했습니다.",
                "용의자는 10대 소녀로, 스테이크 나이프로 주최자 2인을 찔러 살해한 범인으로 지목되었습니다.",
                "목격자들의 증언과 현장 증거가 대부분 일치하는 것으로 보아 수사 종결 시기가 예상보다 앞당겨질 것으로 예상됩니다."
            }, IntroCutsceneContent.NewsLines.ToArray());
        }
    }
}
