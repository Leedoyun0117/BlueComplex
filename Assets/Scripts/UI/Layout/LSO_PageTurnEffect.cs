using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 오른쪽 페이지가 책등을 축으로 넘어가는 연출의 살림살이 — 넘어가기 직전의 페이지를 그림 한 장으로 뜨고,
    /// 그 그림을 <c>LSO/PageTurn</c> 셰이더로 접어 넘긴다. 실제 접는 계산은 전부 셰이더가 한다.
    ///
    /// 왜 그림으로 뜨는가: uGUI는 <see cref="Image"/>·TMP 글자가 저마다 따로 메시를 굽고, 자식이 부모의 정점 변형을
    /// 물려받지 않는다. 페이지 배경에만 셰이더를 걸면 위에 얹힌 글자는 평평하게 남는다. 통째로 한 장으로 떠야
    /// 내용 종류와 무관하게 같이 휜다.
    ///
    /// 어디서 뜨는가: 이 프로젝트의 UI는 이미 UI 카메라가 RT_UI에 그리고 CRT 셰이더가 합성하는 구조다
    /// (UiCompositorRig 참고). 그래서 카메라를 새로 만들 필요 없이, 이미 그려진 RT_UI에서 페이지 사각형만
    /// 잘라 오면 된다. 다만 "이 프레임의 페이지가 다 그려진 뒤"여야 해서 호출 쪽이 프레임 끝까지 기다려야 한다.
    ///
    /// 덮는 범위는 오른쪽 페이지뿐이다 — 넘김이 왼쪽 페이지나 책 바깥으로 새지 않는다. 그래서 페이지는
    /// 책등을 축으로 90도까지만 돌고(그 너머는 이 사각형 밖이라 잘린다), 대신 끝에서 서서히 지워진다.
    /// </summary>
    public sealed class LSO_PageTurnEffect : MonoBehaviour
    {
        private const string ShaderName = "LSO/PageTurn";

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        private readonly Vector3[] _corners = new Vector3[4];

        private RawImage _image;
        private Material _material;
        private RenderTexture _snapshot;

        /// <summary>오른쪽 페이지를 덮는 넘김용 판을 만든다. 실패하면 null — 호출 쪽이 연출 없이 넘어가면 된다.</summary>
        public static LSO_PageTurnEffect Create(RectTransform page)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[LSO_PageTurnEffect] 셰이더 '{ShaderName}'를 못 찾았다. " +
                    "빌드에서는 Project Settings > Graphics > Always Included Shaders에 넣어야 살아남는다.");
                return null;
            }

            // 지원되지 않는 셰이더를 그대로 쓰면 유니티가 아무 말 없이 핑크(에러 셰이더)로 칠한다 —
            // 조용히 깨지느니 연출을 끄고 이유를 남긴다.
            if (!shader.isSupported)
            {
                Debug.LogError($"[LSO_PageTurnEffect] 셰이더 '{ShaderName}'가 이 환경에서 지원되지 않는다(핑크로 나오는 원인). " +
                    "연출 없이 탭만 바뀐다.");
                return null;
            }

            var go = new GameObject("Page Turn", typeof(RectTransform), typeof(RawImage))
            {
                layer = page.gameObject.layer
            };
            var rect = (RectTransform)go.transform;
            rect.SetParent(page, false); // 오른쪽 페이지에 정확히 겹친다 — 넘김이 이 페이지 밖으로 나가지 않는다.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling(); // 페이지 내용 위에 얹혀야 넘어가는 장이 그 위를 지나간다.

            var effect = go.AddComponent<LSO_PageTurnEffect>();
            effect._image = go.GetComponent<RawImage>();
            effect._image.raycastTarget = false; // 연출 중에도 클릭을 가로채지 않는다.
            effect._material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            effect._image.material = effect._material;
            go.SetActive(false);
            return effect;
        }

        /// <summary>
        /// 지금 화면에 그려져 있는 <paramref name="page"/>를 그림으로 떠서 넘길 준비를 한다.
        /// 반드시 그 페이지가 이번 프레임에 다 그려진 뒤(프레임 끝)에 부를 것 — 아니면 이전 프레임이나 빈 화면이 떠진다.
        /// </summary>
        /// <returns>뜨지 못하면 false. 이때는 연출을 건너뛰고 바로 갈아 끼우면 된다.</returns>
        public bool TryCapture(RectTransform page, Canvas canvas)
        {
            var camera = canvas != null ? canvas.worldCamera : null;
            var source = camera != null ? camera.targetTexture : null;
            if (source == null)
            {
                // Screen Space - Overlay이거나 UI 카메라 배선이 빠진 상태. 떠올 원본이 없다.
                Debug.LogWarning("[LSO_PageTurnEffect] UI 카메라의 targetTexture(RT_UI)가 없어 페이지를 뜰 수 없다. " +
                    "넘김 연출 없이 바로 바뀐다. (UICompositorSetupTool 배선 확인)", this);
                return false;
            }

            page.GetWorldCorners(_corners);
            var min = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]); // 좌하단
            var max = RectTransformUtility.WorldToScreenPoint(camera, _corners[2]); // 우상단

            var width = Mathf.RoundToInt(Mathf.Abs(max.x - min.x));
            var height = Mathf.RoundToInt(Mathf.Abs(max.y - min.y));
            if (width < 2 || height < 2) return false;

            EnsureSnapshot(width, height);

            // Blit의 scale/offset은 원본의 정규화 UV다. WorldToScreenPoint가 화면 픽셀을 주므로 화면 크기로 나눈다
            // (RT_UI는 UiCompositorRig가 화면 해상도에 맞춰 두지만, 여기서는 그 가정에 기대지 않는다).
            var screen = new Vector2(Screen.width, Screen.height);
            var scale = new Vector2((max.x - min.x) / screen.x, (max.y - min.y) / screen.y);
            var offset = new Vector2(min.x / screen.x, min.y / screen.y);
            Graphics.Blit(source, _snapshot, scale, offset);

            _image.texture = _snapshot;
            SetProgress(0f);
            gameObject.SetActive(true);
            return true;
        }

        /// <summary>0 = 펼쳐진 그대로, 1 = 왼쪽 페이지 위로 다 넘어감.</summary>
        public void SetProgress(float progress) => _material.SetFloat(ProgressId, progress);

        public void Hide()
        {
            gameObject.SetActive(false);
            _image.texture = null;
        }

        private void EnsureSnapshot(int width, int height)
        {
            if (_snapshot != null && _snapshot.width == width && _snapshot.height == height) return;

            ReleaseSnapshot();
            _snapshot = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "LSO_PageTurnSnapshot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            _snapshot.Create();
        }

        private void ReleaseSnapshot()
        {
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
