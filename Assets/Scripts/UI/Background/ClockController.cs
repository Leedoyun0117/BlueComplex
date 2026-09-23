using BlueComplex.Core.Stage;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 배경의 벽시계. 세션이 진행되는 동안 턴/쿼터와 무관하게 혼자 돈다 — 분침은 60초에 한 바퀴(360°)를 매 프레임 부드럽게,
    /// 시침은 분침이 한 바퀴 돌 때마다 시계판 한 칸(30°)씩 딱 넘어간다.
    ///
    /// 코어 이벤트도 Presenter 호출도 받지 않는다. 이 컴포넌트가 스스로 하는 일은 새 세션이 시작될 때 시작 시각으로
    /// 되돌리는 것(SessionBoundView.Render)뿐이다. 손이 그려진 아트 그대로의 자세가 시작 시각이다.
    /// 시침이 한 칸 넘어가는 순간에 ClockTick 소리가 난다.
    /// </summary>
    public sealed class ClockController : SessionBoundView
    {
        // 시침은 부드럽게 돌지 않는다 — 분침이 한 바퀴 돌 때마다 시계판의 한 칸(12칸 기준 30°)씩 딱 넘어간다.
        private const int HourNotchCount = 12;
        private const float HourNotchDegrees = 360f / HourNotchCount;

        [Header("손 (회전축이 시계 중심에 있는 피벗 Transform)")]
        [SerializeField] private Transform _minuteHand;
        [SerializeField] private Transform _hourHand;

        [Header("시간 흐름")]
        [Tooltip("분침이 한 바퀴(360°) 도는 데 걸리는 실제 시간(초).")]
        [SerializeField] private float _secondsPerMinuteHandLap = 60f;

        private Quaternion _minuteRest = Quaternion.identity;
        private Quaternion _hourRest = Quaternion.identity;
        private float _elapsedSeconds;
        private int _hourNotch;

        protected override void Awake()
        {
            // 아트에 그려진 손 자세를 시작 시각으로 기록한다. base.Awake 이후엔 Render가 불릴 수 있으므로 먼저 캐시한다.
            if (_minuteHand != null) _minuteRest = _minuteHand.localRotation;
            if (_hourHand != null) _hourRest = _hourHand.localRotation;
            base.Awake();
        }

        // 코어 이벤트는 구독하지 않는다(클래스 주석 참고).
        protected override void Subscribe(StageSession session) { }
        protected override void Unsubscribe(StageSession session) { }

        /// <summary>새 세션(스테이지 시작/재시작) — 시작 시각으로 되돌린다.</summary>
        protected override void Render()
        {
            _elapsedSeconds = 0f;
            _hourNotch = 0;
            Apply(playSound: false);
        }

        private void Update()
        {
            // 세션이 없으면(스테이지 시작 전) 아트 그대로의 자세로 멈춰 있다.
            if (Session == null) return;

            _elapsedSeconds += Time.deltaTime;
            Apply(playSound: true);
        }

        private void Apply(bool playSound)
        {
            var laps = _elapsedSeconds / Mathf.Max(0.01f, _secondsPerMinuteHandLap);

            // 카메라가 +Z를 바라볼 때 시계방향 = Z축 음의 회전. 분침은 절대 시간에서 매 프레임 계산한다(누적 오차 없음).
            if (_minuteHand != null)
                _minuteHand.localRotation = _minuteRest * Quaternion.Euler(0f, 0f, -laps * 360f);

            // 분침이 완주한 바퀴 수만큼만 칸을 넘긴다 — 분침이 12시를 지나는 그 순간 시침이 한 칸 딱 넘어간다.
            var notch = Mathf.FloorToInt(laps);
            if (_hourHand != null)
                _hourHand.localRotation = _hourRest * Quaternion.Euler(0f, 0f, -notch * HourNotchDegrees);

            // 칸이 넘어간 프레임에 한 번만 — 한 프레임에 여러 칸을 건너뛰어도(프레임 스파이크) 소리는 한 번이다.
            if (notch > _hourNotch && playSound) UiSoundHooks.Play(UiSoundCue.ClockTick);
            _hourNotch = notch;
        }
    }
}
