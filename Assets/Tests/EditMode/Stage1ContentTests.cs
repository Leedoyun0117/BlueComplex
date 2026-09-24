using System.Linq;
using NUnit.Framework;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 기획서 '스테이지 기획 - 스테이지 1_"가라앉다"' 표와 코드 데이터가 어긋나지 않는지 확인한다.
    /// 기댓값은 PrototypeContent를 거치지 않고 노션 표를 직접 옮겨 적었다 — 표가 바뀌면 여기부터 고친다.
    /// </summary>
    public class Stage1ContentTests
    {
        private static readonly IEmotionPolarityTable Polarity = new DefaultEmotionPolarityTable();

        private static readonly object[] ClueTable =
        {
            new object[] { "가족사진 액자", TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness } },
            new object[] { "꽃 한 송이", TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Love } },
            new object[] { "브로콜리", TimeTag.Present, new[] { PersonTag.Other }, new[] { EmotionTag.Disgust } },
            new object[] { "공포 소설", TimeTag.Present, new[] { PersonTag.Other }, new[] { EmotionTag.Fear } },
            new object[] { "아이들의 낙서", TimeTag.Past, new[] { PersonTag.Other, PersonTag.Friend }, new[] { EmotionTag.Anger } },
            new object[] { "놀이공원 티켓", TimeTag.Future, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness } },
            new object[] { "개학 날짜 달력", TimeTag.Future, new[] { PersonTag.Other }, new[] { EmotionTag.Sadness, EmotionTag.Disgust } },
            new object[] { "찢어진 책가방", TimeTag.Past, new[] { PersonTag.Other, PersonTag.Friend }, new[] { EmotionTag.Anger } },
            new object[] { "낡은 토끼 인형", TimeTag.Past, new[] { PersonTag.Friend }, new[] { EmotionTag.Happiness } },
            new object[] { "쿠키 상자", TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness, EmotionTag.Love } },
            new object[] { "시계", TimeTag.Present, new[] { PersonTag.Family }, new[] { EmotionTag.Sadness } },
            new object[] { "풍경화", TimeTag.Past, new[] { PersonTag.Other }, new[] { EmotionTag.Happiness } }
        };

        private static readonly (string Name, int Duration)[] ComplexTable =
        {
            ("착한 아이 컴플렉스", 2),
            ("반 과거 컴플렉스", 3),
            ("소꿉친구 컴플렉스", 1),
            ("스톡홀름 컴플렉스", 2),
            ("타자화 컴플렉스", 2),
            ("낙관 컴플렉스", 3),
            ("되새김 컴플렉스", 3),
            ("죄책감 컴플렉스", 2),
            ("의존 컴플렉스", 3),
            ("회피 컴플렉스", 2)
        };

        [Test]
        public void Clues_AreExactlyTheTwelveOfTheStageTable()
        {
            var clues = PrototypeContent.Clues();

            Assert.AreEqual(ClueTable.Length, clues.Count);
            CollectionAssert.AreEquivalent(
                ClueTable.Cast<object[]>().Select(row => (string)row[0]).ToList(),
                clues.Select(c => c.DisplayName).ToList());
            Assert.AreEqual(clues.Count, clues.Select(c => c.Id).Distinct().Count(), "단서 id는 서로 달라야 한다.");
        }

        [TestCaseSource(nameof(ClueTable))]
        public void Clue_MatchesItsTableRow(string name, TimeTag time, PersonTag[] persons, EmotionTag[] emotions)
        {
            var clue = PrototypeContent.Clues().Single(c => c.DisplayName == name);

            Assert.AreEqual(time, clue.Time, $"{name}: 시간 태그");
            CollectionAssert.AreEquivalent(persons, clue.Persons, $"{name}: 인물 태그 ('/ /' 는 빈 목록)");
            CollectionAssert.AreEquivalent(emotions, clue.Emotions, $"{name}: 감정 태그");
            Assert.IsFalse(string.IsNullOrWhiteSpace(clue.Story), $"{name}: UI상 표시 텍스트");
        }

        [Test]
        public void Clues_NoneUseTheNonePlaceholderTags()
        {
            foreach (var clue in PrototypeContent.Clues())
            {
                Assert.AreNotEqual(TimeTag.None, clue.Time, $"{clue.DisplayName}: 시간 태그가 비어 있다.");
                CollectionAssert.DoesNotContain(clue.Persons, PersonTag.None,
                    $"{clue.DisplayName}: 인물이 없으면 PersonTag.None이 아니라 빈 목록이어야 한다.");
            }
        }

        [Test]
        public void Complexes_AreExactlyTheTenFromTheStageTable_WithTheirDurations()
        {
            var complexes = PrototypeContent.Complexes(Polarity);

            Assert.AreEqual(ComplexTable.Length, complexes.Count);
            foreach (var (name, duration) in ComplexTable)
            {
                var complex = complexes.Single(c => c.DisplayName == name);
                Assert.AreEqual(duration, complex.DefaultDuration, $"{name}: 지속 시간(턴)");
                Assert.IsFalse(string.IsNullOrWhiteSpace(complex.Description), $"{name}: UI상 표시");
            }

            Assert.AreEqual(complexes.Count, complexes.Select(c => c.Id).Distinct().Count(), "컴플렉스 id는 서로 달라야 한다.");
        }

        [Test]
        public void Stage_UsesTheStage1Pools()
        {
            var config = PrototypeContent.PrototypeStage(Polarity);

            Assert.AreEqual(12, config.Clues.Count);
            Assert.AreEqual(10, config.ComplexPool.Count);
            Assert.AreEqual("가라앉다", config.DisplayName);
        }

        [Test]
        public void Stage_ItemPool_IsTheEightOfTheStage1Table_WithCoexistenceAndWithoutIndifference()
        {
            var names = PrototypeContent.PrototypeStage(Polarity).ItemPool.Select(i => i.DisplayName).ToList();

            CollectionAssert.AreEquivalent(
                new[] { "극복", "감정적 설득", "기억 공감", "회상", "논리적 설득", "착한 사마리아인", "선택적 기억", "공존감" }, names);
        }
    }
}
