using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 기획서 '스테이지 기획 - 스테이지 3_"무제"' 표를 그대로 옮긴 데이터. 스테이지 1(<see cref="PrototypeContent"/>)·2(<see cref="Stage2Content"/>)는 건드리지 않는다.
    /// 표기 규칙은 <see cref="Stage2Content"/>와 같다: UI 표시의 "//"(사물 설명과 혼잣말 사이)는 "\n\n", &lt;br&gt;&lt;br&gt;는 "\n\n", &lt;br&gt;는 "\n".
    /// 컴플렉스 id는 스테이지 1·2와 이름이 겹치는 것(스톡홀름)이 있어 전부 "stage3_" 접두어를 쓴다.
    /// 노션에 상세 정보·지속 턴·반응 대사가 전부 비어 있는 "백지 컴플렉스"는 옮길 수 없어 뺐다(기획 확인 필요).
    /// </summary>
    public static class Stage3Content
    {
        public const string SetKnifeMap = "stage3_set1";

        public static IReadOnlyList<ClueDefinition> Clues() => new[]
        {
            new ClueDefinition("s3_bloody_knife", "피 묻은 나이프",
                "피 묻은 나이프\n\n방금 사용했어. 보기만 해도 그 악마들이 떠올라서 이마가 뜨거워져.",
                new[] { TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Anger },
                SetKnifeMap),

            new ClueDefinition("s3_scribbled_notebook", "낙서가 가득한 공책",
                "낙서가 가득한 공책\n\n어릴 때 미친 녀석들이 매일 괴롭힌 기록이야. 화내고 혐오해봤자 바뀌는건 없었어.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Disgust, EmotionTag.Anger }),

            // 표의 단서 이름은 "낡은 코트", UI 표시는 "비에 젖은 코트"로 시작한다(스테이지 1·2와 같이 이름 = 표의 단서 이름, 본문 = UI 표시).
            new ClueDefinition("s3_old_coat", "낡은 코트",
                "비에 젖은 코트\n\nB씨의 코트야.",
                new[] { TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Sadness }),

            // 표의 단서 이름은 따옴표가 붙은 ‘시계’다(스테이지 1·2의 시계와 구분하려는 표기로 보인다). 게임 안 이름은 다른 스테이지와 같이 "시계".
            new ClueDefinition("s3_clock", "시계",
                "시계\n\n알람이 울리면 액자를 깨뜨리거나 마네킹을 찌르는거야. 그러면, B씨의 웃음을 볼 수 있어. 어제도, 오늘도, 그리고 내일도 그러겠지.",
                new[] { TimeTag.Past, TimeTag.Present, TimeTag.Future },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness, EmotionTag.Love }),

            new ClueDefinition("s3_mannequin", "마네킹",
                "마네킹\n\n액자 안의 여자와 닮았어. 가끔 이것 때문에 화를 주체할 수 없을 때가 있어. 지금도 안에서 무언가가 부글거리는  느낌이 들어.",
                new[] { TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Anger },
                SetKnifeMap),

            new ClueDefinition("s3_marked_map", "장소가 표시된 약도",
                "저택이 표시된 약도\n\n내일이면 B씨와 이 곳에 방문 할 거야. 그 악마들이 여기에 모인데. 그것들이 서로 이야기하며 B씨를 괴롭힌다니,   생각만 해도 역겨워.",
                new[] { TimeTag.Future },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Disgust },
                SetKnifeMap),

            new ClueDefinition("s3_bouquet", "꽃다발",
                "꽃다발\n\nB씨의 선물이야. 내가 뭘 해낸건지 모르겠지만, 칭찬과 함께 웃으며 건내주었어. 시들지 않도록 오래 간직해야지.",
                new[] { TimeTag.Present },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Love }),

            new ClueDefinition("s3_soft_icecream", "소프트 아이스크림",
                "소프트 아이스크림\n\n오랜만에 먹는 아이스크림이야. 처음 B씨를 만났을 때가 떠올라. 그 때는 나도 정말 어렸는데.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s3_old_letter", "낡은 편지",
                "낡은 편지\n\n부모님이 써주신 마지막 생일 편지야. 주머니에 구겨져 있었는데, 오랜만에 보니까 기분이 좋아져.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Family },
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s3_picture_frame", "액자",
                "두 사람의 얼굴이 그려진 액자\n\n이들은 악마야. B씨가 슬퍼하는 이유일지도 몰라. 내일 그들을 만난다는데, 나도 모르게 화가 날까봐 걱정돼.",
                new[] { TimeTag.Future },
                new[] { PersonTag.Other },
                new[] { EmotionTag.Anger, EmotionTag.Fear },
                SetKnifeMap),

            new ClueDefinition("s3_urn", "유골함",
                "유골함\n\n..그리워.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness }),

            // 09/25 노션에서 "우산"이 "코트"로 바뀌었다(표의 "낡은 코트"와 다른 단서 — 이름 "코트", UI 표시는 "물기가 남은 코트").
            new ClueDefinition("s3_coat", "코트",
                "물기가 남은 코트\n\n차라리 장례식에 가지 않았으면 좋았을텐데. 매일 밤 그 장면이 나타나는데,  식은 땀에 이불이 젖을 때 까지 꿈 속에서 아무 말도 하지 못한 내가 싫어.",
                new[] { TimeTag.Past },
                new[] { PersonTag.Family },
                new[] { EmotionTag.Sadness, EmotionTag.Disgust })
        };

        // ── 스테이지 3 컴플렉스 12종(백지 제외). 지속 시간(턴)은 표의 "지속 시간(턴)"이다. ─────────────────

        /// <summary>과거 + 행복/사랑 → 그 감정을 슬픔으로.</summary>
        public static ComplexDefinition Futility() => new(
            "stage3_futility",
            "허망 컴플렉스",
            "과거의 행복한 기억을 전부 슬픔으로 해석합니다.",
            defaultDuration: 5,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                new HasAnyEmotion(EmotionTag.Happiness, EmotionTag.Love)
            },
            new IComplexEffect[]
            {
                new ConvertMatchedEmotionsTo(EmotionTag.Sadness)
            });

        /// <summary>슬픔이 있으면 슬픔 +2.</summary>
        public static ComplexDefinition Lethargy() => new(
            "stage3_lethargy",
            "무기력 컴플렉스",
            "슬픔 감정을 느끼고 있다면 그것이 배가 됩니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasAnyEmotion(EmotionTag.Sadness)
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Sadness, 2)
            });

        /// <summary>타인 + 공포 → 사랑 +1. (스테이지 1의 스톡홀름은 공포를 사랑으로 "변환"하지만 여기는 표에 "사랑 + 1"이라 공포는 그대로 두고 더한다.)</summary>
        public static ComplexDefinition Stockholm() => new(
            "stage3_stockholm",
            "스톡홀름 컴플렉스",
            "공포를 주는 사람에게 사랑의 감정을 느낍니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Other),
                new HasAnyEmotion(EmotionTag.Fear)
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Love, 1)
            });

        /// <summary>타인 → 타인 -1, 가족 +1.</summary>
        public static ComplexDefinition Clinging() => new(
            "stage3_clinging",
            "밀착 컴플렉스",
            "타인을 가족으로 받아들입니다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Other)
            },
            new IComplexEffect[]
            {
                new RemovePerson(PersonTag.Other, 1),
                new AddPerson(PersonTag.Family, 1)
            });

        /// <summary>과거 + 감정 태그 → 감정 태그 × 2, 혐오 +1. (곱하기가 먼저, 혐오 +1이 나중이다 — 표에 적힌 순서.)</summary>
        public static ComplexDefinition Reflection() => new(
            "stage3_reflection",
            "반사 컴플렉스",
            "오랜 시간 받아들인 감정을 더 깊게 느끼지만, 그런 자신의 모습에 혐오감을 가집니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                HasAnyEmotion.Any()
            },
            new IComplexEffect[]
            {
                new DoubleEmotions(),
                new AddEmotion(EmotionTag.Disgust, 1)
            });

        /// <summary>침체 감정 중첩(같은 침체 감정이 2개 이상 겹침) → 미래 +1.</summary>
        public static ComplexDefinition Precipice(IEmotionPolarityTable polarityTable) => new(
            "stage3_precipice",
            "낭떠러지 컴플렉스",
            "침체 감정을 깊게 느끼고 있다면, 그 감정이 미래까지 지속됩니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new HasStackedEmotionOfPolarity(Polarity.Depressed, polarityTable, minStack: 2)
            },
            new IComplexEffect[]
            {
                new AddTime(TimeTag.Future)
            });

        /// <summary>타인 + 행복 → 미래 +1(시간 태그 추가), 혐오 +1.</summary>
        public static ComplexDefinition ExpectationAnxiety() => new(
            "stage3_expectation_anxiety",
            "기대 불안 컴플렉스",
            "타인이 자신에 대한 기대로 행복해한다면, 미래의 일에 대해 혐오를 느낍니다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Other),
                new HasAnyEmotion(EmotionTag.Happiness)
            },
            new IComplexEffect[]
            {
                new AddTime(TimeTag.Future),
                new AddEmotion(EmotionTag.Disgust, 1)
            });

        /// <summary>침체 태그가 더 많으면 행복 +1, 흥분 태그가 더 많으면 혐오 +1(같으면 발동하지 않는다).</summary>
        public static ComplexDefinition SelfDenial(IEmotionPolarityTable polarityTable) => new(
            "stage3_self_denial",
            "자아 부정 컴플렉스",
            "지금 느끼는 감정과 반대되는 감정을 함께 느낍니다.",
            defaultDuration: 5,
            new IComplexCondition[]
            {
                new HasDominantPolarity(polarityTable)
            },
            new IComplexEffect[]
            {
                new ByDominantPolarity(polarityTable,
                    ifDepressed: new AddEmotion(EmotionTag.Happiness, 1),
                    ifExcited: new AddEmotion(EmotionTag.Disgust, 1))
            });

        /// <summary>가족 + 행복 → 침체 감정 모두 제거, 행복 +1.</summary>
        public static ComplexDefinition ParentalAttachment(IEmotionPolarityTable polarityTable) => new(
            "stage3_parental_attachment",
            "부모 애착 컴플렉스",
            "가족에 관한 행복한 기억이 나면, 지금 느끼는 침체되는 감정을 모두 무시하고 행복을 느낍니다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new HasPerson(PersonTag.Family),
                new HasAnyEmotion(EmotionTag.Happiness)
            },
            new IComplexEffect[]
            {
                new RemoveEmotionsOfPolarity(Polarity.Depressed, polarityTable),
                new AddEmotion(EmotionTag.Happiness, 1)
            });

        /// <summary>과거 → 과거 -1, 현재 +1.</summary>
        public static ComplexDefinition TimeMixing() => new(
            "stage3_time_mixing",
            "시간 혼합 컴플렉스",
            "과거의 기억을 현재로 착각합니다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past)
            },
            new IComplexEffect[]
            {
                new RemoveTime(TimeTag.Past),
                new AddTime(TimeTag.Present)
            });

        /// <summary>과거 + 감정 → 슬픔 +1, 혐오 +1.</summary>
        public static ComplexDefinition PastAvoidance() => new(
            "stage3_past_avoidance",
            "과거 회피 컴플렉스",
            "과거의 감정에 슬픔과 혐오감을 느낍니다.",
            defaultDuration: 4,
            new IComplexCondition[]
            {
                new TimeIs(TimeTag.Past),
                HasAnyEmotion.Any()
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Sadness, 1),
                new AddEmotion(EmotionTag.Disgust, 1)
            });

        /// <summary>현재 또는 미래 + 침체 감정 → 분노 +1.</summary>
        public static ComplexDefinition TimeInterpretation(IEmotionPolarityTable polarityTable) => new(
            "stage3_time_interpretation",
            "시간 해석 컴플렉스",
            "현재나 미래의 우울한 감정에 분노를 느낍니다.",
            defaultDuration: 3,
            new IComplexCondition[]
            {
                new TimeIsAnyOf(TimeTag.Present, TimeTag.Future),
                new HasEmotionOfPolarity(Polarity.Depressed, polarityTable)
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Anger, 1)
            });

        public static IReadOnlyList<ComplexDefinition> Complexes(IEmotionPolarityTable polarityTable) => new[]
        {
            Futility(),
            Lethargy(),
            Stockholm(),
            Clinging(),
            Reflection(),
            Precipice(polarityTable),
            ExpectationAnxiety(),
            SelfDenial(polarityTable),
            ParentalAttachment(polarityTable),
            TimeMixing(),
            PastAvoidance(),
            TimeInterpretation(polarityTable)
        };

        /// <summary>
        /// 15턴 = 5쿼터 × 3턴 / 키 3개 필요(5쿼터 중) / 키 폭은 스테이지 1·2와 같은 36.
        /// 기획서 스테이지 3 항목에는 턴 수가 없다 — 09/25 사용자 지시로 스테이지 2와 같은 구조(5분기점 × 3턴)로 맞췄다(처음엔 20턴 임시값이었다).
        /// 키 판정 시점·구역 배치는 <see cref="QuarterSchedule"/>에서 파생돼 스테이지 3 전용 타이밍 코드는 없다. 아이템 풀은 스테이지 3 표가 없어 공용 12종(<see cref="PrototypeContent.Stage1Items"/>).
        /// 컴플렉스 발현 확률은 스테이지 1과 같은 기본표 × <see cref="PrototypeContent.PrototypeComplexWeight"/>이고 키 손패 편향은 없다 — 전부 기획 확인 전 임시값이다.
        ///
        /// 밸런스 기록(09/25 확정, 시드 1000~3999 3000판, 완전 정보 휴리스틱 봇·아이템 미사용 기준 클리어율):
        /// 스테이지 1 19.5% / 스테이지 2 19.9% / 스테이지 3 12.6%. 아이템 봇 포함은 41.7% / 52.9% / 32.7%.
        /// 스테이지 3이 앞 두 스테이지보다 어려운 것은 의도된 난이도 상승이며, 이 15턴·키 3개 구성으로 승인됐다(추가 조정 없음).
        /// 20턴(키 3개)이던 임시 구성은 2.6% / 16.2%(1000시드)였다. 하니스: C:/Temp/bc-s2update/SimAll.cs, 테스트의 Balance_ThousandSeeds_* 로그.
        /// </summary>
        public static StageConfig Stage3(IEmotionPolarityTable polarityTable) => new(
            "stage_3",
            "무제",
            quarterCount: 5,
            turnsPerQuarter: 3,
            requiredKeys: 3,
            complexWeight: PrototypeContent.PrototypeComplexWeight,
            clues: Clues(),
            complexPool: Complexes(polarityTable),
            startingComplex: null,
            itemPool: PrototypeContent.Stage1Items(),
            keyWidth: 36,
            maxComplexSlots: ComplexBoard.DefaultMaxSlots,
            traits: PrototypeContent.Traits(),
            itemSlots: ItemInventory.DefaultCapacity,
            itemParameters: new Dictionary<string, IReadOnlyList<string>>
            {
                // 기획서에 스테이지 3 목록이 없다 — 스테이지 2와 같은 기준(효과가 침체 감정(슬픔·혐오·공포)을 더하거나 빼거나 바꾸는 컴플렉스)으로 잡았다. 기획 확인 필요.
                [PrototypeContent.PersuasionTargetsKey] = new[]
                {
                    "stage3_futility", "stage3_lethargy", "stage3_reflection", "stage3_expectation_anxiety",
                    "stage3_self_denial", "stage3_parental_attachment", "stage3_past_avoidance"
                }
            },
            randomStartingComplex: true);
    }
}
