using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using BlueComplex.UI.Background;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// Assets/Animation의 스테이지 배경 Idle 애니메이션(.aseprite 3종)을 게임이 쓰는 형태로 바꾼다.
    /// 프로젝트에 Aseprite 임포터 패키지가 없어 .aseprite는 Unity가 그냥 바이너리로만 들고 있다 — 여기서 파일을 직접 읽어
    /// 보이는 레이어를 프레임마다 합성하고, 프레임을 가로로 이어 붙인 스프라이트 시트 PNG + 프레임 길이 JSON을
    /// Assets/Resources/StageIdle/에 쓴다(<see cref="StageIdleBackground"/>가 Resources에서 읽는다).
    /// 아트를 고쳐 다시 저장했으면 메뉴를 한 번 더 돌리면 된다(결과 파일을 덮어쓴다).
    /// </summary>
    public static class StageIdleImportTool
    {
        private const string OutputFolder = "Assets/Resources/" + StageIdleBackground.ResourceFolder;

        private static readonly (int Stage, string Source)[] Sources =
        {
            (1, "Assets/Animation/BlueRoomIDLE.aseprite"),
            (2, "Assets/Animation/ClockToweridle.aseprite"),
            (3, "Assets/Animation/GrrenRoomidlefix.aseprite"),
        };

        /// <summary>배치모드용 진입점 — 임포트 뒤 에디터를 종료한다.</summary>
        public static void RunAndExit()
        {
            Run();
            EditorApplication.Exit(0);
        }

        [MenuItem("BlueComplex/Background/Import Stage Idle (Aseprite)")]
        public static void Run()
        {
            Directory.CreateDirectory(OutputFolder);
            var log = new StringBuilder("스테이지 Idle 배경 임포트\n");

            foreach (var (stage, source) in Sources)
            {
                var ase = AsepriteFile.Read(source);
                var frames = ase.ComposeFrames();

                var sheetPath = $"{OutputFolder}/{StageIdleBackground.SheetName(stage)}.png";
                var jsonPath = $"{OutputFolder}/{StageIdleBackground.SheetName(stage)}.json";
                File.WriteAllBytes(sheetPath, BuildSheetPng(ase.Width, ase.Height, frames));

                var data = new StageIdleBackground.SheetData
                {
                    frameWidth = ase.Width,
                    frameHeight = ase.Height,
                    frameCount = frames.Count,
                    durationsMs = ase.DurationsMs.ToArray(),
                };
                File.WriteAllText(jsonPath, JsonUtility.ToJson(data, true), new UTF8Encoding(false));

                AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
                ConfigureSheetTexture(sheetPath);
                AssetDatabase.ImportAsset(jsonPath, ImportAssetOptions.ForceUpdate);

                log.AppendLine($"  스테이지 {stage}: {Path.GetFileName(source)} → {ase.Width}x{ase.Height} × {frames.Count}프레임, " +
                               $"길이(ms) [{string.Join(", ", ase.DurationsMs)}]");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>프레임을 왼쪽부터 순서대로 이어 붙인 PNG. Aseprite는 위→아래 행 순서라 Unity 텍스처(아래→위)에 맞게 뒤집어 쓴다.</summary>
        private static byte[] BuildSheetPng(int frameWidth, int frameHeight, List<Color32[]> frames)
        {
            var sheetWidth = frameWidth * frames.Count;
            var pixels = new Color32[sheetWidth * frameHeight];
            for (var f = 0; f < frames.Count; f++)
            {
                var frame = frames[f];
                for (var y = 0; y < frameHeight; y++)
                {
                    var flippedRow = frameHeight - 1 - y;
                    Array.Copy(frame, y * frameWidth, pixels, flippedRow * sheetWidth + f * frameWidth, frameWidth);
                }
            }

            var texture = new Texture2D(sheetWidth, frameHeight, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            var png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }

        /// <summary>도트 아트 시트: Point, 압축 없음, 밉맵 없음, NPOT 그대로, Clamp. (스프라이트가 아니라 UV로 프레임을 고르므로 Default 텍스처.)</summary>
        private static void ConfigureSheetTexture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 8192;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        /// <summary>.aseprite 바이너리에서 32bpp RGBA 프레임을 합성하는 최소 리더. 일반 블렌드·셀 z-index 0만 쓰는 도트 작업물 기준이다
        /// (다른 블렌드 모드/인덱스·회색조 색상은 지원하지 않고 예외로 알린다).</summary>
        private sealed class AsepriteFile
        {
            private struct Layer
            {
                public ushort Flags;
                public ushort Type;
                public ushort ChildLevel;
                public byte Opacity;
                public string Name;
            }

            private struct Cel
            {
                public int LayerIndex;
                public int X, Y, Width, Height;
                public byte Opacity;
                public Color32[] Pixels; // 위→아래 행 순서. 링크 셀은 원본 셀의 것을 공유한다.
            }

            public int Width { get; private set; }
            public int Height { get; private set; }
            public readonly List<int> DurationsMs = new List<int>();

            private readonly List<Layer> _layers = new List<Layer>();
            private readonly List<List<Cel>> _frameCels = new List<List<Cel>>();

            public static AsepriteFile Read(string path)
            {
                var data = File.ReadAllBytes(path);
                var file = new AsepriteFile();
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                    file.Parse(reader, path);
                return file;
            }

            private void Parse(BinaryReader r, string path)
            {
                r.ReadUInt32(); // 파일 크기
                if (r.ReadUInt16() != 0xA5E0) throw new InvalidDataException($"{path}: .aseprite 파일이 아닙니다.");
                int frameCount = r.ReadUInt16();
                Width = r.ReadUInt16();
                Height = r.ReadUInt16();
                var depth = r.ReadUInt16();
                if (depth != 32) throw new NotSupportedException($"{path}: 32bpp RGBA만 지원합니다(현재 {depth}bpp).");
                r.BaseStream.Position = 128;

                for (var f = 0; f < frameCount; f++)
                {
                    var frameStart = r.BaseStream.Position;
                    var frameSize = r.ReadUInt32();
                    r.ReadUInt16(); // 0xF1FA
                    int oldChunks = r.ReadUInt16();
                    DurationsMs.Add(r.ReadUInt16());
                    r.ReadBytes(2);
                    var newChunks = r.ReadUInt32();
                    var chunkCount = newChunks != 0 ? (int)newChunks : oldChunks;

                    var cels = new List<Cel>();
                    for (var c = 0; c < chunkCount; c++)
                    {
                        var chunkStart = r.BaseStream.Position;
                        var chunkSize = r.ReadUInt32();
                        var chunkType = r.ReadUInt16();
                        var chunkEnd = chunkStart + chunkSize;

                        if (chunkType == 0x2004) _layers.Add(ReadLayer(r));
                        else if (chunkType == 0x2005) cels.Add(ReadCel(r, chunkEnd, f));

                        r.BaseStream.Position = chunkEnd;
                    }

                    _frameCels.Add(cels);
                    r.BaseStream.Position = frameStart + frameSize;
                }
            }

            private static Layer ReadLayer(BinaryReader r)
            {
                var layer = new Layer { Flags = r.ReadUInt16(), Type = r.ReadUInt16(), ChildLevel = r.ReadUInt16() };
                r.ReadUInt16();
                r.ReadUInt16();
                var blend = r.ReadUInt16();
                layer.Opacity = r.ReadByte();
                r.ReadBytes(3);
                var nameLength = r.ReadUInt16();
                layer.Name = Encoding.UTF8.GetString(r.ReadBytes(nameLength));
                if (blend != 0 && layer.Type == 0)
                    Debug.LogWarning($"레이어 '{layer.Name}'의 블렌드 모드 {blend}는 지원하지 않아 일반 블렌드로 합성합니다.");
                return layer;
            }

            private Cel ReadCel(BinaryReader r, long chunkEnd, int frameIndex)
            {
                var cel = new Cel { LayerIndex = r.ReadUInt16(), X = r.ReadInt16(), Y = r.ReadInt16(), Opacity = r.ReadByte() };
                var celType = r.ReadUInt16();
                r.ReadInt16(); // z-index (이 작업물은 전부 0)
                r.ReadBytes(5);

                if (celType == 1) // 링크: 같은 레이어의 앞 프레임 셀을 그대로 쓴다.
                {
                    var linked = _frameCels[r.ReadUInt16()].Find(c => c.LayerIndex == cel.LayerIndex);
                    linked.LayerIndex = cel.LayerIndex;
                    linked.X = cel.X;
                    linked.Y = cel.Y;
                    linked.Opacity = cel.Opacity;
                    return linked;
                }

                if (celType != 0 && celType != 2) return default; // 타일맵 셀 등은 무시(Pixels == null)

                cel.Width = r.ReadUInt16();
                cel.Height = r.ReadUInt16();
                var byteCount = cel.Width * cel.Height * 4;
                byte[] raw;
                if (celType == 0)
                {
                    raw = r.ReadBytes(byteCount);
                }
                else
                {
                    var compressed = r.ReadBytes((int)(chunkEnd - r.BaseStream.Position));
                    raw = new byte[byteCount];
                    // zlib = 2바이트 헤더 + deflate 본문(+ adler32) — 헤더만 건너뛰고 Deflate로 푼다.
                    using (var input = new MemoryStream(compressed, 2, compressed.Length - 2))
                    using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                    {
                        var read = 0;
                        while (read < byteCount)
                        {
                            var n = deflate.Read(raw, read, byteCount - read);
                            if (n <= 0) break;
                            read += n;
                        }
                    }
                }

                cel.Pixels = new Color32[cel.Width * cel.Height];
                for (var i = 0; i < cel.Pixels.Length; i++)
                    cel.Pixels[i] = new Color32(raw[i * 4], raw[i * 4 + 1], raw[i * 4 + 2], raw[i * 4 + 3]);
                return cel;
            }

            /// <summary>레이어 자신과 모든 부모 그룹이 눈(가시성)이 켜져 있어야 보인다.</summary>
            private bool IsVisible(int layerIndex)
            {
                if ((_layers[layerIndex].Flags & 1) == 0) return false;
                var need = _layers[layerIndex].ChildLevel - 1;
                for (var j = layerIndex - 1; j >= 0 && need >= 0; j--)
                {
                    if (_layers[j].Type != 1 || _layers[j].ChildLevel != need) continue;
                    if ((_layers[j].Flags & 1) == 0) return false;
                    need--;
                }

                return true;
            }

            /// <summary>프레임마다 보이는 이미지 레이어를 아래(인덱스 작은 쪽)부터 straight-alpha over로 합성한다. 행은 위→아래.</summary>
            public List<Color32[]> ComposeFrames()
            {
                var visible = new bool[_layers.Count];
                for (var i = 0; i < visible.Length; i++) visible[i] = _layers[i].Type == 0 && IsVisible(i);

                var result = new List<Color32[]>();
                foreach (var cels in _frameCels)
                {
                    var canvas = new Color32[Width * Height];
                    cels.Sort((a, b) => a.LayerIndex.CompareTo(b.LayerIndex));
                    foreach (var cel in cels)
                    {
                        if (cel.Pixels == null || !visible[cel.LayerIndex]) continue;
                        var opacity = _layers[cel.LayerIndex].Opacity * cel.Opacity / (255f * 255f);
                        Blit(canvas, cel, opacity);
                    }

                    result.Add(canvas);
                }

                return result;
            }

            private void Blit(Color32[] canvas, Cel cel, float opacity)
            {
                for (var y = 0; y < cel.Height; y++)
                {
                    var cy = y + cel.Y;
                    if (cy < 0 || cy >= Height) continue;
                    for (var x = 0; x < cel.Width; x++)
                    {
                        var cx = x + cel.X;
                        if (cx < 0 || cx >= Width) continue;

                        var src = cel.Pixels[y * cel.Width + x];
                        var srcA = src.a / 255f * opacity;
                        if (srcA <= 0f) continue;

                        var idx = cy * Width + cx;
                        var dst = canvas[idx];
                        var dstA = dst.a / 255f;
                        var outA = srcA + dstA * (1f - srcA);
                        var k = dstA * (1f - srcA);
                        canvas[idx] = new Color32(
                            (byte)Mathf.Clamp(Mathf.RoundToInt((src.r * srcA + dst.r * k) / outA), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt((src.g * srcA + dst.g * k) / outA), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt((src.b * srcA + dst.b * k) / outA), 0, 255),
                            (byte)Mathf.Clamp(Mathf.RoundToInt(outA * 255f), 0, 255));
                    }
                }
            }
        }
    }
}
