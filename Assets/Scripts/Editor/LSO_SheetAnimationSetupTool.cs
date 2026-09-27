using System.IO;
using BlueComplex.UI.Background;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 고른 배경 레이어 quad에 시트 애니메이션을 붙인다 — 머티리얼 만들기·텍스처 물리기·컴포넌트 붙이기까지.
    ///
    /// ─── 왜 툴인가 ────────────────────────────────────────────────────────
    /// 손으로 하면 네 단계다(머티리얼 복제 → 텍스처 교체 → 렌더러에 물리기 → 컴포넌트 붙이고 프레임 수 입력).
    /// 그중 두 개가 조용히 틀리기 쉽다:
    ///  - 기존 머티리얼을 <b>그대로 고치면 그걸 같이 쓰는 다른 씬까지 바뀐다.</b>
    ///    (실제로 Room2_LeftPeople.mat은 DLJ_GameTestScene도 쓴다.) 그래서 항상 사본을 만든다.
    ///  - 프레임 수를 눈으로 세다 틀린다. 여기서는 텍스처의 스프라이트 슬라이스 수에서 읽어 온다.
    ///
    /// ─── 쓰는 법 ──────────────────────────────────────────────────────────
    /// Renderer가 붙은 quad(예: BothPeople > Quad)를 고르고 메뉴를 누른다.
    /// 시트는 부모 이름으로 찾는다 — 부모가 BothPeople이면 프로젝트에서 "BothPeople-Sheet" 텍스처를 찾는다.
    /// 머티리얼은 원래 머티리얼과 같은 폴더에 "Room2_부모이름.mat"으로 만든다(이미 있으면 그걸 쓴다).
    ///
    /// 되돌리려면 Ctrl+Z. 다만 <b>만들어진 .mat 에셋은 남는다</b> — 필요 없으면 지울 것.
    /// </summary>
    internal static class LSO_SheetAnimationSetupTool
    {
        private const string Menu = "BlueComplex/LSO/Background/Set Up Sheet Animation";
        private const string SheetSuffix = "-Sheet";

        [MenuItem(Menu, priority = 110)]
        private static void SetUp()
        {
            var done = 0;
            foreach (var go in Selection.gameObjects)
                if (SetUpOne(go)) done++;

            if (done == 0)
            {
                Debug.LogWarning($"[{nameof(LSO_SheetAnimationSetupTool)}] 아무것도 붙이지 못했다. " +
                    "Renderer가 붙은 quad를 고를 것 (예: BothPeople > Quad).");
                return;
            }

            EditorSceneManager.MarkSceneDirty(Selection.gameObjects[0].scene);
            Debug.Log($"[{nameof(LSO_SheetAnimationSetupTool)}] {done}개에 시트 애니메이션을 붙였다.");
        }

        [MenuItem(Menu, true)]
        private static bool HasSelection() => Selection.gameObjects.Length > 0;

        private static bool SetUpOne(GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null) return false;

            // quad 이름은 죄다 "Quad"라 시트 이름은 부모에서 가져온다.
            var owner = go.transform.parent != null ? go.transform.parent.name : go.name;

            var sheet = FindSheet(owner);
            if (sheet == null)
            {
                Debug.LogWarning($"[{nameof(LSO_SheetAnimationSetupTool)}] '{owner}{SheetSuffix}' 텍스처를 찾지 못했다.", go);
                return false;
            }

            var frames = CountFrames(sheet);
            if (frames < 1)
            {
                Debug.LogWarning($"[{nameof(LSO_SheetAnimationSetupTool)}] '{sheet.name}'의 프레임 수를 알 수 없다 — " +
                    "스프라이트 에디터에서 가로로 잘라 두면 그 개수를 읽는다. 컴포넌트에서 직접 넣을 것.", go);
                frames = 1;
            }

            var material = GetOrCreateMaterial(renderer.sharedMaterial, owner, sheet, frames);
            if (material == null) return false;

            Undo.RecordObject(renderer, "Set Up Sheet Animation");
            renderer.sharedMaterial = material;

            var animation = go.GetComponent<LSO_LayerSheetAnimation>();
            if (animation == null) animation = Undo.AddComponent<LSO_LayerSheetAnimation>(go);

            // frameCount는 private 직렬화 필드라 SerializedObject로 넣는다.
            var serialized = new SerializedObject(animation);
            serialized.FindProperty("frameCount").intValue = frames;
            serialized.ApplyModifiedProperties();

            Debug.Log($"[{nameof(LSO_SheetAnimationSetupTool)}] {owner}: {sheet.name}, {frames}프레임, {material.name}", go);
            return true;
        }

        private static Texture2D FindSheet(string owner)
        {
            var name = owner + SheetSuffix;
            foreach (var guid in AssetDatabase.FindAssets($"{name} t:Texture2D"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != name) continue; // 부분 일치를 걸러낸다.
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return null;
        }

        /// <summary>스프라이트 에디터로 잘라 둔 조각 수가 곧 프레임 수다. 안 잘라 뒀으면 가로세로비로 짐작한다.</summary>
        private static int CountFrames(Texture2D sheet)
        {
            var path = AssetDatabase.GetAssetPath(sheet);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.spritesheet.Length > 0)
                return importer.spritesheet.Length;

            // 한 줄짜리 시트라면 높이로 나눈 몫이 프레임 수다. 프레임이 정사각이 아니면 틀리니 짐작일 뿐이다.
            if (sheet.height > 0 && sheet.width % sheet.height == 0) return sheet.width / sheet.height;

            return 0;
        }

        /// <summary>
        /// 원래 머티리얼을 그대로 쓰지 않고 사본을 만든다 — 같은 .mat을 쓰는 다른 씬이 딸려 바뀌지 않게.
        /// 셰이더와 맞춰 둔 값(_AlphaPower, Render Queue 등)은 복제로 그대로 따라온다.
        /// </summary>
        private static Material GetOrCreateMaterial(Material source, string owner, Texture2D sheet, int frames)
        {
            var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(source))?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder)) return null;

            var path = $"{folder}/Room2_{owner}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                Undo.RecordObject(material, "Set Up Sheet Animation");
            }

            material.mainTexture = sheet;

            // 에디터에서도 한 프레임만 보이게 타일링을 맞춰 둔다. 런타임에는 컴포넌트가 덮어쓴다.
            material.mainTextureScale = new Vector2(1f / frames, 1f);
            material.mainTextureOffset = Vector2.zero;

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
