using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 심전도 파형. 한 박(P-QRS-T)의 모양은 고정이고 화면에는 항상 세 박이 보이며, 파형이 왼쪽으로 흐르는 속도가 BPM에 비례한다 —
    /// BPM이 높으면 빠르게, 낮으면 느리게 흐른다(1분에 BPM박이 지나간다). 표시만 한다 — 판정 없음.
    /// 목표 BPM으로의 속도 변화는 부드럽게 따라가고, 위상은 계속 이어져서 속도가 바뀌어도 파형이 튀지 않는다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EcgWaveGraphic : MaskableGraphic
    {
        private const float VisibleBeats = 3f;

        /// <summary>기준선 높이(그래픽 높이 대비, 아래에서). 위로는 R파가, 아래로는 S파가 나갈 자리를 둔다.</summary>
        private const float BaselineFraction = 0.40f;

        /// <summary>표시 BPM이 목표 BPM을 따라가는 속도(BPM/초).</summary>
        private const float BpmFollowRate = 90f;

        private const float GlowScale = 3.4f;
        private const float GlowAlpha = 0.20f;

        /// <summary>한 박의 꼭짓점(t: 0~1 박 안 위치, y: 기준선에서의 높이 — 그래픽 높이 대비).</summary>
        private static readonly Vector2[] Beat =
        {
            new(0f, 0f), new(0.08f, 0f), new(0.13f, 0.06f), new(0.19f, 0f),
            new(0.27f, 0f), new(0.305f, -0.10f), new(0.34f, 0.52f), new(0.385f, -0.20f), new(0.425f, 0f),
            new(0.55f, 0f), new(0.62f, 0.10f), new(0.71f, 0f), new(1f, 0f),
        };

        [SerializeField] private float _thickness = 3f;

        private readonly List<Vector2> _points = new();
        private float _targetBpm = 80f;
        private float _shownBpm = 80f;
        private float _phase;

        public float ShownBpm => _shownBpm;

        /// <summary>목표 BPM과 선 색을 정한다. snap이면 속도도 바로 그 값으로(세션 시작·재시작).</summary>
        public void SetPulse(int bpm, Color lineColor, bool snap)
        {
            _targetBpm = Mathf.Max(0, bpm);
            if (snap) _shownBpm = _targetBpm;
            color = lineColor;
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _shownBpm = Mathf.MoveTowards(_shownBpm, _targetBpm, BpmFollowRate * dt);
            _phase += dt * _shownBpm / 60f;
            _phase -= Mathf.Floor(_phase);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 1f || rect.height <= 1f) return;

            BuildPolyline(rect.width, rect.height);

            var glow = color;
            glow.a *= GlowAlpha;
            EmitStroke(vh, rect, glow, _thickness * GlowScale);
            EmitStroke(vh, rect, color, _thickness);
        }

        /// <summary>화면 안 좌표(왼쪽 아래 원점)로 세 박 + 양옆 한 박씩의 꼭짓점을 만든다. 위상만큼 왼쪽으로 밀린다.</summary>
        private void BuildPolyline(float width, float height)
        {
            _points.Clear();

            var beatWidth = width / VisibleBeats;
            var baseline = height * BaselineFraction;
            var lastBeat = Mathf.CeilToInt(VisibleBeats) + 1;

            for (var k = -1; k <= lastBeat; k++)
            {
                // 박 사이 경계 꼭짓점(t=1)은 다음 박의 t=0과 같으므로 마지막 박만 끝까지 쓴다.
                var count = k == lastBeat ? Beat.Length : Beat.Length - 1;
                for (var i = 0; i < count; i++)
                {
                    var x = (k + Beat[i].x - _phase) * beatWidth;
                    _points.Add(new Vector2(x, baseline + Beat[i].y * height));
                }
            }
        }

        private void EmitStroke(VertexHelper vh, Rect rect, Color32 tint, float thickness)
        {
            var half = thickness * 0.5f;
            for (var i = 0; i + 1 < _points.Count; i++)
            {
                var a = _points[i];
                var b = _points[i + 1];
                if (!ClipToWidth(ref a, ref b, rect.width)) continue;

                var direction = (b - a).normalized;
                var normal = new Vector2(-direction.y, direction.x) * half;
                // 양 끝을 반 두께만큼 밀어(사각 캡) 꺾이는 곳의 틈을 메운다.
                var start = a - direction * half;
                var end = b + direction * half;

                var index = vh.currentVertCount;
                vh.AddVert(ToLocal(rect, start - normal), tint, Vector2.zero);
                vh.AddVert(ToLocal(rect, start + normal), tint, Vector2.zero);
                vh.AddVert(ToLocal(rect, end + normal), tint, Vector2.zero);
                vh.AddVert(ToLocal(rect, end - normal), tint, Vector2.zero);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }

        private static Vector3 ToLocal(Rect rect, Vector2 point) => new Vector3(rect.xMin + point.x, rect.yMin + point.y, 0f);

        /// <summary>선분을 0~width 구간으로 자른다. 구간 밖이면 false.</summary>
        private static bool ClipToWidth(ref Vector2 a, ref Vector2 b, float width)
        {
            if ((a.x < 0f && b.x < 0f) || (a.x > width && b.x > width)) return false;

            var dx = b.x - a.x;
            if (Mathf.Abs(dx) < 1e-4f) return a.x >= 0f && a.x <= width;

            var t0 = Mathf.Clamp01((0f - a.x) / dx);
            var t1 = Mathf.Clamp01((width - a.x) / dx);
            if (t0 > t1) (t0, t1) = (t1, t0);

            var start = Vector2.Lerp(a, b, t0);
            var end = Vector2.Lerp(a, b, t1);
            a = start;
            b = end;
            return (b - a).sqrMagnitude > 1e-6f;
        }
    }
}
