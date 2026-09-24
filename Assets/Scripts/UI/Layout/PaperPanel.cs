using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>어느 종이 머티리얼을 쓸지. 패널은 큰 종이, 카드는 패널 위에 얹힌 작은 종이(지금은 같은 값의 별도 머티리얼 — 나중에 따로 조정할 수 있게).</summary>
    public enum PaperKind
    {
        Panel,
        Card,
    }

    /// <summary>
    /// 종이 배경 Image를 "손으로 그은 흔들리는 테두리만 있는 백지"로 그리는 메시 효과. 짝이 되는 셰이더는 BlueComplex/UI/PaperPanel.
    /// 셰이더가 px 단위로 선을 그리려면 "이 사각형이 몇 px인지"를 알아야 해서 정점 UV0에 실어 준다:
    /// uv0.xy = 사각형 안 0..1 좌표, uv0.zw = (가로/세로 캔버스 px 크기 + 시드 소수부).
    /// 크기는 캔버스 단위(localScale과 무관)라 카드가 스케일 애니메이션을 해도 윤곽이 튀지 않는다. 머티리얼의 _BlockSize는 1로 둔다(셀 = 1px).
    ///
    /// <para><b>컴포넌트 순서가 중요하다.</b> Outline/Shadow는 정점을 복제하는데, 이 효과가 먼저 돌아야 복제본도 같은 UV를 물려받아
    /// 그림자가 같은 흔들리는 윤곽을 따른다. 그래서 <see cref="Skin"/>이 이미 붙어 있는 Outline/Shadow를 떼었다가 이 컴포넌트 뒤에 다시 붙인다.</para>
    ///
    /// 텍스트·아이콘은 자식 오브젝트라 이 셰이더를 안 거친다.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public sealed class PaperPanel : BaseMeshEffect
    {
        private const string PanelMaterialPath = "UI/PaperPanel";
        private const string CardMaterialPath = "UI/PaperCard";

        // 0이면 이름·형제 순서에서 자동으로 정한다(같은 크기의 카드 넷도 윤곽의 흔들림이 서로 다르다). 고스트가 원본과 같은 무늬를 물려받을 때 값을 넣는다.
        [SerializeField] private int _seed;

        /// <summary>실제로 쓰는 시드(1..~10^6). 자동이면 오브젝트 이름·부모 이름·형제 순서의 해시.</summary>
        public int Seed => _seed != 0 ? _seed : AutoSeed();

        public static Material Load(PaperKind kind) => Resources.Load<Material>(kind == PaperKind.Panel ? PanelMaterialPath : CardMaterialPath);

        /// <summary>
        /// 이 그래픽을 종이 머티리얼 + 정점 데이터 효과로 만든다(멱등). 이미 붙어 있는 Outline/Shadow는 순서를 맞추려 떼었다가 같은 값으로 다시 붙인다.
        /// 머티리얼 에셋이 아직 없으면(메뉴 BlueComplex/UI/Setup Paper Panels 전) 아무것도 바꾸지 않고 null.
        /// </summary>
        public static PaperPanel Skin(Graphic graphic, PaperKind kind, int seed = 0) => Skin(graphic, Load(kind), seed);

        public static PaperPanel Skin(Graphic graphic, Material material, int seed = 0)
        {
            if (graphic == null || material == null) return null;

            graphic.material = material;

            var panel = graphic.GetComponent<PaperPanel>();
            if (panel != null)
            {
                if (seed != 0) panel._seed = seed;
                return panel;
            }

            var saved = new List<(System.Type Type, Color Color, Vector2 Distance, bool UseAlpha, bool Enabled)>();
            foreach (var effect in graphic.GetComponents<Shadow>())
            {
                saved.Add((effect.GetType(), effect.effectColor, effect.effectDistance, effect.useGraphicAlpha, effect.enabled));
                DestroyImmediate(effect);
            }

            panel = graphic.gameObject.AddComponent<PaperPanel>();
            panel._seed = seed;

            foreach (var entry in saved)
            {
                var restored = (Shadow)graphic.gameObject.AddComponent(entry.Type);
                restored.effectColor = entry.Color;
                restored.effectDistance = entry.Distance;
                restored.useGraphicAlpha = entry.UseAlpha;
                restored.enabled = entry.Enabled;
            }

            return panel;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            var rect = graphic.rectTransform.rect;
            if (rect.width < 1f || rect.height < 1f) return;

            // 시드는 천분의 일 단위 소수부로 px 크기에 얹는다(셰이더가 floor/frac로 다시 분리한다).
            var seed = Seed;
            var cellsX = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            var cellsY = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            var z = cellsX + (seed % 997) / 1000f;
            var w = cellsY + (seed / 997 % 991) / 1000f;

            var vertex = new UIVertex();
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                var position = vertex.position;
                vertex.uv0 = new Vector4(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, position.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, position.y),
                    z, w);
                vh.SetUIVertex(vertex, i);
            }
        }

        private int AutoSeed()
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in name) hash = (hash ^ c) * 16777619u;
                var parent = transform.parent;
                if (parent != null)
                {
                    foreach (var c in parent.name) hash = (hash ^ c) * 16777619u;
                    hash = (hash ^ (uint)transform.GetSiblingIndex()) * 16777619u;
                }

                return (int)(hash % 989999u) + 1;
            }
        }
    }
}
