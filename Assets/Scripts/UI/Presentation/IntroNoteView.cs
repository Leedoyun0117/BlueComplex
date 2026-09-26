using BlueComplex.UI.Layout;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 오프닝 시퀀스의 포스트잇 한 장. 붙는 모션은 게임 안 포스트잇과 같은 <see cref="Postit"/>(<see cref="Postit.Stick"/>)을 그대로 쓴다 —
    /// 여기서는 종이 색(흰색·회색·주황)과 글씨만 정한다. 글씨는 없어도 된다(붙는 것 자체가 연출인 장면이 있다).
    /// 자리(<see cref="RectTransform"/>)는 <see cref="IntroSlide.Place"/>가 잡아 둔 것을 받는다. 붙이기 전엔 만들지 말고, 붙일 때 만들어 곧바로 <see cref="Stick"/>한다.
    /// </summary>
    internal sealed class IntroNoteView
    {
        private readonly Postit _postit;
        private readonly RectTransform _placed;
        private readonly CanvasGroup _sheetGroup;

        /// <summary>글씨. 글씨 없는 포스트잇이면 null.</summary>
        public TMP_Text Label { get; private set; }

        /// <summary>붙는 자리(움직이거나 크기를 바꾸려면 이 RectTransform을 만진다).</summary>
        public RectTransform Rect => _placed;

        private IntroNoteView(Postit postit, RectTransform placed)
        {
            _postit = postit;
            _placed = placed;
            _sheetGroup = postit.Sheet.GetComponent<CanvasGroup>();
        }

        /// <param name="fixedFontSize">0이면 종이 크기에 맞춰 글씨 크기를 자동으로 잡고, 아니면 이 크기로 고정한다.</param>
        public static IntroNoteView Create(RectTransform placed, string text, Color paper, Color ink, TMP_FontAsset font, float fixedFontSize = 0f)
        {
            var tilt = Mathf.DeltaAngle(0f, placed.localEulerAngles.z); // 자리에 준 기울기가 붙어 있을 때의 기울기다.
            var postit = Postit.Attach(placed.gameObject);
            postit.SetRestTilt(tilt);
            postit.SetFlat(true); // 오프닝의 포스트잇은 모서리가 말리지 않은 평평한 사각형이다.
            postit.SetPaperColor(paper);

            var view = new IntroNoteView(postit, placed);
            if (!string.IsNullOrEmpty(text)) view.BuildLabel(placed, text, ink, font, fixedFontSize);
            return view;
        }

        private void BuildLabel(RectTransform placed, string text, Color ink, TMP_FontAsset font, float fixedFontSize)
        {
            var label = RuntimeUi.CreateStretched(_postit.Content, "Label");
            label.offsetMin = new Vector2(placed.sizeDelta.x * 0.06f, placed.sizeDelta.y * 0.06f);
            label.offsetMax = -label.offsetMin;

            var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = PostitStyle.HandFont != null ? PostitStyle.HandFont : font;
            tmp.text = text;
            tmp.color = ink;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (fixedFontSize > 0f)
            {
                tmp.textWrappingMode = TextWrappingModes.NoWrap; // 한 단어짜리 큰 글씨.
                tmp.fontSize = fixedFontSize;
            }
            else
            {
                // 단서 이름처럼 길이가 제각각인 글: 종이 안에서 줄을 바꿔 가며 크기를 맞춘다.
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 18f;
                tmp.fontSizeMax = Mathf.Max(24f, placed.sizeDelta.y * 0.16f);
            }

            Label = tmp;
        }

        /// <summary>종이 색을 바꾼다(색이 서서히 변하는 연출에서 매 프레임 부른다).</summary>
        public void SetPaper(Color paper) => _postit.SetPaperColor(paper);

        /// <summary>붙인다 — <see cref="Postit.Stick"/> 그대로(위에서 내려와 눌리고 압정이 꽂힌다). 끝나면 자리에 붙어 있다.</summary>
        public Sequence Stick(bool sound = true) => _postit.Stick(sound);

        /// <summary>글씨를 종이에서 빼내 <paramref name="newParent"/> 밑으로 옮긴다. 화면에서 보이는 자리와 크기는 그대로다(PPT 10단계: 종이가 사라져도 글씨는 남는다).</summary>
        public void ReleaseLabel(Transform newParent)
        {
            if (Label == null) return;

            var label = (RectTransform)Label.transform;
            var size = label.rect.size;
            var keep = label.position;
            label.SetParent(newParent, true);
            label.anchorMin = label.anchorMax = label.pivot = new Vector2(0.5f, 0.5f);
            label.sizeDelta = size;
            label.position = keep;
        }

        /// <summary>종이(판·그림자·압정 전부)를 서서히 지운다. 글씨를 <see cref="ReleaseLabel"/>로 먼저 빼 두었으면 글씨만 남는다.</summary>
        public Tween FadeOutPaper(float seconds) => _sheetGroup.DOFade(0f, seconds);
    }
}
