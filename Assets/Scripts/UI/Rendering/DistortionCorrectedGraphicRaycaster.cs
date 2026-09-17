using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Rendering
{
    /// <summary>
    /// CrtEffect.shader의 배럴 왜곡(Barrel())은 최종 화면 픽셀이 "왜곡 전" 어느 좌표를 보여주는지
    /// 돌려주는 정방향 함수다. UI는 왜곡 전 좌표계(RT_UI)에 그려지므로, 플레이어가 실제로 보는
    /// 왜곡된 화면에서 클릭한 지점을 그대로 히트테스트하면 어긋난다 — 클릭 좌표에 같은 Barrel()을
    /// 한 번 더 적용해 보정한 뒤 히트테스트해야 한다.
    ///
    /// 알려진 한계: _Shake(화면 흔들림)는 보정하지 않는다. 값 범위가 작고(0~0.03 UV) 시간 위상까지
    /// 맞추려면 복잡도가 늘어나는데 1단계 골격 목적엔 과함 — 필요해지면 나중에 추가.
    /// </summary>
    public sealed class DistortionCorrectedGraphicRaycaster : GraphicRaycaster
    {
        private static readonly int CurvatureId = Shader.PropertyToID("_Curvature");

        [SerializeField] private Material _crtMaterial;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            var curvature = _crtMaterial != null ? _crtMaterial.GetFloat(CurvatureId) : 0f;
            if (curvature == 0f)
            {
                base.Raycast(eventData, resultAppendList);
                return;
            }

            var original = eventData.position;
            eventData.position = ApplyBarrel(original, curvature);
            try
            {
                base.Raycast(eventData, resultAppendList);
            }
            finally
            {
                eventData.position = original;
            }
        }

        /// <summary>CrtEffect.shader의 Barrel()과 동일한 공식. 화면 픽셀 좌표 기준으로 계산한다.</summary>
        private static Vector2 ApplyBarrel(Vector2 screenPos, float curvature)
        {
            var width = Mathf.Max(Screen.width, 1);
            var height = Mathf.Max(Screen.height, 1);

            var uv = new Vector2(screenPos.x / width, screenPos.y / height);
            var cc = uv - new Vector2(0.5f, 0.5f);
            var d = Vector2.Dot(cc, cc);
            var corrected = uv + cc * d * curvature * 1.4f;

            return new Vector2(corrected.x * width, corrected.y * height);
        }
    }
}
