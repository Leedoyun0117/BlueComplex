using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 손으로 그은 흔들리는 테두리 종이 패널(BlueComplex/UI/PaperPanel 셰이더)의 머티리얼 두 개를 만들고 값을 맞춘다(멱등 — 이미 있으면 GUID를 유지한 채 값만 덮어쓴다).
    /// 셰이더를 갈아 끼운 뒤 남는 옛 속성(해칭/잉크/도트/찢김/얼룩 파라미터)은 여기서 지운다.
    /// Resources/UI 아래에 두는 이유: KeyStatusPanel처럼 코드로 짓는 HUD와 카드 고스트가 <c>PaperPanel.Load</c>로 같은 머티리얼을 읽는다.
    /// 프리팹의 종이 배경에 실제로 입히는 것은 UiLayoutCleanupTool(Apply Layout Cleanup)이 한다.
    /// </summary>
    public static class PaperPanelSetupTool
    {
        private const string Folder = "Assets/Resources/UI";
        private const string ShaderName = "BlueComplex/UI/PaperPanel";
        public const string PanelPath = Folder + "/PaperPanel.mat";
        public const string CardPath = Folder + "/PaperCard.mat";

        [MenuItem("BlueComplex/UI/Setup Paper Panels")]
        public static void Run()
        {
            EnsureMaterials();
            UiLayoutCleanupTool.Apply();
        }

        /// <summary>배치모드용 진입점(-executeMethod).</summary>
        public static void RunBatch() => Run();

        public static void EnsureMaterials()
        {
            Directory.CreateDirectory(Folder);

            // 선 반두께 0.8px(위치에 따라 1.2~2.2px), 위치 흔들림 ±1px(파장 34px), 모서리 반지름 5px, 겹쳐 그은 두 번째 선 75%.
            // 패널과 카드는 지금 같은 값이다(따로 조정할 수 있게 머티리얼만 나눠 둔다).
            Ensure(PanelPath);
            Ensure(CardPath);

            AssetDatabase.SaveAssets();
        }

        private static void Ensure(string path)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[PaperPanelSetupTool] 셰이더 '{ShaderName}'를 못 찾았다(컴파일 오류인지 확인).");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;

            material.SetFloat("_BlockSize", 1f);
            material.SetColor("_InkColor", new Color(0.04f, 0.04f, 0.05f, 1f));
            material.SetFloat("_LineWidth", 0.8f);
            material.SetFloat("_Inset", 3.5f);
            material.SetFloat("_CornerRadius", 5f);
            material.SetFloat("_WobbleAmp", 1f);
            material.SetFloat("_WobbleScale", 34f);
            material.SetFloat("_DoubleStroke", 0.75f);

            // 안쪽 면 베이어 디더 명암: 셀 6px(배경 도트 아트 1픽셀과 같은 크기), 4x4 행렬, 가장 어두운 단계 24%. CRT 스캔라인·블룸에 씻겨서 이보다 약하면 안 보인다.
            material.SetFloat("_DitherDepth", 0.24f);
            material.SetFloat("_DitherCell", 6f);
            material.SetFloat("_DitherMatrix", 4f);
            material.SetFloat("_DitherAmount", 1f);

            PruneStaleProperties(material);
            EditorUtility.SetDirty(material);
        }

        /// <summary>셰이더에 더 이상 없는 저장 속성(옛 해칭/얼룩 값)을 머티리얼에서 지운다.</summary>
        private static void PruneStaleProperties(Material material)
        {
            var alive = new HashSet<string>();
            var shader = material.shader;
            for (var i = 0; i < shader.GetPropertyCount(); i++) alive.Add(shader.GetPropertyName(i));

            var serialized = new SerializedObject(material);
            foreach (var group in new[] { "m_Floats", "m_Colors", "m_Ints", "m_TexEnvs" })
            {
                var list = serialized.FindProperty("m_SavedProperties." + group);
                if (list == null) continue;

                for (var i = list.arraySize - 1; i >= 0; i--)
                {
                    var name = list.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;
                    if (!alive.Contains(name)) list.DeleteArrayElementAtIndex(i);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
