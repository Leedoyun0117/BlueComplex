using System.Linq;
using BlueComplex.Core.Stage;
using NUnit.Framework;

namespace BlueComplex.Core.Tests
{
    /// <summary>엔딩 글: 컷신 9 원문, 에필로그가 빈 문단 없이 노션 원문 순서대로, 크레딧 자리 구조.</summary>
    public class EndingContentTests
    {
        [Test]
        public void Capture_KeepsTheSpecSentences()
        {
            Assert.That(EndingContent.CaptureText, Does.StartWith("아수라장이 된 연회장"));
            Assert.That(EndingContent.CaptureText, Does.Contain("B씨를 태운 자동차의 매연"));
            Assert.That(EndingContent.CaptureText, Does.EndWith("경찰이 들이닥치는 광경이 보였다."));
        }

        [Test]
        public void Epilogue_IsSevenNonEmptyParagraphs()
        {
            Assert.AreEqual(7,EndingContent.EpilogueParagraphs.Count);
            Assert.IsTrue(EndingContent.EpilogueParagraphs.All(p => !string.IsNullOrWhiteSpace(p)));
        }

        [Test]
        public void Epilogue_ParagraphsHaveNoBlankLinesInside()
        {
            // 빈 줄은 문단 경계다 — 한 문단 안에는 남지 않는다.
            Assert.IsTrue(EndingContent.EpilogueParagraphs.All(p => !p.Contains("\n\n")));
        }

        [Test]
        public void Epilogue_StartsWithTheCulpritAndEndsWithTheCoffee()
        {
            Assert.That(EndingContent.EpilogueParagraphs[0], Does.StartWith("나는 마침내 진범 B를"));
            Assert.That(EndingContent.EpilogueParagraphs[^1], Does.EndWith("나는 커피 한 잔과 함께 시간을 죽였다."));
        }

        [Test]
        public void Credits_HaveRolesAndNamedSlots()
        {
            Assert.IsNotEmpty(EndingContent.Credits);
            Assert.IsTrue(EndingContent.Credits.All(c => !string.IsNullOrWhiteSpace(c.Role) && c.Names.Count > 0));
        }
    }
}
