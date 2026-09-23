using System.Collections.Generic;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 포스트잇이 우하단 모서리부터 말려 올라가는 정도와 그 기하. 화면 위에서 내려다본 2D 투영이다.
    ///
    /// 종이를 "말림 축"(우하단 모서리 → 압정 쪽 대각선) 방향으로 잰 거리 s로 본다. 접힘선은 s = T에 있고, 접힘선 앞(s ≥ T)은 납작하게 붙어 있다.
    /// 접힘선 뒤(s &lt; T, 모서리 쪽)는 반지름 R의 원통을 타고 위로 말려 올라가 종이 위로 되돌아온다 —
    /// 접힘선에서 호 길이 u = T − s 만큼 떨어진 점은 위에서 볼 때 접힘선에서 R·sin(u/R)만큼 모서리 쪽으로 튀어나갔다가(호의 위쪽 반)
    /// 다시 접힘선을 넘어 종이 위에 눕는다. 위에서 보이는 건 뒷면(θ ≥ 90°)뿐이고, 그 아래 앞면은 가려진다.
    /// R이 0이면 그냥 접어 넘긴 것(접힘선 기준 반사)이 된다.
    ///
    /// 진행도 <see cref="Amount"/> 0 = 평상시(모서리가 살짝 말린 상태), 1 = 통째로 말려 올라간 상태.
    /// </summary>
    public sealed class PostitCurl
    {
        /// <summary>평상시 말림: 종이 대각 길이(S) 대비 접힘선 위치. 모서리 삼각형이 폭의 20% 안팎.</summary>
        public const float RestFold = 0.075f;

        /// <summary>말린 원통의 최대 반지름(S 대비).</summary>
        public const float MaxRadius = 0.055f;

        /// <summary>말림 축의 각도(도, 0 = 오른쪽, 반시계). 135°가 정확한 대각선(좌상단), 압정 쪽(위 가운데)은 약 117° — 그 사이.</summary>
        public float AngleDegrees = 126f;

        private float _amount;
        private float _shadowLift;

        /// <summary>0 = 평상시, 1 = 통째로 말림.</summary>
        public float Amount
        {
            get => _amount;
            set
            {
                if (Mathf.Approximately(_amount, value)) return;
                _amount = value;
                Changed?.Invoke();
            }
        }

        /// <summary>0 = 종이가 책상에 붙어 있다(그림자가 짧고 진하다), 1 = 떠 있다(그림자가 멀고 옅다).</summary>
        public float ShadowLift
        {
            get => _shadowLift;
            set
            {
                if (Mathf.Approximately(_shadowLift, value)) return;
                _shadowLift = value;
                Changed?.Invoke();
            }
        }

        public event System.Action Changed;

        public void Set(float amount, float shadowLift)
        {
            _amount = amount;
            _shadowLift = shadowLift;
            Changed?.Invoke();
        }

        /// <summary>사각형 종이의 말림 기하를 계산한다.</summary>
        public Layout Compute(Rect rect) => new Layout(rect, AngleDegrees, _amount);

        /// <summary>한 프레임의 기하. 종이 국소 좌표(사각형 rect 기준)의 점 p는 O + s·D + w·N이다(s: 말림 축 방향 거리, w: 그에 수직).</summary>
        public readonly struct Layout
        {
            private static readonly int[] EdgeNext = { 1, 2, 3, 0 };

            public readonly Rect Rect;
            public readonly Vector2 O;
            public readonly Vector2 D;
            public readonly Vector2 N;

            /// <summary>말림 축 방향 종이 길이(모서리 → 가장 먼 꼭짓점).</summary>
            public readonly float S;

            /// <summary>접힘선 위치(s = T).</summary>
            public readonly float T;

            /// <summary>말린 원통의 반지름.</summary>
            public readonly float R;

            /// <summary>사각형 꼭짓점 — 좌하, 우하(=O), 우상, 좌상 순서.</summary>
            public readonly Vector2[] Corners;
            private readonly float[] _s;
            private readonly float[] _w;

            public Layout(Rect rect, float angleDegrees, float amount)
            {
                Rect = rect;
                O = new Vector2(rect.xMax, rect.yMin);
                var radians = angleDegrees * Mathf.Deg2Rad;
                D = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                N = new Vector2(-D.y, D.x);

                Corners = new[]
                {
                    new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
                    new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax),
                };
                _s = new float[4];
                _w = new float[4];
                var far = 0f;
                for (var i = 0; i < 4; i++)
                {
                    var offset = Corners[i] - O;
                    _s[i] = Vector2.Dot(offset, D);
                    _w[i] = Vector2.Dot(offset, N);
                    far = Mathf.Max(far, _s[i]);
                }

                S = far;
                var rMax = MaxRadius * S;
                // 끝까지 말려도 원통 한 바퀴 반쯤 더 감기게 — 종이가 남지 않고 두루마리처럼 말려 올라간다.
                var fullFold = S + Mathf.PI * rMax * 1.1f;
                T = Mathf.LerpUnclamped(RestFold * S, fullFold, amount);
                R = Mathf.Min(rMax, T / 6f);
            }

            /// <summary>종이가 그려질 수 있는 크기인가(레이아웃 전에는 0×0이다).</summary>
            public bool IsValid => S > 1f;

            /// <summary>접힘선 앞의 납작하게 붙어 있는 부분(s ≥ T − back). 사각형을 반평면으로 자른 볼록 다각형이고, 비었으면 길이 0.</summary>
            public List<Vector2> FlatPolygon(float back = 0f)
            {
                var result = new List<Vector2>(6);
                var threshold = T - back;
                for (var i = 0; i < 4; i++)
                {
                    var current = Corners[i];
                    var next = Corners[EdgeNext[i]];
                    var sc = _s[i] - threshold;
                    var sn = _s[EdgeNext[i]] - threshold;

                    if (sc >= 0f) result.Add(current);
                    if (sc >= 0f != sn >= 0f) result.Add(current + (next - current) * (sc / (sc - sn)));
                }

                return result;
            }

            /// <summary>말림 축 위치 s에서 종이가 가로지르는 w 구간. 종이 밖이면 false.</summary>
            public bool TryWidthAt(float s, out float wLow, out float wHigh)
            {
                wLow = float.MaxValue;
                wHigh = float.MinValue;

                for (var i = 0; i < 4; i++)
                {
                    var j = EdgeNext[i];
                    var s0 = _s[i];
                    var s1 = _s[j];
                    if ((s0 - s) * (s1 - s) > 0f) continue;

                    if (Mathf.Approximately(s0, s1))
                    {
                        wLow = Mathf.Min(wLow, Mathf.Min(_w[i], _w[j]));
                        wHigh = Mathf.Max(wHigh, Mathf.Max(_w[i], _w[j]));
                        continue;
                    }

                    var w = Mathf.Lerp(_w[i], _w[j], (s - s0) / (s1 - s0));
                    wLow = Mathf.Min(wLow, w);
                    wHigh = Mathf.Max(wHigh, w);
                }

                return wHigh >= wLow;
            }

            /// <summary>접힘선 뒤 종이 위 한 점(접힘선에서 호 길이 u, 수직 위치 w)이 위에서 볼 때 놓이는 자리와, 그 자리의 원통 음영(0.7~1).</summary>
            public Vector2 Roll(float u, float w, out float shade)
            {
                float along;
                if (R > 1e-4f && u <= Mathf.PI * R)
                {
                    var theta = u / R;
                    along = T - R * Mathf.Sin(theta);
                    // θ = 90°(원통 바깥 끝)는 빛을 비스듬히 받아 어둡고, 180°(꼭대기)는 위를 향해 밝다.
                    shade = Mathf.Lerp(0.74f, 1.03f, Mathf.SmoothStep(0f, 1f, (theta - Mathf.PI * 0.5f) / (Mathf.PI * 0.5f)));
                }
                else
                {
                    along = T + (u - Mathf.PI * R);
                    // 다 넘어와 종이 위에 누운 부분은 끝(모서리)으로 갈수록 살짝 어둡다.
                    shade = Mathf.Lerp(1.02f, 0.94f, Mathf.Clamp01((u - Mathf.PI * R) / Mathf.Max(T, 1f)));
                }

                return O + D * along + N * w;
            }

            /// <summary>말려 올라가 뒷면이 보이는 부분을 말림 축을 따라 자른 조각들(호 길이 u 순서). 조각마다 종이 위 폭 [wLow, wHigh]이 있다.</summary>
            public List<float> FlapSlices()
            {
                var result = new List<float>(28);

                var uStart = Mathf.Max(Mathf.PI * R * 0.5f, T - S);
                var uEnd = T;
                if (uEnd - uStart < 0.5f) return result;

                const int steps = 20;
                for (var i = 0; i <= steps; i++) result.Add(Mathf.Lerp(uStart, uEnd, i / (float)steps));

                // 종이 윤곽이 꺾이는 곳(꼭짓점)과 원통이 끝나는 곳에는 조각 경계를 꼭 둔다 — 안 그러면 모서리가 뭉개진다.
                AddBreak(result, Mathf.PI * R, uStart, uEnd);
                for (var i = 0; i < 4; i++) AddBreak(result, T - _s[i], uStart, uEnd);

                result.Sort();
                return result;
            }

            private static void AddBreak(List<float> slices, float u, float min, float max)
            {
                if (u > min + 0.01f && u < max - 0.01f) slices.Add(u);
            }
        }
    }
}
