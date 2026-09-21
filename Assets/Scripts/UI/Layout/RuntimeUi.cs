using System.Collections.Generic;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 프리팹 없이 런타임에 코드로 짓는 UI(쿼터 HUD)가 공유하는 생성 헬퍼.
    /// 만든 오브젝트는 부모의 레이어를 따라간다 — MainHud는 UI 레이어이고 UI 카메라가 그 레이어만
    /// 그리므로, 기본 레이어(0)로 만들면 화면에 안 나온다. Image는 기본적으로 레이캐스트를 받지 않는다
    /// (클릭을 받아야 하는 것만 명시적으로 켠다).
    /// </summary>
    internal static class RuntimeUi
    {
        public static readonly Color PanelColor = new Color32(28, 28, 32, 200);
        public static readonly Color SlotColor = new Color32(45, 45, 52, 200);

        private static Sprite _circle;

        /// <summary>안티앨리어싱된 흰 원. 인디케이터 점과 열쇠 머리에 쓴다(틴트로 색을 입힌다).</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;

                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave,
                };

                var pixels = new Color32[size * size];
                var radius = size * 0.5f;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x + 0.5f - radius;
                        var dy = y + 0.5f - radius;
                        var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, true);

                _circle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                _circle.hideFlags = HideFlags.HideAndDontSave;
                return _circle;
            }
        }

        private static Sprite _triangleDown;

        /// <summary>아래를 가리키는 흰 삼각형(대사 "다음" 표시). 글꼴에 ▼ 글리프가 없어도 그려지도록 스프라이트로 만든다.</summary>
        public static Sprite TriangleDown
        {
            get
            {
                if (_triangleDown != null) return _triangleDown;

                const int size = 64;
                const int samples = 4;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave,
                };

                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var covered = 0;
                        for (var sy = 0; sy < samples; sy++)
                        {
                            for (var sx = 0; sx < samples; sx++)
                            {
                                var u = (x + (sx + 0.5f) / samples) / size;
                                var v = (y + (sy + 0.5f) / samples) / size;
                                // 위쪽 변이 v=0.86, 꼭짓점이 (0.5, 0.14)인 이등변삼각형(좌하단 원점).
                                if (v < 0.14f || v > 0.86f) continue;
                                var halfWidth = (v - 0.14f) / (0.86f - 0.14f) * 0.5f;
                                if (Mathf.Abs(u - 0.5f) <= halfWidth) covered++;
                            }
                        }

                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * covered / (samples * samples)));
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, true);

                _triangleDown = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                _triangleDown.hideFlags = HideFlags.HideAndDontSave;
                return _triangleDown;
            }
        }

        private static Sprite _handDrawnLoop;

        /// <summary>
        /// 생각 공간(기억 풍선)의 손그림 선. 디자인 목업의 흰 선을 따라 그린 불규칙한 열린 곡선이다 — 균일한 타원이 아니라
        /// 획 굵기가 오르내리고 양 끝이 가늘어지며, 왼쪽 아래(유키가 있는 쪽)가 열려 있다.
        /// 안티앨리어싱된 흰 선이라 틴트로 밝기를 준다. 채우지 않는 건 배경 아트가 그대로 비쳐야 해서다. 아트가 생기면 이 스프라이트만 바꾸면 된다.
        /// </summary>
        public static Sprite HandDrawnLoop
        {
            get
            {
                if (_handDrawnLoop != null) return _handDrawnLoop;

                const int width = 512;
                const int height = 488;

                // 목업 스크린샷에서 뽑은 획의 중심선(0~1 정규화, 원점 좌하단). 처음과 끝은 초상화에 가려 있던 자리라 살짝 이어 붙였다.
                var control = new[]
                {
                    new Vector2(0.010f, 0.545f), new Vector2(0.003f, 0.605f),
                    new Vector2(0.005f, 0.657f), new Vector2(0.011f, 0.727f), new Vector2(0.039f, 0.799f), new Vector2(0.083f, 0.863f),
                    new Vector2(0.139f, 0.917f), new Vector2(0.202f, 0.958f), new Vector2(0.271f, 0.988f), new Vector2(0.344f, 0.996f),
                    new Vector2(0.415f, 0.995f), new Vector2(0.483f, 0.995f), new Vector2(0.548f, 0.986f), new Vector2(0.612f, 0.968f),
                    new Vector2(0.673f, 0.945f), new Vector2(0.728f, 0.911f), new Vector2(0.779f, 0.870f), new Vector2(0.823f, 0.823f),
                    new Vector2(0.862f, 0.773f), new Vector2(0.899f, 0.720f), new Vector2(0.930f, 0.662f), new Vector2(0.957f, 0.601f),
                    new Vector2(0.978f, 0.534f), new Vector2(0.989f, 0.463f), new Vector2(0.985f, 0.393f), new Vector2(0.972f, 0.321f),
                    new Vector2(0.950f, 0.251f), new Vector2(0.920f, 0.182f), new Vector2(0.885f, 0.113f), new Vector2(0.825f, 0.069f),
                    new Vector2(0.758f, 0.038f), new Vector2(0.689f, 0.017f), new Vector2(0.619f, 0.007f), new Vector2(0.550f, 0.004f),
                    new Vector2(0.485f, 0.004f), new Vector2(0.417f, 0.014f), new Vector2(0.356f, 0.041f), new Vector2(0.316f, 0.071f),
                    new Vector2(0.285f, 0.100f),
                };

                var path = SamplePath(control, width, height, out var lengths);
                var alpha = new float[width * height];
                var total = lengths[lengths.Count - 1];

                for (var i = 0; i < path.Count; i++)
                {
                    var t = total > 0f ? lengths[i] / total : 0f;

                    // 획 압력: 양 끝은 가늘고 가운데가 굵다. 사인 두 개를 겹쳐 사람 손처럼 굵기가 살짝 출렁이게 한다(무작위 없음 — 항상 같은 모양).
                    var pressure = Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.04f)));
                    var wobble = 0.16f * Mathf.Sin(t * 41f) + 0.10f * Mathf.Sin(t * 17f + 1.3f);
                    var radius = 1.5f + 2.1f * Mathf.Pow(pressure, 0.6f) + wobble;
                    Stamp(alpha, width, height, path[i], Mathf.Max(1.2f, radius));
                }

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave,
                };

                var pixels = new Color32[width * height];
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha[i]) * 255f));

                texture.SetPixels32(pixels);
                texture.Apply(false, true);

                _handDrawnLoop = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
                _handDrawnLoop.hideFlags = HideFlags.HideAndDontSave;
                return _handDrawnLoop;
            }
        }

        /// <summary>Catmull-Rom으로 제어점을 이어 텍스처 픽셀 좌표의 촘촘한 점열을 만든다(누적 길이 포함).</summary>
        private static List<Vector2> SamplePath(Vector2[] control, int width, int height, out List<float> lengths)
        {
            var points = new List<Vector2>();
            lengths = new List<float>();

            // 가장자리에 반 두께가 잘리지 않도록 살짝 안쪽으로 모은다.
            const float margin = 8f;
            Vector2 ToPixel(Vector2 p) => new Vector2(margin + p.x * (width - 2f * margin), margin + p.y * (height - 2f * margin));

            var accumulated = 0f;
            for (var i = 0; i < control.Length - 1; i++)
            {
                var p0 = ToPixel(control[Mathf.Max(i - 1, 0)]);
                var p1 = ToPixel(control[i]);
                var p2 = ToPixel(control[i + 1]);
                var p3 = ToPixel(control[Mathf.Min(i + 2, control.Length - 1)]);

                var steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) * 2f));
                for (var s = 0; s < steps; s++)
                {
                    var t = s / (float)steps;
                    var t2 = t * t;
                    var t3 = t2 * t;
                    var point = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                                        (-p0 + 3f * p1 - 3f * p2 + p3) * t3);

                    if (points.Count > 0) accumulated += Vector2.Distance(points[points.Count - 1], point);
                    points.Add(point);
                    lengths.Add(accumulated);
                }
            }

            return points;
        }

        /// <summary>반지름 radius(픽셀)의 안티앨리어싱된 원을 max로 합성한다 — 점을 촘촘히 찍으면 굵기가 변하는 매끈한 획이 된다.</summary>
        private static void Stamp(float[] alpha, int width, int height, Vector2 center, float radius)
        {
            var reach = Mathf.CeilToInt(radius + 1f);
            var cx = Mathf.RoundToInt(center.x);
            var cy = Mathf.RoundToInt(center.y);

            for (var y = Mathf.Max(0, cy - reach); y <= Mathf.Min(height - 1, cy + reach); y++)
            {
                for (var x = Mathf.Max(0, cx - reach); x <= Mathf.Min(width - 1, cx + reach); x++)
                {
                    var dx = x + 0.5f - center.x;
                    var dy = y + 0.5f - center.y;
                    var coverage = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    var index = y * width + x;
                    if (coverage > alpha[index]) alpha[index] = coverage;
                }
            }
        }

        public static TMP_FontAsset FindFont(Transform canvasRoot) =>
            canvasRoot.GetComponentInChildren<TMP_Text>(true)?.font;

        public static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static RectTransform CreateStretched(Transform parent, string name) =>
            CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Sprite sprite = null, bool raycastTarget = false)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, TMP_FontAsset font, float size,
            Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }

    /// <summary>턴 칸 줄의 색 구성. 어두운 오버레이용(기본)과 노란 포스트잇용이 있다.</summary>
    internal sealed class TurnTrackStyle
    {
        public Color Cell;
        public Color LastCell;
        public Color Dot;

        /// <summary>알파가 0보다 크면 현재 턴의 칸을 이 색으로 칠한다(점은 그 위에 놓인다). 0이면 점만 움직인다.</summary>
        public Color CurrentCell;

        /// <summary>칸 테두리(알파 0이면 없음).</summary>
        public Color CellOutline;

        /// <summary>어두운 배경 위: 회색 칸, 노란 마지막 칸, 하얀 점.</summary>
        public static readonly TurnTrackStyle Dark = new TurnTrackStyle
        {
            Cell = new Color32(140, 140, 150, 200),
            LastCell = new Color32(255, 210, 70, 255),
            Dot = Color.white,
            CurrentCell = Color.clear,
            CellOutline = Color.clear,
        };

        /// <summary>노란 포스트잇 위(목업): 연한 칸에 얇은 테두리, 노란 마지막 칸, 현재 턴은 어두운 칸에 하얀 점.</summary>
        public static readonly TurnTrackStyle Sticky = new TurnTrackStyle
        {
            Cell = new Color32(236, 236, 232, 255),
            LastCell = new Color32(255, 236, 0, 255),
            Dot = Color.white,
            CurrentCell = new Color32(44, 44, 46, 255),
            CellOutline = new Color32(78, 72, 64, 255),
        };
    }

    /// <summary>
    /// 인디케이터 점 + 칸 줄. 턴 수만큼의 칸을 늘어놓고(마지막 칸은 노랑 — 키를 얻는 턴) 하얀 점이 현재 턴의 칸에 놓인다.
    /// 쿼터 진행 창과 전체 스테이지 오버레이의 쿼터 블록이 같은 모양이라 함께 쓴다.
    /// </summary>
    internal sealed class TurnTrack
    {
        private const float DotMoveDuration = 0.25f;

        private readonly RectTransform _dot;
        private readonly Image _dotImage;
        private readonly Image[] _cells;
        private readonly TurnTrackStyle _style;
        private readonly int _turns;

        private float _dotFraction;
        private bool _dotShown;
        private Tween _moveTween;

        public TurnTrack(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int turns, float dotSize,
            TurnTrackStyle style = null)
        {
            _turns = turns;
            _style = style ?? TurnTrackStyle.Dark;

            var root = RuntimeUi.CreateRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            _cells = new Image[turns];
            for (var i = 0; i < turns; i++)
            {
                var isLast = i == turns - 1;
                var cell = RuntimeUi.CreateImage(root, $"Turn {i + 1}", isLast ? _style.LastCell : _style.Cell,
                    new Vector2(i / (float)turns, 0.12f), new Vector2((i + 1) / (float)turns, 0.88f),
                    new Vector2(3f, 0f), new Vector2(-3f, 0f));

                if (_style.CellOutline.a > 0f)
                {
                    var outline = cell.gameObject.AddComponent<Outline>();
                    outline.effectColor = _style.CellOutline;
                    outline.effectDistance = new Vector2(1.2f, -1.2f);
                    outline.useGraphicAlpha = false;
                }

                _cells[i] = cell;
            }

            _dotImage = RuntimeUi.CreateImage(root, "Dot", _style.Dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, RuntimeUi.Circle);
            _dot = _dotImage.rectTransform;
            _dot.sizeDelta = new Vector2(dotSize, dotSize);
            _dot.gameObject.SetActive(false);
        }

        /// <summary>현재 턴(1부터)의 칸으로 점을 옮긴다. 범위 밖(0 등)이면 점을 감춘다.
        /// animate가 아니거나 점이 처음 나타나는 경우(쿼터가 바뀌어 리셋된 경우 포함)에는 그 자리로 바로 놓는다.</summary>
        public void SetTurn(int turn, bool animate)
        {
            _moveTween?.Kill();
            PaintCells(turn);

            if (turn < 1 || turn > _turns)
            {
                _dotShown = false;
                _dot.gameObject.SetActive(false);
                return;
            }

            var target = (turn - 0.5f) / _turns;
            _dot.gameObject.SetActive(true);

            if (animate && _dotShown && !Mathf.Approximately(_dotFraction, target))
                _moveTween = DOVirtual.Float(_dotFraction, target, DotMoveDuration, MoveDot).SetEase(Ease.OutQuad);
            else
                MoveDot(target);

            _dotShown = true;
        }

        public void Kill() => _moveTween?.Kill();

        /// <summary>칸 색을 되돌리고, 스타일이 원하면 현재 턴의 칸만 어둡게 칠한다. 점은 마지막 칸이면 노랑(키를 얻는 턴이라는 표시를 유지한다).</summary>
        private void PaintCells(int turn)
        {
            var paintCurrent = _style.CurrentCell.a > 0f;
            for (var i = 0; i < _cells.Length; i++)
            {
                var isLast = i == _cells.Length - 1;
                var isCurrent = i + 1 == turn;
                _cells[i].color = paintCurrent && isCurrent ? _style.CurrentCell : isLast ? _style.LastCell : _style.Cell;
            }

            var currentIsLast = turn == _cells.Length;
            _dotImage.color = paintCurrent && currentIsLast ? _style.LastCell : _style.Dot;
        }

        private void MoveDot(float fraction)
        {
            _dotFraction = fraction;
            _dot.anchorMin = new Vector2(fraction, 0.5f);
            _dot.anchorMax = new Vector2(fraction, 0.5f);
            _dot.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 키 아이콘. 카탈로그에 "key" 스프라이트(Assets/Art/UI/Icons/key.png)가 있으면 그걸 쓰고, 없으면 임시 도형(원 머리 + 축 + 톱니)으로 그린다.
    /// 정사각형 안에 그려지므로 AspectRatioFitter로 부모 칸 안에 정사각형으로 맞춘다. 색은 틴트로 준다(획득 파랑 / 미획득 회색).
    /// </summary>
    internal sealed class KeyGlyph
    {
        private readonly Image[] _parts;

        public RectTransform Root { get; }

        public KeyGlyph(Transform slot, string name)
        {
            Root = RuntimeUi.CreateStretched(slot, name);
            var fitter = Root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            var sprite = UiIcons.GetExact(UiIconCatalog.KeyId);
            if (sprite != null)
            {
                var icon = RuntimeUi.CreateImage(Root, "Icon", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, sprite);
                icon.preserveAspect = true;
                _parts = new[] { icon };
                return;
            }

            _parts = new[]
            {
                RuntimeUi.CreateImage(Root, "Head", Color.white, new Vector2(0.08f, 0.30f), new Vector2(0.42f, 0.70f),
                    Vector2.zero, Vector2.zero, RuntimeUi.Circle),
                RuntimeUi.CreateImage(Root, "Shaft", Color.white, new Vector2(0.38f, 0.45f), new Vector2(0.94f, 0.55f),
                    Vector2.zero, Vector2.zero),
                RuntimeUi.CreateImage(Root, "Tooth A", Color.white, new Vector2(0.68f, 0.28f), new Vector2(0.76f, 0.47f),
                    Vector2.zero, Vector2.zero),
                RuntimeUi.CreateImage(Root, "Tooth B", Color.white, new Vector2(0.84f, 0.33f), new Vector2(0.92f, 0.47f),
                    Vector2.zero, Vector2.zero),
            };
        }

        public void SetColor(Color color)
        {
            foreach (var part in _parts) part.color = color;
        }

        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        public void Pop()
        {
            Root.DOKill();
            Root.localScale = Vector3.one;
            Root.DOScale(1.35f, 0.18f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
        }

        public void Kill()
        {
            Root.DOKill();
            Root.localScale = Vector3.one;
        }
    }
}
