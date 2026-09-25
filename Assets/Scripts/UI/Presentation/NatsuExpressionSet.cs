using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 나츠 표정 시트(Assets/Art/Portraits/Natsu/Natsu_Fix_*-Sheet)에서 잘라 둔 프레임들을 재생 순서대로 담는다.
    /// 시트는 Art 폴더에 있어 Resources.Load로 못 찾으므로, Resources 쪽의 이 에셋이 스프라이트를 참조로 들고 있다.
    /// 프레임 배열은 시트의 칸 순서(왼쪽 위부터 가로로) 그대로다 — 재생 순서는 <see cref="NatsuPortraitView"/>가 정한다.
    /// </summary>
    public sealed class NatsuExpressionSet : ScriptableObject
    {
        public const string ResourcePath = "UI/Portraits/Natsu/NatsuExpressionSet";

        [Tooltip("눈 깜박임(Normal·Focus·Fury 공통) 사이 간격(초). 한 번 깜박이고 나서 다음 깜박임까지 기다리는 시간.")]
        [Min(0.1f)] public float blinkIntervalSeconds = 3f;

        [Tooltip("깜박임 한 프레임을 보여주는 시간(초).")]
        [Min(0.01f)] public float blinkFrameSeconds = 0.05f;

        /// <summary>CloseEyes 시트 6장: 뜬 눈(평소 기준 프레임) → 반쯤 감김 → 감김 → 고개를 살짝 숙이며 눈 감김. 안도 시퀀스와 눈 깜박임이 쓴다.</summary>
        public Sprite[] closeEyes;

        /// <summary>Confused 시트 5장: 식은땀이 흐르고 입꼬리가 흔들리는 당황 표정.</summary>
        public Sprite[] confused;

        /// <summary>Focus 시트 5장: 평소 → 손을 올려 턱을 짚는 집중 동작(마지막 장이 집중 표정).</summary>
        public Sprite[] focus;

        /// <summary>TakeOffHand 시트 5장: 턱을 짚은 손을 떼어 평소로 돌아오는 동작.</summary>
        public Sprite[] takeOffHand;

        /// <summary>Focus_Blink 시트 3장: 집중 표정(손으로 턱을 짚음)의 뜬 눈 → 반쯤 감김 → 감김. 첫 장은 Focus 시트 마지막 장과 같다.</summary>
        public Sprite[] focusBlink;

        /// <summary>Confused_Blink 시트 3장: 당황 표정의 뜬 눈 → 반쯤 감김 → 감김. 첫 장은 Confused 시트 마지막 장과 같다.</summary>
        public Sprite[] confusedBlink;

        public static NatsuExpressionSet Load() => Resources.Load<NatsuExpressionSet>(ResourcePath);
    }
}
