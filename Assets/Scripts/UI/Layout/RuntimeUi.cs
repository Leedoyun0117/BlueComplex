using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
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

        private static Sprite _roundedRect;

        /// <summary>안티앨리어싱된 흰 둥근 사각형(9-슬라이스). 결과 태그 칩처럼 크기가 제각각인 알약형 배경에 쓴다 — Image.Type.Sliced로 늘리고 틴트로 색을 입힌다.</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_roundedRect != null) return _roundedRect;

                const int size = 64;
                const float radius = 22f;
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
                        // 가장 가까운 "안쪽 사각형" 점까지의 거리로 모서리 둥글기를 만든다.
                        var dx = Mathf.Max(radius - (x + 0.5f), 0f, (x + 0.5f) - (size - radius));
                        var dy = Mathf.Max(radius - (y + 0.5f), 0f, (y + 0.5f) - (size - radius));
                        var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, true);

                _roundedRect = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                _roundedRect.hideFlags = HideFlags.HideAndDontSave;
                return _roundedRect;
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
    /// 움직임(찍히기 등)은 AspectRatioFitter가 위치·크기를 쥐고 있는 Root가 아니라 그 안의 <see cref="Mover"/>에 준다.
    /// </summary>
    internal sealed class KeyGlyph
    {
        private readonly Image[] _parts;
        private readonly CanvasGroup _group;
        private Tween _stamp;
        private Tween _flash;

        public RectTransform Root { get; }

        /// <summary>열쇠 그림만 담은 안쪽 판. 위치·기울기·크기·투명도를 트윈으로 움직이는 대상.</summary>
        public RectTransform Mover { get; }

        public KeyGlyph(Transform slot, string name)
        {
            Root = RuntimeUi.CreateStretched(slot, name);
            var fitter = Root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            Mover = RuntimeUi.CreateStretched(Root, "Mover");
            _group = Mover.gameObject.AddComponent<CanvasGroup>();

            var sprite = UiIcons.GetExact(UiIconCatalog.KeyId);
            if (sprite != null)
            {
                var icon = RuntimeUi.CreateImage(Mover, "Icon", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, sprite);
                icon.preserveAspect = true;
                _parts = new[] { icon };
                return;
            }

            _parts = new[]
            {
                RuntimeUi.CreateImage(Mover, "Head", Color.white, new Vector2(0.08f, 0.30f), new Vector2(0.42f, 0.70f),
                    Vector2.zero, Vector2.zero, RuntimeUi.Circle),
                RuntimeUi.CreateImage(Mover, "Shaft", Color.white, new Vector2(0.38f, 0.45f), new Vector2(0.94f, 0.55f),
                    Vector2.zero, Vector2.zero),
                RuntimeUi.CreateImage(Mover, "Tooth A", Color.white, new Vector2(0.68f, 0.28f), new Vector2(0.76f, 0.47f),
                    Vector2.zero, Vector2.zero),
                RuntimeUi.CreateImage(Mover, "Tooth B", Color.white, new Vector2(0.84f, 0.33f), new Vector2(0.92f, 0.47f),
                    Vector2.zero, Vector2.zero),
            };
        }

        public void SetColor(Color color)
        {
            foreach (var part in _parts) part.color = color;
        }

        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        /// <summary>빈 칸에 열쇠가 "탁" 찍힌다: 위에서 크게 떨어져 칸에 닿으며 살짝 눌리고, 닿는 순간 밝아졌다가 작게 튀며 가라앉는다.
        /// <paramref name="onImpact"/>는 닿는 순간 한 번 불린다(소리 훅·칸 배경 번쩍임용).</summary>
        public void PlayStamp(Color finalColor, Color flashColor, Action onImpact)
        {
            var motion = UiMotion.Settings;
            Kill();

            SetColor(finalColor);
            _group.alpha = 0f;
            Mover.anchoredPosition = new Vector2(0f, motion.keyDropHeight);
            Mover.localScale = Vector3.one * 1.55f;
            Mover.localRotation = Quaternion.Euler(0f, 0f, 14f);

            var bounceHeight = motion.keyDropHeight * 0.14f;
            var up = motion.keyBounce * 0.4f;
            var down = motion.keyBounce * 0.6f;

            _stamp = DOTween.Sequence().SetUpdate(true).SetTarget(Mover)
                .Append(_group.DOFade(1f, motion.keyFall * 0.4f))
                .Join(Mover.DOAnchorPosY(0f, motion.keyFall).SetEase(Ease.InQuad))
                .Join(Mover.DOScale(0.9f, motion.keyFall).SetEase(Ease.InQuad))
                .Join(Mover.DOLocalRotate(Vector3.zero, motion.keyFall).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    PlayFlash(flashColor, finalColor, motion.keyFlash);
                    onImpact?.Invoke();
                })
                .Append(Mover.DOAnchorPosY(bounceHeight, up).SetEase(Ease.OutQuad))
                .Join(Mover.DOScale(1.1f, up).SetEase(Ease.OutQuad))
                .Append(Mover.DOAnchorPosY(0f, down).SetEase(Ease.InQuad))
                .Join(Mover.DOScale(1f, down).SetEase(Ease.InQuad));
        }

        private void PlayFlash(Color from, Color to, float seconds)
        {
            _flash?.Kill();
            var current = from;
            SetColor(from);
            _flash = DOTween.To(() => current, c =>
                {
                    current = c;
                    SetColor(c);
                }, to, seconds)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(Mover);
        }

        /// <summary>진행 중인 움직임을 멈추고 원래 자세로 되돌린다.</summary>
        public void Kill()
        {
            _stamp?.Kill();
            _flash?.Kill();
            Mover.DOKill();
            _group.DOKill();
            _group.alpha = 1f;
            Mover.anchoredPosition = Vector2.zero;
            Mover.localScale = Vector3.one;
            Mover.localRotation = Quaternion.identity;
        }
    }
}
