using System;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 침체 ── 안정 ── 흥분 축. 0~(Slots-1) 칸이며 시작은 중앙.
    /// 값을 올리는 게임이 아니라 중앙을 유지하는 게임이므로, 중앙으로부터의 거리가 핵심 지표다.
    /// </summary>
    public sealed class StabilityIndicator
    {
        public int Slots { get; }
        public int Position { get; private set; }
        public int Center { get; }

        /// <summary>중앙에서 벗어난 칸 수. 컴플렉스 발현 확률의 입력값.</summary>
        public int DistanceFromCenter => Math.Abs(Position - Center);

        public event Action<int, int> Moved; // (from, to)

        public StabilityIndicator(int slots = 10)
        {
            if (slots < 2) throw new ArgumentOutOfRangeException(nameof(slots));
            Slots = slots;
            Center = slots / 2;
            Position = Center;
        }

        public void Move(int delta)
        {
            if (delta == 0) return;
            var from = Position;
            Position = Math.Clamp(Position + delta, 0, Slots - 1);
            if (from != Position) Moved?.Invoke(from, Position);
        }

        public void Reset() => Position = Center;
    }
}
