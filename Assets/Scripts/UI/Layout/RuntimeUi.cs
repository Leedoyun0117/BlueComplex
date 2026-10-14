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

        /// <summary>
        /// 포스트잇 손글씨(<see cref="PostitStyle.HandFont"/>)를 뺀 모든 글자의 글꼴 — NotoSansKR SDF. TMP Settings의 기본 글꼴로 물려 있어
        /// 글꼴을 따로 지정하지 않은 TMP_Text도 같은 글꼴이 된다. 예전처럼 캔버스의 "아무 글자"에서 빌리면 포스트잇이 먼저 걸릴 때 손글씨체가 옮는다.
        /// </summary>
        public static TMP_FontAsset GameFont => TMP_Settings.defaultFontAsset;

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
}
