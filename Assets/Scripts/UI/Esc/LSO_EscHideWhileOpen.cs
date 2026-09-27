using BlueComplex.UI.Background;
using UnityEngine;

namespace UI.Esc
{
    /// <summary>
    /// 설정창이 열려 있는 동안 스테이지 Idle 배경 오브젝트를 비활성화한다.
    /// 창 뿌리(<see cref="LSO_EscPanel"/>이 붙은 것)에 붙인다.
    ///
    /// 배경은 씬에 없다 — <see cref="StageIdleBackground"/>가 RuntimeInitializeOnLoadMethod로 스스로 만든다.
    /// 그래서 인스펙터에 물릴 수 없고 열 때마다 타입으로 찾는다.
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
            var background = FindFirstObjectByType<StageIdleBackground>();
            if (background == null)
            {
                Debug.LogWarning("[LSO_EscHideWhileOpen] StageIdleBackground를 찾지 못했다 — 배경이 그대로 남는다.", this);
                return;
            }

            _background = background.gameObject;
            _background.SetActive(false);
        }

        private void Restore()
        {
            if (_background != null) _background.SetActive(true);
            _background = null;
        }
    }
}
