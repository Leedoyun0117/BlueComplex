using System.Linq;
using BlueComplex.Core.Stage;
using NUnit.Framework;

namespace BlueComplex.Core.Tests
{
    /// <summary>시작 컷신의 뉴스 자막: PPT 애니마틱 기준 3줄(줄당 2초 = 6초)이고, 빈 줄이 없어야 화면에 빈 자막 띠가 뜨지 않는다.</summary>
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
    }
}
