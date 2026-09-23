using BlueComplex.UI.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>
    /// DistortionCorrectedGraphicRaycaster 검증용 임시 오브젝트. 화면 네 모서리 + 중앙에 버튼을
    /// 두고, 클릭 시 어느 버튼이 맞았는지와 raw/보정 좌표, 버튼의 실제 화면 좌표, 그 편차(px)를
    /// 콘솔에 출력한다. 1단계 클릭 좌표 검증이 끝나면 이 파일과 씬의 인스턴스를 통째로 지운다.
    ///
    /// 씬 배선: 빈 GameObject에 이 컴포넌트만 붙이고 _crtMaterial에 CRT_PostProcess.mat을 물리면
    /// 된다(다른 필드는 자동으로 찾는다). GameObject의 활성/비활성 체크박스로 껐다 켤 수 있다 —
    /// OnEnable에서 UI를 조립하고 OnDisable에서 통째로 파괴한다.
    /// </summary>
    public sealed class DistortionClickTestHarness : MonoBehaviour
    {
        private static readonly int CurvatureId = Shader.PropertyToID("_Curvature");

        [SerializeField] private Material _crtMaterial;
        [SerializeField] private Vector2 _buttonSize = new Vector2(90, 90);

        private static readonly (string Label, float X, float Y)[] Points =
        {
            ("TL", 0.03f, 0.97f),
            ("TR", 0.97f, 0.97f),
            ("BL", 0.03f, 0.03f),
            ("BR", 0.97f, 0.03f),
            ("C", 0.5f, 0.5f),
        };

        private GameObject _root;

        private void OnEnable()
        {
            if (_crtMaterial == null)
            {
                Debug.LogError("[DistortionClickTestHarness] _crtMaterial이 비어 있습니다. " +
                                "CRT_PostProcess.mat을 인스펙터에 물려주세요.", this);
                return;
            }

            Build();
        }

        private void OnDisable()
        {
            if (_root != null) Destroy(_root);
            _root = null;
        }

        private void Build()
        {
            EnsureEventSystem();

            var uiCameraRig = FindFirstObjectByType<UiCompositorRig>();
            var uiCamera = uiCameraRig != null ? uiCameraRig.GetComponent<Camera>() : null;
            if (uiCamera == null)
                Debug.LogWarning("[DistortionClickTestHarness] UiCompositorRig(UI Camera)를 찾지 못했습니다. " +
                                  "Canvas가 CRT 합성 대상(RT_UI)이 아니라 화면에 직접 그려질 수 있습니다.", this);

            _root = new GameObject("DistortionClickTestCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(DistortionCorrectedGraphicRaycaster));
            _root.transform.SetParent(transform, false);

            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) SetLayerRecursively(_root, uiLayer);

            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 10f;

            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _root.GetComponent<DistortionCorrectedGraphicRaycaster>().SetCrtMaterial(_crtMaterial);

            var background = CreateMissPanel((RectTransform)_root.transform);

            foreach (var p in Points)
                CreateCornerButton((RectTransform)_root.transform, p.Label, p.X, p.Y);

            Debug.Log($"[DistortionClickTestHarness] 검증 캔버스 생성 완료. UI Camera={uiCamera}, " +
                      $"curvature={_crtMaterial.GetFloat(CurvatureId):F3}, screen={Screen.width}x{Screen.height}. " +
                      "배경(빗나간 클릭)도 로그를 남깁니다.");

            background.transform.SetAsFirstSibling();
        }

        private RectTransform CreateMissPanel(RectTransform parent)
        {
            var go = new GameObject("MissBackground", typeof(RectTransform), typeof(Image), typeof(MissClickLogger));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.02f);
            image.raycastTarget = true;

            go.GetComponent<MissClickLogger>().Init(_crtMaterial);
            return rt;
        }

        private void CreateCornerButton(RectTransform parent, string label, float anchorX, float anchorY)
        {
            var go = new GameObject($"TestButton_{label}", typeof(RectTransform), typeof(Image), typeof(CornerClickTarget));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = _buttonSize;
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.15f, 0.85f, 0.35f, 0.85f);
            image.raycastTarget = true;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.raycastTarget = false;

            go.GetComponent<CornerClickTarget>().Init(label, _crtMaterial);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem (DistortionClickTest)", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        /// <summary>CrtEffect.shader의 Barrel()과 DistortionCorrectedGraphicRaycaster.ApplyBarrel()과 동일한 공식.</summary>
        internal static Vector2 ApplyBarrel(Vector2 screenPos, float curvature)
        {
            var width = Mathf.Max(Screen.width, 1);
            var height = Mathf.Max(Screen.height, 1);

            var uv = new Vector2(screenPos.x / width, screenPos.y / height);
            var cc = uv - new Vector2(0.5f, 0.5f);
            var d = Vector2.Dot(cc, cc);
            var corrected = uv + cc * d * curvature * 1.4f;

            return new Vector2(corrected.x * width, corrected.y * height);
        }

        /// <summary>모서리/중앙 버튼. 맞았을 때 raw/보정 좌표, 버튼 실제 화면 좌표, 편차(px)를 로그로 남긴다.</summary>
        private sealed class CornerClickTarget : MonoBehaviour, IPointerClickHandler
        {
            private string _label;
            private Material _crtMaterial;

            public void Init(string label, Material crtMaterial)
            {
                _label = label;
                _crtMaterial = crtMaterial;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                var curvature = _crtMaterial != null ? _crtMaterial.GetFloat(CurvatureId) : 0f;
                var raw = eventData.position;
                var corrected = ApplyBarrel(raw, curvature);

                var rt = (RectTransform)transform;
                var camera = eventData.pressEventCamera;
                var buttonScreenPos = RectTransformUtility.WorldToScreenPoint(camera, rt.position);
                var deviation = Vector2.Distance(corrected, buttonScreenPos);

                Debug.Log($"[DistortionClickTest] HIT={_label}  raw={raw}  corrected={corrected}  " +
                          $"buttonScreenPos={buttonScreenPos}  deviation={deviation:F1}px  curvature={curvature:F3}");
            }
        }

        /// <summary>버튼을 벗어난 클릭. 전부 빗나갔을 때도 raw/보정 좌표를 볼 수 있게 배경에 붙인다.</summary>
        private sealed class MissClickLogger : MonoBehaviour, IPointerClickHandler
        {
            private Material _crtMaterial;

            public void Init(Material crtMaterial) => _crtMaterial = crtMaterial;

            public void OnPointerClick(PointerEventData eventData)
            {
                var curvature = _crtMaterial != null ? _crtMaterial.GetFloat(CurvatureId) : 0f;
                var raw = eventData.position;
                var corrected = ApplyBarrel(raw, curvature);

                Debug.Log($"[DistortionClickTest] MISS (버튼 없음)  raw={raw}  corrected={corrected}  curvature={curvature:F3}");
            }
        }
    }
}
