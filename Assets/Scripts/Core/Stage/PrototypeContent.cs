using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 기획서 '스테이지 기획 - 스테이지 1_"가라앉다"' 표를 그대로 옮긴 데이터.
    /// 본 빌드에서는 ScriptableObject로 교체하고 이 클래스는 테스트용으로만 남긴다.
    /// </summary>
    public static class PrototypeContent
    {
        private static readonly PersonTag[] NoPerson = System.Array.Empty<PersonTag>();

        /// <summary>UI상 표시는 (사물 이름/묘사) + 빈 줄 + (유키의 혼잣말) 두 문단이다. 문단 구분은 "\n\n"으로 옮겼다.</summary>
        public static IReadOnlyList<ClueDefinition> Clues() => new[]
        {
            new ClueDefinition("s1_family_photo", "가족사진 액자",
                "먼지가 쌓인 사진.\n\n아, 저 때 먹은 쿠키가 그리워. 엄마의 솜씨를 자랑하고 싶을 정도야.",
                TimeTag.Past,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s1_flower", "꽃 한 송이",
                "꽃 한 송이\n\n그 애는 뭐하고 있으려나?",
                TimeTag.Past,
                new[] { PersonTag.Friend },
                new[] { EmotionTag.Love }),

            new ClueDefinition("s1_broken_toy", "망가진 장난감",
                "망가진 장난감\n\n바닥에 떨어져 망가진 장난감. 우연히 책상이 흔들렸었나.",
                TimeTag.Present,
                NoPerson,
                new[] { EmotionTag.Sadness }),

            new ClueDefinition("s1_broccoli", "브로콜리",
                "브로콜리\n\n으엑. 브로콜리는 정말 싫어.",
                TimeTag.Present,
                NoPerson,
                new[] { EmotionTag.Disgust }),

            new ClueDefinition("s1_horror_novel", "공포 소설",
                "소설 책\n\n공포 소설이라.. 왜 무서운걸 이렇게 좋아하는거야?",
                TimeTag.Present,
                NoPerson,
                new[] { EmotionTag.Fear }),

            new ClueDefinition("s1_kids_doodle", "아이들의 낙서",
                "낙서\n\n..그녀석들을 친구라 부를 수 있을지 모르겠네. 아무렇게나 낙서해놓고 낄낄대는데 말이야.",
                TimeTag.Past,
                new[] { PersonTag.Other, PersonTag.Friend },
                new[] { EmotionTag.Anger }),

            new ClueDefinition("s1_amusement_ticket", "놀이공원 티켓",
                "놀이공원 티켓\n\n며칠 뒤면 다 함께 놀러가네!",
                TimeTag.Future,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s1_school_calendar", "개학 날짜 달력",
                "달력\n\n얼마 안남았네.. 도대체 개학이 왜 있는거야? 그녀석들 얼굴을 다시 봐야하다니..",
                TimeTag.Future,
                new[] { PersonTag.Other },
                new[] { EmotionTag.Sadness, EmotionTag.Disgust }),

            new ClueDefinition("s1_torn_backpack", "찢어진 책가방",
                "찢어진 책가방\n\n대뜸 칼을 들고와선 가방을 전부 찢어놓았지. 나쁜 자식..",
                TimeTag.Past,
                new[] { PersonTag.Other, PersonTag.Friend },
                new[] { EmotionTag.Fear, EmotionTag.Anger }),

            new ClueDefinition("s1_old_rabbit", "낡은 토끼 인형",
                "낡은 토끼 인형\n\n오래 되었지만 여전히 포근해.",
                TimeTag.Past,
                NoPerson,
                new[] { EmotionTag.Happiness })
        };

        // ── 스테이지 1 컴플렉스 10종. 지속 시간(턴)은 표의 값이다. ──────────────────────────

        /// <summary>가족 + 침체 감정 → 행복 추가.</summary>
        public static ComplexDefinition GoodChild(IEmotionPolarityTable polarityTable) => new(
            "complex_good_child",
            "착한 아이 컴플렉스",
            "가족과 관련된 기억에 행복한 감정을 더합니다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Family),
                new HasEmotionOfPolarity(Polarity.Depressed, polarityTable)
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Happiness)
            });

        /// <summary>과거 + 임의의 인물 + 행복/사랑 → 해당 감정을 혐오로.</summary>
        public static ComplexDefinition AntiPast(IEmotionPolarityTable _) => new(
            "complex_anti_past",
            "반 과거 컴플렉스",
            "과거의 행복한 감정을 혐오한다.",
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

        /// <summary>과거 + 친구 → 친구를 연인으로.</summary>
        public static ComplexDefinition ChildhoodFriend() => new(
            "complex_childhood_friend",
            "소꿉친구 컴플렉스",
            "어릴 적 친구를 연인으로 과대 해석 한다.",
            defaultDuration: 1,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                new HasPerson(PersonTag.Friend)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedPersonsTo(PersonTag.Lover)
            });

        /// <summary>인물 태그 + 공포 → 공포를 사랑으로.</summary>
        public static ComplexDefinition Stockholm() => new(
            "complex_stockholm",
            "스톡홀름 컴플렉스",
            "공포의 대상을 미화하여 사랑으로 받아들인다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new HasAnyPerson(),
                new HasAnyEmotion(EmotionTag.Fear)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedEmotionsTo(EmotionTag.Love)
            });

        /// <summary>연인 또는 친구 → 타인으로.</summary>
        public static ComplexDefinition Othering() => new(
            "complex_othering",
            "타자화 컴플렉스",
            "일종의 방어 기제. 모든 사건의 주체를 타인으로 돌린다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new HasAnyPersonOf(PersonTag.Lover, PersonTag.Friend)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedPersonsTo(PersonTag.Other)
            });

        /// <summary>타인 + 감정 태그 → 행복 추가.</summary>
        public static ComplexDefinition Optimism() => new(
            "complex_optimism",
            "낙관 컴플렉스",
            "세상을 낙관적으로 해석하는 컴플렉스.",
            defaultDuration: 1,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Other),
                HasAnyEmotion.Any()
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Happiness)
            });

        /// <summary>과거 → 그 단서의 감정을 하나씩 더 붙인다.</summary>
        public static ComplexDefinition Rumination() => new(
            "complex_rumination",
            "되새김 컴플렉스",
            "과거의 감정을 깊게 느끼는 컴플렉스.",
            defaultDuration: 1,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past)
            },
            new IComplexEffect[]
            {
                new RepeatEachEmotion()
            });

        /// <summary>가족 + 행복 → 행복을 슬픔으로.</summary>
        public static ComplexDefinition Guilt() => new(
            "complex_guilt",
            "죄책감 컴플렉스",
            "가족과의 행복에서 죄책감을 느낀다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Family),
                new HasAnyEmotion(EmotionTag.Happiness)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedEmotionsTo(EmotionTag.Sadness)
            });

        /// <summary>타인 + 슬픔 → 슬픔을 사랑으로.</summary>
        public static ComplexDefinition Dependence() => new(
            "complex_dependence",
            "의존 컴플렉스",
            "멀어진 사람에게서 오히려 애정을 찾는다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Other),
                new HasAnyEmotion(EmotionTag.Sadness)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedEmotionsTo(EmotionTag.Love)
            });

        /// <summary>과거 + 공포/혐오 → 과거를 현재로.</summary>
        public static ComplexDefinition Avoidance() => new(
            "complex_avoidance",
            "회피 컴플렉스",
            "고통스러운 과거를 현재의 일처럼 바꾸어 받아들인다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                new HasAnyEmotion(EmotionTag.Fear, EmotionTag.Disgust)
            },
            new IComplexEffect[]
            {
                new ShiftTime(TimeTag.Present)
            });

        public static IReadOnlyList<ComplexDefinition> Complexes(IEmotionPolarityTable polarityTable) => new[]
        {
            GoodChild(polarityTable),
            AntiPast(polarityTable),
            ChildhoodFriend(),
            Stockholm(),
            Othering(),
            Optimism(),
            Rumination(),
            Guilt(),
            Dependence(),
            Avoidance()
        };

        public static IReadOnlyList<ItemDefinition> Items(IEmotionPolarityTable polarityTable)
        {
            // 침체 감정에 관여하는 컴플렉스를 저작 단계에서 명시한다.
            var depressionTagger = new IdListDepressionTagger("complex_anti_past");

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

        /// <summary>12턴 = 3쿼터 × 4턴 / 쿼터마다 키 판정 1회(쿼터 마지막 턴 종료 시점) / 키 2개 필요 / 키 폭 36.</summary>
        public static StageConfig PrototypeStage(IEmotionPolarityTable polarityTable) => new(
            "stage_1",
            "가라앉다",
            quarterCount: 3,
            turnsPerQuarter: 4,
            requiredKeys: 2,
            complexWeight: 1.0,
            clues: Clues(),
            complexPool: Complexes(polarityTable),
            startingComplex: AntiPast(polarityTable),
            itemPool: Items(polarityTable),
            keyWidth: 36);
    }
}
