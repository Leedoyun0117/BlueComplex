using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// Assets/Resources/StageDialogueLines.asset의 스테이지 3 항목이 노션 "스테이지 시작 대화"(09/25 13:47) 표와 어긋나지 않는지 확인한다.
    /// 이 테스트 어셈블리는 UI 어셈블리를 참조하지 못해 에셋 YAML을 직접 읽는다(ComplexReactionLines 테스트와 같은 방식).
    /// 기댓값은 에셋을 거치지 않고 노션 표를 직접 옮겨 적었다. 화자: 0 = 플레이어(아저씨), 1 = 유키 — 스테이지 3 원문은 양쪽 다 큰따옴표라 내용으로 나눴다.
    /// 스테이지 3 클리어 대사는 노션이 비어 있어 아직 없다.
    /// </summary>
    public class StageDialogueLinesTests
    {
        private const int Player = 0;
        private const int Yuki = 1;

        private sealed class Parsed
        {
            public readonly Dictionary<string, List<List<(int Speaker, string Text)>>> Pools = new();
        }

        /// <summary>스테이지 id → ("replay" | "quarter.excited" | "clear.stable" …) → 변형 목록(줄 = 화자 + 텍스트).</summary>
        private static Dictionary<string, Parsed> ParseAsset()
        {
            var path = Path.Combine(Application.dataPath, "Resources", "StageDialogueLines.asset");
            var result = new Dictionary<string, Parsed>();
            Parsed entry = null;
            string section = null;
            string pool = null;
            var speaker = -1;

            foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                var line = raw.TrimEnd();
                var trimmed = line.TrimStart();

                if (line.StartsWith("  - stageId: "))
                {
                    entry = new Parsed();
                    result[line.Substring("  - stageId: ".Length)] = entry;
                    section = pool = null;
                    continue;
                }

                if (entry == null) continue;

                if (line == "    stageStartFirst:") { section = "first"; pool = "first"; continue; }
                if (line == "    stageStartReplay:") { section = "replay"; pool = "replay"; continue; }
                if (line == "    quarterEnd:") { section = "quarter"; pool = null; continue; }
                if (line == "    stageClear:") { section = "clear"; pool = null; continue; }

                if ((section == "quarter" || section == "clear") && line.StartsWith("      ") && !line.StartsWith("       ") && !trimmed.StartsWith("-") && trimmed.EndsWith(":"))
                {
                    pool = section + "." + trimmed.TrimEnd(':');
                    if (!entry.Pools.ContainsKey(pool)) entry.Pools[pool] = new List<List<(int, string)>>();
                    continue;
                }

                if (pool == null) continue;
                if (!entry.Pools.ContainsKey(pool)) entry.Pools[pool] = new List<List<(int, string)>>();

                if (trimmed == "- lines:")
                {
                    entry.Pools[pool].Add(new List<(int, string)>());
                }
                else if (trimmed.StartsWith("- speaker: ") || trimmed.StartsWith("speaker: "))
                {
                    speaker = int.Parse(trimmed.Substring(trimmed.IndexOf("speaker: ") + "speaker: ".Length));
                }
                else if (trimmed.StartsWith("text: '") && trimmed.EndsWith("'"))
                {
                    var text = trimmed.Substring("text: '".Length, trimmed.Length - "text: '".Length - 1).Replace("''", "'");
                    var variants = entry.Pools[pool];
                    if (variants.Count == 0) variants.Add(new List<(int, string)>());
                    variants[variants.Count - 1].Add((speaker, text));
                }
            }

            return result;
        }

        private static List<List<(int Speaker, string Text)>> Pool(Parsed entry, string pool) =>
            entry.Pools.TryGetValue(pool, out var variants) ? variants : new List<List<(int, string)>>();

        private static (int, string)[] Script(params (int, string)[] lines) => lines;

        [Test]
        public void Stage3_StartDialogue_IsTheTwoRandomVariantsFromTheTable_WithNoFixedFirstEntry()
        {
            var stage3 = ParseAsset()["stage_3"];

            Assert.AreEqual(0, Pool(stage3, "first").Sum(v => v.Count), "스테이지 3 원문은 '(랜덤'으로만 나뉜다 — 고정 '처음' 대사가 없다(스테이지 2와 같다).");

            var expected = new[]
            {
                Script(
                    (Player, "유키, 하던 이야기를 계속 해줄래?"),
                    (Yuki, "그러니까, 오늘 밤에 가는 연회가 두렵지는 않아요. 하지만, 뭔가 느낌이 좋지 않다고 할까.."),
                    (Player, "왜 이렇게 갑작스럽게 일정이 정해졌는지 알고 있어?"),
                    (Yuki, "아뇨. 전혀요. 오늘 낮 까지만 해도 평소랑 완전히 똑같았는 걸요. 평소처럼 액자를 깨고, 마네킹을 찔렀거든요."),
                    (Player, "평소와 같은 하루였구나."),
                    (Yuki, "네. 그리고 함께 점심도 먹었고, 창문에 붙은 먼지도 청소했죠."),
                    (Player, "좋아, 이제 내 이야기를 들어주겠니?")),
                Script(
                    (Player, "오늘 하루는 어떻게 보냈니?"),
                    (Yuki, "평소처럼요. 일어나서, 창 밖을 바라보며 시간을 떼우다가 밥을 먹었죠. 아, 지하실에도 내려갔다 왔고요."),
                    (Player, "그래, 이만하면 충분해."),
                    (Yuki, "가시려고요? 그러고 보니, 아저씨가 언제부터 여기 있었는지 잘 기억이 안나요."),
                    (Player, "…"),
                    (Yuki, "뭔가 꿈 속 같기도 하고.. B씨와 친구라고 하셨었나?"),
                    (Player, "B가 평소와 다른 말을 하지 않았니?"),
                    (Yuki, "아, 좀 전에 내일 연회장에 간다고 했었나."),
                    (Player, "좋아, 조금만 더 있다 가도 될까?"),
                    (Yuki, "네, 뭐. 저야 상관 없죠."))
            };

            var actual = Pool(stage3, "replay");
            Assert.AreEqual(2, actual.Count);
            for (var i = 0; i < expected.Length; i++)
                CollectionAssert.AreEqual(expected[i], actual[i].Select(l => (l.Speaker, l.Text)).ToArray(), $"시작 대사 {(char)('A' + i)}");
        }

        [Test]
        public void Stage3_QuarterEndDialogue_HasTwoExcited_ThreeStable_TwoDepressed_AllSpokenByYuki()
        {
            var stage3 = ParseAsset()["stage_3"];

            var expected = new Dictionary<string, string[]>
            {
                ["quarter.excited"] = new[]
                {
                    "생각해 보니, 아저씨 상당히 수상해요. 어떻게 몰래 들어왔어요?",
                    "아무 것도 기억나지 않아요. 뇌가 굳어버렸다고요."
                },
                ["quarter.stable"] = new[]
                {
                    "결국 처음부터 아무 것도 아니었던 거에요. 그걸 인정하면 마음이 편해지네요.",
                    "아이스크림도, 유골함도, 액자의 사진도, 전부 사라질 운명인거죠. 저도 예외는 아니겠지만.",
                    "바닥에 떨어져 녹아버린 아이스크림을 핥아 먹는 개미는 청소부일까요, 도둑 일까요?"
                },
                ["quarter.depressed"] = new[]
                {
                    "B씨라는 구멍을 매워버리면, 제 도화지에는 아무 것도 남지 않겠죠.",
                    "추억은 어릴 적 놀이터에서 생긴 흉터와 닮았죠. 좋은 감정이었지만 지금 보면 흉하거든요."
                }
            };

            foreach (var pair in expected)
            {
                var variants = Pool(stage3, pair.Key);
                Assert.AreEqual(pair.Value.Length, variants.Count, pair.Key);
                for (var i = 0; i < pair.Value.Length; i++)
                {
                    Assert.AreEqual(1, variants[i].Count, $"{pair.Key}[{i}]: 한 줄짜리 대사");
                    Assert.AreEqual(Yuki, variants[i][0].Speaker, $"{pair.Key}[{i}]: 화자");
                    Assert.AreEqual(pair.Value[i], variants[i][0].Text, $"{pair.Key}[{i}]");
                }
            }
        }

        [Test]
        public void Stage3_ClearDialogue_IsStillEmpty_BecauseTheTableIsEmpty()
        {
            var stage3 = ParseAsset()["stage_3"];

            foreach (var mood in new[] { "excited", "stable", "depressed" })
                Assert.AreEqual(0, Pool(stage3, "clear." + mood).Count, $"clear.{mood}: 노션이 비어 있어 넣지 않는다.");
        }

        [Test]
        public void Stage1And2Dialogue_AreUntouched_AndPoolsMayHaveDifferentSizesPerMood()
        {
            var all = ParseAsset();

            Assert.AreEqual((3, 3, 4), Sizes(all["stage_1"]), "스테이지 1: 흥분 3 / 안정 3 / 침체 4");
            Assert.AreEqual((3, 3, 3), Sizes(all["stage_2"]), "스테이지 2: 흥분 3 / 안정 3 / 침체 3");
            Assert.AreEqual((2, 3, 2), Sizes(all["stage_3"]), "스테이지 3: 흥분 2 / 안정 3 / 침체 2");
            Assert.AreEqual(3, Pool(all["stage_1"], "replay").Count);
            Assert.AreEqual(2, Pool(all["stage_3"], "replay").Count);
        }

        private static (int Excited, int Stable, int Depressed) Sizes(Parsed entry) =>
            (Pool(entry, "quarter.excited").Count, Pool(entry, "quarter.stable").Count, Pool(entry, "quarter.depressed").Count);
    }
}
