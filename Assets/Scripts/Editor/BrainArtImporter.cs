using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// Assets/Art/UI/Brain 아래 도트 뇌 아트(기획서의 컴플렉스 인터페이스 이미지)의 임포트 설정을 강제한다: 스프라이트, 점 필터(도트가 뭉개지지 않게), 압축 없음,
    /// 읽기 가능(뇌 영역 오버레이는 알파로 호버 영역을 판정한다 — Image.alphaHitTestMinimumThreshold가 텍스처를 읽는다).
    /// 후처리기라서 어느 프로젝트 사본에서 임포트해도 같은 설정이 된다.
    /// </summary>
    public sealed class BrainArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Art/UI/Brain/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
        }
    }
}
