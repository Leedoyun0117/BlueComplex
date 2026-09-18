using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>MainHud에 하나만 두는 공유 팝업. 같은 캔버스 안이면 월드 좌표가 부모와 무관하게 공유되므로 그대로 쓴다.</summary>
    public sealed class TooltipPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;

        private void Awake() => Hide();

        public void Show(string title, string body, Vector3 worldPosition)
        {
            _root.gameObject.SetActive(true);
            _root.position = worldPosition;
            _titleText.text = title;
            _bodyText.text = body;
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }
}
