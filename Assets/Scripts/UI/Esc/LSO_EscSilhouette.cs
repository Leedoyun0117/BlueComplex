using UnityEngine;
using UnityEngine.UI;

namespace UI.Esc
{
    /// <summary>
    /// 설정창이 열릴 때, 창에 가려지는 UI의 실루엣을 창 뒤에 비춘다.
    /// 이 컴포넌트는 <see cref="RawImage"/>에 붙인다 — 그 RawImage가 실루엣이 그려질 판이다.
    /// 창 배경(어두운 막)보다 앞, 창 내용보다는 뒤에 두면 된다.
    ///
    /// ─── 왜 스냅샷인가 ─────────────────────────────────────────────────
    /// 창이 열리면 <see cref="LSO_GameplayLock"/>이 Time.timeScale을 0으로 만든다. 그러면 뒤쪽 UI의
    /// 트윈도, 셰이더의 _Time 기반 움직임도 멈춘다 — 멈춰 있는 그림이라 실시간으로 떠 와도 결과가 같다.
    /// 그래서 카메라를 새로 달지 않고 열리는 순간 한 번만 뜬다.
    /// 시간을 안 멈추는 설정으로 바꾸면 이 그림이 굳어 보인다. 그때는 UI 전용 레이어를 하나 더 파고
    /// 실루엣용 카메라가 그 레이어만 RT에 계속 그리게 해야 한다(셰이더는 그대로 쓸 수 있다).
    ///
    /// ─── 왜 프레임 끝을 기다리지 않는가 ────────────────────────────────
    /// 페이지 넘김(LSO_PageTurnEffect)은 "이번 프레임"을 떠야 해서 프레임 끝까지 기다렸지만, 여기는
    /// 반대로 <b>창이 나타나기 전</b>의 화면이 필요하다. 카메라는 Update 뒤에 그리므로, 열리는 순간의
    /// RT_UI에는 아직 지난 프레임(= 창이 없는 화면)이 들어 있다. 그대로 뜨면 된다.
    /// 프레임 끝까지 기다리면 그새 창이 번져 들어와 자기 자신이 실루엣에 찍힌다.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class LSO_EscSilhouette : MonoBehaviour
    {
        private const string ShaderName = "BlueComplex/LSO/ScreenSilhouette";

        [Tooltip("비워 두면 부모에서 찾는다.")]
        [SerializeField] private LSO_EscPanel panel;

        [Tooltip("실루엣 색과 진하기. 알파가 진하기다.")]
        [SerializeField] private Color silhouetteColor = new Color(1f, 1f, 1f, 0.18f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private RawImage _image;
        private Material _material;
        private RenderTexture _snapshot;
        private Canvas _canvas;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _image.raycastTarget = false; // 창 조작을 가로채지 않는다.

            var shader = Shader.Find(ShaderName);
            if (shader == null || !shader.isSupported)
            {
                // 조용히 핑크로 칠해지느니 실루엣만 끄고 이유를 남긴다.
                Debug.LogWarning($"[LSO_EscSilhouette] 셰이더 '{ShaderName}'를 쓸 수 없다. 실루엣 없이 동작한다. " +
                    "빌드에서는 Project Settings > Graphics > Always Included Shaders에 넣어야 한다.", this);
                enabled = false;
                return;
            }

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _material.SetColor(ColorId, silhouetteColor);
            _image.material = _material;
            _image.enabled = false;

            _canvas = GetComponentInParent<Canvas>();
            if (panel == null) panel = GetComponentInParent<LSO_EscPanel>();

            if (panel == null)
                Debug.LogWarning("[LSO_EscSilhouette] LSO_EscPanel을 찾지 못했다. 실루엣이 뜨지 않는다.", this);
        }

        private void OnEnable()
        {
            if (panel == null) return;
            panel.Opened += Capture;
            panel.Closed += Clear;
        }

        private void OnDisable()
        {
            if (panel == null) return;
            panel.Opened -= Capture;
            panel.Closed -= Clear;
        }

        private void OnDestroy()
        {
            ReleaseSnapshot();
            if (_material != null) Destroy(_material);
        }

        /// <summary>창이 나타나기 직전의 화면을 떠서 실루엣 판에 물린다.</summary>
        private void Capture()
        {
            var camera = _canvas != null ? _canvas.worldCamera : null;
            var source = camera != null ? camera.targetTexture : null;
            if (source == null)
            {
                // Screen Space - Overlay 이거나 UI 카메라 배선이 빠진 상태 — 떠올 원본이 없다.
                Debug.LogWarning("[LSO_EscSilhouette] UI 카메라의 targetTexture(RT_UI)가 없어 실루엣을 뜰 수 없다. " +
                    "(UICompositorSetupTool 배선 확인)", this);
                return;
            }

            EnsureSnapshot(source.width, source.height);
            Graphics.Blit(source, _snapshot);

            _image.texture = _snapshot;
            _image.enabled = true;
        }

        private void Clear() => _image.enabled = false;

        private void EnsureSnapshot(int width, int height)
        {
            if (_snapshot != null && _snapshot.width == width && _snapshot.height == height) return;

            ReleaseSnapshot();
            _snapshot = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "LSO_EscSilhouetteSnapshot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            _snapshot.Create();
        }

        private void ReleaseSnapshot()
        {
            if (_snapshot == null) return;

            if (_image != null) _image.texture = null;
            _snapshot.Release();
            Destroy(_snapshot);
            _snapshot = null;
        }
    }
}
