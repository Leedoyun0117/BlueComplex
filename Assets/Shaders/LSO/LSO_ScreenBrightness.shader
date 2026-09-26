Shader "BlueComplex/LSO/ScreenBrightness"
{
    // 플레이어가 설정창에서 조절하는 화면 밝기. 모든 것이 다 그려진 뒤 맨 마지막에 한 번 곱한다.
    //
    // ─── 렌더러 피처 순서: 반드시 CRT 뒤에 둘 것 ──────────────────────────────
    // UI는 화면에 따로 그려지지 않고 CRT 셰이더 안에서 _UITex로 합성된다(UICompositorSetupTool 참고).
    // 그래서 이 패스를 CRT보다 앞에 두면 씬만 어두워지고 메뉴·단서창은 밝은 채로 남는다.
    // 내장 FullScreenPassRendererFeature의 주입 지점은 AfterRenderingPostProcessing(600)이 최대라
    // CRT와 같은 값인데, 같은 지점이면 렌더러 피처 목록 순서대로 실행된다 — 목록에서 CRT 아래에 둔다.
    //
    // ─── 왜 CRT 셰이더를 고치지 않고 패스를 따로 뒀나 ──────────────────────────
    // CRT의 _Brightness는 이미 임자가 있다. CrtEffectDriver가 심박수에 따라 매 프레임 덮어쓰고,
    // LampLightDriver가 그 감소폭을 전제로 전구 배율을 좁게 잡아 두었다. 거기에 사용자 설정을 얹으면
    // 서로 싸운다. DLJ의 HeartbeatMood 변종에도 같은 _Brightness가 있어 두 군데를 고쳐야 한다.
    // 맨 뒤에서 한 번 곱하면 어느 CRT 변종이 돌든 상관없고, 연출용 밝기와도 층이 분리된다.
    //
    // 이 프로젝트는 예전에 풀스크린 패스를 둘로 나눴다가 "두 번째 패스의 좌표계가 깨져 화면이
    // 새까맣게" 되는 문제를 겪었다(CrtEffect.shader 상단 주석). 그건 배럴 왜곡이 UV를 옮겨 가며
    // 샘플하기 때문인데, 이 패스는 제자리에서 곱하기만 해서 좌표가 조금 어긋나도 결과가 같다.
    // 그러니 이 셰이더에 UV를 옮기는 연산을 넣지 말 것 — 넣는 순간 그 문제에 노출된다.
    Properties
    {
        _UserBrightness ("User Brightness", Range(0.2, 2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "ScreenBrightness"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // 포함 순서와 경로는 CrtEffect.shader와 똑같이 맞춘다. URP의 Core.hlsl을 먼저 넣어야
            // Blit.hlsl이 쓰는 TEXTURE2D_X()가 제대로 풀린다 — core 패키지의 TextureXR.hlsl을 직접
            // 넣으면 D3D11에서 Texture2DArray로 풀려 Render Graph가 바인딩한 _BlitTexture와 타입이
            // 어긋나고, 유니티가 기본 더미(회색) 텍스처로 대체해 화면이 통째로 회색이 된다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _UserBrightness;
            CBUFFER_END

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                col.rgb *= _UserBrightness;
                return col;
            }
            ENDHLSL
        }
    }
}
