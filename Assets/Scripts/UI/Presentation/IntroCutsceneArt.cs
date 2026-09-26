using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신이 쓰는 그림 묶음(Resources/IntroCutsceneArt.asset). 그림 파일은 제자리(Assets/Art)에 두고 여기서 참조만 한다 —
    /// 메뉴 BlueComplex/Cutscene/Rebuild Intro Art가 파일 이름으로 채운다. 그림이 없으면 컷신은 그 조각을 건너뛴다.
    /// </summary>
    public sealed class IntroCutsceneArt : ScriptableObject
    {
        public const string ResourcePath = "IntroCutsceneArt";

        [Tooltip("IntroCar.png — 포드 모델 T 도트 그림(64x64, 오른쪽을 본다). 노을 진 도시를 가로질러 달린다.")]
        public Texture2D car;

        [Tooltip("IntroClock.png — PPT의 로마 숫자 아날로그 시계 문자판(바늘을 뺀 것, 테두리 원 바깥은 투명). 작게 나타났다가 확대된다.")]
        public Texture2D clock;

        [Tooltip("IntroClockHour.png — 시침만 떼어 낸 그림. 문자판과 같은 크기이고 회전 중심(허브)이 문자판 한가운데라 문자판 위에 그대로 겹쳐 돌린다.")]
        public Texture2D clockHour;

        [Tooltip("IntroClockMinute.png — 분침만 떼어 낸 그림(시침과 같은 규칙).")]
        public Texture2D clockMinute;

        [Tooltip("InterrogationRoom.png — 뉴스 뒤에 드러나는 경찰서(취조실). 창밖 시계탑 실루엣에 두 눈이 빛난다.")]
        public Texture2D room;

        public static IntroCutsceneArt Load() => Resources.Load<IntroCutsceneArt>(ResourcePath);
    }
}
