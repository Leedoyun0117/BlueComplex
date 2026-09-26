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

        [Tooltip("BlackEye.png — 눈꺼풀 사이로 처음 보이는 어두운 눈(검은 동공 + 흰 테두리).")]
        public Texture2D blackEye;

        [Tooltip("EyeEffect.png — 완전히 뜬(불이 켜진) 눈. 동공 확대에 쓴다.")]
        public Texture2D eyeEffect;

        [Tooltip("WrinkleUp, WrinkleUp2, WrinkleUp3 — 위 눈꺼풀. 1 = 닫힘(대각선 이음매), 2 = 열림, 3 = 2와 같은 모양에 캔버스만 위아래로 80px 넓힌 것(밀어 걷어낼 때 쓴다).")]
        public Texture2D[] upperLid = new Texture2D[3];

        [Tooltip("WrinkleDown, WrinkleDown2, WrinkleDown3 — 아래 눈꺼풀. 위와 같은 순서.")]
        public Texture2D[] lowerLid = new Texture2D[3];

        [Tooltip("InterrogationRoom.png — 뉴스 뒤에 드러나는 경찰서(취조실). 창밖 시계탑 실루엣에 두 눈이 빛난다.")]
        public Texture2D room;

        public static IntroCutsceneArt Load() => Resources.Load<IntroCutsceneArt>(ResourcePath);
    }
}
