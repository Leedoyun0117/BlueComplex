using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 프로토타입 가이드의 표를 그대로 옮긴 데이터.
    /// 본 빌드에서는 ScriptableObject로 교체하고 이 클래스는 테스트용으로만 남긴다.
    /// </summary>
    public static class PrototypeContent
    {
        public static IReadOnlyList<ClueDefinition> Clues() => new[]
        {
            new ClueDefinition("clue_rabbit", "헤진 토끼 인형",
                "어린 시절 부모님이 유키에게 선물한 토끼 인형. 오랜 흔적이 가득하다.",
                TimeTag.Past,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Happiness, EmotionTag.Love }),

            new ClueDefinition("clue_adoption", "입양 동의서",
                "아직 잉크도 마르지 않은 문서. 처음 보는 누군가의 서명이 적혀있다.",
                TimeTag.Present,
                new[] { PersonTag.Other },
                new[] { EmotionTag.Fear, EmotionTag.Sadness }),

            new ClueDefinition("clue_urn", "유골함",
                "비교적 최근으로 보이는 도자기 함. 한 쌍의 남녀 사진과 함께 날짜가 적혀있다.",
                TimeTag.Past,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness }),

            new ClueDefinition("clue_toycar", "자동차 장난감",
                "일부러 망가뜨린 흔적이 가득한 자동차 장난감. 유키의 눈물 자국이 묻어있다.",
                TimeTag.Past,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness, EmotionTag.Disgust }),

            new ClueDefinition("clue_calendar", "달력",
                "유키의 입학일이 표시된 달력. 2주 뒤 입학하는 날이 표시되어 있다.",
                TimeTag.Future,
                new[] { PersonTag.Other, PersonTag.Family },
                new[] { EmotionTag.Fear }),

            // 기획서상 시간이 (과거, 미래) 두 개다. 시간 축은 1개만 유지하는 구조이므로
            // 런 시작 시 둘 중 하나를 뽑는 방식으로 확정할지 별도 결정이 필요하다.
            new ClueDefinition("clue_ticket", "비행기 티켓",
                "몇 주 전, 일본으로 떠나는 비행기 티켓. 여행 날짜가 쓰여있다.",
                TimeTag.Past,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness, EmotionTag.Happiness }),

            // 흥분 단서 5종. 이름/스토리는 기획 확정 전 placeholder — 임의로 지어내지 말 것.
            new ClueDefinition("clue_new_1", "(미정 단서 1)",
                "(스토리 미정 — 기획 확정 후 채워짐)",
                TimeTag.Past,
                new[] { PersonTag.Other },
                new[] { EmotionTag.Anger }),

            new ClueDefinition("clue_new_2", "(미정 단서 2)",
                "(스토리 미정 — 기획 확정 후 채워짐)",
                TimeTag.Present,
                new[] { PersonTag.Friend },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("clue_new_3", "(미정 단서 3)",
                "(스토리 미정 — 기획 확정 후 채워짐)",
                TimeTag.Past,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Love }),

            new ClueDefinition("clue_new_4", "(미정 단서 4)",
                "(스토리 미정 — 기획 확정 후 채워짐)",
                TimeTag.Future,
                new[] { PersonTag.Friend },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("clue_new_5", "(미정 단서 5)",
                "(스토리 미정 — 기획 확정 후 채워짐)",
                TimeTag.Past,
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness })
        };

        public static ComplexDefinition AntiPast(IEmotionPolarityTable _) => new(
            "complex_anti_past",
            "반 과거 컴플렉스",
            "과거의 좋은 감정들에 혐오를 느낍니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                new HasAnyPerson(),
                new HasAnyEmotion(EmotionTag.Happiness, EmotionTag.Love)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedEmotionsTo(EmotionTag.Disgust)
            });

        public static ComplexDefinition Stockholm() => new(
            "complex_stockholm",
            "스톡홀름 컴플렉스",
            "공포심을 주는 대상에게 사랑을 느낍니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasAnyPerson(),
                new HasAnyEmotion(EmotionTag.Fear)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedEmotionsTo(EmotionTag.Love)
            });

        public static ComplexDefinition Trauma(IEmotionPolarityTable polarityTable) => new(
            "complex_trauma",
            "트라우마 컴플렉스",
            "과거의 부정적인 감정이 현재까지 공포를 일으킵니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                new HasAnyPerson(),
                new HasEmotionOfPolarity(Polarity.Depressed, polarityTable)
            },
            new IComplexEffect[]
            {
                new ShiftTime(TimeTag.Present),
                new AddEmotion(EmotionTag.Fear)
            });

        public static IReadOnlyList<ComplexDefinition> Complexes(IEmotionPolarityTable polarityTable) => new[]
        {
            AntiPast(polarityTable),
            Stockholm(),
            Trauma(polarityTable)
        };

        public static IReadOnlyList<ItemDefinition> Items(IEmotionPolarityTable polarityTable)
        {
            // 침체 감정에 관여하는 컴플렉스를 저작 단계에서 명시한다.
            var depressionTagger = new IdListDepressionTagger("complex_anti_past", "complex_trauma");

            return new[]
            {
                new ItemDefinition("item_persuasion", "감정적 설득",
                    "'침체' 감정에 영향을 주는 컴플렉스를 무시합니다.",
                    duration: 2,
                    new EmotionalPersuasion(depressionTagger)),

                new ItemDefinition("item_empathy", "기억 공감",
                    "'공포', '슬픔' 감정을 결과에서 1씩 제거합니다. 예민 특성을 부여합니다.",
                    duration: 1,
                    new MemoryEmpathy(sensitiveDuration: 3)),

                new ItemDefinition("item_recollection", "회상",
                    "모든 단서를 풀에 넣고 다시 랜덤으로 부여합니다.",
                    duration: 0,
                    new Recollection())
            };
        }

        /// <summary>10턴 / 키 2개 / 3·5·7·9턴 키 등장.</summary>
        public static StageConfig PrototypeStage(IEmotionPolarityTable polarityTable) => new(
            "stage_prototype",
            "프로토타입",
            totalTurns: 10,
            requiredKeys: 2,
            keyTurns: new[] { 3, 5, 7, 9 },
            complexWeight: 1.0,
            clues: Clues(),
            complexPool: Complexes(polarityTable),
            startingComplex: AntiPast(polarityTable),
            itemPool: Items(polarityTable));
    }
}
