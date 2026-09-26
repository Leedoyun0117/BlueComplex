using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>Assets/Art의 시작 컷신·오프닝 그림(Opening/ · IntroClock+바늘 · InterrogationRoom)을 <see cref="IntroCutsceneArt"/>(Assets/Resources)에 파일 이름으로 채운다. 그림을 바꾼 뒤 한 번 돌리면 된다.</summary>
    public static class IntroCutsceneArtTool
    {
        private const string YukiRoomPath = "Assets/Art/Opening/OpeningYukiRoom.png";
        private const string MainRoomPath = "Assets/Art/Opening/OpeningMainRoom.jpg";
        private const string MainChairPath = "Assets/Art/Opening/OpeningMainChair.png";
        private const string MainTablePath = "Assets/Art/Opening/OpeningMainTable.png";
        private const string MainCoffeePath = "Assets/Art/Opening/OpeningMainCoffee.png";
        private const string MainTowerPath = "Assets/Art/Opening/OpeningMainTower.png";
        private const string GirlSilhouettePath = "Assets/Art/girl_silhouette.png";
        private const string GirlWalkPath = "Assets/Art/content.png";
        private const string ClockPath = "Assets/Art/IntroClock.png";
        private const string ClockHourPath = "Assets/Art/IntroClockHour.png";
        private const string ClockMinutePath = "Assets/Art/IntroClockMinute.png";
        private const string RoomPath = "Assets/Art/InterrogationRoom.png";
        private const string AssetPath = "Assets/Resources/IntroCutsceneArt.asset";

        [MenuItem("BlueComplex/Cutscene/Rebuild Intro Art")]
        public static void Rebuild()
        {
            // 도트 그림(유키 방)은 뭉개지지 않게 점 샘플링, 나머지는 원본 그대로(압축 없음). 크기 제한은 원본을 넘지 않게 넉넉히.
            Import(YukiRoomPath, FilterMode.Point);
            Import(MainRoomPath, FilterMode.Bilinear);
            Import(MainChairPath, FilterMode.Bilinear);
            Import(MainTablePath, FilterMode.Bilinear);
            Import(MainCoffeePath, FilterMode.Bilinear);
            Import(MainTowerPath, FilterMode.Bilinear);
            Import(GirlSilhouettePath, FilterMode.Bilinear);
            // GirlWalkPath(content.png)는 건드리지 않는다 — 스프라이트 시트로 잘라 둔 설정(Sprite, 8칸)을 Import가 Default로 되돌리지 않게.

            var art = AssetDatabase.LoadAssetAtPath<IntroCutsceneArt>(AssetPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<IntroCutsceneArt>();
                AssetDatabase.CreateAsset(art, AssetPath);
            }

            art.yukiRoom = Load(YukiRoomPath);
            art.clock = Load(ClockPath);
            art.clockHour = Load(ClockHourPath);
            art.clockMinute = Load(ClockMinutePath);
            art.mainRoom = Load(MainRoomPath);
            art.mainChair = Load(MainChairPath);
            art.mainTable = Load(MainTablePath);
            art.mainCoffee = Load(MainCoffeePath);
            art.mainTower = Load(MainTowerPath);
            art.room = Load(RoomPath);
            art.girlSilhouette = Load(GirlSilhouettePath);
            art.girlWalk = Load(GirlWalkPath);

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("[IntroCutsceneArt] 시작 컷신·오프닝 그림 표를 다시 만들었다.");
        }

        private static void Import(string path, FilterMode filter)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            var changed = importer.textureType != TextureImporterType.Default
                          || importer.filterMode != filter
                          || importer.mipmapEnabled
                          || importer.textureCompression != TextureImporterCompression.Uncompressed
                          || !importer.alphaIsTransparency;
            if (!changed) return;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        private static Texture2D Load(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) Debug.LogWarning($"[IntroCutsceneArt] {path}를 찾지 못했다.");
            return texture;
        }
    }
}
