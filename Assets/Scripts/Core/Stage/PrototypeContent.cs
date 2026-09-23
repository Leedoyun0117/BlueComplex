using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

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
                "꽃 한 송이\n\n그 애는 뭐하고 있으려나? 몇 년이 흘러도 걔가 준 꽃을 보면 늘 떠올라. 꽃 이름이 뭐였더라?",
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
                new[] { EmotionTag.Happiness }),

            new ClueDefinition("s1_cookie_box", "쿠키 상자",
                "쿠키 상자\n\n김이 올라오는 갓 구운 쿠키야. 여러 모양들로 정성 들여 만들어진 것 같아.",
                TimeTag.Past,
                new[] { PersonTag.Family },
                new[] { EmotionTag.Happiness, EmotionTag.Love }),

            new ClueDefinition("s1_clock", "시계",
                "시계\n\n지금도 계속 움직이고 있어. 오늘도 시간을 허비한 걸까?",
                TimeTag.Present,
                NoPerson,
                new[] { EmotionTag.Sadness })
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

        /// <summary>현재 또는 미래 + 공포/혐오 → 과거로.</summary>
        public static ComplexDefinition Avoidance() => new(
            "complex_avoidance",
            "회피 컴플렉스",
            "고통스러운 현재나 미래를 과거의 일처럼 바꾸어 받아들인다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new TimeIsAnyOf(TimeTag.Present, TimeTag.Future),
                new HasAnyEmotion(EmotionTag.Fear, EmotionTag.Disgust)
            },
            new IComplexEffect[]
            {
                new ShiftTime(TimeTag.Past)
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

        // ------------------------------------------------------------------
        // 특성 (데이터) — 효과는 TraitDefinition의 숫자 필드로만 적는다. 계산 순서는 TurnRunner.PlayClue 주석.
        // ------------------------------------------------------------------

        public const string TraitSensitive = "trait_sensitive";
        public const string TraitHallucination = "trait_hallucination";
        public const string TraitGrandiosity = "trait_grandiosity";
        public const string TraitLethargy = "trait_lethargy";
        public const string TraitHighFunctioningDepression = "trait_high_functioning_depression";
        public const string TraitHyperexcitement = "trait_hyperexcitement";

        public static IReadOnlyList<TraitDefinition> Traits() => new[]
        {
            // 일반 특성 — 아이템 사용으로 발현, 1턴.
            new TraitDefinition(TraitSensitive, "예민", "한 턴간 심박수가 3배로 변화합니다.", TraitKind.Normal)
                { DefaultDuration = 1, HeartbeatMultiplier = 3.0 },
            new TraitDefinition(TraitHallucination, "환각", "최종 결과에서, 침체 감정과 흥분 감정을 반대로 받아들입니다. 심박수가 원래와 반대로 변화합니다.", TraitKind.Normal)
                { DefaultDuration = 1, InvertsPolarity = true },
            new TraitDefinition(TraitGrandiosity, "과대 망상", "단서의 감정을 두배로 느낍니다. 단서의 원래 감정 수 * 2가 전달됩니다.", TraitKind.Normal)
                { DefaultDuration = 1, EmotionCountMultiplier = 2 },
            new TraitDefinition(TraitLethargy, "무력", "한 턴간 심박수가 1/2배로 변화합니다.", TraitKind.Normal)
                { DefaultDuration = 1, HeartbeatMultiplier = 0.5 },

            // 특수 특성 — 컴플렉스가 최대 중첩을 넘쳐 발현될 때 붙고, 안정 구간에 들어서면 치유된다.
            new TraitDefinition(TraitHighFunctioningDepression, "고기능 우울증", "흥분 감정의 영향이 1/2가 됩니다. 침체 상태에서 컴플렉스가 넘치면 부여되고, 안정 상태에 들어서면 사라집니다.", TraitKind.Special)
                { ExcitedInfluence = 0.5, OverflowSide = Polarity.Depressed },
            new TraitDefinition(TraitHyperexcitement, "과흥분", "침체 감정의 영향이 1/2가 됩니다. 흥분 상태에서 컴플렉스가 넘치면 부여되고, 안정되면 치유됩니다.", TraitKind.Special)
                { DepressedInfluence = 0.5, OverflowSide = Polarity.Excited },
        };

        // ------------------------------------------------------------------
        // 아이템 7종 (데이터). 행동 조각(PrototypeItemBehaviours)에 숫자·감정·파라미터 키를 넘겨 조합한다.
        // ------------------------------------------------------------------

        /// <summary>StageConfig.ItemParameters에서 감정적 설득이 무시할 컴플렉스 id 목록을 찾는 키.</summary>
        public const string PersuasionTargetsKey = "persuasion.ignored_complexes";

        public static IReadOnlyList<ItemDefinition> Items() => new[]
        {
            new ItemDefinition("item_overcome", "극복",
                "지정한 컴플렉스의 지속 시간을 절반으로 줄인다.",
                duration: 0, new HalveComplexDuration(), ItemTargetKind.Complex),

            // 지속 시간 2턴은 기획서에 없어 프로토타입 값을 그대로 유지했다.
            new ItemDefinition("item_persuasion", "감정적 설득",
                "'침체' 감정에 영향을 주는 컴플렉스를 무시합니다.",
                duration: 2, new IgnoreStageComplexes(PersuasionTargetsKey)),

            new ItemDefinition("item_empathy", "기억 공감",
                "'공포', '슬픔' 감정을 결과에서 1씩 제거합니다. 환각 특성을 부여합니다.",
                duration: 1, new RemoveEmotions(1, EmotionTag.Fear, EmotionTag.Sadness),
                grantedTraitId: TraitHallucination),

            new ItemDefinition("item_recollection", "회상",
                "모든 단서를 풀에 넣고 다시 랜덤으로 부여합니다. 과대 망상 특성을 부여합니다.",
                duration: 0, new RedrawHand(),
                grantedTraitId: TraitGrandiosity),

            new ItemDefinition("item_logic", "논리적 설득",
                "결과에서 중복되는 감정을 하나씩 남기고 지웁니다.",
                duration: 1, new CollapseDuplicateEmotions()),

            new ItemDefinition("item_samaritan", "착한 사마리아인",
                "현재 심박수가 침체에 머물고 있다면, 심박수를 10 올립니다. 무력 특성을 부여합니다.",
                duration: 0, new RaiseHeartbeatInZone(Polarity.Depressed, 10),
                grantedTraitId: TraitLethargy),

            new ItemDefinition("item_selective_memory", "선택적 기억",
                "보유한 단서 중 선택한 하나를 랜덤으로 교체합니다.",
                duration: 0, new ReplaceClue(), ItemTargetKind.Clue),
        };

        /// <summary>구간 확률에 곱해지는 컴플렉스 발현 배율. 기획자 피드백으로 1.0에서 12.5% 올렸다 — 침체/흥분 30% → 33.75%, 매우 침체/흥분 50% → 56.25%, 안정 0% 유지.
        /// 스테이지 설정(<see cref="StageConfig.ComplexWeight"/>)의 값이라 스테이지마다 다르게 줄 수 있다.</summary>
        public const double PrototypeComplexWeight = 1.125;

        /// <summary>12턴 = 3쿼터 × 4턴 / 쿼터마다 키 판정 1회(쿼터 마지막 턴 종료 시점) / 키 2개 필요 / 키 폭 36.</summary>
        public static StageConfig PrototypeStage(IEmotionPolarityTable polarityTable) => new(
            "stage_1",
            "가라앉다",
            quarterCount: 3,
            turnsPerQuarter: 4,
            requiredKeys: 2,
            complexWeight: PrototypeComplexWeight,
            clues: Clues(),
            complexPool: Complexes(polarityTable),
            startingComplex: AntiPast(polarityTable),
            itemPool: Items(),
            keyWidth: 36,
            maxComplexSlots: ComplexBoard.DefaultMaxSlots,
            traits: Traits(),
            itemSlots: ItemInventory.DefaultCapacity,
            itemParameters: new Dictionary<string, IReadOnlyList<string>>
            {
                // 스테이지 1에서 '침체' 감정에 영향을 주는 컴플렉스 — 스톡홀름, 의존, 회피.
                [PersuasionTargetsKey] = new[] { "complex_stockholm", "complex_dependence", "complex_avoidance" }
            });
    }
}
