using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 포스트잇 종이의 한 겹을 메시로 그린다. 모양은 <see cref="PostitCurl"/>이 정하고, 겹마다 다른 부분을 그린다:
    /// <list type="bullet">
    /// <item><description><see cref="LayerKind.DropShadow"/> — 종이 전체의 우하단 드롭 섀도(종이 뒤).</description></item>
    /// <item><description><see cref="LayerKind.Paper"/> — 접힘선 앞의 납작한 종이. Mask가 이 모양이라 안쪽 글씨는 접힘선 뒤로 넘어가면 잘려 나간다(말려 들어간 것처럼 보인다).</description></item>
    /// <item><description><see cref="LayerKind.CurlShadow"/> — 말린 부분이 종이 위에 드리우는 반투명 그림자(Mask 안쪽이라 종이 밖으로 안 나간다).</description></item>
    /// <item><description><see cref="LayerKind.Flap"/> — 말려 올라간 부분의 뒷면(한 톤 진한 노랑, 원통 음영).</description></item>
    /// </list>
    /// 격자 하나를 마는 게 아니라 말림 축을 따라 자른 조각 띠를 원통 곡선에 얹는다 — 종이 윤곽(꼭짓점)에 정확히 맞고 정점 수가 적다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PostitGraphic : MaskableGraphic
    {
        public enum LayerKind
        {
            DropShadow,
            Paper,
            CurlShadow,
            Flap,
        }

        private static readonly Color ShadowBlack = new Color(0.05f, 0.04f, 0.02f, 1f);

        private PostitCurl _curl;
        private LayerKind _kind;

        public void Init(PostitCurl curl, LayerKind kind)
        {
            _curl = curl;
            _kind = kind;
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void Dirty() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_curl == null) return;

            var rect = GetPixelAdjustedRect();
            if (rect.width < 1f || rect.height < 1f) return;

            var layout = _curl.Compute(rect);
            switch (_kind)
            {
                case LayerKind.DropShadow: BuildDropShadow(vh, layout); break;
                case LayerKind.Paper: BuildPaper(vh, layout); break;
                case LayerKind.CurlShadow: BuildCurlShadow(vh, layout); break;
                case LayerKind.Flap: BuildFlap(vh, layout); break;
            }
        }

        // ------------------------------------------------------------------
        // 겹별 메시
        // ------------------------------------------------------------------

        /// <summary>붙어 있으면 짧고 진하게, 떠 있으면 멀고 옅게. 두 겹을 겹쳐 가장자리를 부드럽게 만든다.</summary>
        private void BuildDropShadow(VertexHelper vh, PostitCurl.Layout layout)
        {
            // 말린 원통의 바깥 끝까지가 종이의 실루엣이다.
            var silhouette = layout.FlatPolygon(layout.R);
            if (silhouette.Count < 3) return;

            var lift = _curl.ShadowLift;
            var near = new Vector2(4f, -5f) * Mathf.Lerp(1f, 4.2f, lift);
            var far = new Vector2(8f, -10f) * Mathf.Lerp(1f, 4.2f, lift);
            var strength = Mathf.Lerp(1f, 0.55f, lift);

            AddPolygon(vh, silhouette, far, 1.03f, WithAlpha(ShadowBlack, 0.14f * strength));
            AddPolygon(vh, silhouette, near, 1.005f, WithAlpha(ShadowBlack, 0.24f * strength));
        }

        private void BuildPaper(VertexHelper vh, PostitCurl.Layout layout)
        {
            var polygon = layout.FlatPolygon();
            if (polygon.Count < 3) return;

            var rect = layout.Rect;
            var start = vh.currentVertCount;
            foreach (var point in polygon)
            {
                // 좌상단에서 우하단으로 살짝 어두워지는 종이 결.
                var gradient = Mathf.Clamp01(((point.x - rect.xMin) / rect.width + (rect.yMax - point.y) / rect.height) * 0.5f);
                vh.AddVert(point, Color.Lerp(PostitStyle.Paper, PostitStyle.PaperShade, gradient) * color, Vector2.zero);
            }

            AddFan(vh, start, polygon.Count);
        }

        /// <summary>말린 부분이 종이 위에 드리우는 그림자 — 말린 조각을 살짝 키워 옆으로 밀어 두 겹으로 깐다. 접힘선에 가까울수록 짙다.</summary>
        private void BuildCurlShadow(VertexHelper vh, PostitCurl.Layout layout)
        {
            var slices = FlapStrips(layout);
            if (slices.Count < 2) return;

            var pivot = layout.O + layout.D * layout.T;
            AddStrips(vh, slices, pivot, 1.28f, new Vector2(2f, -3f), WithAlpha(ShadowBlack, 0.10f));
            AddStrips(vh, slices, pivot, 1.10f, new Vector2(1.5f, -2.5f), WithAlpha(ShadowBlack, 0.20f));
        }

        private void BuildFlap(VertexHelper vh, PostitCurl.Layout layout)
        {
            var slices = FlapStrips(layout);
            if (slices.Count < 2) return;

            for (var i = 0; i < slices.Count - 1; i++)
            {
                var a = slices[i];
                var b = slices[i + 1];
                var start = vh.currentVertCount;
                vh.AddVert(a.Low, Shaded(a.Shade), Vector2.zero);
                vh.AddVert(a.High, Shaded(a.Shade), Vector2.zero);
                vh.AddVert(b.Low, Shaded(b.Shade), Vector2.zero);
                vh.AddVert(b.High, Shaded(b.Shade), Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 1, start + 3, start + 2);
            }
        }

        private Color Shaded(float shade)
        {
            var back = PostitStyle.Back;
            return new Color(Mathf.Clamp01(back.r * shade), Mathf.Clamp01(back.g * shade), Mathf.Clamp01(back.b * shade), back.a) * color;
        }

        // ------------------------------------------------------------------
        // 메시 조각
        // ------------------------------------------------------------------

        private struct Strip
        {
            public Vector2 Low;
            public Vector2 High;
            public float Shade;
        }

        /// <summary>뒷면이 보이는 말린 부분을 조각 띠(좌우 두 점 + 음영)로 만든다.</summary>
        private static List<Strip> FlapStrips(PostitCurl.Layout layout)
        {
            var strips = new List<Strip>(28);
            foreach (var u in layout.FlapSlices())
            {
                if (!layout.TryWidthAt(layout.T - u, out var wLow, out var wHigh)) continue;

                strips.Add(new Strip
                {
                    Low = layout.Roll(u, wLow, out var shade),
                    High = layout.Roll(u, wHigh, out _),
                    Shade = shade,
                });
            }

            return strips;
        }

        private void AddStrips(VertexHelper vh, List<Strip> strips, Vector2 pivot, float scale, Vector2 offset, Color tint)
        {
            for (var i = 0; i < strips.Count - 1; i++)
            {
                var start = vh.currentVertCount;
                foreach (var point in new[] { strips[i].Low, strips[i].High, strips[i + 1].Low, strips[i + 1].High })
                    vh.AddVert(pivot + (point - pivot) * scale + offset, tint * color, Vector2.zero);

                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 1, start + 3, start + 2);
            }
        }

        /// <summary>볼록 다각형을 한 색으로 채운다. 중심 기준으로 <paramref name="scale"/>배 키우고 <paramref name="offset"/>만큼 민다.</summary>
        private void AddPolygon(VertexHelper vh, List<Vector2> polygon, Vector2 offset, float scale, Color tint)
        {
            var center = Vector2.zero;
            foreach (var point in polygon) center += point;
            center /= polygon.Count;

            var start = vh.currentVertCount;
            foreach (var point in polygon)
                vh.AddVert(center + (point - center) * scale + offset, tint * color, Vector2.zero);

            AddFan(vh, start, polygon.Count);
        }

        private static void AddFan(VertexHelper vh, int start, int count)
        {
            for (var i = 1; i < count - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }

        private static Color WithAlpha(Color source, float alpha) => new Color(source.r, source.g, source.b, alpha);
    }
}
