using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 정식 UI 1단계 셋업을 순서대로 한 번에 실행한다: TMP+한글 폰트 → 레이아웃 프리팹 →
    /// 렌더링 배선(UI 카메라/Base_Renderer/CRT 머티리얼의 _UITex/씬 배선). 배치 모드
    /// (-executeMethod BlueComplex.EditorTools.Stage1UISetupTool.SetupAll)로도 그대로 쓸 수 있다.
    /// </summary>
    public static class Stage1UISetupTool
    {
        [MenuItem("BlueComplex/UI/Setup Stage 1 (All)")]
        public static void SetupAll()
        {
            TmpKoreanFontSetupTool.SetupAll();
            UICompositorSetupTool.SetupAll();
            Debug.Log("[Stage1UISetupTool] 정식 UI 1단계 셋업 완료.");
        }
    }
}
