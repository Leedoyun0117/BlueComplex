using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 유키/나츠 공용 Portrait 뷰. 같은 클래스를 두 프리팹 인스턴스(유키용/나츠용)로 배치한다.
    /// </summary>
    public sealed class PortraitView : MonoBehaviour
    {
        [SerializeField] private Image _portraitImage;

        public RectTransform Root => (RectTransform)transform;
        public Image PortraitImage => _portraitImage;

        public void SetSprite(Sprite sprite)
        {
            if (_portraitImage != null) _portraitImage.sprite = sprite;
        }
    }
}
