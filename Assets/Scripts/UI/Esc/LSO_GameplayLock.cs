using BlueComplex.UI.Bootstrap;
using UnityEngine;

namespace UI.Esc
{
    /// <summary>
    /// 전체 화면을 덮는 창이 떠 있는 동안 게임 쪽을 멈춰 두는 잠금. 시간(Time.timeScale)과
    /// 게임 입력(<see cref="StageBootstrapper.InputBlocked"/>) 두 가지를 같이 쥔다.
    ///
    /// 핵심은 "풀 때 이전 상태로 되돌린다"는 것이다. 무조건 timeScale=1, InputBlocked=false 로 풀면
    /// 이미 잠가 둔 쪽(대사 재생 중인 StageBootstrapper 등)의 잠금까지 같이 풀어 버린다.
    /// 그래서 잠글 때의 값을 기억했다가 그대로 복원한다.
    ///
    /// 잡지 않은 상태에서 풀거나 두 번 잡아도 아무 일이 없다 — OnDestroy 처럼 상태를 확신할 수 없는
    /// 자리에서 그냥 불러도 안전해야 한다.
    /// </summary>
    public sealed class LSO_GameplayLock
    {
        private readonly bool _stopTime;
        private readonly StageBootstrapper _bootstrapper;

        private bool _held;
        private bool _previousInputBlocked;
        private float _previousTimeScale = 1f;

        public bool IsHeld => _held;

        /// <param name="stopTime">시간까지 멈출지. 끄면 입력만 잠근다.</param>
        public LSO_GameplayLock(bool stopTime)
        {
            _stopTime = stopTime;
            // 씬에 프로토타입 스테이지가 없을 수도 있다(타이틀 등) — 없으면 시간만 다룬다.
            _bootstrapper = Object.FindFirstObjectByType<StageBootstrapper>();
        }

        public void Acquire()
        {
            if (_held) return;
            _held = true;

            if (_bootstrapper != null)
            {
                _previousInputBlocked = _bootstrapper.InputBlocked;
                _bootstrapper.InputBlocked = true;
            }

            if (_stopTime)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
        }

        public void Release()
        {
            if (!_held) return;
            _held = false;

            if (_bootstrapper != null) _bootstrapper.InputBlocked = _previousInputBlocked;
            if (_stopTime) Time.timeScale = _previousTimeScale;
        }
    }
}
