using System;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 0~200 심박수. 값을 유지하는 게임이 아니라 시작값(기본 80) 주변의 안정 구간을 지키는 게임이므로,
    /// 수치 자체는 여기서 관리하고 구간 판정은 <see cref="HeartbeatZone"/>에 위임한다.
    /// </summary>
    public sealed class Heartbeat
    {
        public const int MinValue = 0;
        public const int MaxValue = 200;
        public const int DefaultStartValue = 80;

        public int Value { get; private set; }
        public int StartValue { get; }

        public event Action<int, int> Changed; // (from, to)

        public Heartbeat(int startValue = DefaultStartValue)
        {
            if (startValue < MinValue || startValue > MaxValue)
                throw new ArgumentOutOfRangeException(nameof(startValue));

            StartValue = startValue;
            Value = startValue;
        }

        public void Change(int delta)
        {
            if (delta == 0) return;
            var from = Value;
            Value = Math.Clamp(Value + delta, MinValue, MaxValue);
            if (from != Value) Changed?.Invoke(from, Value);
        }

        public void Reset() => Value = StartValue;
    }
}
