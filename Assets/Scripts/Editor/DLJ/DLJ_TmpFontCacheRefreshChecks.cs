using System;
using System.Linq;
using System.Reflection;
using BlueComplex.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace BlueComplex.Editor.DLJ
{
    /// <summary>DLJ: 다중 아틀라스 사용 후 Git 재임포트에 해당하는 직렬화 교체를 재현한다.</summary>
    public static class DLJ_TmpFontCacheRefreshChecks
    {
        [MenuItem("Tools/BlueComplex/DLJ/Validate TMP Font Cache Refresh")]
        public static void Run()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(TmpKoreanFontSetupTool.SourceFontPath);
            Require(source != null, "Korean source font is required.");
            var font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA,
                256, 256, AtlasPopulationMode.Dynamic, true);
            var original = EditorJsonUtility.ToJson(font);
            var initialTexture = font.atlasTextures[0];
            var initialMaterial = font.material;
            var textures = new System.Collections.Generic.HashSet<Texture2D>();
            GameObject textObject = null;
            try
            {
                var text = new string(Enumerable.Range(0xAC00, 80).Select(value => (char)value).ToArray());
                Require(font.TryAddCharacters(text), "Test characters must exist in the source font.");
                foreach (var texture in font.atlasTextures)
                    if (texture != null) textures.Add(texture);

                var cached = font.characterLookupTable.Values.First(character => character.glyph.atlasIndex > 0);
                var oldAtlasIndex = cached.glyph.atlasIndex;

                // Unity는 에셋 재임포트 시 직렬화 필드를 교체하지만 TMP의 조회 사전은 남길 수 있다.
                EditorJsonUtility.FromJsonOverwrite(original, font);
                // 임시 텍스처/머티리얼에는 에셋 GUID가 없어 JSON 왕복 시 참조를 복원한다.
                font.atlasTextures = new[] { initialTexture };
                font.material = initialMaterial;
                Require(font.atlasTextures.Length == 1, "Reimport must restore a single atlas.");
                Require(font.characterLookupTable[cached.unicode].glyph.atlasIndex == oldAtlasIndex,
                    "Reimport must reproduce the stale lookup entry.");

                var fallback = typeof(TMP_MaterialManager).GetMethod("GetFallbackMaterial",
                    BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(TMP_FontAsset), typeof(Material), typeof(int) }, null);
                Require(fallback != null, "TMP multi-atlas material method must exist.");
                var reproduced = false;
                try { fallback.Invoke(null, new object[] { font, font.material, oldAtlasIndex }); }
                catch (TargetInvocationException ex) when (ex.InnerException is IndexOutOfRangeException)
                {
                    reproduced = true;
                }
                Require(reproduced, "The original GetFallbackMaterial exception must reproduce before repair.");

                DLJ_TmpFontCacheRefresh.Refresh(font);
                Require(!font.characterLookupTable.ContainsKey(cached.unicode), "Stale character must be removed.");
                Require(font.TryAddCharacters(text), "Characters must be regenerated after repair.");
                foreach (var character in font.characterLookupTable.Values)
                    Require(character.glyph.atlasIndex >= 0 && character.glyph.atlasIndex < font.atlasTextures.Length,
                        "Every character must reference a valid atlas.");

                textObject = new GameObject("DLJ TMP Cache Check", typeof(Canvas));
                textObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(textObject.transform, false);
                var label = labelObject.GetComponent<TextMeshProUGUI>();
                label.font = font;
                label.text = text;
                label.rectTransform.sizeDelta = new Vector2(1200f, 1200f);
                label.ForceMeshUpdate(true, true);
                Require(label.textInfo.characterCount == text.Length, "All characters must render after repair.");

                DLJ_TmpFontCacheRefresh.Refresh(font);
                label.ForceMeshUpdate(true, true);
                Require(label.textInfo.characterCount == text.Length, "Repeated refresh must preserve valid text.");
                Debug.Log("[DLJ TMP] Reproduced stale-atlas exception; cache refresh and multi-atlas rendering passed.");
            }
            finally
            {
                if (textObject != null) Object.DestroyImmediate(textObject);
                foreach (var texture in font.atlasTextures)
                    if (texture != null) textures.Add(texture);
                // TMP_FontAsset.OnDestroy가 현재 아틀라스와 머티리얼을 해제한다.
                Object.DestroyImmediate(font);
                foreach (var texture in textures)
                    if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
