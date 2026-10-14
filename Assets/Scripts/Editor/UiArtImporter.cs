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
}
