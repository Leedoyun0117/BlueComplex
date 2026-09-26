using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>Assets/Art의 시작 컷신 그림(BlackEyes 폴더 + InterrogationRoom)을 <see cref="IntroCutsceneArt"/>(Assets/Resources)에 파일 이름으로 채운다. 그림을 바꾼 뒤 한 번 돌리면 된다.</summary>
    public static class IntroCutsceneArtTool
    {
        private const string EyeFolder = "Assets/Art/BlackEyes/";
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

            art.blackEye = Load(EyeFolder + "BlackEye.png");
            art.eyeEffect = Load(EyeFolder + "EyeEffect.png");
            art.upperLid = new[] { Load(EyeFolder + "WrinkleUp.png"), Load(EyeFolder + "WrinkleUp2.png"), Load(EyeFolder + "WrinkleUp3.png") };
            art.lowerLid = new[] { Load(EyeFolder + "WrinkleDown.png"), Load(EyeFolder + "WrinkleDown2.png"), Load(EyeFolder + "WrinkleDown3.png") };
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
