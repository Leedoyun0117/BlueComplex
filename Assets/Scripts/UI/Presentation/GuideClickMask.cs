using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 화면 전체를 덮는 투명 막에 붙는 클릭 필터. 허용 사각형 안의 클릭은 "이 막은 없는 것"으로 통과시키고(밑의 요소가 받는다), 그 밖은 막이 받아 삼킨다.
    /// 좌표는 CRT 배럴 보정을 이미 거친 화면 좌표로 온다(DistortionCorrectedGraphicRaycaster).
    /// </summary>
    public sealed class GuideClickMask : MonoBehaviour, ICanvasRaycastFilter
    {
        /// <summary>지금 뚫어 줄 사각형들. null이면 아무 데도 뚫지 않는다(전부 막는다).</summary>
        public Func<IEnumerable<RectTransform>> Allowed { get; set; }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var rects = Allowed?.Invoke();
            if (rects == null) return true;

            foreach (var rect in rects)
            {
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, eventCamera)) return false;
            }

            return true;
        }
    }
}
