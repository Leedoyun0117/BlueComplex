using BlueComplex.UI.Background;
using UnityEngine;

namespace UI.Esc
{
    /// <summary>
    /// 설정창이 열려 있는 동안 배경을 꺼 둔다. 창 뿌리(<see cref="LSO_EscPanel"/>이 붙은 것)에 붙인다.
    ///
    /// 배경은 둘 중 하나다 — <see cref="LSO_StageBackgroundSpawner"/>가 깐 스테이지 프리팹이거나,
    /// 아직 프리팹으로 옮기지 않은 방의 <see cref="StageIdleBackground"/>다.
    /// 둘 다 씬에 미리 있지 않고 런타임에 생기므로 인스펙터에 물릴 수 없어 열 때마다 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LSO_EscPanel))]
    public sealed class LSO_EscHideWhileOpen : MonoBehaviour
    {
        private LSO_EscPanel _panel;
        private GameObject _background;
        private bool _isHiding;

        private void Awake() => _panel = GetComponent<LSO_EscPanel>();

        /// <summary>이벤트가 아니라 상태로 판단한다 — 연타나 중간 취소로 어긋나도 다음 프레임에 스스로 맞춰진다.</summary>
        private void LateUpdate()
        {
            if (_panel == null || _panel.IsOpen == _isHiding) return;

            _isHiding = _panel.IsOpen;
            if (_isHiding) Hide();
            else Restore();
        }

        private void OnDisable()
        {
            // 창이 열린 채로 이 컴포넌트가 꺼지면 배경이 사라진 상태로 남는다.
            if (_isHiding) Restore();
            _isHiding = false;
        }

        private void Hide()
        {
            _background = FindBackground();
            if (_background == null)
            {
                Debug.LogWarning("[LSO_EscHideWhileOpen] 배경을 찾지 못했다 — 그대로 비친다.", this);
                return;
            }

            _background.SetActive(false);
        }

        /// <summary>스포너가 깐 프리팹이 먼저다. 그게 없는 방만 StageIdleBackground를 본다.</summary>
        private GameObject FindBackground()
        {
            var spawner = FindFirstObjectByType<LSO_StageBackgroundSpawner>();
            if (spawner != null && spawner.Background != null) return spawner.Background;

            var idle = FindFirstObjectByType<StageIdleBackground>();
            return idle != null ? idle.gameObject : null;
        }

        private void Restore()
        {
            if (_background != null) _background.SetActive(true);
            _background = null;
        }
    }
}
