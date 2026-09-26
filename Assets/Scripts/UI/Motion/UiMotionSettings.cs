using UnityEngine;

namespace BlueComplex.UI.Motion
{
    /// <summary>
    /// 책상 위 종이 연출의 시간·크기 값 전부. 인스펙터에서 한 곳(Assets/Resources/UiMotionSettings.asset)만 고치면 된다 —
    /// 키 카드·쿼터 창처럼 코드로 짓는 UI는 프리팹이 없어 필드를 직렬화해 둘 곳이 없어서 Resources의 ScriptableObject로 모았다
    /// (UiIconCatalog와 같은 방식). 에셋이 없으면 아래 기본값으로 동작한다. 모든 시간은 초.
    /// </summary>
    [CreateAssetMenu(fileName = "UiMotionSettings", menuName = "BlueComplex/UI Motion Settings")]
    public sealed class UiMotionSettings : ScriptableObject
    {
        public const string ResourcePath = "UiMotionSettings";

        [Header("심박수 모니터")]
        [Tooltip("심박수가 바뀔 때 파형 높이·속도·BPM 숫자가 새 값으로 넘어가는 시간. 턴 결과 연출의 태그 상승(0.9초)과 맞춘다.")]
        public float heartTransition = 0.9f;
        [Tooltip("분기점이 바뀌어 목표 띠가 새 자리로 옮겨 가는 시간.")]
        public float bandMove = 0.6f;
        [Tooltip("키 턴에 목표 띠가 강조되며 맥동하는 한 주기.")]
        public float keyBandPulse = 0.7f;
        [Tooltip("키 판정 결과(성공/실패)가 띠에 번쩍이는 시간.")]
        public float bandResultFlash = 0.5f;
        [Tooltip("심박수 구간이 바뀔 때 상태 배지가 깜빡이며 교체되는 전체 시간.")]
        public float badgeFlicker = 0.5f;

        [Header("키 카드")]
        [Tooltip("열쇠가 찍히기 시작하는 높이(캔버스 픽셀).")]
        public float keyDropHeight = 90f;
        [Tooltip("열쇠가 위에서 떨어지는 시간.")]
        public float keyFall = 0.16f;
        [Tooltip("닿은 뒤 작게 튀었다 가라앉는 시간.")]
        public float keyBounce = 0.22f;
        [Tooltip("닿는 순간 밝아졌다가 원래 색으로 돌아오는 시간.")]
        public float keyFlash = 0.38f;
        [Tooltip("키 카드가 단서 패널 뒤에서 미끄러져 올라와 열리는 시간.")]
        public float keyDrawerOpen = 0.30f;
        [Tooltip("키 카드가 단서 패널 뒤로 다시 들어가는 시간.")]
        public float keyDrawerClose = 0.24f;
        [Tooltip("포인터가 열림 영역을 벗어난 뒤 닫히기 시작하기까지의 유예(초) — 열림 영역과 카드 사이를 오가는 동안 깜박이지 않게.")]
        public float keyDrawerCloseDelay = 0.25f;
        [Tooltip("키를 얻어 열쇠가 찍힌 뒤 카드가 스스로 열려 있는 시간(초). 포인터가 올라와 있으면 닫히지 않는다.")]
        public float keyPeekHold = 1.4f;

        [Header("단서 카드")]
        public float clueHover = 0.14f;
        [Tooltip("호버 시 떠오르는 높이(캔버스 픽셀).")]
        public float clueHoverLift = 6f;
        public float clueHoverScale = 1.04f;
        [Tooltip("집을 때 더 크게 들리는 시간.")]
        public float cluePickup = 0.12f;
        public float cluePickupScale = 1.16f;
        [Tooltip("집었을 때 기울어지는 각도(도).")]
        public float cluePickupTilt = 4f;
        [Tooltip("놓기 실패: 원위치로 날아가는 시간.")]
        public float clueReturnFly = 0.3f;
        [Tooltip("놓기 실패: 도착 뒤 종이처럼 흔들리며 안착하는 시간.")]
        public float clueReturnSettle = 0.42f;
        [Tooltip("놓기 성공: 기억 풍선으로 빨려 들어가는 시간.")]
        public float clueSuck = 0.38f;

        [Header("기억 풍선")]
        [Tooltip("선이 한 바퀴 돌며 그려지는 시간.")]
        public float bubbleDraw = 0.55f;
        [Tooltip("지우개로 지우듯 사라지는 시간.")]
        public float bubbleErase = 0.65f;

