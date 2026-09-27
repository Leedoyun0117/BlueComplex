using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 엑스레이 뇌의 영역 오버레이를 기획서 원본 4장에서 뽑는다. 원본은 평상시(Brain.png)와 좌/중/우가 발현된 세 장(Brain_Left/Middle/RIght.png)인데,
    /// 발현본은 뇌 <b>전체</b>가 불투명하게 다시 그려진 그림이라 그대로 겹치면 나중 장이 앞 장의 발현을 덮어 버린다(여러 슬롯 동시 발현 불가).
    /// 그래서 평상시와 픽셀이 다른 곳(= 그 영역의 색이 바뀐 곳)만 남기고 나머지는 투명하게 한 오버레이(Overlay/*_Lit.png)를 만든다 —
    /// 세 영역은 서로 겹치지 않고 외곽선은 그대로라(1장 = 영역 색 픽셀만 다름) 평상시 위에 어떤 조합으로 얹어도 원본 발현본과 똑같이 합성된다.
    /// 영역 모양 알파는 호버 판정에도 쓰인다(<see cref="BlueComplex.UI.Presentation.BrainRegionView"/>).
    /// 원본이 바뀌면 다시 돌린다 — 결과가 같으면 파일을 건드리지 않는다(멱등).
    /// </summary>
    public static class BrainOverlayBuilder
    {
        public const string BaseFile = "Brain.png";

        /// <summary>코어 슬롯 순서(첫 번째 = 왼쪽, 두 번째 = 가운데, 세 번째 = 오른쪽)와 같다. 원본 파일명의 "RIght" 대소문자는 받은 그대로다.</summary>
        public static readonly string[] SourceFiles = { "Brain_Left.png", "Brain_Middle.png", "Brain_RIght.png" };

        public static readonly string[] OverlayFiles = { "Overlay/Brain_Left_Lit.png", "Overlay/Brain_Middle_Lit.png", "Overlay/Brain_Right_Lit.png" };

        [MenuItem("BlueComplex/UI/Rebuild Brain Overlays")]
        public static void Rebuild()
        {
            var basePixels = Load(BrainArtImporter.ArtFolder + BaseFile, out var width, out var height);
            var changed = false;

            for (var i = 0; i < SourceFiles.Length; i++)
            {
                var lit = Load(BrainArtImporter.ArtFolder + SourceFiles[i], out var w, out var h);
                if (w != width || h != height)
                    throw new IOException($"뇌 아트 크기가 다르다: {SourceFiles[i]} {w}×{h} ≠ {BaseFile} {width}×{height}");

                var overlay = new Color32[lit.Length];
                var painted = 0;
                for (var p = 0; p < lit.Length; p++)
                {
                    if (Same(lit[p], basePixels[p])) continue;

                    overlay[p] = lit[p];
                    painted++;
                }

                if (painted == 0) throw new IOException($"{SourceFiles[i]}에서 평상시와 다른 픽셀을 못 찾았다 — 오버레이가 비어 있다.");

                changed |= WriteIfChanged(BrainArtImporter.ArtFolder + OverlayFiles[i], overlay, width, height);
            }

            if (changed) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        /// <summary>임포트 설정(읽기 가능 여부)과 상관없이 파일에서 바로 읽는다.</summary>
        private static Color32[] Load(string assetPath, out int width, out int height)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(assetPath))) throw new IOException($"PNG를 못 읽었다: {assetPath}");

                width = texture.width;
                height = texture.height;
                return texture.GetPixels32();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static bool WriteIfChanged(string assetPath, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            var bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            if (File.Exists(assetPath) && System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(assetPath), bytes)) return false;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, bytes);
            return true;
        }
    }
}
