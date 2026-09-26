using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신·오프닝 시퀀스가 쓰는 그림 묶음(Resources/IntroCutsceneArt.asset). 그림 파일은 제자리(Assets/Art)에 두고 여기서 참조만 한다 —
    /// 메뉴 BlueComplex/Cutscene/Rebuild Intro Art가 파일 이름으로 채운다. 그림이 없는 조각은 건너뛰거나 색 면으로 대신한다.
    /// </summary>
    public sealed class IntroCutsceneArt : ScriptableObject
    {
        public const string ResourcePath = "IntroCutsceneArt";

        [Header("오프닝 시퀀스 (PPT 목업 그림 — 자리표시)")]
        [Tooltip("OpeningYukiRoom.png — 1~2단계 배경: 유키 방(창문, 벽시계, 화분, 노을빛). PPT처럼 왼쪽 17.174%를 잘라 쓴다.")]
        public Texture2D yukiRoom;

        [Tooltip("IntroClock.png — PPT의 로마 숫자 아날로그 시계 문자판(바늘을 뺀 것, 테두리 원 바깥은 투명). 13단계에서 작게 나타났다가 14단계에서 확대된다.")]
        public Texture2D clock;

        [Tooltip("IntroClockHour.png — 시침만 떼어 낸 그림. 문자판과 같은 크기이고 회전 중심(허브)이 문자판 한가운데라 문자판 위에 그대로 겹쳐 돌린다.")]
        public Texture2D clockHour;

        [Tooltip("IntroClockMinute.png — 분침만 떼어 낸 그림(시침과 같은 규칙).")]
        public Texture2D clockMinute;

        [Tooltip("OpeningMainRoom.jpg — 19단계 가운데: 벽에 단서가 붙은 취조실(PPT처럼 왼쪽 9.563%·아래 13.852%를 잘라 쓴다).")]
        public Texture2D mainRoom;

        [Tooltip("OpeningMainChair.png — 19단계 의자(투명 배경).")]
        public Texture2D mainChair;

        [Tooltip("OpeningMainTable.png — 19단계 테이블(투명 배경, 스톡 사이트 워터마크 줄은 지웠다).")]
        public Texture2D mainTable;

        [Tooltip("OpeningMainCoffee.png — 19단계 테이블 위 모카포트와 잔(투명 배경, 좌우로 뒤집어 쓴다).")]
        public Texture2D mainCoffee;

        [Tooltip("OpeningMainTower.png — 19단계 왼쪽 창문의 시계탑 실루엣(투명 배경).")]
        public Texture2D mainTower;

        [Tooltip("girl_silhouette.png — 7~10단계: 주황 창문 앞에 정지 자세로 나타나는 소녀 실루엣(반신, 투명 배경).")]
        public Texture2D girlSilhouette;

        [Tooltip("content.png(소녀 걷기 8프레임 가로 시트, 2146×733, 투명 배경) — 실루엣이 화면을 가로질러 걷고 위로 빠져나간다. 원본은 오른쪽을 본다.")]
        public Texture2D girlWalk;

        [Header("튜토리얼 시작 컷신")]
        [Tooltip("InterrogationRoom.png — 뉴스 뒤에 드러나는 경찰서(취조실). 주황빛 하늘의 창, 나츠가 앉아 있다.")]
        public Texture2D room;

        public static IntroCutsceneArt Load() => Resources.Load<IntroCutsceneArt>(ResourcePath);
    }
}
