using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueComplex.UI.Menu
{
    /// <summary>
    /// 마우스 위치에 따라 UI 레이어들을 서로 다른 폭으로 밀어 시차(패럴랙스)를 만든다. 메인 화면 배경용.
    ///
    /// 화면 중앙을 0, 가장자리를 ±1로 보고 그 값에 레이어별 <see cref="Layer.strength"/>를 곱해 민다.
    /// 앞에 있어 보여야 할 레이어에 큰 값을, 멀어 보여야 할 레이어에 작은 값을 준다.
    ///
    /// 월드 배경용인 <c>BackgroundLayerRig</c>와는 다른 것이다 — 그쪽은 레이어를 Z로 벌려 카메라 원근으로
    /// 시차를 만들고, 여기는 캔버스 위에서 anchoredPosition을 직접 민다(UI는 원근이 없다).
    ///
    /// <b>이 컴포넌트가 레이어의 anchoredPosition을 쥔다.</b> 같은 레이어를 다른 데서도 움직이면 서로 덮어써
    /// 떨린다. 켜질 때의 위치를 원점으로 기억했다가 꺼질 때 되돌려 놓는다.
    /// </summary>
    public sealed class LSO_MouseParallax : MonoBehaviour
    {
        [Serializable]
        public struct Layer
        {
            [Tooltip("밀릴 UI 레이어.")]
            public RectTransform target;

            [Tooltip("화면 가장자리까지 갔을 때 밀리는 거리(px). 앞쪽 레이어일수록 크게.")]
            public float strength;

            [Tooltip("반대 방향으로 민다. 창밖 풍경처럼 마우스를 따라와야 하는 레이어에 쓴다.")]
            public bool invert;
        }

        [SerializeField] private Layer[] layers = Array.Empty<Layer>();

        [Tooltip("따라붙는 속도. 클수록 빠르게 따라온다. 0이면 즉시(마우스에 딱 붙어 딱딱해 보인다).")]
        [SerializeField] [Min(0f)] private float smoothing = 8f;

        [Tooltip("최대로 밀리는 지점. 1이면 화면 가장자리, 0.6이면 화면의 60% 지점에서 이미 최대로 밀린다.")]
        [SerializeField] [Range(0.1f, 1f)] private float edge = 0.8f;

        [Tooltip("메뉴가 시간을 멈춘 상태에서도 움직여야 하면 켠다(보통 켜 둔다).")]
        [SerializeField] private bool useUnscaledTime = true;

        private Vector2[] _origins;
        private Vector2 _offset;

        private void Awake()
        {
            _origins = new Vector2[layers.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i].target == null)
                {
                    Debug.LogWarning($"[LSO_MouseParallax] layers[{i}].target이 비어 있다. 이 레이어는 움직이지 않는다.", this);
                    continue;
                }

                _origins[i] = layers[i].target.anchoredPosition;
            }
        }

        private void OnEnable() => _offset = Vector2.zero;

        private void OnDisable()
        {
            // 꺼질 때 제자리로 — 밀린 채로 남으면 다음에 켤 때 원점이 어긋난 것처럼 보인다.
            _offset = Vector2.zero;
            ApplyOffset();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return; // 마우스가 없는 환경(패드 전용 등)에서는 그냥 가만히 있는다.

            var target = ReadNormalizedMouse(mouse);

            // 프레임레이트와 무관한 감쇠. Lerp에 deltaTime을 그대로 넣으면 프레임이 빠를수록 더 느리게 따라온다.
            var delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var k = smoothing > 0f ? 1f - Mathf.Exp(-smoothing * delta) : 1f;
            _offset = Vector2.Lerp(_offset, target, k);

            ApplyOffset();
        }

        /// <summary>화면 중앙 0, 가장자리 ±1. CRT 배럴 왜곡 보정은 하지 않는다 —
        /// 손에 쥔 마우스의 물리적 위치를 그대로 쓰는 게 맞고, 이 연출은 정확한 좌표가 필요 없다.</summary>
        private Vector2 ReadNormalizedMouse(Mouse mouse)
        {
            var position = mouse.position.ReadValue();
            if (Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;

            var normalized = new Vector2(
                position.x / Screen.width * 2f - 1f,
                position.y / Screen.height * 2f - 1f);

            // edge보다 바깥은 전부 최대치로 본다. 커서가 창 밖으로 나가도 과하게 밀리지 않는다.
            return Vector2.ClampMagnitude(normalized / edge, 1f);
        }

        private void ApplyOffset()
        {
            for (var i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (layer.target == null) continue;

                var amount = layer.invert ? -layer.strength : layer.strength;
                layer.target.anchoredPosition = _origins[i] + _offset * amount;
            }
        }
    }
}
