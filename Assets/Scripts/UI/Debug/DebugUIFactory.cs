using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>
    /// 디버그 플레이 화면용 uGUI 생성 헬퍼. 전부 런타임 코드로 조립해 씬 배선을 최소화한다.
    /// 미적 완성도는 신경 쓰지 않고 동작만 보장한다.
    /// </summary>
    internal static class DebugUIFactory
    {
        private static Font _koreanFont;

        /// <summary>레거시 Text가 한글을 그리려면 한글 글리프가 있는 OS 폰트가 필요하다 (프로젝트에 TextMeshPro 미포함).</summary>
        public static Font KoreanFont
        {
            get
            {
                if (_koreanFont == null)
                    _koreanFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Gulim", "Arial" }, 16);
                return _koreanFont;
            }
        }

        public static Canvas CreateRootCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();

            // Screen Space - Camera로 시도해봤지만 URP에서는 렌더 모드와 무관하게 uGUI Canvas가
            // SRP의 커스텀 렌더 패스(AfterRenderingPostProcessing 포함)가 전부 끝난 뒤 별도로 합성된다.
            // 그래서 CRT FullScreenPassRendererFeature(fetchColorBuffer)가 캡처하는 컬러 버퍼 안에
            // UI가 애초에 없다 — 실측으로 확인함(배치 모드 렌더 캡처, CRT 피처 끄면 UI가 바로 보임).
            // Canvas 렌더 모드를 바꾸는 것만으로는 해결할 수 없는 구조적 한계라 Overlay로 되돌린다.
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            // activeInputHandler가 새 Input System 전용이므로 레거시 StandaloneInputModule은 동작하지 않는다.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = background;
            return (RectTransform)go.transform;
        }

        public static RectTransform CreateEmpty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Text CreateText(Transform parent, string name, int fontSize = 16,
            TextAnchor anchor = TextAnchor.UpperLeft, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = KoreanFont;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color ?? Color.white;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, out Text label, Color? background = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = background ?? new Color(0.18f, 0.18f, 0.24f);
            go.GetComponent<Button>().targetGraphic = image;

            label = CreateText(go.transform, "Label", 14, TextAnchor.UpperLeft, Color.white);
            var rt = (RectTransform)label.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 8);
            rt.offsetMax = new Vector2(-8, -8);
            label.verticalOverflow = VerticalWrapMode.Truncate;

            return go.GetComponent<Button>();
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, int spacing = 8,
            bool controlWidth = true, bool controlHeight = true, bool expandWidth = false, bool expandHeight = false)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = controlWidth;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            return layout;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, int spacing = 4,
            bool controlWidth = true, bool controlHeight = true, bool expandWidth = true, bool expandHeight = false)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = controlWidth;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            return layout;
        }

        public static LayoutElement AddLayoutElement(GameObject go, float? minWidth = null, float? minHeight = null,
            float? preferredWidth = null, float? preferredHeight = null, float? flexibleWidth = null, float? flexibleHeight = null)
        {
            var element = go.AddComponent<LayoutElement>();
            if (minWidth.HasValue) element.minWidth = minWidth.Value;
            if (minHeight.HasValue) element.minHeight = minHeight.Value;
            if (preferredWidth.HasValue) element.preferredWidth = preferredWidth.Value;
            if (preferredHeight.HasValue) element.preferredHeight = preferredHeight.Value;
            if (flexibleWidth.HasValue) element.flexibleWidth = flexibleWidth.Value;
            if (flexibleHeight.HasValue) element.flexibleHeight = flexibleHeight.Value;
            return element;
        }
    }
}
