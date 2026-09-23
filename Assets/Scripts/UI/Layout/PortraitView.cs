using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 유키/나츠 공용 Portrait 뷰. 같은 클래스를 두 프리팹 인스턴스(유키용/나츠용)로 배치한다.
    /// </summary>
    public sealed class PortraitView : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Image _portraitImage;

        public RectTransform Root => (RectTransform)transform;
        public Image PortraitImage => _portraitImage;

        public void SetSprite(Sprite sprite)
        {
            if (_portraitImage != null) _portraitImage.sprite = sprite;
        }

        /// <summary>
        /// 엑스레이 판넬을 유키 위로 드래그해서 놓으면 펼쳐진다(UI 디자인 가이드 작동 조건 1).
        /// 유키/나츠가 같은 프리팹을 공유해서 인스턴스를 구분하는 직렬화 필드가 없다 —
        /// UiLayoutSetupTool.BuildMainHud가 붙이는 인스턴스 이름("Yuki Portrait")으로만 가른다.
        /// 나중에 프리팹을 다시 굽게 되면 이름 대신 [SerializeField] bool로 바꾸는 게 더 깔끔하다.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (gameObject.name != "Yuki Portrait") return;

            var panel = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<ComplexXrayPanel>() : null;
            panel?.Open();
        }
    }
}
