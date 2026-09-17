using UnityEngine;

namespace BlueComplex.UI.Rendering
{
    /// <summary>
    /// UI 카메라의 targetTexture(RT_UI)를 화면 해상도에 맞춰 리사이즈한다.
    ///
    /// RT_UI는 이 스크립트가 런타임에 새로 만드는 게 아니라, UICompositorSetupTool이 미리 만들어
    /// UI 카메라에 고정 할당하고 CRT_PostProcess.mat(CrtEffect.shader)의 _UITex 슬롯에도 직접
    /// 물려 둔 에셋(Assets/Settings/UI/RT_UI.renderTexture)이다. 리사이즈는 같은 RenderTexture
    /// 객체를 Release()/Create()로 재구성할 뿐 참조 자체는 바뀌지 않으므로, 머티리얼이 들고 있는
    /// 텍스처 참조는 리사이즈 후에도 그대로 유효하다 — 전역 텍스처 재바인딩이 필요 없다.
    ///
    /// (이전엔 Shader.SetGlobalTexture로 매 프레임 전역 바인딩했지만, 프레임 디버거로 확인한 결과
    /// 그 전역 바인딩이 렌더 패스까지 도달하지 못하고 Unity 기본 더미 텍스처가 대신 샘플링되고
    /// 있었다. 머티리얼 직접 참조로 바꾸면서 이 컴포넌트의 역할은 리사이즈만 남았다.)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class UiCompositorRig : MonoBehaviour
    {
        [SerializeField] private Camera _uiCamera;

        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            if (_uiCamera == null) _uiCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            ResizeIfNeeded();
        }

        private void Update()
        {
            ResizeIfNeeded();
        }

        private void ResizeIfNeeded()
        {
            var rt = _uiCamera != null ? _uiCamera.targetTexture : null;
            if (rt == null)
            {
                Debug.LogError("[UiCompositorRig] UI Camera에 targetTexture(RT_UI 에셋)가 없습니다. " +
                                "BlueComplex/UI/Setup UI Compositor를 다시 실행하세요.", this);
                return;
            }

            var width = Mathf.Max(Screen.width, 1);
            var height = Mathf.Max(Screen.height, 1);
            if (width == _lastWidth && height == _lastHeight && rt.IsCreated()) return;

            if (rt.IsCreated()) rt.Release();
            rt.width = width;
            rt.height = height;
            rt.Create();

            _lastWidth = width;
            _lastHeight = height;
            Debug.Log($"[UiCompositorRig] RT_UI를 {width}x{height}로 (재)생성했습니다. " +
                      $"format={rt.format}, graphicsFormat={rt.graphicsFormat}, sRGB={rt.sRGB}.", this);
        }
    }
}
