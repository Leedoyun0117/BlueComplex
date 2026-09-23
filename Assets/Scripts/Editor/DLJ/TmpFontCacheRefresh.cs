using BlueComplex.EditorTools;
using TMPro;
using UnityEditor;

namespace BlueComplex.Editor.DLJ
{
    /// <summary>DLJ: Git으로 동적 폰트가 교체되면 직렬화되지 않은 TMP 조회 캐시도 갱신한다.</summary>
    public sealed class TmpFontCacheRefresh : AssetPostprocessor
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // 이미 열린 씬도 복구한다. 에셋 로딩은 도메인 리로드/임포트가 끝난 뒤 실행한다.
            EditorApplication.delayCall += RefreshKoreanFont;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path != TmpKoreanFontSetupTool.FontAssetPath) continue;
                // 다음 Canvas 리빌드 전에 갱신해야 이전 글리프의 atlasIndex가 사용되지 않는다.
                RefreshKoreanFont();
                break;
            }
        }

        [MenuItem("Tools/BlueComplex/DLJ/Refresh TMP Font Cache")]
        public static void RefreshKoreanFont()
        {
            Refresh(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontSetupTool.FontAssetPath));
        }

        internal static void Refresh(TMP_FontAsset font)
        {
            if (font == null) return;

            // OnValidate는 기존 사전이 null일 때만 재구축한다. 재임포트 후에도 남은 사전은
            // 사라진 두 번째 이후 아틀라스를 가리킬 수 있으므로 현재 테이블에서 다시 읽는다.
            font.ReadFontAssetDefinition();
            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, font);
        }
    }
}
