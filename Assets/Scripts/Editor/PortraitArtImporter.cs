using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// Assets/Art/UI/Portraits와 Assets/Resources/UI/Portraits 아래 캐릭터 얼굴 아트(유키 쪽은 엑스레이 판넬의 PortraitXrayView,
    /// 나츠 쪽은 그냥 정지 사진 카드)의 임포트 설정을 강제한다: 스프라이트, 점 필터(도트 아트가 뭉개지지 않게 — BrainArtImporter와
    /// 같은 이유), 압축 없음. 알파 히트 테스트는 안 쓰므로(장식용, 클릭·호버 없음) 읽기 가능은 켜지 않는다. 후처리기라서 어느
    /// 프로젝트 사본에서 임포트해도 같은 설정이 된다. 캐릭터별 하위 폴더(Yuki/, Natsu/) 전부를 덮는다 — 폴더 이름 자체는 판정에 안 쓰인다.
    /// Resources 쪽은 PortraitXrayView·NatsuPortraitView가 Resources.Load로 찾는 분노·슬픔·기쁨 등 표정 스프라이트가 놓이는 자리다.
    /// 주의: 유키의 모든 표정 스프라이트는 같은 캔버스(163×201)에 목·어깨 기준으로 정합돼 있어야 한다. Image는 스프라이트의 피벗을 무시하고
    /// 전체 사각형을 Rect에 맞춰 그리므로, 캔버스 크기나 여백이 다르면 표정이 바뀔 때 얼굴이 옆으로 튄다.
    /// </summary>
    public sealed class PortraitArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Art/UI/Portraits/";
        public const string ResourcesFolder = "Assets/Resources/UI/Portraits/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) && !assetPath.StartsWith(ResourcesFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            // 첫 임포트(메타 없음) 때 Unity가 이 텍스처를 왜인지 큐브맵으로 잡는 경우가 있었다 — 명시적으로 2D로 고정한다.
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
        }
    }
}