        [Header("대사창")]
        [Tooltip("한 글자가 나오는 간격.")]
        public float dialogueSecondsPerChar = 0.03f;
        [Tooltip("타이핑 소리를 몇 글자마다 한 번 낼지(1 = 매 글자).")]
        [Min(1)] public int dialogueSoundEvery = 1;
        [Tooltip("우하단 ▼가 한 번 어두워지는 시간(왕복은 두 배).")]
        public float dialogueNextBlink = 0.9f;

        [Header("엑스레이 판넬 (컴플렉스 인터페이스)")]
        [Tooltip("관절 팔이 펴지며 판넬이 나오는 시간. 단서를 집는 순간 발동하므로 너무 길면 드래그하는 동안 뇌가 안 보인다.")]
        public float xrayUnfold = 0.55f;
        [Tooltip("컴플렉스 반응이 끝나 판넬이 접혀 들어가는 시간.")]
        public float xrayFold = 0.45f;
        [Tooltip("판넬을 끌다 놓았을 때 제자리(접힘/펼침)로 돌아가는 시간.")]
        public float xrayReturn = 0.3f;
        [Tooltip("컴플렉스 발동 시 뇌의 해당 부분이 빛나는 시간(올라갔다 내려오는 전체).")]
        public float brainGlow = 0.4f;
        [Tooltip("컴플렉스 발동 시 초상화가 반응 표정으로 바뀌었다가 무표정으로 돌아오는 전체 시간.")]
        public float portraitFlash = 0.5f;

        [Header("기억 공간 결과 태그")]
        [Tooltip("결과 태그 칩이 풍선 안에 하나씩 나타나는 시간.")]
        public float tagPop = 0.3f;
        [Tooltip("칩이 다 나타난 뒤 가만히 읽히는 최소 시간(그 뒤 상승·페이드).")]
        public float tagHold = 0.8f;
        [Tooltip("태그가 위로 올라가며 서서히 사라지는 시간. 심박수 모니터 전환과 같이 시작한다.")]
        public float tagRise = 1.0f;

        [Header("아이템 패널")]
        [Tooltip("사용한 카드가 구겨져 사라지는 시간.")]
        public float itemUse = 0.5f;
        [Tooltip("빈 칸 옆에서 튀어나온 카드가 매우 작은 상태에서 빠르게 커지는 시간.")]
        public float itemPop = 0.14f;
        [Tooltip("커진 카드가 튀어나오는 옆자리(캔버스 픽셀, 가로 오프셋) — 이 자리에서 시작해 빈 칸 쪽으로 움직인다.")]
        public float itemPopOffset = 70f;
        [Tooltip("커진 카드가 빈 칸 쪽으로 움직여 빠르게 끼워지는 시간(찰칵 소리는 이 끝에서 난다).")]
        public float itemInsert = 0.42f;
        [Tooltip("대상 선택 모드에서 고를 수 있는 대상의 강조 테두리가 한 번 깜박이는(밝았다 어두워지는) 시간.")]
        public float targetPulse = 0.7f;

        [Header("포스트잇 (턴마다 떼어졌다 붙는 연출)")]
        [Tooltip("포스트잇이 떼어지는 시간: 우하단 모서리가 말려 올라가고 → 압정이 빠지며 → 튀었다가 떨어진다.")]
        public float postitPeel = 0.5f;
        [Tooltip("압정이 빠지는 시점(떼어지는 시간 중 몇 %). 이만큼 들린 뒤에야 압정이 빠지고 종이가 튄다.")]
        [Range(0.3f, 0.9f)] public float postitPinPopAt = 0.6f;
        [Tooltip("새 포스트잇이 위에서 내려와 눌리고 압정이 꽂히기까지 걸리는 시간.")]
        public float postitStick = 0.55f;
        [Tooltip("두 포스트잇(컴플렉스 → 분기)이 떼어지고 붙는 시작 시차.")]
        public float postitStagger = 0.1f;
        [Tooltip("떼어진 사이에 글자 쓰는 소리가 나며 내용이 새로 쓰이는 시간.")]
        public float postitWrite = 0.55f;

        [Header("키 턴 연출 (암전 + 나츠 독백)")]
        [Tooltip("화면이 어두워지거나 다시 밝아지는 시간. 어두워지는 건 포스트잇이 떼어지는 동안, 밝아지는 건 붙는 동안 함께 일어난다.")]
        public float keyTurnFade = 0.6f;
        [Tooltip("어두워졌을 때의 화면 어둡기(0 = 그대로, 1 = 완전한 암흑). 뒤의 방이 희미하게 보이는 정도.")]
        [Range(0f, 1f)] public float keyTurnDim = 0.86f;
        [Tooltip("독백 한 글자가 나오는 간격.")]
        public float keyTurnSecondsPerChar = 0.06f;
        [Tooltip("독백이 다 나온 뒤 화면이 밝아지기 전까지 머무는 시간(클릭하면 건너뛴다).")]
        public float keyTurnHold = 1.1f;

