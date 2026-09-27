using System;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 오프닝 시퀀스 19단계: 메인 화면. PPT 목업 그대로 — 왼쪽에 노을빛 창과 시계탑 실루엣(창 아래 어둠 속에 노란 불빛 다섯 점), 가운데에 스포트라이트가 비치는 취조실(테이블·의자·커피),
    /// 오른쪽에 버튼 셋("단서 노트" · "취조시작" · "나가기"). 그림은 목업의 자리표시 그림이고 좌표는 PPT의 것을 <see cref="IntroSlide"/> 비율로 옮겼다.
    /// 이 클래스는 화면을 짓고 눌림을 콜백으로 알리기만 한다 — 게임을 시작하는 일은 부른 쪽(<see cref="IntroSequencePlayer"/> → StageBootstrapper)이 한다.
    /// </summary>
    internal sealed class IntroMainMenuView : MonoBehaviour
    {
        private static readonly Color WindowSky = new Color32(0xA9, 0x7A, 0x2D, 255);
        private static readonly Color Lamp = new Color32(0xFF, 0xFF, 0x00, 255);
        private static readonly Color ButtonFill = new Color(0.09f, 0.06f, 0.03f, 0.9f);
        private static readonly Color ButtonHoverFill = new Color32(0xCA, 0x66, 0x02, 240);
        private static readonly Vector2[] LampSpots =
        {
            new Vector2(0.267f, 0.377f), new Vector2(0.288f, 0.532f), new Vector2(0.265f, 0.559f),
            new Vector2(0.241f, 0.529f), new Vector2(0.238f, 0.562f)
        };

        private CanvasGroup _group;
        private RawImage _beam;
        private Image[] _lampGlows;
        private TMP_Text _toast;
        private Tween _toastTween;

        public static IntroMainMenuView Create(IntroSlide slide, IntroCutsceneArt art, TMP_FontAsset font, Action onStart, Action onNotes, Action onQuit)
        {
            var root = RuntimeUi.CreateStretched(slide.Root, "Main Menu");
            var view = root.gameObject.AddComponent<IntroMainMenuView>();
            view._group = root.gameObject.AddComponent<CanvasGroup>();
            view.Build(slide, root, art, font, onStart, onNotes, onQuit);
            return view;
        }

        /// <summary>버튼을 잠그거나 푼다(시작을 누른 뒤 두 번 눌리지 않게).</summary>
        public void SetInteractable(bool interactable)
        {
            _group.interactable = interactable;
            _group.blocksRaycasts = interactable;
        }

        /// <summary>버튼 아래에 짧은 안내 문구를 띄웠다가 지운다.</summary>
        public void ShowToast(string message, float seconds = 1.6f)
        {
            _toastTween?.Kill();
            _toast.text = message;
            _toast.alpha = 1f;
            _toastTween = DOTween.To(() => _toast.alpha, value => _toast.alpha = value, 0f, 0.4f)
                .SetDelay(seconds).SetUpdate(true).SetTarget(this);
        }

        private void Update()
        {
            // 창 아래 불빛과 스포트라이트가 숨 쉬듯 미세하게 흔들린다.
            var t = Time.unscaledTime;
            for (var i = 0; i < _lampGlows.Length; i++)
                _lampGlows[i].color = new Color(Lamp.r, Lamp.g, Lamp.b, 0.16f + 0.1f * Mathf.Sin(t * (1.3f + i * 0.37f) + i * 1.9f));

            if (_beam != null) _beam.color = new Color(1f, 1f, 1f, 0.9f + 0.1f * Mathf.Sin(t * 0.8f));
        }

        private void OnDestroy() => DOTween.Kill(this);

        private void Build(IntroSlide slide, RectTransform root, IntroCutsceneArt art, TMP_FontAsset font, Action onStart, Action onNotes, Action onQuit)
        {
            const float E = 12192000f;
            const float H = 6858000f;

            // 그림 순서는 PPT 장표의 쌓임 순서 그대로: 취조실 사진 → 의자 → 테이블 → 빛줄기 → 창 → 가림 띠 → 커피 → 시계탑 → 가림 띠 → 불빛.
            AddPicture(slide, root, "Room", art?.mainRoom, 0.329f, 0.171f, 0.431f, 0.507f, 0f,
                new Rect(0.09563f, 0.13852f, 1f - 0.09563f, 1f - 0.13852f), new Color(0.16f, 0.15f, 0.15f, 1f));
            AddPicture(slide, root, "Chair", art?.mainChair, 0.554f, 0.423f, 0.172f, 0.427f);
            AddPicture(slide, root, "Table", art?.mainTable, 0.438f, 0.51f, 0.198f, 0.379f);

            // 창에서 방 안으로 비스듬히 뻗는 빛줄기 — PPT의 그라데이션 사각형(회전 281.9°).
            var beamRect = slide.Place(root, "Light Beam", 4527489f / E, 754430f / H, 3571341f / E, 5452660f / H, 281.918f);
            _beam = beamRect.gameObject.AddComponent<RawImage>();
            _beam.texture = BuildBeamTexture();
            _beam.raycastTarget = false;

            var window = slide.Place(root, "Window", 2159169f / E, 1170550f / H, 1874808f / E, 3467818f / H);
            window.gameObject.AddComponent<Image>().color = WindowSky;
            window.GetComponent<Image>().raycastTarget = false;

            AddBlack(slide, root, "Cover Top", 7278740f / E, -533314f / H, 673681f / E, 3467818f / H, 270f);
            AddPicture(slide, root, "Coffee", art?.mainCoffee, 0.469f, 0.471f, 0.061f, 0.168f, 0f, new Rect(1f, 0f, -1f, 1f));
            AddPicture(slide, root, "Clock Tower", art?.mainTower, 0.177f, 0.276f, 0.152f, 0.244f);
            AddBlack(slide, root, "Cover Ground", 2095794f / E, 2704434f / H, 1139357f / E, 2728509f / H, 270f);

            _lampGlows = new Image[LampSpots.Length];
            for (var i = 0; i < LampSpots.Length; i++)
            {
                var spot = LampSpots[i];
                var dot = slide.Place(root, "Lamp", spot.x, spot.y, 0.011f, 0.021f);
                var dotImage = dot.gameObject.AddComponent<Image>();
                dotImage.sprite = RuntimeUi.Circle;
                dotImage.color = Lamp;
                dotImage.raycastTarget = false;

                var glow = slide.Place(root, "Lamp Glow", spot.x - 0.011f, spot.y - 0.021f, 0.033f, 0.063f);
                glow.SetSiblingIndex(dot.GetSiblingIndex());
                var glowImage = glow.gameObject.AddComponent<Image>();
                glowImage.sprite = RuntimeUi.Circle;
                glowImage.raycastTarget = false;
                _lampGlows[i] = glowImage;
            }

            // 버튼: 위치·크기는 PPT 그대로. "단서 노트"는 그룹의 오른쪽 위 사각형(왼쪽 위 모서리에 빨간 점).
            AddButton(slide, root, font, "단서 노트", 9114971f / E, 1973109f / H, 1435306f / E, 622663f / H, onNotes);
            AddBadge(slide, root, 8986522f / E, 1858777f / H, 332185f / E, 332185f / H); // 버튼 왼쪽 위 모서리에 걸친다(장표 좌표라 틀의 자식으로 둔다).
            AddButton(slide, root, font, "취조시작", 0.742f, 0.455f, 0.188f, 0.091f, onStart);
            AddButton(slide, root, font, "나가기", 0.742f, 0.625f, 0.188f, 0.091f, onQuit);

            var toast = slide.Place(root, "Toast", 0.742f, 0.385f, 0.188f, 0.05f);
            _toast = toast.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) _toast.font = font;
            _toast.fontSize = 24f;
            _toast.color = MockupStyle.Paper;
            _toast.alignment = TextAlignmentOptions.Center;
            _toast.textWrappingMode = TextWrappingModes.NoWrap;
            _toast.raycastTarget = false;
            _toast.alpha = 0f;
        }

        private static void AddPicture(IntroSlide slide, RectTransform parent, string name, Texture2D texture, float x, float y, float w, float h,
            float pptRotation = 0f, Rect? uv = null, Color? fallback = null)
        {
            var rect = slide.Place(parent, name, x, y, w, h, pptRotation);
            if (texture == null)
            {
                // 그림이 없으면 색 면(fallback)이 있는 것만 자리를 지킨다 — 없는 조각은 건너뛴다.
                if (fallback.HasValue)
                {
                    var flat = rect.gameObject.AddComponent<Image>();
                    flat.color = fallback.Value;
                    flat.raycastTarget = false;
                }

                return;
            }

            var raw = rect.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            if (uv.HasValue) raw.uvRect = uv.Value;
        }

        private static void AddBlack(IntroSlide slide, RectTransform parent, string name, float x, float y, float w, float h, float pptRotation)
        {
            var rect = slide.Place(parent, name, x, y, w, h, pptRotation);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        private static void AddBadge(IntroSlide slide, Transform parent, float x, float y, float w, float h)
        {
            var rect = slide.Place(parent, "New Badge", x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RuntimeUi.Circle;
            image.color = new Color32(0xFF, 0x00, 0x00, 255);
            image.raycastTarget = false;
        }

        private static void AddButton(IntroSlide slide, RectTransform parent, TMP_FontAsset font, string label,
            float x, float y, float w, float h, Action onClick)
        {
            var rect = slide.Place(parent, label, x, y, w, h);
            var fill = rect.gameObject.AddComponent<Image>();
            fill.color = ButtonFill;
            fill.raycastTarget = true;

            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(MockupStyle.Paper.r, MockupStyle.Paper.g, MockupStyle.Paper.b, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            var text = RuntimeUi.CreateText(rect, "Label", label, font, rect.sizeDelta.y * 0.4f, MockupStyle.Paper,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            var button = rect.gameObject.AddComponent<MenuButton>();
            button.Init(fill, text, onClick);
        }

        /// <summary>빛줄기 그라데이션: PPT 사각형의 다섯 색 멈춤(0%·40%·60.5%·74%·98%)을 그대로 옮긴 세로 1×256 텍스처. 위(0%)가 텍스처 위쪽이다.</summary>
        private static Texture2D BuildBeamTexture()
        {
            const int Height = 256;
            var stops = new (float pos, Color color)[]
            {
                (0f, new Color32(0xFF, 0xFF, 0x00, 115)),
                (0.40f, new Color32(0x6E, 0x74, 0x24, 59)),
                (0.60502f, new Color32(0x6E, 0x74, 0x24, 89)),
                (0.74f, new Color32(0xFF, 0xFF, 0x00, 48)),
                (0.98f, new Color32(0x6E, 0x74, 0x24, 38)),
            };

            var texture = new Texture2D(1, Height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[Height];
            for (var row = 0; row < Height; row++)
                pixels[row] = Sample(stops, 1f - (row + 0.5f) / Height); // 행 0이 아래이므로 위(pos 0)는 맨 윗행

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Color32 Sample((float pos, Color color)[] stops, float pos)
        {
            if (pos <= stops[0].pos) return stops[0].color;
            for (var i = 1; i < stops.Length; i++)
            {
                if (pos > stops[i].pos) continue;
                var t = Mathf.InverseLerp(stops[i - 1].pos, stops[i].pos, pos);
                return Color.Lerp(stops[i - 1].color, stops[i].color, t);
            }

            return stops[stops.Length - 1].color;
        }

        /// <summary>메인 화면 버튼: 마우스를 올리면 주황으로 켜지고, 누르면 클릭음과 함께 콜백을 부른다.</summary>
        private sealed class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            private Image _fill;
            private TMP_Text _label;
            private Action _onClick;

            public void Init(Image fill, TMP_Text label, Action onClick)
            {
                _fill = fill;
                _label = label;
                _onClick = onClick;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                _fill.color = ButtonHoverFill;
                _label.color = new Color(0.08f, 0.05f, 0.02f, 1f);
            }

            public void OnPointerExit(PointerEventData eventData) => Reset();

            public void OnPointerClick(PointerEventData eventData)
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                _onClick?.Invoke();
            }

            private void OnDisable() => Reset();

            private void Reset()
            {
                if (_fill == null) return;
                _fill.color = ButtonFill;
                _label.color = MockupStyle.Paper;
            }
        }
    }
}
