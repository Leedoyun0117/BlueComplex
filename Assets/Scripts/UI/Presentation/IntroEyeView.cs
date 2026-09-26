using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신 2막: 눈. 세 단계다 — (a) 감긴 눈: 가로선 하나가 서서히 나타난다 (b) 눈이 뜨인다: 눈꺼풀 사이로 홍채가 드러난다
    /// (c) 동공 중심으로 화면이 빨려 들어가듯 확대되어 화면이 동공의 검정으로 가득 찬다.
    ///
    /// 눈 모양은 아몬드 모양 <see cref="Mask"/> 하나다 — 높이(sizeDelta.y)를 0에 가깝게 줄이면 흰자위가 가로선 하나가 되고(감긴 눈),
    /// 높이를 키우면 마스크 안의 홍채·동공이 드러난다. 홍채·동공은 마스크 높이와 무관하게 크기가 고정이다.
    /// </summary>
    internal sealed class IntroEyeView : MonoBehaviour
    {
        /// <summary>감긴 눈 선의 두께(캔버스 픽셀).</summary>
        private const float ClosedThickness = 4f;

        private RectTransform _root;
        private RectTransform _lid;
        private CanvasGroup _group;
        private Image _highlight;
        private Sprite _almond;
        private Sprite _iris;
        private Sprite _disc;
        private float _width;
        private float _openHeight;
        private float _pupilRadius;
        private Vector2 _canvasSize;

        public static IntroEyeView Create(Transform parent, Vector2 canvasSize)
        {
            var go = new GameObject("Intro Eye", typeof(RectTransform), typeof(CanvasGroup)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var view = go.AddComponent<IntroEyeView>();
            view.Build(canvasSize);
            return view;
        }

        private void Build(Vector2 canvasSize)
        {
            _canvasSize = canvasSize;
            _width = canvasSize.x * 0.5f;
            _openHeight = _width * 0.42f;

            _root = (RectTransform)transform;
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(_width, _openHeight);

            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _almond = IntroProceduralArt.Almond();
            _iris = IntroProceduralArt.Iris();
            _disc = IntroProceduralArt.Disc();

            var lidImage = CreateImage("Eyelids", _root, _almond, Color.white);
            lidImage.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _lid = lidImage.rectTransform;
            _lid.sizeDelta = new Vector2(_width, ClosedThickness);

            var irisSize = _openHeight * 0.92f;
            var irisRect = CreateImage("Iris", _lid, _iris, Color.white).rectTransform;
            irisRect.sizeDelta = new Vector2(irisSize, irisSize);

            var pupilSize = irisSize * 0.42f;
            _pupilRadius = pupilSize * 0.5f;
            var pupilRect = CreateImage("Pupil", irisRect, _disc, new Color(0.01f, 0.01f, 0.02f, 1f)).rectTransform;
            pupilRect.sizeDelta = new Vector2(pupilSize, pupilSize);

            _highlight = CreateImage("Highlight", pupilRect, _disc, new Color(1f, 1f, 1f, 0.85f));
            _highlight.rectTransform.sizeDelta = new Vector2(pupilSize * 0.22f, pupilSize * 0.22f);
            _highlight.rectTransform.anchoredPosition = new Vector2(-pupilSize * 0.2f, pupilSize * 0.22f);
        }

        /// <summary>(a) 감긴 눈: 가로선이 서서히 나타나(짧아졌다 늘어나며) 머문다. <paramref name="seconds"/>는 나타남 + 머묾의 합.</summary>
        public IEnumerator PlayClosed(float seconds)
        {
            SetOpen(0f);
            _root.localScale = new Vector3(0.35f, 1f, 1f);
            _group.alpha = 0f;

            var appear = Mathf.Min(seconds * 0.6f, 0.9f);
            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(_group.DOFade(1f, appear).SetEase(Ease.OutSine));
            sequence.Join(_root.DOScale(Vector3.one, appear).SetEase(Ease.OutCubic));
            yield return sequence.WaitForCompletion(true);

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds - appear));
        }

        /// <summary>(b) 눈이 뜨인다: 눈꺼풀 높이가 열리며 홍채가 드러난다.</summary>
        public IEnumerator PlayOpening(float seconds)
        {
            yield return DOTween.To(() => 0f, SetOpen, 1f, Mathf.Max(0.01f, seconds))
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
        }

        /// <summary>(c) 동공 중심(눈의 정중앙)으로 확대: 처음엔 천천히, 끝으로 갈수록 빨려 들어가며 동공의 검정이 화면을 채운다. 다 끝나면 화면 전체가 검정이다.</summary>
        public IEnumerator PlayZoom(float seconds)
        {
            // 동공 반지름이 화면 대각선의 절반을 넘겨 덮을 때까지 — 여유를 두어 가장자리에 홍채가 남지 않게 한다.
            var halfDiagonal = _canvasSize.magnitude * 0.5f;
            var target = halfDiagonal / _pupilRadius * 1.2f;

            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(_root.DOScale(target, seconds).SetEase(Ease.InCubic));
            sequence.Join(_highlight.DOFade(0f, seconds * 0.45f));
            yield return sequence.WaitForCompletion(true);
        }

        public void Hide()
        {
            DOTween.Kill(this);
            _group.alpha = 0f;
        }

        /// <summary>0 = 가로선 하나, 1 = 활짝.</summary>
        private void SetOpen(float open) => _lid.sizeDelta = new Vector2(_width, Mathf.Lerp(ClosedThickness, _openHeight, open));

        private void OnDestroy()
        {
            DOTween.Kill(this);
            IntroProceduralArt.Release(_almond);
            IntroProceduralArt.Release(_iris);
            IntroProceduralArt.Release(_disc);
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
