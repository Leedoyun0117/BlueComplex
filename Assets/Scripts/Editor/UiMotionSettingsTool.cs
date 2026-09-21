using BlueComplex.UI.Motion;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>연출 시간·크기 값 에셋(Assets/Resources/UiMotionSettings.asset)을 없으면 만든다. 있으면 손대지 않는다 — 인스펙터에서 조정한 값이 남아야 한다.</summary>
    public static class UiMotionSettingsTool
    {
        private const string AssetPath = "Assets/Resources/UiMotionSettings.asset";

        [MenuItem("BlueComplex/UI/Ensure Motion Settings")]
        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<UiMotionSettings>(AssetPath) != null) return;

            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<UiMotionSettings>(), AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiMotionSettingsTool] 연출 설정 에셋을 만들었다: {AssetPath}");
        }
    }
}
