using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 기획서 '스테이지 기획 - 스테이지 2_"천사"' 표를 그대로 옮긴 데이터. 스테이지 1(<see cref="PrototypeContent"/>)은 건드리지 않는다.
    /// 표의 "//"는 UI 표시 안에서 사물 설명과 유키의 혼잣말 사이 칸(스테이지 1은 빈 줄)이라 "\n\n"으로 옮겼고,
    /// 표의 &lt;br&gt;&lt;br&gt;는 "\n\n", &lt;br&gt;는 "\n"이다. 컴플렉스 id는 스테이지 1과 이름이 겹치는 것(착한아이)이 있어 전부 "stage2_" 접두어를 쓴다.
    /// </summary>
    public static class Stage2Content
    {
        private static readonly PersonTag[] NoPerson = System.Array.Empty<PersonTag>();

        public const string SetApplication = "stage2_set1";
        public const string SetBrokenFrame = "stage2_set2";
        public const string SetRain = "stage2_set3";

        public static IReadOnlyList<ClueDefinition> Clues() => new[]
        {
            new ClueDefinition("s2_application_form", "지원 신청서",
                "아동 지원 신청서 - 날짜\n\n19XX년 1월 19일.\n\n본인은 사회적 약자 아동 후원 및 보호를 신청합니다.\n\n서명 B.\n\n" +
                "B씨 덕분에 밀린 집세를 내고 집에 머무를 수 있게 되었어. 정말 고마운 분이야. 가족은 아니지만 그 만큼 친절하게 대해주시니까.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness },
                SetApplication),

            new ClueDefinition("s2_death_notice", "사망 통지서",
                "사망 통지서\n\n호시노 유키(모)와 하시모토 유키(부)의 사망을 통지합니다.\n\n사인 : 익사\n\n센 브람스 병원 19XX년 3월 20일\n\n…",
                new[] { TimeTag.Past },
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness }),

            new ClueDefinition("s2_soft_icecream", "소프트 아이스크림",
                "소프트 아이스크림.\n\nB씨가 방금 놓고 간 아이스크림이야. 이 아이스크림이 없었다면 B씨의 도움을 받지 못했겠지.",
                new[] { TimeTag.Past, TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s2_b_letter", "B의 편지",
                "유키, 이번 주말에 가족들과 바닷가로 놀러가는 거 어떠니? 어제 가봤는데 날씨가 정말 좋더구나.\n…\n" +
                "네가 하고싶어했던 ‘숨바꼭질’ 놀이를 해보렴. 바닷가에서 놀다가 아무도 모르게 바위 뒤에 숨는거지.\n" +
                "네가 가족과의 추억을 더 만들면 좋겠어. 그게 내 기쁨이란다.\n\nB씨가. 19XX.3.18\n\n" +
                "B씨는 날씨를 몰랐던 거겠지. 그래도.. 내 잘못은 아닐거야.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Other, PersonTag.Family },
                new[] { EmotionTag.Sadness },
                SetApplication),

            new ClueDefinition("s2_water_cup", "물이 담긴 컵",
                "물이 담긴 컵\n\n뭔가 두려워. 마실 수 없어..",
                new[] { TimeTag.Past },
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness, EmotionTag.Fear }),

            // 태그 칸의 "• 타인 태그 제거 고려(B에게 느끼는 감정이 아닐 수도 있음)"는 검토 메모라 태그는 표의 본문 그대로(타인 포함) 두었다.
            new ClueDefinition("s2_tv_news", "TV 뉴스",
                "TV 재난 보도 채널\n\n두 달전, 지역을 강타한 폭풍의 여파가 아직도 가시지 않고 있습니다. 폭풍의 징조는 몇 주 전부터 예고되었지만, 충분한 준비에도 불구하고 치명적인 피해를 피할 수 없었습니다.\n" +
                "—…\n19XX년 4월 19일 뉴스를 마칩니다.\n\n" +
                "그 날 바닷가에 가는게 아니었는데. 내가 정말 멍청했어.. 이제 되돌릴수도 없지만.. 겁쟁이 같이 구하러 뛰어들지도 못했으면서 뭘 후회 하는 걸까?",
                new[] { TimeTag.Past },
                new[] { PersonTag.Family, PersonTag.Other },
                new[] { EmotionTag.Sadness, EmotionTag.Fear, EmotionTag.Disgust },
                SetRain),

            new ClueDefinition("s2_table_note", "식탁 맡의 쪽지",
                "구겨진 쪽지.\n\n1. 식사 전에 이 액자를 포크로 3번 찔러 깨뜨린다.\n2. “시계”가 울리면 액자 속 종이를 꺼내 완전히 찢는다.\n\n" +
                "유키, 이 규칙은 너를 위한것이지만, 나를 위한 것이기도 해. 언제나 지켜주면 좋겠구나.\n\n" +
                "이 쪽지가 눈에 보이면 식사 시간이란 뜻이지!\nB씨의 규칙이 뭔가 이상하지만, 그의 요리는 정말 맛있어. 내일은 어떤 요리를 만들어주실까?",
                new[] { TimeTag.Past, TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness },
                SetBrokenFrame),

            new ClueDefinition("s2_broken_frame", "깨진 액자",
                "두 인물의 사진이 담긴 액자. 겉면이 깨지고 사진이 나뒹굴고있다.\n\n" +
                "B씨는 이 사람들이 악마와 같다고 매일 말하는데, 악마가 뭘까?\n\n" +
                "B씨를 화나게 했다면 좋은 사람들은 아니겠지. 이 사람은 왜 이렇게 무섭게 생긴거야?  으, 나중에 만날 일이 없으면 좋겠어.",
                new[] { TimeTag.Past, TimeTag.Future },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Disgust, EmotionTag.Fear },
                SetBrokenFrame),

            new ClueDefinition("s2_withered_flower", "시든 꽃",
                "시든 꽃\n\n마지막으로 본 지 한 달이 넘은 것 같아. 그 학교에서도 잘 지내고 있을까? 다시 만나고 싶어.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Friend },
                new[] { EmotionTag.Love, EmotionTag.Sadness }),

            // 09/25 노션에서 비어 있던 시계 칸이 채워졌다(스테이지 1의 시계와는 다른 스테이지 2 전용 항목).
            new ClueDefinition("s2_clock", "시계",
                "시계\n\n항상 움직이는 시곗 바늘을 보면, B씨와 연결된 느낌이 들어.",
                new[] { TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s2_rainwater_bowl", "빗물이 고인 그릇",
                "빗물이 고인 그릇\n\n지금도 조금씩 차오르고 있어. 비 따위는 평생 안 와도 돼.",
                new[] { TimeTag.Present },
                NoPerson,
                new[] { EmotionTag.Disgust },
                SetRain),

            new ClueDefinition("s2_white_flower", "흰 꽃",
                "흰 꽃\n\n부모님이 받아주실까?",
                new[] { TimeTag.Past },
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness }),

            new ClueDefinition("s2_baguette", "바게트",
                "바게트\n\nB씨가 저녁 식사를 위해 만들어 주셨어. 고소한 냄새가 기분을 좋게 만들어.",
                new[] { TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s2_fountain_pen", "만년필",
                "만년필\n\nB씨가 지원서에 서명한 만년필이야. 오래된 흔적이 보여.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness })
        };

        // ── 스테이지 2 컴플렉스 13종. 지속 시간(턴)은 표의 "지속 턴 수"다. ─────────────────────────

        /// <summary>과거 + 슬픔 → 슬픔 태그 -1.</summary>
        public static ComplexDefinition PastDenial() => new(
            "stage2_past_denial",
            "과거 부정 컴플렉스",
            "과거의 슬픔을 부정하여, 느끼지 않는다.",
            defaultDuration: 5,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                new HasAnyEmotion(EmotionTag.Sadness)
            },
            new IComplexEffect[]
            {
                new RemoveEmotion(EmotionTag.Sadness, 1)
            });

        /// <summary>타인 + 슬픔 → 슬픔 +3. (스테이지 1의 "착한 아이 컴플렉스"와는 다른 컴플렉스라 id를 나눴다.)</summary>
        public static ComplexDefinition KindChild() => new(
            "stage2_kind_child",
            "착한아이 컴플렉스",
            "타인의 슬픔에 자신 또한 동화되어 깊은 슬픔을 느낍니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Other),
                new HasAnyEmotion(EmotionTag.Sadness)
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Sadness, 3)
            });

        /// <summary>감정 태그 2개 이상(서로 다른 감정 종류) → 분노 +1.</summary>
        public static ComplexDefinition MixedEmotion() => new(
            "stage2_mixed_emotion",
            "복합 감정 컴플렉스",
            "한 번에 여러 감정이 들어오면, 분노를 느낍니다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new EmotionKindCountAtLeast(2)
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Anger, 1)
            });

        /// <summary>가족 + 감정 태그 → 가족을 타인으로.</summary>
        public static ComplexDefinition Rationalization() => new(
            "stage2_rationalization",
            "합리화 컴플렉스",
            "가족과 관련된 감정의 주체를 타인으로 변형해 합리화합니다.",
            defaultDuration: 5,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Family),
                HasAnyEmotion.Any()
            },
            new IComplexEffect[]
            {
                new ConvertMatchedPersonsTo(PersonTag.Other)
            });

        /// <summary>가족 + 침체 감정 → 해당 침체 감정 태그를 낱개(중첩 포함)마다 하나씩 더.</summary>
        public static ComplexDefinition SelfBlame(IEmotionPolarityTable polarityTable) => new(
            "stage2_self_blame",
            "자책 컴플렉스",
            "가족과 관련된 침체되는 감정을 자책하며 더 깊게 느낍니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Family),
                new HasEmotionOfPolarity(Polarity.Depressed, polarityTable)
            },
            new IComplexEffect[]
            {
                new RepeatEachEmotionOfPolarity(Polarity.Depressed, polarityTable)
            });

        /// <summary>현재 + 행복/사랑 → 시간 태그 '미래' 추가 + 행복 +2.</summary>
        public static ComplexDefinition OverExpectation() => new(
            "stage2_over_expectation",
            "과한 기대 컴플렉스",
            "현재의 행복이 미래까지 이어질 것이라고 기대하며, 강한 행복을 느낀다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Present),
                new HasAnyEmotion(EmotionTag.Happiness, EmotionTag.Love)
            },
            new IComplexEffect[]
            {
                new AddTime(TimeTag.Future),
                new AddEmotion(EmotionTag.Happiness, 2)
            });

        /// <summary>인물 태그 2개 이상 + 감정 태그 → 슬픔 +1.</summary>
        public static ComplexDefinition ScatteredMind() => new(
            "stage2_scattered_mind",
            "의식 분산 컴플렉스",
            "두 명 이상의 인물에 대한 감정을 느끼면, 의식이 분산되어 침체됩니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new PersonCountAtLeast(2),
                HasAnyEmotion.Any()
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Sadness, 1)
            });

        /// <summary>침체 + 흥분 감정이 동시에 → 행복을 (단서의 흥분 태그 개수, 중첩 포함) × 2만큼 추가. 개수는 추가 전 값이다.</summary>
        public static ComplexDefinition SelfAnger(IEmotionPolarityTable polarityTable) => new(
            "stage2_self_anger",
            "자기 분노 컴플렉스",
            "침체와 흥분 감정을 동시에 느끼고 있다면 흥분 감정을 더 깊게 느낀다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new HasBothPolarities(polarityTable)
            },
            new IComplexEffect[]
            {
                new AddEmotionPerEmotionOfPolarity(EmotionTag.Happiness, Polarity.Excited, 2, polarityTable)
            });

        /// <summary>표의 상세 정보가 `타인 → 타인→가족`으로만 적혀 있어 "타인 태그가 있으면 무조건 가족으로 변환"으로 옮겼다.</summary>
        public static ComplexDefinition Transference() => new(
            "stage2_transference",
            "전이 컴플렉스",
            "타인에게 가족의 모습을 겹쳐 본다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Other)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedPersonsTo(PersonTag.Family)
            });

        /// <summary>연인 + 사랑 → 사랑을 공포로.</summary>
        public static ComplexDefinition Distrust() => new(
            "stage2_distrust",
            "불신 컴플렉스",
            "가까운 사람의 애정을 두려움으로 받아들인다.",
            defaultDuration: 5,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Lover),
                new HasAnyEmotion(EmotionTag.Love)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedEmotionsTo(EmotionTag.Fear)
            });

        /// <summary>가족 + 연인 + 사랑 → 다른 감정 태그 모두 제거, 사랑 +1.</summary>
        public static ComplexDefinition OverInterpretation() => new(
            "stage2_over_interpretation",
            "과대 해석 컴플렉스",
            "가까운 사람의 사랑을 느끼면, 다른 감정은 모두 무시하고, 사랑만 받아들입니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Family),
                new HasPerson(PersonTag.Lover),
                new HasAnyEmotion(EmotionTag.Love)
            },
            new IComplexEffect[]
            {
                new KeepOnlyEmotion(EmotionTag.Love),
                new AddEmotion(EmotionTag.Love, 1)
            });

        /// <summary>가족 또는 연인 + 슬픔 → 행복 -1, 슬픔 +1.</summary>
        public static ComplexDefinition Persecution() => new(
            "stage2_persecution",
            "피해 망상 컴플렉스",
            "가까운 사람에게 슬픔을 느끼면 행복한 감정은 잊고, 슬픔을 더 깊게 느낍니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasAnyPersonOf(PersonTag.Family, PersonTag.Lover),
                new HasAnyEmotion(EmotionTag.Sadness)
            },
            new IComplexEffect[]
            {
                new RemoveEmotion(EmotionTag.Happiness, 1),
                new AddEmotion(EmotionTag.Sadness, 1)
            });

        /// <summary>과거 + 감정 → 감정 * 2 / 과거가 아니면(미래 또는 현재) + 감정 → 슬픔 +1.
        /// UI 표시 "과거의 감정이 아니라면 슬픔을 느낍니다"를 근거로 두 갈래를 배타로 옮겼다(과거+현재 단서는 배로만 느낀다).</summary>
        public static ComplexDefinition Overthinking() => new(
            "stage2_overthinking",
            "사고 과다 컴플렉스",
            "과거의 감정을 배로 느낍니다. 과거의 감정이 아니라면 슬픔을 느낍니다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new TimeIsAnyOf(TimeTag.Past, TimeTag.Present, TimeTag.Future),
                HasAnyEmotion.Any()
            },
            new IComplexEffect[]
            {
                new ByTime(TimeTag.Past, new DoubleEmotions(), new AddEmotion(EmotionTag.Sadness, 1))
            });

        public static IReadOnlyList<ComplexDefinition> Complexes(IEmotionPolarityTable polarityTable) => new[]
        {
            PastDenial(),
            KindChild(),
            MixedEmotion(),
            Rationalization(),
            SelfBlame(polarityTable),
            OverExpectation(),
            ScatteredMind(),
            SelfAnger(polarityTable),
            Transference(),
            Distrust(),
            OverInterpretation(),
            Persecution(),
            Overthinking()
        };

        /// <summary>스테이지 2 전용 컴플렉스 발현 확률(최종 값). 기본표(50/30/0/30/50%)에 배율을 곱한 값이 아니라 구간별로 직접 지정한다.</summary>
        public static IReadOnlyDictionary<HeartbeatState, double> SpawnChances { get; } = new Dictionary<HeartbeatState, double>
        {
            [HeartbeatState.VeryDepressed] = 0.8,
            [HeartbeatState.Depressed] = 0.5,
            [HeartbeatState.Stable] = 0.0,
            [HeartbeatState.Excited] = 0.5,
            [HeartbeatState.VeryExcited] = 0.8
        };

        /// <summary>
        /// 15턴 = 5쿼터 × 3턴 / 키 3개 필요(5쿼터 중) / 키 폭은 스테이지 1과 같은 36.
        /// 3쿼터 × 5턴은 손패(4장)보다 쿼터가 길어 쿼터 5번째 턴에 낼 카드가 없다 — 손패는 쿼터 시작에만 채워지므로 쿼터당 턴은 4 이하여야 한다.
        /// </summary>
        public static StageConfig Stage2(IEmotionPolarityTable polarityTable) => new(
            "stage_2",
            "천사",
            quarterCount: 5,
            turnsPerQuarter: 3,
            requiredKeys: 3,
            complexWeight: 1.0, // 전용 확률표(complexSpawnChances)가 있어 쓰이지 않는다
            clues: Clues(),
            complexPool: Complexes(polarityTable),
            startingComplex: null,
            itemPool: PrototypeContent.Stage2Items(),
            keyWidth: 36,
            maxComplexSlots: ComplexBoard.DefaultMaxSlots,
            traits: PrototypeContent.Traits(),
            itemSlots: ItemInventory.DefaultCapacity,
            itemParameters: new Dictionary<string, IReadOnlyList<string>>
            {
                // 기획서에 스테이지 2 목록이 없다 — 감정적 설득이 무시할 '침체 감정에 영향을 주는' 컴플렉스는
                // 효과가 침체 감정(슬픔·혐오·공포)을 더하거나 빼거나 바꾸는 것으로 잡았다. 기획 확인 필요.
                [PrototypeContent.PersuasionTargetsKey] = new[]
                {
                    "stage2_past_denial", "stage2_kind_child", "stage2_self_blame", "stage2_scattered_mind",
                    "stage2_distrust", "stage2_over_interpretation", "stage2_persecution", "stage2_overthinking"
                }
            },
            randomStartingComplex: true,
            complexSpawnChances: SpawnChances);
    }
}
