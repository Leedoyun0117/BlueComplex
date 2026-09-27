using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 유키 숨쉬기 시트(Assets/Art/Portraits/Yuki/Yuki_Fix_*_Breath-Sheet)에서 잘라 둔 프레임과 재생 속도를 담는다. 시트는 Art 폴더에 있어
    /// Resources.Load로 못 찾으므로, Resources 쪽의 이 에셋이 스프라이트를 참조로 들고 있다(NatsuExpressionSet과 같은 이유).
    /// 시트마다 4장이고 재생 순서는 1→2→3→4→3→2→(반복)이다. 1번·3번 프레임이 들숨/날숨의 정지점이라 숨쉬기 빈도(속도)는
    /// 그 두 프레임의 유지 시간으로 조절한다 — 아래 세 값이 그것이고, 에셋 인스펙터에서 바로 고칠 수 있다.
    /// 모든 프레임은 기존 정지 표정(163×201)이 덮던 영역과 같은 자리·같은 비율(65×79)로 잘려 있어 표정이 바뀌어도 얼굴이 튀지 않는다.
    /// </summary>
    public sealed class YukiBreathSet : ScriptableObject
    {
        public const string ResourcePath = "UI/Portraits/Yuki/YukiBreathSet";

        [Tooltip("1번 프레임(정지점)을 보여주는 시간(초). 길수록 숨쉬기가 느려진다.")]
        [Min(0.02f)] public float frame1Hold = 1.4f;

        [Tooltip("3번 프레임(정지점)을 보여주는 시간(초). 3번은 한 주기에 두 번(4번 전후) 나온다.")]
        [Min(0.02f)] public float frame3Hold = 0.7f;

        [Tooltip("2번·4번 프레임(오가는 중)을 보여주는 시간(초).")]
        [Min(0.02f)] public float stepSeconds = 0.25f;

        public Sprite[] normal;
        public Sprite[] sadness;
        public Sprite[] joy;
        public Sprite[] anger;

        public static YukiBreathSet Load() => Resources.Load<YukiBreathSet>(ResourcePath);

        public Sprite[] For(BlueComplex.Core.Stability.YukiReaction mood) => mood switch
        {
            BlueComplex.Core.Stability.YukiReaction.Anger => anger,
            BlueComplex.Core.Stability.YukiReaction.Sadness => sadness,
            BlueComplex.Core.Stability.YukiReaction.Joy => joy,
            _ => normal
        };

        /// <summary>한 주기의 (프레임 인덱스 0~3, 유지 시간) 목록. 1→2→3→4→3→2 — 끝의 1은 다음 주기의 첫 프레임이라 겹쳐 안 넣는다.</summary>
        public (int frame, float seconds)[] Cycle() => new[]
        {
            (0, frame1Hold), (1, stepSeconds), (2, frame3Hold), (3, stepSeconds), (2, frame3Hold), (1, stepSeconds)
        };
    }
}
