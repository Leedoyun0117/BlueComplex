using System.Collections;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신 4막: TV 뉴스 자막. 검은 화면에 지직거리는 잡음·주사선·번쩍이는 줄이 깔리고, 화면 아래쪽 자막 띠에 한 줄씩 나온다.
    /// 줄이 바뀔 때마다 잡음 효과음(<see cref="UiSoundCue.TvStatic"/>)이 울린다.
    /// </summary>
    internal sealed class IntroNewsView : MonoBehaviour
    {
        private const float NoiseAlpha = 0.16f;
        private const float FlickerInterval = 0.05f;
        private const int GlitchBarCount = 2;

        private CanvasGroup _group;
        private RawImage _noise;
        private Texture2D _noiseTexture;
        private Texture2D _scanTexture;
        private TMP_Text _caption;
        private CanvasGroup _captionGroup;
        private readonly RectTransform[] _bars = new RectTransform[GlitchBarCount];
        private float _nextFlicker;
        private float _noiseBoost;

        public static IntroNewsView Create(Transform parent, Vector2 canvasSize, TMP_FontAsset font)
        {
            var go = new GameObject("Intro News", typeof(RectTransform), typeof(CanvasGroup)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);

            var view = go.AddComponent<IntroNewsView>();
            view.Build(canvasSize, font);
            return view;
        }

        /// <summary>TV가 켜진다 → 줄마다 <paramref name="lineSeconds"/>씩 자막을 보여 준다 → 끝. 줄마다 잡음 효과음이 울린다.</summary>
        public IEnumerator Play(IReadOnlyList<string> lines, float lineSeconds)
        {
            enabled = true;
            _group.alpha = 1f;
            _noiseBoost = 1f; // 켜지는 순간 잡음이 세게 튀었다 가라앉는다.
            DOTween.To(() => _noiseBoost, v => _noiseBoost = v, 0f, 0.5f).SetUpdate(true).SetTarget(this);

            foreach (var line in lines)
            {
                _caption.text = line;
                _captionGroup.alpha = 0f;
                _captionGroup.DOFade(1f, 0.15f).SetUpdate(true).SetTarget(_captionGroup);
                _noiseBoost = 0.8f;
                DOTween.To(() => _noiseBoost, v => _noiseBoost = v, 0f, 0.4f).SetUpdate(true).SetTarget(this);

                UiSoundHooks.Play(UiSoundCue.TvStatic);
                yield return new WaitForSecondsRealtime(lineSeconds);
            }
        }

        public void Hide()
        {
            DOTween.Kill(this);
            _captionGroup.DOKill();
            _group.alpha = 0f;
            enabled = false;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextFlicker) return;
            _nextFlicker = Time.unscaledTime + FlickerInterval;

            // 잡음 무늬를 매번 다른 자리·크기로 옮겨 지직거림으로 보이게 한다.
            var scale = Random.Range(0.6f, 1f);
            _noise.uvRect = new Rect(Random.value, Random.value, scale, scale);
            _noise.color = new Color(1f, 1f, 1f, (NoiseAlpha + _noiseBoost * 0.45f) * Random.Range(0.6f, 1.4f));

            foreach (var bar in _bars)
            {
                var visible = Random.value < 0.6f;
                bar.gameObject.SetActive(visible);
                if (!visible) continue;

                var y = Random.value;
                bar.anchorMin = new Vector2(0f, y);
                bar.anchorMax = new Vector2(1f, y + Random.Range(0.004f, 0.02f));
                bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, Random.Range(0.05f, 0.22f));
            }
        }

        private void Build(Vector2 canvasSize, TMP_FontAsset font)
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _noiseTexture = BuildNoise();
            _noise = CreateRaw("Noise", _noiseTexture, new Color(1f, 1f, 1f, NoiseAlpha));

            for (var i = 0; i < _bars.Length; i++)
            {
                var bar = new GameObject("Glitch Bar", typeof(RectTransform), typeof(Image)) { layer = gameObject.layer };
                bar.transform.SetParent(transform, false);
                bar.GetComponent<Image>().raycastTarget = false;
                _bars[i] = (RectTransform)bar.transform;
                _bars[i].offsetMin = _bars[i].offsetMax = Vector2.zero;
            }

            _scanTexture = BuildScanlines();
            var scan = CreateRaw("Scanlines", _scanTexture, Color.white);
            scan.uvRect = new Rect(0f, 0f, 1f, canvasSize.y / _scanTexture.height);

            // 자막 띠: 화면 아래쪽, 어두운 반투명 판 + 왼쪽 붉은 표식.
            var strip = new GameObject("Caption Strip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup)) { layer = gameObject.layer };
            strip.transform.SetParent(transform, false);
            var stripRect = (RectTransform)strip.transform;
            stripRect.anchorMin = new Vector2(0.08f, 0.1f);
            stripRect.anchorMax = new Vector2(0.92f, 0.28f);
            stripRect.offsetMin = stripRect.offsetMax = Vector2.zero;
            var stripImage = strip.GetComponent<Image>();
            stripImage.color = new Color(0.03f, 0.05f, 0.1f, 0.88f);
            stripImage.raycastTarget = false;
            _captionGroup = strip.GetComponent<CanvasGroup>();
            _captionGroup.alpha = 0f;

            var mark = new GameObject("Mark", typeof(RectTransform), typeof(Image)) { layer = gameObject.layer };
            mark.transform.SetParent(stripRect, false);
            var markRect = (RectTransform)mark.transform;
            markRect.anchorMin = Vector2.zero;
            markRect.anchorMax = new Vector2(0f, 1f);
            markRect.pivot = new Vector2(0f, 0.5f);
            markRect.sizeDelta = new Vector2(10f, 0f);
            markRect.anchoredPosition = Vector2.zero;
            var markImage = mark.GetComponent<Image>();
            markImage.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            markImage.raycastTarget = false;

            var text = new GameObject("Caption", typeof(RectTransform)) { layer = gameObject.layer };
            text.transform.SetParent(stripRect, false);
            var textRect = (RectTransform)text.transform;
            Stretch(textRect);
            textRect.offsetMin = new Vector2(36f, 12f);
            textRect.offsetMax = new Vector2(-28f, -12f);

            _caption = text.AddComponent<TextMeshProUGUI>();
            if (font != null) _caption.font = font;
            _caption.color = new Color(0.94f, 0.95f, 0.98f, 1f);
            _caption.alignment = TextAlignmentOptions.MidlineLeft;
            _caption.textWrappingMode = TextWrappingModes.Normal;
            _caption.enableAutoSizing = true;
            _caption.fontSizeMin = 26f;
            _caption.fontSizeMax = 44f;
            _caption.raycastTarget = false;

            enabled = false;
        }

        private RawImage CreateRaw(string name, Texture texture, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            Stretch((RectTransform)go.transform);

            var raw = go.GetComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            return raw;
        }

        /// <summary>TV 잡음: 픽셀마다 무작위 회색. RawImage의 uvRect를 흔들어 지직거리게 한다.</summary>
        private static Texture2D BuildNoise()
        {
            const int size = 256;
            var random = new System.Random(926);
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                var g = (byte)random.Next(0, 256);
                pixels[i] = new Color32(g, g, g, 255);
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>주사선: 4픽셀마다 어두운 줄 두 개. 검정 RawImage에 얹어 세로로 반복시킨다.</summary>
        private static Texture2D BuildScanlines()
        {
            var texture = new Texture2D(1, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            texture.SetPixels32(new[]
            {
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 110), new Color32(0, 0, 0, 110)
            });
            texture.Apply(false, true);
            return texture;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (_noiseTexture != null) Destroy(_noiseTexture);
            if (_scanTexture != null) Destroy(_scanTexture);
        }
    }
}