        [Header("분기 대사 장면 (쿼터가 끝날 때 게임 UI가 빠지고 배경과 대사만 남는다)")]
        [Tooltip("게임 UI가 사라지는 시간, 그리고 대사가 끝난 뒤 다시 나타나는 시간.")]
        public float branchSceneFade = 0.5f;

        [Header("튜토리얼 시작 컷신 (암흑 → TV 뉴스 → 경찰서). 전체 = 암전 + 암전 + 뉴스 줄 수×줄 시간 + 잡음 + 밝아짐 + 머묾. 게임 첫 실행의 오프닝은 아래 '오프닝 시퀀스'")]
        [Tooltip("컷신이 시작되며 화면이 검게 덮이는 시간.")]
        public float introFadeIn = 0.6f;
        [Tooltip("완전한 암흑만 있는 시간(뉴스가 흘러들어오기 전).")]
        public float introDark = 0.8f;
        [Tooltip("뉴스가 흘러들어오기 전의 잠잠해진 암흑.")]
        public float introBlackout = 1f;
        [Tooltip("뉴스가 암흑 속에서 흘러들어오며 나타나는 시간.")]
        public float introNewsFadeIn = 0.7f;
        [Tooltip("뉴스 자막 한 줄이 떠 있는 시간.")]
        public float introNewsLine = 2f;
        [Tooltip("자막이 다 끝난 뒤 지직거리는 잡음만 남아 있는 시간.")]
        public float introNewsStatic = 1.2f;
        [Tooltip("뉴스가 끝난 뒤 검은 화면에서 경찰서(취조실 그림)가 밝아지며 드러나는 시간.")]
        public float introReveal = 1.4f;
        [Tooltip("경찰서 그림이 완전히 밝아진 뒤 오프닝 대화가 시작되기까지 머무는 시간(그림 속 나츠, 주황빛 하늘의 창을 볼 시간).")]
        public float introRoomHold = 1.5f;
        [Tooltip("오프닝 대화가 끝난 뒤 경찰서 그림이 걷히며 게임(취조실) 화면으로 넘어가는 시간.")]
        public float introRoomOut = 0.8f;

        [Header("오프닝 시퀀스 (유키 방 → 포스트잇 → 단어 포스트잇 4장 → 창문 → 소녀 → Death. By. Complex. ?. → 뉴스 → 타이틀 → 메인 화면)")]
        [Tooltip("장면 사이 암전(검게 덮이고 다시 걷히는) 한 번의 시간.")]
        public float seqFade = 0.6f;
        [Tooltip("1: 유키 방이 밝아지며 머무는 시간(포스트잇이 붙기 전).")]
        public float seqRoomHold = 2.4f;
        [Tooltip("2: 유키 위에 포스트잇이 붙은 뒤 머무는 시간.")]
        public float seqYukiNoteHold = 1.8f;
        [Tooltip("3·6·12: 암전이 유지되는 시간.")]
        public float seqBlackout = 0.7f;
        [Tooltip("4: 단어 포스트잇 4장이 하나씩 붙는 간격(붙는 모션이 끝난 뒤부터). 장 사이 세 번이라 전체 등장 구간은 이 값의 세 배만큼 늘어난다.")]
        public float seqWindowInterval = 0.733f;
        [Tooltip("4: 4장이 다 붙은 뒤 2×2 창문 모양으로 미끄러져 정렬되는 시간(빠른 스냅).")]
        public float seqNotesSnap = 0.3f;
        [Tooltip("5: 정렬된 4장의 색이 흰색에서 주황으로 바뀌는 시간(배경은 그대로 어둡다).")]
        public float seqNotesRecolor = 0.45f;
        [Tooltip("7: 창문 앞에 소녀 실루엣이 나타나 정지 자세로 머무는 시간.")]
        public float seqWindowHold = 1.2f;
        [Tooltip("7: 소녀 실루엣이 창문 앞에 나타나는(밝아지는) 시간.")]
        public float seqGirlAppear = 0.6f;
        [Tooltip("8: 화면 전체가 주황빛으로 변하는 시간(소녀가 걷기 시작하기 직전).")]
        public float seqOrangeFill = 1f;
        [Tooltip("8~9: 소녀가 왼쪽 가장자리에서 오른쪽으로 걸어가는(키프레임 A→B→C) 시간.")]
        public float seqGirlWalk = 4f;
        [Tooltip("걷기 8프레임이 한 바퀴 도는 동안 나아가는 거리(화면 너비 비율). 클수록 발이 천천히 구른다 — 이동 속도에 프레임 속도를 맞추는 기준. 시트의 발 간격(앞발·뒷발 최대 벌림 약 197px)으로 잰 값이 약 0.18이다.")]
        [Range(0.05f, 0.6f)] public float seqGirlStride = 0.18f;
        [Tooltip("11: 종이색 전환이 끝난 뒤 화면 오른쪽 위에 거꾸로 선 소녀가 나타나기까지의 빈 시간. (소녀 사라짐 → 종이색 전환 → 이 시간 → 거꾸로 노출까지 합쳐 약 3.4초.)")]
        public float seqGirlGap = 0.3f;
        [Tooltip("11: 종이색 화면 위에 거꾸로 선 소녀가 정지 이미지로 보이는 시간.")]
        public float seqGirlFlash = 1.9f;
        [Tooltip("10: 소녀가 사라진 직후 화면이 종이색으로 바뀌고 포스트잇 바탕이 사라지는 시간.")]
        public float seqPaperTint = 1.2f;
        [Tooltip("12: 글자가 한 줄로 모이는 시간.")]
        public float seqTextGather = 1.3f;
        [Tooltip("12: 한 줄 문장이 타이핑되는 글자당 간격.")]
        public float seqTypeInterval = 0.11f;
        [Tooltip("12: 다 쳐진 뒤 머무는 시간.")]
        public float seqTextHold = 1.6f;
        [Tooltip("18: 타이틀이 나타나는 시간.")]
        public float seqTitleFade = 1.2f;
        [Tooltip("18: 타이틀이 머무는 시간.")]
        public float seqTitleHold = 1.8f;
        [Tooltip("19: 메인 화면이 밝아지는 시간.")]
        public float seqMenuFade = 1.2f;
        [Tooltip("취조시작을 누른 뒤 메인 화면이 어두워지는 시간, 그리고 게임 화면이 드러나는 시간.")]
        public float seqStartFade = 0.7f;

