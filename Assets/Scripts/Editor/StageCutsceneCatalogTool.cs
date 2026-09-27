using System.Collections.Generic;
using System.Linq;
using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>Assets/TimeLine/Prefabs의 컷신 프리팹(NN_이름)을 훑어 <see cref="StageCutsceneCatalog"/>(Assets/Resources)를 다시 채운다. 컷신 프리팹을 추가·교체한 뒤 한 번 돌리면 된다.</summary>
    public static class StageCutsceneCatalogTool
    {
        private const string PrefabFolder = "Assets/TimeLine/Prefabs";
        private const string AssetPath = "Assets/Resources/" + StageCutsceneCatalog.ResourcePath + ".asset";

        [MenuItem("BlueComplex/Cutscene/Rebuild Cutscene Catalog")]
        public static void Rebuild()
        {
            var entries = new List<StageCutsceneCatalog.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                if (!StageCutsceneCatalog.TryParseNumber(prefab.name, out var number))
                {
                    Debug.LogWarning($"[StageCutsceneCatalogTool] 이름이 번호로 시작하지 않아 건너뜀: {path}");
                    continue;
                }

                entries.Add(new StageCutsceneCatalog.Entry { number = number, prefab = prefab });
            }

            entries = entries.OrderBy(e => e.number).ToList();
            foreach (var duplicate in entries.GroupBy(e => e.number).Where(g => g.Count() > 1))
                Debug.LogWarning($"[StageCutsceneCatalogTool] 컷신 번호 {duplicate.Key}가 프리팹 {duplicate.Count()}개에 겹친다 — 앞의 것만 쓰인다.");

            var catalog = AssetDatabase.LoadAssetAtPath<StageCutsceneCatalog>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StageCutsceneCatalog>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }

            catalog.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageCutsceneCatalogTool] 컷신 {entries.Count}개: {string.Join(", ", entries.Select(e => $"{e.number}={e.prefab.name}"))}");
        }
    }
}
