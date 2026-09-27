using UnityEngine;
using UnityEngine.UI;

namespace UI.Esc
{
    /// <summary>
    /// ESC가 나타나기 전 RT_UI를 저장하고 알파 형태만 단색으로 표시한다.
    /// ESC 종이 배경 위, 설정 컨트롤 아래에 배치하고 화면 좌표를 유지한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class LSO_EscSilhouette : MonoBehaviour
    {
        [SerializeField] private Color silhouetteColor = new Color(1f, 1f, 1f, 0.18f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private LSO_EscPanel _panel;
        private CanvasGroup _panelGroup;
        private RawImage _image;
        private Material _material;
        private RenderTexture _snapshot;
        private Transform _background;
        private readonly Vector3[] _screenCorners = new Vector3[4];
        private int _lastVisibleFrame = -2;

        internal static void Ensure(LSO_EscPanel panel)
        {
            if (panel.GetComponent<LSO_EscBackdrop>() == null)
                panel.gameObject.AddComponent<LSO_EscBackdrop>();
            if (panel.GetComponentInChildren<LSO_EscSilhouette>(true) != null) return;

            var go = new GameObject("UI Silhouette", typeof(RectTransform), typeof(RawImage));
            go.layer = panel.gameObject.layer;
            go.transform.SetParent(panel.transform, false);
            go.AddComponent<LSO_EscSilhouette>();
        }

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _image.enabled = false;
            _image.raycastTarget = false;
            _panel = GetComponentInParent<LSO_EscPanel>();
            if (_panel == null) { enabled = false; return; }

            var rect = _image.rectTransform;
            // 루트의 Backdrop 위에만 놓으면 Content/BG의 불투명 종이가 다시 덮는다.
            _background = _panel.Content != null ? _panel.Content.Find("BG") : null;
            rect.SetParent(_background != null ? _panel.Content : _panel.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            UpdatePlacement();

            var shader = Shader.Find("BlueComplex/LSO/ScreenSilhouette");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[LSO_EscSilhouette] 실루엣 셰이더를 사용할 수 없습니다.", this);
                enabled = false;
                return;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _material.SetColor(ColorId, silhouetteColor);
            _image.material = _material;
        }

        private void OnEnable()
        {
            if (_panel == null || _material == null) return;
            _panel.Opened += Capture;
            _panel.Closed += Clear;
        }

        private void OnDisable()
        {
            if (_panel != null)
            {
                _panel.Opened -= Capture;
                _panel.Closed -= Clear;
            }
            Clear();
        }

        private void LateUpdate()
        {
            if (_panel == null) return;
            if (_panelGroup == null) _panelGroup = _panel.GetComponent<CanvasGroup>();
            if (_panel.IsOpen || (_panelGroup != null && _panelGroup.alpha > 0f))
                _lastVisibleFrame = Time.frameCount;

            UpdatePlacement();
        }

        private void UpdatePlacement()
        {
            var background = _background;
            if (background == null)
            {
                var backdrop = _panel.GetComponent<LSO_EscBackdrop>();
                background = backdrop != null ? backdrop.Surface : null;
            }
            if (background != null && transform.GetSiblingIndex() != background.GetSiblingIndex() + 1)
            {
                transform.SetAsLastSibling();
                transform.SetSiblingIndex(background.GetSiblingIndex() + 1);
            }

            // Content의 슬라이드 이동을 상쇄한다. 캡처한 UI는 원래 화면 위치에 남는다.
            var screen = (RectTransform)_panel.transform;
            screen.GetWorldCorners(_screenCorners);
            var parent = transform.parent;
            var bottomLeft = parent.InverseTransformPoint(_screenCorners[0]);
            var topRight = parent.InverseTransformPoint(_screenCorners[2]);
            var rect = _image.rectTransform;
            rect.sizeDelta = new Vector2(topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
            rect.localPosition = (bottomLeft + topRight) * 0.5f;
        }

        private void Capture()
        {
            // 닫는 도중 연타하면 ESC가 찍힌 RT 대신 이전 스냅샷을 재사용한다.
            if (_snapshot != null && Time.frameCount <= _lastVisibleFrame + 1)
            {
                _image.enabled = true;
                return;
            }

            // RT_UI를 그리는 루트 Canvas의 카메라를 사용한다.
            var canvas = _panel.GetComponentInParent<Canvas>();
            var camera = canvas != null ? canvas.rootCanvas.worldCamera : null;
            var source = camera != null ? camera.targetTexture : null;
            if (source == null || !source.IsCreated())
            {
                Clear();
                Debug.LogWarning("[LSO_EscSilhouette] UI 카메라의 RT_UI가 없어 실루엣을 표시할 수 없습니다.", this);
                return;
            }

            if (_snapshot == null || _snapshot.width != source.width || _snapshot.height != source.height)
            {
                ReleaseSnapshot();
                _snapshot = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "LSO_EscSilhouetteSnapshot",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _snapshot.Create();
            }

            // Opened는 렌더링 전이다. 프레임 끝까지 기다리면 ESC까지 찍힌다.
            var previous = RenderTexture.active;
            try { Graphics.Blit(source, _snapshot); }
            finally { RenderTexture.active = previous; }
            _image.texture = _snapshot;
            _image.enabled = true;
        }

        private void Clear()
        {
            if (_image != null) _image.enabled = false;
        }

        private void ReleaseSnapshot()
        {
            if (_image != null) _image.texture = null;
            if (_snapshot == null) return;
            _snapshot.Release();
            Destroy(_snapshot);
            _snapshot = null;
        }

        private void OnDestroy()
        {
            ReleaseSnapshot();
            if (_material != null) Destroy(_material);
        }
    }
}
