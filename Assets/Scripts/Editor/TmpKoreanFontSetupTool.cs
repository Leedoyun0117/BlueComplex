using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// TextMeshPro Essential Resources 임포트 + 한글 TMP Font Asset 생성을 헤드리스로 처리한다.
    /// 실제 폰트 파일은 Noto Sans KR(OFL, Google Fonts 저장소에서 받음) — 나중에 교체하려면
    /// SourceFontPath만 바꾸면 된다. TMP_FontAsset.CreateFontAsset(공개 API)로 만든다 —
    /// TMP_FontAsset_CreationMenu.cs가 쓰는 내부 필드(m_SourceFontFileGUID 등)는 TMP 자체
    /// 에디터 어셈블리에만 InternalsVisibleTo로 노출돼 있어 이 프로젝트 어셈블리에서는 접근 불가.
    /// </summary>
    public static class TmpKoreanFontSetupTool
    {
        public const string SourceFontPath = "Assets/Fonts/NotoSansKR-Regular.ttf";
        public const string FontAssetPath = "Assets/Fonts/NotoSansKR SDF.asset";

        [MenuItem("BlueComplex/UI/Setup TMP + Korean Font")]
        public static void SetupAll()
        {
            EnsureTmpEssentials();
            var fontAsset = EnsureKoreanFontAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var msg = fontAsset != null
                ? $"한글 TMP Font Asset 준비 완료: {FontAssetPath}\n(소스: {SourceFontPath}, Noto Sans KR, OFL)"
                : "한글 TMP Font Asset 생성 실패 — Console 로그를 확인하세요.";
            Debug.Log("[TmpKoreanFontSetupTool] " + msg.Replace("\n", " / "));
            if (Application.isBatchMode) return;
            EditorUtility.DisplayDialog("TMP + 한글 폰트 셋업", msg, "확인");
        }

        /// <summary>
        /// 소스 폰트 파일을 바꾼 뒤(예: 가변 폰트를 정적 Regular 인스턴스로 교체) 동적 아틀라스에 남은 옛 글리프를 비운다.
        /// 동적 아틀라스는 글자가 처음 쓰일 때 소스 폰트에서 글리프를 구워 넣으므로, 비워 두면 새 폰트로 다시 구워진다.
        /// Font Asset 자체(GUID, 머티리얼 참조)는 그대로 둔다 — 프리팹의 TMP 텍스트가 이걸 참조하고 있다.
        /// </summary>
        [MenuItem("BlueComplex/UI/Refresh Korean Font Atlas")]
        public static void RefreshAtlas()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                Debug.LogWarning($"[TmpKoreanFontSetupTool] Font Asset이 없다: {FontAssetPath}");
                return;
            }

            fontAsset.ClearFontAssetData(false);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("[TmpKoreanFontSetupTool] 동적 아틀라스를 비웠다 — 다음에 글자가 쓰일 때 현재 소스 폰트로 다시 구워진다.");
        }

        /// <summary>배치모드용 진입점(-executeMethod).</summary>
        public static void RefreshAtlasBatch() => RefreshAtlas();

        private static void EnsureTmpEssentials()
        {
            if (TMP_Settings.instance != null) return;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();

            if (TMP_Settings.instance == null)
                Debug.LogWarning("[TmpKoreanFontSetupTool] TMP Essential Resources 임포트를 시도했지만 TMP_Settings.instance가 여전히 null입니다. " +
                                  "Unity Editor에서 Window > TextMeshPro > Import TMP Essential Resources를 한 번 더 실행해야 할 수 있습니다.");
        }

        public static TMP_FontAsset EnsureKoreanFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            if (TMP_Settings.instance == null)
            {
                Debug.LogError("[TmpKoreanFontSetupTool] TMP Essential Resources가 없어 Font Asset을 만들 수 없습니다.");
                return null;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                Debug.LogError($"[TmpKoreanFontSetupTool] 소스 폰트를 찾을 수 없습니다: {SourceFontPath}");
                return null;
            }

            FontEngine.InitializeFontEngine();

            // 공개 API. 동적 아틀라스(런타임에 필요한 글리프만 채움) — 한글처럼 글리프 수가 많은
            // 문자셋을 미리 다 구울 필요가 없다. 소스 폰트 참조/GUID, 아틀라스 텍스처, 머티리얼은
            // 이 호출 안에서 다 만들어진다 — 내부 필드를 직접 건드릴 필요가 없다.
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError($"[TmpKoreanFontSetupTool] TMP_FontAsset.CreateFontAsset 실패: {font.name}");
                return null;
            }

            EnsureFolder(Path.GetDirectoryName(FontAssetPath)?.Replace('\\', '/'));

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
