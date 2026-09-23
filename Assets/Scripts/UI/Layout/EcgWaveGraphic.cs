using System;
using System.Collections.Generic;
using BlueComplex.Core.Stability;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 심전도 모니터의 화면: 흐르는 파형 + 목표 심박수 띠. 표시만 한다 — 판정 없음.
    ///
    /// 세로축이 심박수 눈금이다(<see cref="HeartbeatMonitorScale"/>). 파형 봉우리(R파)의 높이가 현재 BPM에 따라 정해지고,
    /// 목표 띠의 위아래 경계도 같은 함수로 놓인다 — 두 그림이 이 그래픽 한 장 안에서 같은 <see cref="Scale"/>을 부르므로 어긋날 수 없다.
    /// 흥분할수록 봉우리가 높아지고 침체할수록 낮아진다(저심박 쪽 곡선이 더 가팔라 침체될수록 작아지는 게 뚜렷이 보인다). P·T파도 봉우리에 비례해 커지고 작아진다.
    ///
    /// 파형은 왼쪽으로 흐른다. 화면을 지나가는 봉우리의 수는 BPM에 정비례(1분에 BPM박)하고, 빠른 심박일수록 봉우리 간격이 좁아진다 —
    /// 간격은 BPM의 <see cref="_spacingExponent"/>제곱에 반비례하게 줄어든다(지수 1이면 흐르는 속도가 BPM과 무관해진다). 그 결과 흐르는 속도는 BPM이 오르면 빨라진다.
    /// 표시 BPM(<see cref="ShownBpm"/>)은 트윈으로 새 값에 따라가고 위상은 끊기지 않고 이어져, 값이 바뀌어도 파형이 튀지 않는다.
    /// 박마다 크기 자체도 큼직하게 들쭉날쭉하다(<see cref="BeatAmplitudeScale"/>) — 같은 BPM, 같은 상태 구간 안에서도 매 박이 다르게 뛰어야
    /// 살아있어 보인다. 이 흔들림 폭은 특정 상태 이름(매우 침체 등)이 아니라 BPM이 생존 구간 중앙에서 얼마나 먼지로 정해져서, 구간을
    /// 넘나들 때 갑자기 규칙이 바뀐 것처럼 보이지 않고 침체·흥분이 심할수록 계속 더 커진다.
    /// 매우 침체·매우 흥분(<c>irregular</c>)에서는 그와 별개로 봉우리 <b>간격</b>이 흔들리고 기준선에도 미세한 떨림이 얹힌다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EcgWaveGraphic : MaskableGraphic
    {
        /// <summary>기준 BPM(스테이지 시작값)에서 화면에 보이는 박 수.</summary>
        private const float BeatsVisibleAtReference = 3f;

        /// <summary>불규칙할 때 박 하나가 앞뒤로 흔들리는 최대 폭(박 단위). 인접한 박이 겹치지 않는 한도(0.37) 안에서 잡는다.</summary>
        private const float JitterBeats = 0.16f;

        /// <summary>박마다 크기(진폭)가 얼마나 들쭉날쭉한지 — 안정 구간 한복판(생존 구간의 정중앙)에서도 걸리는 바닥값. 상태 구간이 바뀌어야만
        /// 흔들리면 그 사이(같은 구간 안)에서는 매번 똑같아 보여 기계적으로 느껴진다 — 그래서 특정 상태 분기가 아니라 항상 큼직하게 걸어 둔다.
        /// 진폭에 곱하는 비율이라 침체돼서 진폭 자체가 작아지면 이 흔들림의 실제 픽셀 크기도 함께 작아진다.</summary>
        private const float BeatSizeJitterBase = 0.32f;

        /// <summary>생존 구간 중앙에서 멀어질수록(침체·흥분이 심할수록) <see cref="BeatSizeJitterBase"/>에 더해지는 몫. 상태 이름(매우 침체 등)이
        /// 아니라 BPM 자체와 연속적으로 이어져서, 구간 경계를 넘는 순간 흔들림 폭이 뚝 뛰지 않고 슬며시 커진다.</summary>
        private const float BeatSizeJitterExtreme = 0.4f;

        /// <summary>불규칙할 때 기준선에 얹는 떨림의 크기(픽셀).</summary>
        private const float NoisePixels = 1.7f;

        private const float BaselineRunStep = 7f;
        private const float IrregularityRate = 1.6f; // 초당 변화량 = 약 0.6초에 전환
        private const float EdgeFadeFraction = 0.06f;

        private const float GlowScale = 2.6f;
        private const float GlowAlpha = 0.14f;

        private const float BandFillAlpha = 0.20f;
        private const float BandEdgeAlpha = 0.65f;
        private const float BandEdgeThickness = 1.6f;

        /// <summary>한 박의 꼭짓점. x = 박 안의 위치(0~1), y = 기준선 위 높이(R파를 1로 한 비율). P파 → QRS → T파만 두고, 박 사이는 기준선으로 이어진다.</summary>
        private static readonly Vector2[] Beat =
        {
            new(0.08f, 0f), new(0.13f, 0.10f), new(0.19f, 0f),
            new(0.27f, 0f), new(0.305f, -0.06f), new(0.34f, 1f), new(0.385f, -0.10f), new(0.425f, 0f),
            new(0.55f, 0f), new(0.62f, 0.20f), new(0.71f, 0f),
        };

        private const int RPeakIndex = 5;

        [SerializeField] private float _thickness = 3f;

        [Tooltip("봉우리 간격이 BPM에 따라 줄어드는 정도. 0이면 간격 고정(속도가 BPM에 정비례), 1이면 흐르는 속도가 BPM과 무관해진다.")]
        [SerializeField, Range(0f, 1f)] private float _spacingExponent = 0.35f;

        private readonly List<Vector2> _points = new();

        private float _targetBpm = Heartbeat.DefaultStartValue;
        private float _shownBpm = Heartbeat.DefaultStartValue;
        private double _phase;
        private float _irregularity;
        private float _irregularityTarget;

        private bool _bandVisible;
        private float _bandLo;
        private float _bandHi;
        private float _bandFade;
        private float _bandEmphasis;
        private float _bandFlash;
        private Color _bandColor = Color.yellow;

        private Tween _bpmTween;
        private Tween _colorTween;
        private Tween _bandMoveTween;
        private Tween _bandFadeTween;
        private Tween _emphasisTween;
        private Tween _flashTween;

        /// <summary>심박수 → 화면 높이. 세션이 시작될 때 코어의 구간표에서 다시 만들어 넣는다.</summary>
        public HeartbeatMonitorScale Scale { get; set; } = HeartbeatMonitorScale.Default;

        /// <summary>지금 그려지는(트윈 중이면 따라가는 중인) BPM.</summary>
        public float ShownBpm => _shownBpm;

        public bool BandVisible => _bandVisible;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnDisable()
        {
            DOTween.Kill(this);
            base.OnDisable();
        }

        /// <summary>목표 BPM과 선 색을 정한다. snap이면 바로 그 값으로(세션 시작·재시작), 아니면 seconds 동안 부드럽게 따라간다.</summary>
        /// <param name="irregular">매우 침체·매우 흥분·즉사 구간이면 true — 봉우리 간격이 흔들리고 기준선이 떨린다.</param>
        public void SetPulse(int bpm, Color lineColor, bool irregular, bool snap, float seconds)
        {
            _targetBpm = Mathf.Max(0, bpm);
            _irregularityTarget = irregular ? 1f : 0f;

            _bpmTween?.Kill();
            _colorTween?.Kill();

            if (snap || seconds <= 0f || !isActiveAndEnabled)
            {
                _shownBpm = _targetBpm;
                _irregularity = _irregularityTarget;
                color = lineColor;
                return;
            }

            _bpmTween = DOTween.To(() => _shownBpm, v => _shownBpm = v, _targetBpm, seconds)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
            _colorTween = DOTween.To(() => color, c => color = c, lineColor, seconds)
                .SetUpdate(true).SetTarget(this);
        }

        /// <summary>목표 띠를 lo~hi BPM(양 끝 포함)에 놓는다. 이미 보이는 띠면 새 자리로 부드럽게 옮기고, 처음 나타나는 띠면 그 자리에서 떠오른다.</summary>
        public void ShowBand(float lo, float hi, Color bandColor, bool animate, float moveSeconds)
        {
            _bandColor = bandColor;
            _bandMoveTween?.Kill();

            if (_bandVisible && animate && isActiveAndEnabled && moveSeconds > 0f)
            {
                var fromLo = _bandLo;
                var fromHi = _bandHi;
                var progress = 0f;
                _bandMoveTween = DOTween.To(() => progress, p =>
                    {
                        progress = p;
                        _bandLo = Mathf.Lerp(fromLo, lo, p);
                        _bandHi = Mathf.Lerp(fromHi, hi, p);
                    }, 1f, moveSeconds)
                    .SetEase(Ease.InOutCubic).SetUpdate(true).SetTarget(this);
            }
            else
            {
                _bandLo = lo;
                _bandHi = hi;
            }

            if (_bandVisible) return;

            _bandVisible = true;
            FadeBand(1f, animate ? 0.3f : 0f);
        }

        public void HideBand()
        {
            _bandVisible = false;
            _bandMoveTween?.Kill();
            SetBandEmphasis(false, 0f);
            FadeBand(0f, 0f);
        }

        public void SetBandColor(Color bandColor) => _bandColor = bandColor;

        /// <summary>키 턴 강조: 띠가 살짝 밝아지며 맥동한다. 끄면 원래 밝기로 돌아온다.</summary>
        public void SetBandEmphasis(bool on, float pulseSeconds)
        {
            _emphasisTween?.Kill();
            _emphasisTween = null;

            if (on && pulseSeconds > 0f && isActiveAndEnabled)
            {
                _bandEmphasis = 0f;
                _emphasisTween = DOTween.To(() => _bandEmphasis, v => _bandEmphasis = v, 1f, pulseSeconds * 0.5f)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetTarget(this);
                return;
            }

            if (_bandEmphasis <= 0.001f || !isActiveAndEnabled)
            {
                _bandEmphasis = 0f;
                return;
            }

            _emphasisTween = DOTween.To(() => _bandEmphasis, v => _bandEmphasis = v, 0f, 0.2f)
                .SetUpdate(true).SetTarget(this);
        }

        /// <summary>키 판정 결과 등 한 번 번쩍이는 강조.</summary>
        public void FlashBand(float seconds)
        {
            _flashTween?.Kill();
            if (!isActiveAndEnabled || seconds <= 0f) return;

            _bandFlash = 1f;
            _flashTween = DOTween.To(() => _bandFlash, v => _bandFlash = v, 0f, seconds)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);
        }

        private void FadeBand(float target, float seconds)
        {
            _bandFadeTween?.Kill();
            if (seconds <= 0f || !isActiveAndEnabled)
            {
                _bandFade = target;
                return;
            }

            _bandFadeTween = DOTween.To(() => _bandFade, v => _bandFade = v, target, seconds)
                .SetUpdate(true).SetTarget(this);
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _phase += dt * _shownBpm / 60.0;
            _irregularity = Mathf.MoveTowards(_irregularity, _irregularityTarget, IrregularityRate * dt);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 1f || rect.height <= 1f) return;

            EmitBand(vh, rect);

            BuildPolyline(rect.width, rect.height);

            var glow = color;
            glow.a *= GlowAlpha;
            EmitStroke(vh, rect, glow, _thickness * GlowScale);
            EmitStroke(vh, rect, color, _thickness);
        }

        /// <summary>목표 띠. 위아래 경계가 <see cref="Scale"/>의 목표 BPM 높이 그대로다 — 경계선은 띠 안쪽에 그려서 바깥 경계가 정확히 그 높이다.</summary>
        private void EmitBand(VertexHelper vh, Rect rect)
        {
            if (_bandFade <= 0.01f) return;

            var yLo = Scale.ToY(_bandLo) * rect.height;
            var yHi = Scale.ToY(_bandHi) * rect.height;
            if (yHi < yLo) (yLo, yHi) = (yHi, yLo);

            var fill = Color.Lerp(_bandColor, Color.white, _bandFlash * 0.6f);
            fill.a = _bandFade * Mathf.Clamp01(BandFillAlpha + 0.13f * _bandEmphasis + 0.45f * _bandFlash);
            EmitQuad(vh, rect, 0f, yLo, rect.width, yHi, fill);

            var edge = Color.Lerp(_bandColor, Color.white, 0.25f + _bandFlash * 0.5f);
            edge.a = _bandFade * Mathf.Clamp01(BandEdgeAlpha + 0.3f * _bandEmphasis);
            var thickness = Mathf.Min(BandEdgeThickness, (yHi - yLo) * 0.5f);
            EmitQuad(vh, rect, 0f, yLo, rect.width, yLo + thickness, edge);
            EmitQuad(vh, rect, 0f, yHi - thickness, rect.width, yHi, edge);
        }

        private static void EmitQuad(VertexHelper vh, Rect rect, float x0, float y0, float x1, float y1, Color32 tint)
        {
            var index = vh.currentVertCount;
            vh.AddVert(ToLocal(rect, new Vector2(x0, y0)), tint, Vector2.zero);
            vh.AddVert(ToLocal(rect, new Vector2(x0, y1)), tint, Vector2.zero);
            vh.AddVert(ToLocal(rect, new Vector2(x1, y1)), tint, Vector2.zero);
            vh.AddVert(ToLocal(rect, new Vector2(x1, y0)), tint, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        /// <summary>화면 안 좌표(왼쪽 아래 원점)로 꼭짓점을 만든다. 박마다 위상(<c>_phase</c>)만큼 왼쪽으로 밀리고,
        /// 박 하나의 크기는 <c>Scale.ToY(shownBpm)</c> 기준 진폭에 <see cref="BeatAmplitudeScale"/>(박마다 다른, 예측하기 어려운 배율)을 곱한 값이다 —
        /// 그래서 심박수가 바뀌면 봉우리가 커지고 작아지는 것뿐 아니라, 박마다 크기가 들쭉날쭉한 폭 자체도 함께 커지고 작아진다.</summary>
        private void BuildPolyline(float width, float height)
        {
            _points.Clear();

            var spacing = BeatSpacing(width, _shownBpm);
            var baseline = HeartbeatMonitorScale.BaselineY * height;
            var amplitude = Scale.AboveBaseline(_shownBpm) * height;
            var noise = NoisePixels * _irregularity;

            var first = (long)Math.Floor(_phase) - 1;
            var count = Mathf.CeilToInt(width / spacing) + 3;

            for (var b = 0; b < count; b++)
            {
                var n = first + b;
                var jitter = (Hash01(n) - 0.5f) * 2f * JitterBeats * _irregularity;
                var beatAmplitude = amplitude * BeatAmplitudeScale(n);

                for (var i = 0; i < Beat.Length; i++)
                {
                    var x = (float)((n + jitter + Beat[i].x - _phase) * spacing);

                    if (i == 0 && noise > 0.01f && _points.Count > 0)
                        AddBaselineRun(_points[_points.Count - 1].x, x, baseline, noise, spacing, height);

                    var y = baseline + Beat[i].y * beatAmplitude;
                    if (i != RPeakIndex) y += Noise((float)(n + Beat[i].x)) * noise;

                    _points.Add(new Vector2(x, Mathf.Clamp(y, 0.5f, height - 0.5f)));
                }
            }
        }

        /// <summary>박 번호 n의 크기 배율. 1 근처를 오르내리는 예측 불가능한 값(같은 박은 항상 같은 값)이라 매 박이 조금씩
        /// 더 크거나 작게 뛴다. 흔들림 폭은 <see cref="BeatSizeJitterBase"/>(항상)에 심박수가 생존 구간 중앙에서 먼 정도(0~1, 상태
        /// 분기가 아니라 BPM 자체의 연속값)를 곱한 <see cref="BeatSizeJitterExtreme"/>이 더해진다. 가로 지터(<see cref="Hash01(long)"/>를
        /// 그대로 씀)와 겹쳐 보이지 않게 다른 해시 오프셋을 쓴다.</summary>
        private float BeatAmplitudeScale(long n)
        {
            var centerFraction = Scale.TopBpm > 0 ? _shownBpm / Scale.TopBpm : 0f;
            var extremity = Mathf.Clamp01(Mathf.Abs(centerFraction - 0.5f) * 2f);
            var range = BeatSizeJitterBase + BeatSizeJitterExtreme * extremity;
            var r = Hash01(n * 7 + 3) - 0.5f;
            return Mathf.Max(0.15f, 1f + r * 2f * range);
        }

        /// <summary>박과 박 사이 기준선 구간에 중간 점을 촘촘히 넣어 떨림이 보이게 한다.</summary>
        private void AddBaselineRun(float fromX, float toX, float baseline, float noise, float spacing, float height)
        {
            for (var x = fromX + BaselineRunStep; x < toX - BaselineRunStep * 0.5f; x += BaselineRunStep)
            {
                var y = baseline + Noise((float)(x / spacing + _phase)) * noise;
                _points.Add(new Vector2(x, Mathf.Clamp(y, 0.5f, height - 0.5f)));
            }
        }

        /// <summary>박 하나의 가로 폭(픽셀). 빠른 심박일수록 좁고 느린 심박일수록 넓다.</summary>
        private float BeatSpacing(float width, float bpm)
        {
            var reference = width / BeatsVisibleAtReference;
            var ratio = Mathf.Pow(Heartbeat.DefaultStartValue / Mathf.Max(bpm, 1f), _spacingExponent);
            return reference * Mathf.Clamp(ratio, 0.45f, 2.2f);
        }

        /// <summary>박 번호 → 0~1의 고정된 의사난수. 같은 박은 언제나 같은 값이라 파형이 흐르는 동안 흔들림이 박에 붙어 다닌다.</summary>
        private static float Hash01(long n)
        {
            var s = Math.Sin(n * 12.9898 + 78.233) * 43758.5453;
            return (float)(s - Math.Floor(s));
        }

        /// <summary>박 좌표 u → -1~1의 매끈한 떨림. 흐르는 파형에 붙어 다닌다.</summary>
        private static float Noise(float u) => 0.6f * Mathf.Sin(u * 17.3f) + 0.4f * Mathf.Sin(u * 41.1f + 1.7f);

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

                var startTint = tint;
                var endTint = tint;
                startTint.a = (byte)(tint.a * EdgeFade(a.x, rect.width));
                endTint.a = (byte)(tint.a * EdgeFade(b.x, rect.width));

                var index = vh.currentVertCount;
                vh.AddVert(ToLocal(rect, start - normal), startTint, Vector2.zero);
                vh.AddVert(ToLocal(rect, start + normal), startTint, Vector2.zero);
                vh.AddVert(ToLocal(rect, end + normal), endTint, Vector2.zero);
                vh.AddVert(ToLocal(rect, end - normal), endTint, Vector2.zero);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }

        /// <summary>양 끝에서 선이 서서히 사라진다 — 화면 가장자리에서 뚝 잘리지 않게.</summary>
        private static float EdgeFade(float x, float width)
        {
            var distance = Mathf.Min(x, width - x) / (width * EdgeFadeFraction);
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance));
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
