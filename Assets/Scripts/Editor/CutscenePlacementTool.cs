using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 컷씬 프리팹 중 이미지(스프라이트가 들어간 Image)가 여러 장인 것만 골라 열린 씬 Hierarchy에 올린다.
    /// 검은 페이드 오버레이처럼 스프라이트가 없는 Image는 장수에 넣지 않는다.
    /// 이미 씬에 같은 프리팹 인스턴스가 있으면 건너뛴다. Ctrl+Z로 되돌릴 수 있다.
    /// </summary>
    public static class CutscenePlacementTool
    {
        private const string CutscenePrefabFolder = "Assets/TimeLine/Prefabs";

        [MenuItem("BlueComplex/Cutscene/Place Multi-Image Cutscenes")]
        private static void PlaceMultiImageCutscenes()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var existing = new HashSet<GameObject>(scene.GetRootGameObjects()
                .Select(PrefabUtility.GetCorrespondingObjectFromSource)
                .Where(p => p != null));

            var placed = new List<GameObject>();
            var skipped = new List<string>();

            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { CutscenePrefabFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p);

            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var imageCount = prefab.GetComponentsInChildren<Image>(true).Count(i => i.sprite != null);
                if (imageCount < 2) continue;

                if (existing.Contains(prefab))
                {
                    skipped.Add(prefab.name);
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(instance, "Place Multi-Image Cutscenes");
                placed.Add(instance);
                Debug.Log($"[CutscenePlacementTool] {prefab.name} 배치 (이미지 {imageCount}장)", instance);
            }

            if (placed.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.objects = placed.Cast<Object>().ToArray();
            }

            Debug.Log($"[CutscenePlacementTool] 배치 {placed.Count}개" +
                      (skipped.Count > 0 ? $", 이미 씬에 있어서 건너뜀: {string.Join(", ", skipped)}" : ""));
        }
    }
}
