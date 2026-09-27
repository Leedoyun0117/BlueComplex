Shader "BlueComplex/LSO/UiAlphaSeal"
{
    // RT_UI의 알파 채널만 1로 덮어써서 "여기는 씬이 비치지 않는다"를 확정한다. 색은 건드리지 않는다.
    //
    // ─── 왜 필요한가 ──────────────────────────────────────────────────────
    // 이 프로젝트의 UI는 화면에 직접 그려지지 않는다. UI 카메라가 RT_UI에 그리고 CRT 셰이더가
    // lerp(scene, ui.rgb, ui.a)로 합성한다 — RT_UI의 알파가 곧 "씬을 얼마나 가릴지"다.
    //
    // 그런데 유니티 기본 UI 셰이더는 Blend SrcAlpha OneMinusSrcAlpha 라, 알파 채널에도 같은 식이 적용돼
    //     dstA = srcA² + dstA(1 − srcA)
    // 가 된다. srcA² < srcA 이므로 <b>반투명 요소가 지나갈 때마다 이미 쌓인 알파가 깎인다.</b>
    // 불투명한 검은 막으로 알파 1을 깔아 둬도, 그 위의 글자 안티에일리어싱 가장자리나 부드러운
    // 테두리가 알파를 도로 끌어내려 씬이 비친다(실측: RT alpha min 0.357 = 씬이 64% 비침).
    //
    // 그래서 UI를 다 그린 뒤 맨 마지막에 이 판을 깔아 알파를 1로 봉한다. ColorMask A 라 색은 그대로고,
    // Blend One Zero 라 누적이 아니라 덮어쓰기다 — 무엇이 앞서 지나갔든 결과가 1로 고정된다.
    //
    // 주의: 이 판이 덮는 영역은 씬이 완전히 가려진다. 창 일부만 덮고 싶다면 판의 사각형을 그만큼만 둘 것.
    Properties
    {
        // 쓰지 않지만 반드시 선언해야 한다. uGUI의 CanvasRenderer가 Image의 텍스처를 _MainTex 슬롯에
        // 물리려 하고, 없으면 매 프레임 "doesn't have a texture property '_MainTex'" 경고가 쏟아진다.
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        ColorMask A     // 알파만 쓴다 — RGB는 아래 그려진 UI 색 그대로 둔다.
        Blend One Zero  // 누적하지 않고 덮어쓴다.

        Pass
        {
            Name "AlphaSeal"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // ColorMask A 라 rgb는 버려진다. 알파 1만 기록된다.
                return fixed4(0, 0, 0, 1);
            }
        ENDCG
        }
    }
}
