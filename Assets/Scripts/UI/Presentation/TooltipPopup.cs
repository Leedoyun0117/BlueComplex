using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>MainHud에 하나만 두는 공유 팝업. 호출자는 호버 대상의 월드 위치만 넘기고, 어디에 띄울지(대상 위쪽, 화면 밖으로 안 나가게)는 여기서 정한다.</summary>
    public sealed class TooltipPopup : MonoBehaviour
    {
        /// <summary>호버 대상과 팝업 사이 간격(캔버스 기준 픽셀).</summary>
        private const float Gap = 24f;

        [SerializeField] private RectTransform _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;

        private void Awake()
        {
            // 팝업이 커서 밑에 깔리면 호버 대상이 포인터 이탈로 받아들여 팝업이 깜빡인다 — 입력은 전부 통과시킨다.
            foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Hide();
        }

        /// <param name="anchorWorld">호버 대상의 월드 위치(<c>transform.position</c>). 오프셋은 여기서 캔버스 픽셀로 더한다 —
        /// 월드 단위로 더하면 ScreenSpaceCamera 캔버스의 스케일 때문에 화면 밖으로 튀어나간다.</param>
        public void Show(string title, string body, Vector3 anchorWorld)
        {
            _titleText.text = title;
            _bodyText.text = body;
            _root.gameObject.SetActive(true);

            var canvasRect = (RectTransform)_root.GetComponentInParent<Canvas>().rootCanvas.transform;
            var bounds = canvasRect.rect;
            var half = _root.rect.size * 0.5f;
            var anchor = (Vector2)canvasRect.InverseTransformPoint(anchorWorld);

            // 대상 위쪽이 기본이고, 위로 넘치면 아래쪽에 띄운다.
            var position = anchor + new Vector2(0f, half.y + Gap);
            if (position.y + half.y > bounds.yMax) position.y = anchor.y - half.y - Gap;

            position.x = Mathf.Clamp(position.x, bounds.xMin + half.x, bounds.xMax - half.x);
            position.y = Mathf.Clamp(position.y, bounds.yMin + half.y, bounds.yMax - half.y);

            _root.position = canvasRect.TransformPoint(position);
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }
}
