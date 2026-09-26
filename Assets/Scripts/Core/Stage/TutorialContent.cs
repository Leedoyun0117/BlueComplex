using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 기획서 '게임 시작 연출 / 튜토리얼' 표를 옮긴 데이터와 스크립트. 일반 스테이지와 완전히 분리된 스테이지(<see cref="StageId"/>)다 —
    /// 스테이지 번호 흐름(1→2→3)에 들어 있지 않고, 단서 id는 전부 "tutorial_" 접두어, 해금 장부(<see cref="ClueKnowledgeLedger"/>)도 본편과 따로 쓴다(<see cref="CreateSession"/>).
    ///
    /// 구조: 2쿼터 × 2턴(총 4턴, 키 2개). 균일한 <see cref="QuarterSchedule"/>을 그대로 쓰려고 원문의 3단계를 이렇게 재배치했다.
    ///  · 턴 1 — 흥분 단서를 내는 실전(정답 = 달력). 낸 뒤 손패를 비워 턴 2는 시간만 흐른다(필러): 결과 화면 뒤에 곧바로 첫 키가 판정된다("잘했어 → 키 획득 → 분기 변환").
    ///  · 턴 3 — 반 과거 하나가 붙은 첫 컴플렉스 실전(정답 = 어린 딸의 사진). 손패는 표 2의 4장.
    ///  · 턴 4 — 반 과거 + 자아 비판 중첩(아이템 자아비대를 함께 써야 두 번째 키). 손패는 표 2의 4장이 다시 채워지고, 아이템이 이 턴에 처음 손에 들어온다.
    ///    정답은 어린 딸의 사진 하나 — 반 과거(행복 → 혐오)와 자아 비판(침체가 있으니 슬픔 +1)이 둘 다 발동해 침체 둘이 된다. 나머지 셋은 흥분 그대로라 오답이다.
    ///
    /// 키 구역은 좌/우 끝이 아니라 정확한 심박수 범위다: 1쿼터 90~100, 2쿼터 70~80. 이 좁은 범위에 4턴 경로가 들어가도록 태그 하나의 영향력을 5로 줄였다(스테이지 기본 10).
    /// 영향력 10이면 자아비대를 켠 턴 4(−4태그)가 −40이라 범위를 지나쳐 버려 아이템이 오히려 독이 된다. 경로 계산의 근거는 TutorialContentTests에 검증으로 남아 있다.
    /// </summary>
    public static class TutorialContent
    {
        public const string StageId = "tutorial";

        /// <summary>태그 하나가 심박수에 미치는 영향력(기본 10의 절반). 본편은 기본값 그대로다 — 튜토리얼 세션에만 주입한다(<see cref="StageFactory.Create"/>의 tagMagnitude).</summary>
        public const int TagMagnitude = 5;

        /// <summary>시작 심박수. 경로: 93 → 턴 1 후 98 → 턴 2(넘김) 98 → 턴 3 후 93 → 턴 4 후 73(자아비대 사용, 아이템 없이는 83).</summary>
        public const int StartHeartbeat = 93;

        /// <summary>1쿼터 키 구역(양 끝 포함) — 턴 2가 끝날 때 판정한다.</summary>
        public const int Quarter1KeyMin = 90;
        public const int Quarter1KeyMax = 100;

        /// <summary>2쿼터 키 구역(양 끝 포함) — 턴 4가 끝날 때 판정한다.</summary>
        public const int Quarter2KeyMin = 70;
        public const int Quarter2KeyMax = 80;

        public const string EgoInflationItemId = "item_ego_inflation";

        // ── 단서 8종(표 1: 쿼터 1, 표 2: 쿼터 2). 이름 아래 본문은 스테이지 1~3과 같은 "이름\n\n본문" 표기다. 아이콘은 아직 없다(자리표시가 대신한다). ──

        public static ClueDefinition StaleDonut { get; } = new("tutorial_stale_donut", "상한 도넛",
            "상한 도넛\n\n반장님이 사오신, 일주일이 지나 상해버린 도넛. 보기만 해도 구역질이 나오는군.",
            TimeTag.Past, new[] { PersonTag.Other }, new[] { EmotionTag.Disgust });

        public static ClueDefinition HorrorPoster { get; } = new("tutorial_horror_poster", "공포 영화 포스터",
            "공포 영화 포스터\n\n일주일 뒤에 개봉하는 공포 영화야. 저런 영화를 왜 만드는지 모르겠어. 친구와 함께 영화관에 가기로 했는데, 포스터를 보기만 해도 살이 떨린다고!",
            TimeTag.Future, new[] { PersonTag.Friend }, new[] { EmotionTag.Fear });

        public static ClueDefinition Calendar { get; } = new("tutorial_calendar", "달력",
            "달력\n\n내일이면 긴 휴가 기간이야. 벌써부터 가족들과 보낼 시간이 기대되는군.",
            TimeTag.Future, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness });

        public static ClueDefinition EmptyFishbowl { get; } = new("tutorial_empty_fishbowl", "빈 어항",
            "빈 어항\n\n저번 주에 키우던 금붕어가 죽었어. 밥을 주는 걸 깜박 했었나? 괜시리 울적해 지니 어항을 치우던지 해야지.",
            TimeTag.Past, new[] { PersonTag.Other }, new[] { EmotionTag.Sadness });

        public static ClueDefinition PaperPile { get; } = new("tutorial_paper_pile", "산더미처럼 쌓인 서류",
            "산더미처럼 쌓인 서류\n\n어제 일을 그만둔 누군가가 나한테 떠넘긴 일들이 산더미야. 미안하지도 않나?",
            TimeTag.Past, new[] { PersonTag.Other }, new[] { EmotionTag.Anger });

        public static ClueDefinition PhoneRing { get; } = new("tutorial_phone_ring", "전화벨 소리",
            "전화벨 소리\n\n딸의 전화가 울리고 있어. 빨리 가서 받아야지.",
            TimeTag.Present, new[] { PersonTag.Family }, new[] { EmotionTag.Love });

        public static ClueDefinition FishingRod { get; } = new("tutorial_fishing_rod", "낙싯대",
            "낙싯대\n\n이번 주말에는 친구를 만나서 낚시를 갈거야.",
            TimeTag.Future, new[] { PersonTag.Friend }, new[] { EmotionTag.Happiness });

        public static ClueDefinition DaughterPhoto { get; } = new("tutorial_daughter_photo", "어린 딸의 사진",
            "어린 딸의 사진\n\n딸의 어릴 적 모습이야. 힘들 때 마다 보면 기분이 좋아져.",
            TimeTag.Past, new[] { PersonTag.Family }, new[] { EmotionTag.Happiness });

        /// <summary>턴 1의 손패(표 1): 침체 셋과 흥분 하나.</summary>
        public static IReadOnlyList<ClueDefinition> Quarter1Hand() => new[] { StaleDonut, HorrorPoster, Calendar, EmptyFishbowl };

        /// <summary>턴 3·4의 손패(표 2): 감정이 분노/사랑/행복/행복.</summary>
        public static IReadOnlyList<ClueDefinition> Quarter2Hand() => new[] { PaperPile, PhoneRing, FishingRod, DaughterPhoto };

        public static IReadOnlyList<ClueDefinition> AllClues() => Quarter1Hand().Concat(Quarter2Hand()).ToArray();

        // ── 컴플렉스 ─────────────────────────────────────────────────────────────

        /// <summary>반 과거는 스테이지 1의 것과 조건·효과·설명이 표와 같아 그대로 쓴다(<see cref="PrototypeContent.AntiPast"/>).</summary>
        public static ComplexDefinition AntiPast(IEmotionPolarityTable polarityTable) => PrototypeContent.AntiPast(polarityTable);

        /// <summary>침체 감정이 있으면 슬픔 +1(흥분 감정만 있으면 반응하지 않는다). 스테이지 3의 자아 부정(<c>Stage3Content.SelfDenial</c>)과는 별개다.
        /// 설명 문구와 지속 턴(2)은 확정값이다.</summary>
        public static ComplexDefinition SelfCriticism(IEmotionPolarityTable polarityTable) => new(
            "tutorial_self_criticism",
            "자아 비판 컴플렉스",
            "침체된 감정을 느끼면 자신을 탓하며 슬픔을 더한다.",
            defaultDuration: 2,
            new IComplexCondition[]
            {
                new HasEmotionOfPolarity(Polarity.Depressed, polarityTable)
            },
            new IComplexEffect[]
            {
                new AddEmotion(EmotionTag.Sadness)
            });

        // ── 아이템 ───────────────────────────────────────────────────────────────

        /// <summary>자아비대(결과 감정에 2를 곱한다) — 스테이지 1·2와 같은 정의다.</summary>
        public static ItemDefinition EgoInflation() =>
            PrototypeContent.CommonItems().First(item => item.Id == EgoInflationItemId);

        // ── 스테이지 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 단서 풀은 비워 둔다 — 손패는 전부 스크립트가 정해 주는 카드로 채워진다(<see cref="TutorialScript"/>). 컴플렉스도 확률로 붙지 않고 정해진 턴에 붙는다(<see cref="Schedule"/>).
        /// 아이템 풀도 비워 둔다 — 아이템 칸은 하나이고, 자아비대는 턴 4가 시작될 때 스크립트가 쥐여 준다(그 전에는 손에 없어 미리 쓸 수 없다).
        /// </summary>
        public static StageConfig Config(IEmotionPolarityTable polarityTable) => new(
            StageId,
            "튜토리얼",
            quarterCount: 2,
            turnsPerQuarter: 2,
            requiredKeys: 2,
            complexWeight: 1.0,
            clues: new List<ClueDefinition>(),
            complexPool: new[] { AntiPast(polarityTable), SelfCriticism(polarityTable) },
            startingComplex: null,
            itemPool: new List<ItemDefinition>(),
            keyWidth: Quarter1KeyMax - Quarter1KeyMin + 1,
            maxComplexSlots: ComplexBoard.DefaultMaxSlots,
            itemSlots: 1);

        /// <summary>컴플렉스 발현 표: 턴 2가 끝나면(= 2쿼터 시작) 반 과거, 턴 3이 끝나면 자아 비판.</summary>
        public static ScriptedComplexSchedule Schedule(IEmotionPolarityTable polarityTable) => new(new Dictionary<int, ComplexDefinition>
        {
            [2] = AntiPast(polarityTable),
            [3] = SelfCriticism(polarityTable)
        });

        public static TutorialScript Script(IEmotionPolarityTable polarityTable) => new(new[]
        {
            new TutorialStep(1, hand: Quarter1Hand(), targetPolarity: Polarity.Excited),
            new TutorialStep(2, hand: new ClueDefinition[0]),
            new TutorialStep(3, hand: Quarter2Hand(), targetPolarity: Polarity.Depressed),
            new TutorialStep(4, hand: Quarter2Hand(), targetPolarity: Polarity.Depressed,
                requiredActiveItemId: EgoInflationItemId, grantItem: EgoInflation())
        }, polarityTable);

        /// <summary>
        /// 튜토리얼 세션을 만든다. 일반 스테이지와 같은 <see cref="StageFactory"/>를 쓰되 키 구역·컴플렉스 발현·시작 심박수·태그 영향력을 정해진 값으로 주입하고, 스크립트(손패·게이트)를 붙인다.
        /// <paramref name="ledger"/>는 본편 장부를 넘기면 안 된다 — 튜토리얼에서 해금한 단서 속성이 본편에 새어 든다(호출자가 튜토리얼 전용 장부를 새로 만들어 넘긴다).
        /// 반환된 세션은 아직 시작 전이다: 호출자가 뷰를 붙인 뒤 <c>Runner.StartStage()</c>를 부른다.
        /// </summary>
        public static StageSession CreateSession(IEmotionPolarityTable polarityTable, IRandomSource random, ClueKnowledgeLedger ledger)
        {
            var config = Config(polarityTable);
            var schedule = Schedule(polarityTable);
            var keyPlacer = ScriptedKeyPlacer.FromRanges(
                (Quarter1KeyMin, Quarter1KeyMax),
                (Quarter2KeyMin, Quarter2KeyMax));

            var session = StageFactory.Create(config, random, ledger, polarityTable,
                heartbeatStartValue: StartHeartbeat,
                keyPlacer: keyPlacer,
                spawner: schedule,
                spawnPolicy: schedule,
                tagMagnitude: TagMagnitude);

            schedule.BindTurn(() => session.Runner.CurrentTurn);
            Script(polarityTable).Attach(session);
            return session;
        }
    }
}
