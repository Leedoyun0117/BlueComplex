using System.Collections;
using System.Text;
using BlueComplex.Core.Stage;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 엔딩 크레딧: 검은 화면 아래에서 글이 올라와 위로 다 빠져나갈 때까지 일정한 속도로 스크롤된다(제목 → 역할별 이름 → 맺음말).
    /// 잠깐(<see cref="SkipGraceSeconds"/>) 뒤부터는 클릭하면 바로 끝낸다 — 에필로그 마지막 문단을 넘기는 클릭이 그대로 크레딧을 건너뛰지 않게.
    /// 명단은 <see cref="EndingContent.Credits"/>(지금은 자리표시)에서 읽는다.
    /// </summary>
    internal sealed class EndingCreditsView : MonoBehaviour, IPointerClickHandler
    {
        private const float ScrollPixelsPerSecond = 90f;
        private const float SkipGraceSeconds = 1.5f;
        private const float ContentWidthRatio = 0.6f;

        private static readonly Color TitleInk = new Color32(238, 240, 246, 255);
        private static readonly Color RoleInk = new Color32(146, 168, 204, 255);
        private static readonly Color NameInk = new Color32(214, 220, 232, 255);

        private CanvasGroup _group;
        private RectTransform _content;
        private TMP_Text _text;
        private Tween _scroll;
        private bool _skip;
        private float _acceptSkipAt;

        public static EndingCreditsView Create(Transform parent, TMP_FontAsset font)
        {
            var go = new GameObject("Ending Credits", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var view = go.AddComponent<EndingCreditsView>();
            view.Build(font);
            return view;
        }

        /// <summary>글이 화면 아래에서 시작해 위로 완전히 빠져나갈 때까지 스크롤한다. 끝나면 글은 화면 밖에 있다.</summary>
        public IEnumerator Play()
        {
            var screenHeight = ((RectTransform)transform).rect.height;
            var screenWidth = ((RectTransform)transform).rect.width;

            _text.text = BuildText();
            _content.sizeDelta = new Vector2(screenWidth * ContentWidthRatio, 0f);
            var height = _text.GetPreferredValues(_text.text, screenWidth * ContentWidthRatio, 0f).y;
            _content.sizeDelta = new Vector2(screenWidth * ContentWidthRatio, height);

            // 위쪽 기준(pivot 위): anchoredPosition.y = -screenHeight면 글 윗변이 화면 아래 끝, +height면 글 아랫변이 화면 위 끝.
            _content.anchoredPosition = new Vector2(0f, -screenHeight);
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _skip = false;
            _acceptSkipAt = Time.unscaledTime + SkipGraceSeconds;

            var distance = screenHeight + height;
            _scroll = _content.DOAnchorPosY(height, distance / ScrollPixelsPerSecond).SetEase(Ease.Linear).SetUpdate(true).SetTarget(this);
            while (_scroll.IsActive() && _scroll.IsPlaying() && !_skip) yield return null;

            _scroll.Kill();
            _scroll = null;
            _group.blocksRaycasts = false;
            _group.alpha = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Time.unscaledTime >= _acceptSkipAt) _skip = true;
        }

        public void ResetNow()
        {
            _scroll?.Kill();
            _scroll = null;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _skip = false;
        }

        private static string BuildText()
        {
            var sb = new StringBuilder();
            sb.Append($"<size=120%><color=#{ColorUtility.ToHtmlStringRGB(TitleInk)}>{EndingContent.CreditsTitle}</color></size>\n\n\n\n");

            foreach (var section in EndingContent.Credits)
            {
                sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(RoleInk)}>{section.Role}</color>\n");
                foreach (var name in section.Names)
                    sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(NameInk)}>{name}</color>\n");
                sb.Append("\n\n\n");
            }

            sb.Append($"\n<color=#{ColorUtility.ToHtmlStringRGB(TitleInk)}>{EndingContent.CreditsClosing}</color>\n");
            return sb.ToString();
        }

        private void Build(TMP_FontAsset font)
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var click = GetComponent<Image>();
            click.color = Color.clear;
            click.raycastTarget = true;

            var go = new GameObject("Content", typeof(RectTransform)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            _content = (RectTransform)go.transform;
            _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0.5f, 1f);

            _text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) _text.font = font;
            _text.fontSize = 40f;
            _text.color = NameInk;
            _text.alignment = TextAlignmentOptions.Top;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.richText = true;
            _text.lineSpacing = 18f;
            _text.raycastTarget = false;
        }

        private void OnDestroy() => DOTween.Kill(this);
    }
}
