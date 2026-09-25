using UnityEngine;

namespace BlueComplex.UI.Rendering
{
    /// <summary>
    /// CrtEffect.shader의 배럴 왜곡(Barrel())과 동일한 공식을 화면 픽셀 좌표 기준으로 계산한다.
    /// 원래 DistortionCorrectedGraphicRaycaster 안에 private으로만 있었는데, 드래그 중인 UI
    /// 요소를 화면에 "그리는" 좌표도 똑같이 보정해야 해서(레이캐스터의 히트테스트 보정과는 별개
    /// 필요) 한 곳으로 뺐다 — 수식 자체는 그대로, 위치만 옮긴 것이라 레이캐스터의 동작은 변하지
    /// 않는다.
    ///
    /// _Shake(화면 흔들림)는 보정할 필요가 없다 — CrtEffect.shader가 씬 샘플링에만 적용하고 UI는 흔들지 않는다.
    /// </summary>
    public static class DistortionMath
    {
        public static Vector2 ApplyBarrel(Vector2 screenPos, float curvature, int screenWidth, int screenHeight)
        {
            var width = Mathf.Max(screenWidth, 1);
            var height = Mathf.Max(screenHeight, 1);

            var uv = new Vector2(screenPos.x / width, screenPos.y / height);
            var cc = uv - new Vector2(0.5f, 0.5f);
            var d = Vector2.Dot(cc, cc);
            var corrected = uv + cc * d * curvature * 1.4f;

            return new Vector2(corrected.x * width, corrected.y * height);
        }
    }
}
