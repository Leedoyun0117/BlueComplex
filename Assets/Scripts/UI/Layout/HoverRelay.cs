using System.Collections.Generic;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Rendering;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>포인터 진입/이탈을 콜백으로 넘기는 작은 릴레이. 끌기(단서 카드 드래그) 중의 진입은 무시할 수 있다 —
    /// 카드를 끌고 지나가는 것만으로 서랍이 열리지 않게. 이탈은 진입을 받아들인 경우에만 알린다.</summary>
    internal sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private System.Action<bool> _changed;
        private bool _ignoreWhileDragging;
        private bool _inside;

        public void Bind(System.Action<bool> changed, bool ignoreWhileDragging)
        {
            _changed = changed;
            _ignoreWhileDragging = ignoreWhileDragging;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inside || _ignoreWhileDragging && eventData.pointerDrag != null) return;

            _inside = true;
            _changed?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_inside) return;

            _inside = false;
            _changed?.Invoke(false);
        }

        private void OnDisable()
        {
            if (!_inside) return;

            _inside = false;
            _changed?.Invoke(false);
        }
    }
}
