using System;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 현재 심박수 구간의 검열 수준을 노출한다. 상태 기억 없이 구간에 완전히 종속되므로,
    /// 구간을 벗어나는 즉시(다음 Heartbeat.Changed) 수준이 바뀐다.
    /// </summary>
    public sealed class CensorshipState
    {
        private readonly HeartbeatZone _zone;

        public CensorshipLevel Level { get; private set; }

        public event Action<CensorshipLevel> LevelChanged;

        public CensorshipState(Heartbeat heartbeat, HeartbeatZone zone)
        {
            if (heartbeat == null) throw new ArgumentNullException(nameof(heartbeat));
            _zone = zone ?? throw new ArgumentNullException(nameof(zone));

            Level = _zone.CensorshipOf(heartbeat.Value);
            heartbeat.Changed += OnHeartbeatChanged;
        }

        private void OnHeartbeatChanged(int from, int to) => SetLevel(_zone.CensorshipOf(to));

        private void SetLevel(CensorshipLevel level)
        {
            if (Level == level) return;
            Level = level;
            LevelChanged?.Invoke(level);
        }
    }
}
