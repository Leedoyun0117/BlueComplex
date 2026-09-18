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

        /// <summary>런타임에 조립되는 캔버스(예: 검증용 디버그 하네스)용. 일반적으로는 인스펙터에서 직접 물린다.</summary>
        public void SetCrtMaterial(Material material) => _crtMaterial = material;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            var curvature = _crtMaterial != null ? _crtMaterial.GetFloat(CurvatureId) : 0f;
            if (curvature == 0f)
            {
                base.Raycast(eventData, resultAppendList);
                return;
            }

            var original = eventData.position;
            eventData.position = DistortionMath.ApplyBarrel(original, curvature, Screen.width, Screen.height);
            try
            {
                base.Raycast(eventData, resultAppendList);
            }
            finally
            {
                eventData.position = original;
            }
        }

        /// <summary>현재 _Curvature 값. 드래그 중인 요소가 같은 보정을 적용하려 할 때 쓴다.</summary>
        public float CurrentCurvature => _crtMaterial != null ? _crtMaterial.GetFloat(CurvatureId) : 0f;
    }
}
