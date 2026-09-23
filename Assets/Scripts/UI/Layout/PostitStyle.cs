using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 포스트잇의 색과 손글씨 폰트. 종이 색은 목업의 연노랑(#FFF6A8 부근)이고, 접힌 뒷면은 한 톤 진하다.
    /// 손글씨는 나눔손글씨 펜(OFL, Assets/Resources/Fonts) — 한글 11,172자를 전부 갖고 있어 컴플렉스 이름에 어떤 글자가 와도 빈칸이 안 난다.
    /// TMP 폰트 에셋을 미리 굽지 않고 처음 쓸 때 소스 폰트에서 동적 아틀라스로 만든다 — 에셋 GUID를 프리팹에 물릴 일이 없다.
    /// </summary>
    public static class PostitStyle
    {
        public const string HandFontResourcePath = "Fonts/NanumPenScript-Regular";

        /// <summary>손글씨 폰트는 같은 크기의 고딕보다 작아 보여서 글자 크기를 키워 맞춘다.</summary>
        public const float HandSizeScale = 1.24f;

        public static readonly Color Paper = new Color32(255, 246, 168, 255);

        /// <summary>종이의 우하단 쪽 끝 색 — 좌상단(Paper)에서 살짝 어두워지는 그라데이션.</summary>
        public static readonly Color PaperShade = new Color32(244, 230, 140, 255);

        /// <summary>종이 가장자리 — 본체보다 약간 진하다.</summary>
        public static readonly Color Edge = new Color32(206, 186, 92, 255);

        /// <summary>말린 부분의 뒷면 — 종이보다 한 톤 진한 노랑.</summary>
        public static readonly Color Back = new Color32(238, 212, 88, 255);

        public static readonly Color Ink = new Color32(44, 36, 28, 255);

        /// <summary>새로 붙은 컴플렉스 표시 등 "빨간 펜" 글씨.</summary>
        public static readonly Color RedPen = new Color32(206, 46, 38, 255);

        public static readonly Color PinHead = new Color32(214, 52, 44, 255);
        public static readonly Color PinRim = new Color32(146, 28, 26, 255);

        private static TMP_FontAsset _handFont;
        private static bool _handFontSearched;

        /// <summary>손글씨 TMP 폰트. 소스 폰트가 없으면 null — 호출자는 원래 폰트를 그대로 둔다.</summary>
        public static TMP_FontAsset HandFont
        {
            get
            {
                if (_handFont != null || _handFontSearched) return _handFont;

                _handFontSearched = true;
                var source = Resources.Load<Font>(HandFontResourcePath);
                if (source == null)
                {
                    Debug.LogWarning($"[PostitStyle] 손글씨 폰트를 못 찾았다: Resources/{HandFontResourcePath} — 원래 폰트로 표시한다.");
                    return null;
                }

                FontEngine.InitializeFontEngine();
                _handFont = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                if (_handFont != null) _handFont.name = "PostitHand (runtime)";
                return _handFont;
            }
        }

        /// <summary>글자를 손글씨로 바꾼다(폰트 + 크기 보정). 굵기는 폰트 자체가 이미 굵은 펜이라 굵게 강조는 뺀다.
        /// 한 텍스트에 두 번 부르면 크기가 두 번 커지므로 포스트잇이 내용을 넣을 때 한 번만 부른다.</summary>
        public static void ApplyHand(TMP_Text text)
        {
            var font = HandFont;
            if (font == null || text == null) return;

            text.font = font;
            text.fontStyle &= ~FontStyles.Bold;
            text.fontSize *= HandSizeScale;
            if (text.enableAutoSizing)
            {
                text.fontSizeMin *= HandSizeScale;
                text.fontSizeMax *= HandSizeScale;
            }
        }

        /// <summary>빈 상태로 다시 찾도록 캐시를 비운다(플레이 모드 재진입 때 도메인 리로드를 끄면 정적 필드가 남는다).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _handFont = null;
            _handFontSearched = false;
        }
    }
}
