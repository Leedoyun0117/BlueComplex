using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 화면 전체를 덮는 어두운 막. 알파가 0보다 크면 뒤 UI의 클릭을 막는다. 스테이지 클리어 연출의 암전·밝아짐(자물쇠 화면, 컷신 앞뒤, 다음 스테이지 시작)이 이 막 하나로 이루어진다.
    /// 표시만 한다 — 언제 어둡게/밝게 할지는 <see cref="StageClearDirector"/>가 정한다.
    /// </summary>
    public sealed class ScreenCurtain : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.016f, 0.024f, 0.05f, 1f);

        private Image _image;

        public float Alpha => _image.color.a;

        public static ScreenCurtain Create(Transform parent)
        {
            var rect = new GameObject("Screen Curtain", typeof(RectTransform), typeof(Image)) { layer = parent.gameObject.layer }
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var curtain = rect.gameObject.AddComponent<ScreenCurtain>();
            curtain._image = rect.GetComponent<Image>();
            curtain.SetAlpha(0f);
            return curtain;
        }

        public Tween FadeTo(float alpha, float duration)
        {
            DOTween.Kill(this);
            return DOTween.To(() => _image.color.a, SetAlpha, alpha, Mathf.Max(0.01f, duration))
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
        }

        public void SetAlpha(float alpha)
        {
            _image.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            _image.raycastTarget = alpha > 0.001f;
        }

        public void ResetNow()
        {
            DOTween.Kill(this);
            SetAlpha(0f);
        }
    }
}
