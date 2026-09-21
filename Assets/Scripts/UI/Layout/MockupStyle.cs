using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 디자인 목업(책상 위에 종이·포스트잇·카드를 붙여둔 느낌)의 색과 종이 모서리 처리. 에디터 도구(프리팹 굽기)와 런타임(코드로 짓는 HUD)이 같은 값을 쓴다.
    /// 색은 목업 스크린샷에서 직접 뽑은 값이다.
    /// </summary>
    public static class MockupStyle
    {
        /// <summary>대사창·단서·아이템 패널 같은 흰 종이.</summary>
        public static readonly Color Paper = new Color32(242, 239, 230, 255);

        /// <summary>종이 위에 얹힌 카드(아이템 칸, 단서 카드, 이름표).</summary>
        public static readonly Color Card = new Color32(236, 233, 224, 255);

        /// <summary>노란 포스트잇(컴플렉스, 쿼터 진행).</summary>
        public static readonly Color Sticky = new Color32(246, 224, 132, 255);

        /// <summary>종이 위 글자.</summary>
        public static readonly Color Ink = new Color32(30, 28, 26, 255);

        public static readonly Color InkSoft = new Color32(104, 96, 86, 255);

        /// <summary>종이 테두리(얇은 선).</summary>
        public static readonly Color Border = new Color32(78, 72, 64, 255);

        public static readonly Color MonitorCase = new Color32(220, 220, 211, 255);
        public static readonly Color MonitorScreen = new Color32(21, 52, 62, 255);
        public static readonly Color Cyan = new Color32(72, 222, 234, 255);

        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.38f);

        private const float EdgeThickness = 1.5f;
        private static readonly Vector2 ShadowOffset = new Vector2(4f, -4f);

        /// <summary>종이 가장자리: 얇은 어두운 테두리 + 아래로 떨어지는 그림자. 이미 붙어 있으면 값만 다시 맞춘다(멱등).</summary>
        public static void AddPaperEdge(GameObject go, bool shadow = true)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(EdgeThickness, -EdgeThickness);
            outline.useGraphicAlpha = false;

            if (shadow) AddShadow(go);
        }

        /// <summary>테두리 없이 그림자만(포스트잇). 이미 붙어 있으면 값만 다시 맞춘다(멱등). 연출이 그림자 거리를 움직일 수 있게 컴포넌트를 돌려준다.</summary>
        public static Shadow AddShadow(GameObject go)
        {
            // Outline도 Shadow를 상속하므로 정확히 Shadow 타입만 골라야 한다.
            Shadow shadowEffect = null;
            foreach (var candidate in go.GetComponents<Shadow>())
            {
                if (candidate.GetType() != typeof(Shadow)) continue;
                shadowEffect = candidate;
                break;
            }

            if (shadowEffect == null) shadowEffect = go.AddComponent<Shadow>();
            shadowEffect.effectColor = Shadow;
            shadowEffect.effectDistance = ShadowOffset;
            shadowEffect.useGraphicAlpha = false;
            return shadowEffect;
        }
    }
}