        [Header("스테이지 클리어 연출 (자물쇠 → 대사 → 컷신 → 다음 스테이지)")]
        [Tooltip("자물쇠 화면으로 넘어가며 화면이 어두워지는 시간.")]
        public float clearDimFade = 0.9f;
        [Tooltip("자물쇠가 어둠 속에 나타나는 시간.")]
        public float lockAppear = 0.5f;
        [Tooltip("열쇠가 자물쇠 쪽으로 날아 들어가는 시간.")]
        public float lockKeyFly = 0.5f;
        [Tooltip("열쇠가 꽂힌 채 돌아가는 시간.")]
        public float lockKeyTwist = 0.3f;
        [Tooltip("자물쇠 고리가 열리며 들리는 시간.")]
        public float lockUnlatch = 0.35f;
        [Tooltip("자물쇠가 열린 뒤 열쇠 그림만 서서히 사라지는 시간(자물쇠는 열린 채 남는다).")]
        public float lockKeyFade = 0.4f;
        [Tooltip("자물쇠 하나가 열린 뒤 다음 자물쇠로 넘어가기 전 쉼.")]
        public float lockInterval = 0.3f;
        [Tooltip("자물쇠가 다 열린 뒤(또는 못 열린 채로) 다음 단계로 넘어가기 전에 머무는 시간.")]
        public float lockHold = 0.9f;
        [Tooltip("클리어 대사가 나오는 동안 화면이 밝아지는 정도: 마지막 줄이 끝났을 때 남는 어둡기(0 = 완전히 밝음).")]
        [Range(0f, 1f)] public float clearBrightenFloor = 0.25f;
        [Tooltip("대사가 끝난 뒤 남은 어둠이 걷히며 컷신으로 넘어가는 시간.")]
        public float clearBrightenFinish = 1.1f;
        [Tooltip("컷신이 끝난 뒤 화면이 어두워지는 시간, 그리고 새 스테이지가 시작되며 밝아지는 시간.")]
        public float stageChangeFade = 1.0f;
        [Tooltip("스테이지 실패: 자물쇠도 함께 어두워지며 화면이 완전한 암흑이 되는 시간.")]
        public float failBlackout = 1.4f;
        [Tooltip("완전한 암흑에서 시작 화면으로 돌아가기 전에 머무는 시간.")]
        public float failBlackHold = 0.7f;
    }

    /// <summary>모션 값 조회. 에셋이 없으면 기본값 인스턴스를 만들어 쓴다.</summary>
    public static class UiMotion
    {
        private static UiMotionSettings _settings;

        public static UiMotionSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;

                _settings = Resources.Load<UiMotionSettings>(UiMotionSettings.ResourcePath);
                if (_settings != null) return _settings;

                _settings = ScriptableObject.CreateInstance<UiMotionSettings>();
                _settings.hideFlags = HideFlags.HideAndDontSave;
                return _settings;
            }
        }
    }
}
