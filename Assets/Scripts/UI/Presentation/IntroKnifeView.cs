using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>시작 컷신 1막: 검은 화면 위로 나이프가 우상단에서 좌하단으로 대각선 낙하하며 돈다. 스프라이트는 단서 카탈로그의 피 묻은 나이프를 그대로 쓴다.</summary>
    internal sealed class IntroKnifeView : MonoBehaviour
    {
        /// <summary>스테이지 3 단서 "피 묻은 나이프"(Art/Clue/Room3/bloody_knife)의 <see cref="UiIconCatalog"/> id.</summary>
        private const string KnifeIconId = "s3_bloody_knife";

        private RectTransform _rect;
        private Vector2 _from;
        private Vector2 _to;

        public static IntroKnifeView Create(Transform parent, Vector2 canvasSize, float sizeRatio)
        {
            var go = new GameObject("Intro Knife", typeof(RectTransform), typeof(Image)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var size = canvasSize.y * sizeRatio;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = go.GetComponent<Image>();
            image.sprite = UiIcons.GetExact(KnifeIconId);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = image.sprite != null; // 카탈로그에 없으면 회전 없이 조용히 건너뛴다(빈 사각형을 돌리지 않는다).

            var view = go.AddComponent<IntroKnifeView>();
            view._rect = rect;

            // 화면 밖 우상단 모서리에서 좌하단 모서리로 — 나이프가 통째로 사라질 만큼 바깥에서 시작하고 끝난다.
            var margin = size * 0.8f;
            view._from = new Vector2(canvasSize.x * 0.5f + margin, canvasSize.y * 0.5f + margin);
            view._to = new Vector2(-view._from.x, -view._from.y);
            rect.anchoredPosition = view._from;
            return view;
        }

        /// <summary>낙하 + 회전. 중력처럼 조금씩 빨라지고, 도는 속도는 일정하다.</summary>
        public Tween Fall(float seconds, float spinDegrees)
        {
            _rect.anchoredPosition = _from;
            _rect.localRotation = Quaternion.identity;

            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(_rect.DOAnchorPos(_to, seconds).SetEase(Ease.InSine));
            sequence.Join(_rect.DOLocalRotate(new Vector3(0f, 0f, spinDegrees), seconds, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            return sequence;
        }

        private void OnDestroy() => DOTween.Kill(this);
    }
}
