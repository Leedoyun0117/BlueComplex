using BlueComplex.Core.Stage;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 배경의 벽시계. 턴이 하나 끝날 때마다 시간이 흐른 만큼 분침/시침을 돌린다.
    ///
    /// 코어 이벤트(TurnResolved)를 직접 구독하지 않는다 — 다른 뷰들과 같은 규칙이다. TurnResolved는 연출보다 먼저
    /// 동기로 발동하므로 여기서 바로 돌리면 컴플렉스 발광/대사보다 시계가 먼저 움직여 순서가 뒤집힌다.
    /// 시계를 언제 돌릴지는 ITurnResultPresenter가 <see cref="AdvanceTo"/>를 부르는 시점이 쥔다.
    /// 이 컴포넌트가 스스로 하는 일은 새 세션이 시작될 때 시작 시각으로 되돌리는 것(SessionBoundView.Render)뿐이다.
    ///
    /// 시각은 "스테이지 시작 후 경과 분"의 절대값으로 계산한다(누적 증분이 아님) — 같은 턴 번호로 여러 번 불려도,
    /// 연출 도중 다음 호출이 와도 어긋나지 않는다. 분침은 시계방향으로 계속 누적 회전한다(0°로 되감기지 않는다).
    /// 손이 그려진 아트 그대로의 자세가 스테이지 시작 시각이다.
    /// </summary>
    public sealed class ClockController : SessionBoundView
    {
        private const float MinuteHandDegreesPerMinute = 6f;
        // 분침이 한 바퀴(360°) 도는 데 걸리는 경과 시간(분) = 360 / 6 = 60분.
        private const float MinutesPerMinuteHandLap = 360f / MinuteHandDegreesPerMinute;
        // 시침은 부드럽게 돌지 않는다 — 분침이 한 바퀴 돌 때마다 시계판의 한 칸(12칸 기준 30°)씩 딱 넘어간다.
        private const int HourNotchCount = 12;
        private const float HourNotchDegrees = 360f / HourNotchCount;

        [Header("손 (회전축이 시계 중심에 있는 피벗 Transform)")]
        [SerializeField] private Transform _minuteHand;
        [SerializeField] private Transform _hourHand;

        [Header("시간 흐름")]
        [Tooltip("한 턴에 흐르는 시간(분). 12턴 1스테이지 기준 15분이면 스테이지 하나에 3시간이 흐르고, " +
                 "분침은 턴마다 정확히 90°(12→3→6→9)씩 돌아 각 턴 끝에서 도트 격자에 딱 맞는다.")]
        [SerializeField] private float _minutesPerTurn = 15f;

        [Header("연출")]
        [Tooltip("턴 결과 연출(태그 상승 0.9초)보다 짧아야 다음 연출과 겹치지 않는다.")]
        [SerializeField] private float _tickDuration = 0.7f;
        [Tooltip("시계 바늘이 목표를 살짝 넘었다 돌아오는 정도. 0이면 오버슈트 없음.")]
        [SerializeField] private float _tickOvershoot = 0.6f;

        private Quaternion _minuteRest = Quaternion.identity;
        private Quaternion _hourRest = Quaternion.identity;
        private float _elapsedMinutes;
        private Tween _tween;

        protected override void Awake()
        {
            // 아트에 그려진 손 자세를 시작 시각으로 기록한다. base.Awake 이후엔 Render가 불릴 수 있으므로 먼저 캐시한다.
            if (_minuteHand != null) _minuteRest = _minuteHand.localRotation;
            if (_hourHand != null) _hourRest = _hourHand.localRotation;
            base.Awake();
        }

        private void OnDestroy() => _tween?.Kill();

        // 코어 이벤트는 구독하지 않는다(클래스 주석 참고).
        protected override void Subscribe(StageSession session) { }
        protected override void Unsubscribe(StageSession session) { }

        /// <summary>새 세션(스테이지 시작/재시작) — 애니메이션 없이 시작 시각으로 되돌린다.</summary>
        protected override void Render() => SetElapsedMinutes(0f, animate: false);

        /// <summary>turn번째 턴이 끝난 시각으로 시계를 옮긴다(스테이지 시작 후 turn × 턴당 분).
        /// 호출 시점은 Presenter가 쥔다. 재생 중인 트윈을 돌려주므로 필요하면 기다릴 수 있다.
        /// 시계 소리는 매 턴이 아니라 쿼터가 넘어가는 턴에서만 난다(기획서).</summary>
        public Tween AdvanceTo(int turn, bool animate = true)
        {
            if (animate && Session != null && Session.Runner.Schedule.IsQuarterEnd(turn))
                UiSoundHooks.Play(UiSoundCue.ClockTick);

            return SetElapsedMinutes(Mathf.Max(0, turn) * _minutesPerTurn, animate);
        }

        private Tween SetElapsedMinutes(float minutes, bool animate)
        {
            _tween?.Kill();
            _tween = null;

            if (!animate || _tickDuration <= 0f)
            {
                Apply(minutes);
                return null;
            }

            _tween = DOTween.To(() => _elapsedMinutes, Apply, minutes, _tickDuration)
                .SetEase(Ease.OutBack, _tickOvershoot)
                .SetLink(gameObject);
            return _tween;
        }

        private void Apply(float minutes)
        {
            _elapsedMinutes = minutes;

            // 카메라가 +Z를 바라볼 때 시계방향 = Z축 음의 회전.
            if (_minuteHand != null)
                _minuteHand.localRotation = _minuteRest * Quaternion.Euler(0f, 0f, -minutes * MinuteHandDegreesPerMinute);

            if (_hourHand != null)
            {
                // 분침이 완주한 바퀴 수만큼만 칸을 넘긴다 — 애니메이션 도중 분침이 12시를 지나는 그 순간 시침이 한 칸 딱 넘어간다.
                var laps = Mathf.Floor(minutes / MinutesPerMinuteHandLap);
                _hourHand.localRotation = _hourRest * Quaternion.Euler(0f, 0f, -laps * HourNotchDegrees);
            }
        }
    }
}
