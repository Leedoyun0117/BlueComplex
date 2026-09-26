using UnityEngine;

namespace BlueComplex.UI.Rendering
{
    /// <summary>
    /// 무드 화면 효과(DLJ 침체 파랑/흥분 핑크/글리치)를 받지 않는 UI(감정 태그 칩)를 위한 별도 렌더 레이어.
    ///
    /// UI 카메라는 UI 레이어(5)만 RT_UI에 그리고, 무드 셰이더가 그 RT_UI 위에 색조·글리치를 입힌다. 태그 칩만 <see cref="Layer"/>에 두면
    /// 평소의 UI 카메라 렌더에는 안 들어간다. 이 컴포넌트가 매 프레임 그 UI 카메라를 컬링 마스크만 <see cref="Layer"/>로, 타깃만 <see cref="Texture"/>로 바꿔 한 번 더 그린다
    /// (스크린스페이스-카메라 캔버스는 자기 <c>worldCamera</c>로만 그려져서 별도 카메라를 복제해서는 안 그려진다).
    /// 무드 셰이더는 이 텍스처를 색조/글리치 <b>뒤에</b> 얹어서 태그만 원래 색으로 남긴다(<c>_TagTex</c>).
    ///
    /// 무드 컨트롤러가 셰이더를 잡고 있는 동안에만 존재한다(<see cref="Current"/>) — 없으면 칩은 예전처럼 UI 레이어에 그려져 RT_UI에 합쳐진다.
    /// 그래서 이 텍스처를 합성해 줄 셰이더가 없는 씬에서 칩이 사라질 일이 없다.
    /// </summary>
    public sealed class UiTagLayer : MonoBehaviour
    {
        /// <summary>TagManager에 "UITag"로 이름 붙인 레이어. UI(5) 바로 다음 빈 칸.</summary>
        public const int Layer = 6;

        public const int Mask = 1 << Layer;

        /// <summary>지금 살아 있는 태그 레이어. 무드 컨트롤러가 켜져 있는 동안만 non-null.</summary>
        public static UiTagLayer Current { get; private set; }

        private Camera _uiCamera;
        private RenderTexture _texture;

        /// <summary>태그만 그린 텍스처. 무드 셰이더의 <c>_TagTex</c>에 물린다.</summary>
        public RenderTexture Texture => _texture;

        /// <summary>UI 카메라(targetTexture가 <paramref name="uiTexture"/>인 카메라)를 찾아 태그 레이어를 만든다. 못 찾으면 null — 호출자는 분리 없이 예전 그대로 둔다.</summary>
        public static UiTagLayer Create(RenderTexture uiTexture)
        {
            if (uiTexture == null) return null;

            Camera uiCamera = null;
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (camera.targetTexture == uiTexture) { uiCamera = camera; break; }
            if (uiCamera == null) return null;

            var go = new GameObject("UI Tag Layer");
            go.transform.SetParent(uiCamera.transform, false);
            var layer = go.AddComponent<UiTagLayer>();
            layer._uiCamera = uiCamera;
            layer._texture = new RenderTexture(uiTexture.descriptor) { name = "RT_UITag", hideFlags = HideFlags.HideAndDontSave };
            layer._texture.Create();

            Current = layer;
            layer.HideTagLayerFromOtherCameras();
            return layer;
        }

        private void LateUpdate()
        {
            if (_uiCamera == null || _texture == null) return;

            // UiCompositorRig가 RT_UI를 화면 크기에 맞춰 다시 만들면 같은 크기로 따라간다.
            var source = _uiCamera.targetTexture;
            if (source == null) return;
            if (_texture.width != source.width || _texture.height != source.height)
            {
                if (_texture.IsCreated()) _texture.Release();
                _texture.width = source.width;
                _texture.height = source.height;
                _texture.Create();
            }

            // 같은 카메라로 태그 레이어만 한 번 더. 끝나면 원래 마스크/타깃으로 되돌려 이어지는 자동 렌더(RT_UI)가 그대로 돌게 한다.
            var mask = _uiCamera.cullingMask;
            _uiCamera.cullingMask = Mask;
            _uiCamera.targetTexture = _texture;
            _uiCamera.Render();
            _uiCamera.targetTexture = source;
            _uiCamera.cullingMask = mask;
        }

        /// <summary>UI 레이어를 안 그리는 카메라(주 카메라)는 이 레이어도 그리면 안 된다 — 안 그러면 캔버스가 3D 씬 안에 떠 보일 수 있다.</summary>
        private void HideTagLayerFromOtherCameras()
        {
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera == _uiCamera || (camera.cullingMask & Mask) == 0) continue;
                camera.cullingMask &= ~Mask;
            }
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
