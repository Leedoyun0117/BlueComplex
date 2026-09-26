using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>Assets/Art의 시작 컷신 그림(IntroCar · IntroClock+바늘 · InterrogationRoom)을 <see cref="IntroCutsceneArt"/>(Assets/Resources)에 파일 이름으로 채운다. 그림을 바꾼 뒤 한 번 돌리면 된다.</summary>
    public static class IntroCutsceneArtTool
    {
        private const string CarPath = "Assets/Art/IntroCar.png";
        private const string ClockPath = "Assets/Art/IntroClock.png";
        private const string ClockHourPath = "Assets/Art/IntroClockHour.png";
        private const string ClockMinutePath = "Assets/Art/IntroClockMinute.png";
        private const string RoomPath = "Assets/Art/InterrogationRoom.png";
        private const string AssetPath = "Assets/Resources/IntroCutsceneArt.asset";

        [MenuItem("BlueComplex/Cutscene/Rebuild Intro Art")]
        public static void Rebuild()
        {
            var art = AssetDatabase.LoadAssetAtPath<IntroCutsceneArt>(AssetPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<IntroCutsceneArt>();
                AssetDatabase.CreateAsset(art, AssetPath);
            }

            art.car = Load(CarPath);
            art.clock = Load(ClockPath);
            art.clockHour = Load(ClockHourPath);
            art.clockMinute = Load(ClockMinutePath);
            art.room = Load(RoomPath);

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("[IntroCutsceneArt] 시작 컷신 그림 표를 다시 만들었다.");
        }

        private static Texture2D Load(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) Debug.LogWarning($"[IntroCutsceneArt] {path}를 찾지 못했다.");
            return texture;
        }
    }
}
