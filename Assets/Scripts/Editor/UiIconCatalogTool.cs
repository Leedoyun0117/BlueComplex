using System.Collections.Generic;
using System.IO;
using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>Assets/Art/UI 아래로 처음 들어오는 PNG를 스프라이트로 임포트한다(이미 설정이 있는 에셋은 건드리지 않는다).</summary>
    public sealed class UiArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/UI/")) return;

            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            Configure(importer);
        }

        /// <summary>UI 스프라이트 임포트 설정. 임포터 코드가 컴파일되기 전에 이미 임포트된 에셋(처음 클론한 프로젝트 등)은 카탈로그 도구가 이걸로 고친다.</summary>
        public static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }

    /// <summary>
    /// Assets/Art/UI/Icons/*.png 를 훑어 <see cref="UiIconCatalog"/>(Assets/Resources/UiIconCatalog.asset)를 다시 만든다. id는 파일 이름이다
    /// (아이템 id, 단서 id, "key", "placeholder"). 아이콘을 추가하거나 바꾼 뒤 한 번 돌리면 된다.
    /// </summary>
    public static class UiIconCatalogTool
    {
        private const string IconFolder = "Assets/Art/UI/Icons";
        private const string CatalogPath = "Assets/Resources/UiIconCatalog.asset";

        [MenuItem("BlueComplex/UI/Rebuild Icon Catalog")]
        public static void Rebuild()
        {
            // 스프라이트로 임포트되지 않은 PNG가 있으면(임포터가 생기기 전에 들어온 파일) 먼저 고친다 — GUID는 그대로다.
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder }))
            {
                var texturePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(texturePath) is TextureImporter importer)) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) continue;

                UiArtImporter.Configure(importer);
                importer.SaveAndReimport();
            }

            var entries = new List<UiIconCatalog.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { IconFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                entries.Add(new UiIconCatalog.Entry { Id = Path.GetFileNameWithoutExtension(path), Sprite = sprite });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var catalog = AssetDatabase.LoadAssetAtPath<UiIconCatalog>(CatalogPath);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                catalog = ScriptableObject.CreateInstance<UiIconCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiIconCatalogTool] 아이콘 {entries.Count}개를 카탈로그에 담았다: {string.Join(", ", entries.ConvertAll(e => e.Id))}");
        }

        /// <summary>배치모드용 진입점(-executeMethod).</summary>
        public static void RebuildBatch() => Rebuild();
    }
}
