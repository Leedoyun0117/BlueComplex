using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 배경 레이어 quad의 <c>sortingOrder</c>를 z 위치대로 다시 매긴다.
    ///
    /// ─── 왜 필요한가 ──────────────────────────────────────────────────────
    /// 배경 셰이더(BlueComplex/Background/Layer)는 ZWrite Off라 레이어끼리 깊이를 쓰지 않는다.
    /// 그래서 z 위치는 앞뒤에 아무 역할을 못 하고, 순서는 오직 "누가 나중에 그려지나"로 정해진다:
    ///     Renderer.sortingLayer → Renderer.sortingOrder → material.renderQueue → 카메라 거리
    /// sortingOrder가 renderQueue보다 우선하므로, 머티리얼 큐를 맞춰 놔도 렌더러 순번이 어긋나 있으면
    /// 뒤 레이어가 앞 레이어를 덮는다. (실제로 BackIronBar z=11.8 order 6 이 BothPeople z=11.45 order 5 를 덮었다.)
    ///
    /// ─── 왜 툴인가 ────────────────────────────────────────────────────────
    /// MeshRenderer의 sortingOrder는 일반 인스펙터에 없고, Unity 6에서는 Debug 인스펙터에도 나오지 않는다.
    /// 코드로 넣는 길밖에 없다.
    ///
    /// ─── 간격을 둔다 ──────────────────────────────────────────────────────
    /// 0,1,2…로 촘촘히 매기면 나중에 레이어 하나를 사이에 끼울 자리가 없어서 전부 다시 매겨야 한다.
    /// (room2가 지금 그 상태다 — 5·6·7이 붙어 있어 BothPeople을 넣을 칸이 없었다.)
    /// 그래서 <see cref="Step"/> 간격으로 둔다. 상대 순서만 맞으면 되므로 값 자체는 뭐든 상관없다.
    ///
    /// 파티클(컵 김·램프 연기)은 BackgroundSorting이 Things quad의 순번을 런타임에 읽어 따라가므로
    /// 다시 매겨도 알아서 맞는다.
    /// </summary>
    internal static class LSO_BackgroundSortingTool
    {
        private const string SortMenu = "BlueComplex/LSO/Background/Sort Layers By Z";
        private const string LogMenu = "BlueComplex/LSO/Background/Log Layer Order";
        private const int Step = 10;

        [MenuItem(LogMenu, priority = 100)]
        private static void LogOrder()
        {
            var renderers = Collect();
            if (renderers.Count == 0) return;

            Debug.Log(Describe(renderers, "지금 순서 (뒤 → 앞)"));
        }

        /// <summary>
        /// 고르는 건 "부모"다 — 예: Background Rig 아래 Layers, 또는 방 오브젝트들을 통째로.
        /// 고른 것 아래의 Renderer를 전부 모아 z가 먼 것부터 순번을 올려 준다.
        /// </summary>
        [MenuItem(SortMenu, priority = 101)]
        private static void SortByZ()
        {
            var renderers = Collect();
            if (renderers.Count == 0) return;

            var before = Describe(renderers, "고치기 전 (뒤 → 앞)");

            Undo.RecordObjects(renderers.Cast<Object>().ToArray(), "Sort Background Layers By Z");
            for (var i = 0; i < renderers.Count; i++)
            {
                renderers[i].sortingOrder = i * Step;
                EditorUtility.SetDirty(renderers[i]);
            }

            EditorSceneManager.MarkSceneDirty(renderers[0].gameObject.scene);

            Debug.Log($"{before}\n{Describe(renderers, "고친 뒤 (뒤 → 앞)")}");
        }

        [MenuItem(SortMenu, true)]
        [MenuItem(LogMenu, true)]
        private static bool HasSelection() => Selection.gameObjects.Length > 0;

        /// <summary>
        /// 고른 것들 아래의 Renderer를 카메라에서 먼 것부터 모은다.
        ///
        /// z가 같은 짝(건물과 그 빛처럼)은 지금 순번을, 그것도 같으면 하이어라키 순서를 따른다 —
        /// 툴을 두 번 돌려도 결과가 달라지지 않게.
        /// </summary>
        private static List<Renderer> Collect()
        {
            var renderers = new List<Renderer>();
            foreach (var go in Selection.gameObjects)
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                    if (!renderers.Contains(renderer)) renderers.Add(renderer);

            if (renderers.Count == 0)
            {
                Debug.LogWarning("[LSO_BackgroundSortingTool] 고른 것 아래에 Renderer가 없다. " +
                    "배경 레이어를 품은 부모(예: Background Rig의 Layers)를 고를 것.");
                return renderers;
            }

            return renderers
                .Select((r, index) => (r, index))
                .OrderByDescending(x => x.r.transform.position.z)
                .ThenBy(x => x.r.sortingOrder)
                .ThenBy(x => x.index)
                .Select(x => x.r)
                .ToList();
        }

        private static string Describe(IReadOnlyList<Renderer> renderers, string title)
        {
            var report = new StringBuilder($"[LSO_BackgroundSortingTool] {title}\n");
            foreach (var renderer in renderers)
            {
                var material = renderer.sharedMaterial;
                report.AppendLine(
                    $"  order {renderer.sortingOrder,5}   z {renderer.transform.position.z,8:0.00}   " +
                    $"queue {(material != null ? material.renderQueue : -1),5}   " +
                    $"{Path(renderer.transform)}");
            }

            return report.ToString();
        }

        /// <summary>quad 이름이 죄다 "Quad"라 부모까지 붙여야 어느 레이어인지 알아볼 수 있다.</summary>
        private static string Path(Transform transform)
        {
            var parent = transform.parent;
            return parent != null ? $"{parent.name}/{transform.name}" : transform.name;
        }
    }
}
