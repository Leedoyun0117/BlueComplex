using System.Collections.Generic;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 기억 풍선의 손으로 그린 흰 선. 균일한 타원이 아니라 굵기가 오르내리고 양 끝이 가늘어지는 열린 곡선이고, 다시 그릴 때마다 모양이 조금씩 다르다
    /// (<see cref="Regenerate"/>가 목업의 선을 뼈대로 제어점을 흔든다). 스프라이트가 아니라 메시라서 그리는 중간 상태를 표현할 수 있다:
    /// <see cref="Draw"/>는 펜이 한 바퀴 돌며 선을 완성하고, <see cref="Erase"/>는 지우개가 같은 방향으로 선을 지운다.
    /// 배경 아트가 그대로 비쳐야 해서 채우지 않는다. 아트가 생기면 이 그래픽을 스프라이트로 바꾸면 된다.
    /// 살짝 어긋난 얇은 두 번째 선이 뒤따라 덧그려져 손으로 두 번 그은 느낌을 준다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HandDrawnStrokeGraphic : MaskableGraphic
    {
        /// <summary>목업 스크린샷에서 뽑은 획의 중심선(0~1 정규화, 원점 좌하단). 왼쪽 위(유키 쪽)에서 시계 방향으로 한 바퀴 돌아 왼쪽 아래에서 끝난다 — 왼쪽 아래가 열려 있다.</summary>
        private static readonly Vector2[] Template =
        {
            new(0.010f, 0.545f), new(0.003f, 0.605f),
            new(0.005f, 0.657f), new(0.011f, 0.727f), new(0.039f, 0.799f), new(0.083f, 0.863f),
            new(0.139f, 0.917f), new(0.202f, 0.958f), new(0.271f, 0.988f), new(0.344f, 0.996f),
            new(0.415f, 0.995f), new(0.483f, 0.995f), new(0.548f, 0.986f), new(0.612f, 0.968f),
            new(0.673f, 0.945f), new(0.728f, 0.911f), new(0.779f, 0.870f), new(0.823f, 0.823f),
            new(0.862f, 0.773f), new(0.899f, 0.720f), new(0.930f, 0.662f), new(0.957f, 0.601f),
            new(0.978f, 0.534f), new(0.989f, 0.463f), new(0.985f, 0.393f), new(0.972f, 0.321f),
            new(0.950f, 0.251f), new(0.920f, 0.182f), new(0.885f, 0.113f), new(0.825f, 0.069f),
            new(0.758f, 0.038f), new(0.689f, 0.017f), new(0.619f, 0.007f), new(0.550f, 0.004f),
            new(0.485f, 0.004f), new(0.417f, 0.014f), new(0.356f, 0.041f), new(0.316f, 0.071f),
            new(0.285f, 0.100f),
        };

        /// <summary>그래픽 폭 대비 선 굵기(가장 굵은 곳의 지름). 폭 442px 기준 대략 2.6~6.2px.</summary>
        private const float MinWidthFraction = 0.0059f;
        private const float MaxWidthFraction = 0.0141f;

        /// <summary>선이 사각형 가장자리에 붙어 잘리지 않게 안쪽으로 모으는 정도.</summary>
        private const float Inset = 0.012f;

        private const float FeatherPixels = 1f;
        private const float StepPerSegment = 8f;
        private const float SecondPassLag = 0.07f;

        private sealed class Pass
        {
            public readonly List<Vector2> Points = new();
            public readonly List<float> Along = new(); // 누적 길이(0~1)
            public readonly List<float> Pressure = new(); // 획 압력(0~1)
        }

        private readonly Pass _main = new();
        private readonly Pass _sketch = new();
        private readonly List<Vector2> _samplePos = new();
        private readonly List<float> _sampleAlong = new();
        private readonly List<float> _samplePressure = new();

        private float _from;
        private float _to;
        private Tween _tween;

        /// <summary>선이 조금이라도 보이는 동안 true.</summary>
        public bool IsVisible => _to - _from > 0.001f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            if (_main.Points.Count == 0) Regenerate(0);
        }

        protected override void OnDisable()
        {
            _tween?.Kill();
            base.OnDisable();
        }

        /// <summary>새 모양을 만든다. 같은 시드는 같은 모양이다(0이면 매번 다른 무작위).</summary>
        public void Regenerate(int seed)
        {
            var random = seed == 0 ? new System.Random() : new System.Random(seed);
            BuildPass(_main, random, 0.012f);
            BuildPass(_sketch, random, 0.022f);
            SetVerticesDirty();
        }

        /// <summary>펜이 선을 처음부터 끝까지 그린다(한 바퀴).</summary>
        public void Draw(float seconds)
        {
            _tween?.Kill();
            _from = 0f;

            if (seconds <= 0f || !isActiveAndEnabled)
            {
                _to = 1f;
                SetVerticesDirty();
                return;
            }

            _to = 0f;
            _tween = DOTween.To(() => _to, v =>
                {
                    _to = v;
                    SetVerticesDirty();
                }, 1f, seconds)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
        }

        /// <summary>지우개가 그린 방향 그대로 선을 지운다. 다 지워지면 선이 없는 상태로 돌아간다.</summary>
        public void Erase(float seconds)
        {
            _tween?.Kill();

            if (!IsVisible) return;

            if (seconds <= 0f || !isActiveAndEnabled)
            {
                Clear();
                return;
            }

            // 그리는 도중에 지우면 그려진 데까지만 지운다.
            var end = _to;
            _tween = DOTween.To(() => _from, v =>
                {
                    _from = v;
                    SetVerticesDirty();
                }, end, seconds)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this)
                .OnComplete(Clear);
        }

        public void Clear()
        {
            _tween?.Kill();
            _from = 0f;
            _to = 0f;
            SetVerticesDirty();
        }

        private static void BuildPass(Pass pass, System.Random random, float jitter)
        {
            float Range(float amplitude) => (float)(random.NextDouble() * 2.0 - 1.0) * amplitude;

            // 전체를 살짝 돌리고 늘려서 매번 다른 크기·기울기로, 제어점마다 따로 흔들어 삐뚤빼뚤하게.
            var angle = Range(2.5f) * Mathf.Deg2Rad;
            var scale = new Vector2(1f + Range(0.03f), 1f + Range(0.03f));
            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);
            var wavePhase = (float)random.NextDouble() * Mathf.PI * 2f;

            var control = new Vector2[Template.Length];
            for (var i = 0; i < Template.Length; i++)
            {
                var p = Template[i] - new Vector2(0.5f, 0.5f);
                p = new Vector2(p.x * scale.x, p.y * scale.y);
                p = new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos) + new Vector2(0.5f, 0.5f);

                // 부드럽게 출렁이는 성분 + 점마다 무작위 성분.
                var wave = 0.006f * Mathf.Sin(i * 0.55f + wavePhase);
                p += new Vector2(Range(jitter) + wave, Range(jitter) - wave);
                control[i] = new Vector2(Mathf.Clamp(p.x, 0.005f, 0.995f), Mathf.Clamp(p.y, 0.005f, 0.995f));
            }

            SampleCurve(pass, control);

            // 획 압력: 양 끝은 가늘고 가운데가 굵다. 사인 두 개를 겹쳐 굵기가 사람 손처럼 살짝 출렁이게 한다.
            var phaseA = (float)random.NextDouble() * 6f;
            var phaseB = (float)random.NextDouble() * 6f;
            pass.Pressure.Clear();
            foreach (var t in pass.Along)
            {
                var pressure = Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.04f)));
                var wobble = 0.16f * Mathf.Sin(t * 41f + phaseA) + 0.10f * Mathf.Sin(t * 17f + phaseB);
                pass.Pressure.Add(Mathf.Clamp01(Mathf.Pow(pressure, 0.6f) + wobble * 0.5f));
            }
        }

        /// <summary>Catmull-Rom으로 제어점을 이어 촘촘한 점열(정규화 좌표)과 누적 길이를 만든다.</summary>
        private static void SampleCurve(Pass pass, Vector2[] control)
        {
            pass.Points.Clear();
            pass.Along.Clear();

            var lengths = new List<float>();
            var accumulated = 0f;
            for (var i = 0; i < control.Length - 1; i++)
            {
                var p0 = control[Mathf.Max(i - 1, 0)];
                var p1 = control[i];
                var p2 = control[i + 1];
                var p3 = control[Mathf.Min(i + 2, control.Length - 1)];

                for (var s = 0; s < StepPerSegment; s++)
                {
                    var t = s / StepPerSegment;
                    var t2 = t * t;
                    var t3 = t2 * t;
                    var point = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                                        (-p0 + 3f * p1 - 3f * p2 + p3) * t3);

                    if (pass.Points.Count > 0) accumulated += Vector2.Distance(pass.Points[pass.Points.Count - 1], point);
                    pass.Points.Add(point);
                    lengths.Add(accumulated);
                }
            }

            var last = control[control.Length - 1];
            accumulated += Vector2.Distance(pass.Points[pass.Points.Count - 1], last);
            pass.Points.Add(last);
            lengths.Add(accumulated);

            foreach (var length in lengths) pass.Along.Add(accumulated > 0f ? length / accumulated : 0f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!IsVisible || _main.Points.Count < 2) return;

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 1f || rect.height <= 1f) return;

            var from = Mathf.Min(_from, _to);

            // 두 번째 선은 조금 늦게 시작해 조금 늦게 끝난다(그릴 때는 뒤따르고, 지울 때는 먼저 지워진다).
            var sketchTo = Mathf.Clamp01((_to - SecondPassLag) / (1f - SecondPassLag));
            var sketchFrom = Mathf.Clamp01(from * (1f + SecondPassLag));

            EmitPass(vh, rect, _sketch, sketchFrom, sketchTo, 0.5f, 0.45f);
            EmitPass(vh, rect, _main, from, _to, 1f, 1f);
        }

        /// <summary>from~to(선 길이 비율) 구간을 굵기가 변하는 띠로 만든다. 가장자리는 1픽셀 부드럽게 옅어진다(안티앨리어싱).</summary>
        private void EmitPass(VertexHelper vh, Rect rect, Pass pass, float from, float to, float widthScale, float alpha)
        {
            if (to - from < 0.0005f) return;

            CollectSamples(pass, from, to);
            if (_samplePos.Count < 2) return;

            var maxWidth = rect.width * MaxWidthFraction * widthScale;
            var minWidth = rect.width * MinWidthFraction * widthScale;
            var eraseSoft = Mathf.Max(0.02f, (to - from) * 0.25f);
            var erasing = from > 0.0005f;

            var firstRow = vh.currentVertCount;
            for (var i = 0; i < _samplePos.Count; i++)
            {
                var prev = _samplePos[Mathf.Max(i - 1, 0)];
                var next = _samplePos[Mathf.Min(i + 1, _samplePos.Count - 1)];
                var tangent = ToRectSize(rect, next) - ToRectSize(rect, prev);
                tangent = tangent.sqrMagnitude > 1e-8f ? tangent.normalized : Vector2.right;
                var normal = new Vector2(-tangent.y, tangent.x);

                var u = _sampleAlong[i];

                // 펜 끝: 그리는 중인 머리 부분은 살짝 둥글게 가늘어진다(다 그린 뒤에는 압력 곡선이 양 끝을 가늘게 한다).
                var head = Mathf.SmoothStep(0.4f, 1f, Mathf.Clamp01((to - u) / 0.03f));
                // 지우개: 지워지는 경계에서 옅어지며 살짝 가늘어진다.
                var tail = erasing ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - from) / eraseSoft)) : 1f;

                var diameter = Mathf.Lerp(minWidth, maxWidth, _samplePressure[i]) * head * Mathf.Lerp(0.6f, 1f, tail);
                var half = Mathf.Max(0.35f, diameter * 0.5f);

                var center = ToLocal(rect, _samplePos[i]);
                var core = new Color(color.r, color.g, color.b, color.a * alpha * tail);
                var edge = new Color(core.r, core.g, core.b, 0f);

                vh.AddVert(center + (Vector3)(normal * (half + FeatherPixels)), edge, Vector2.zero);
                vh.AddVert(center + (Vector3)(normal * half), core, Vector2.zero);
                vh.AddVert(center - (Vector3)(normal * half), core, Vector2.zero);
                vh.AddVert(center - (Vector3)(normal * (half + FeatherPixels)), edge, Vector2.zero);
            }

            for (var i = 0; i + 1 < _samplePos.Count; i++)
            {
                var a = firstRow + i * 4;
                var b = a + 4;
                for (var k = 0; k < 3; k++)
                {
                    vh.AddTriangle(a + k, a + k + 1, b + k);
                    vh.AddTriangle(a + k + 1, b + k + 1, b + k);
                }
            }
        }

        /// <summary>선 길이 비율 from~to에 걸친 점열을 모은다(경계는 보간).</summary>
        private void CollectSamples(Pass pass, float from, float to)
        {
            _samplePos.Clear();
            _sampleAlong.Clear();
            _samplePressure.Clear();

            for (var i = 0; i + 1 < pass.Points.Count; i++)
            {
                var u0 = pass.Along[i];
                var u1 = pass.Along[i + 1];
                if (u1 < from) continue;
                if (u0 > to) break;

                if (_samplePos.Count == 0) AddSample(pass, i, Mathf.InverseLerp(u0, u1, Mathf.Max(from, u0)), Mathf.Max(from, u0));
                AddSample(pass, i, Mathf.InverseLerp(u0, u1, Mathf.Min(to, u1)), Mathf.Min(to, u1));
            }
        }

        private void AddSample(Pass pass, int segment, float t, float along)
        {
            _samplePos.Add(Vector2.Lerp(pass.Points[segment], pass.Points[segment + 1], t));
            _sampleAlong.Add(along);
            _samplePressure.Add(Mathf.Lerp(pass.Pressure[segment], pass.Pressure[segment + 1], t));
        }

        private static Vector2 ToRectSize(Rect rect, Vector2 normalized) =>
            new Vector2(normalized.x * rect.width, normalized.y * rect.height);

        private static Vector3 ToLocal(Rect rect, Vector2 normalized) =>
            new Vector3(rect.xMin + (Inset + normalized.x * (1f - 2f * Inset)) * rect.width,
                rect.yMin + (Inset + normalized.y * (1f - 2f * Inset)) * rect.height, 0f);
    }
}
