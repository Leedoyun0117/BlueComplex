using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Stability
{
    public enum HeartbeatState
    {
        Fatal,
        VeryDepressed,
        Depressed,
        Stable,
        Excited,
        VeryExcited
    }

    /// <summary>단서 정보를 얼마나 가릴지의 수준. 무엇을 가릴지는 UI(표시 계층)의 책임이다.</summary>
    public enum CensorshipLevel
    {
        None,
        Partial,
        Full
    }

    /// <summary>
    /// 심박수 구간 정의와 판정을 담당한다. 경계값·검열 수준·컴플렉스 발현 확률은 전부
    /// 생성자로 주입 가능하며, 기본값은 기획표를 그대로 옮긴 것이다.
    /// </summary>
    public sealed class HeartbeatZone
    {
        /// <summary>구간 하나의 정의. Min/Max는 양 끝을 포함한다.</summary>
        public readonly struct Definition
        {
            public int Min { get; }
            public int Max { get; }
            public HeartbeatState State { get; }
            public CensorshipLevel Censorship { get; }
            public double ComplexSpawnChance { get; }

            public Definition(int min, int max, HeartbeatState state, CensorshipLevel censorship, double complexSpawnChance)
            {
                Min = min;
                Max = max;
                State = state;
                Censorship = censorship;
                ComplexSpawnChance = complexSpawnChance;
            }

            public bool Contains(int value) => value >= Min && value <= Max;
        }

        public static IReadOnlyList<Definition> DefaultBoundaries { get; } = new[]
        {
            new Definition(0, 9, HeartbeatState.Fatal, CensorshipLevel.Full, 0.0),
            new Definition(10, 39, HeartbeatState.VeryDepressed, CensorshipLevel.Full, 0.5),
            new Definition(40, 70, HeartbeatState.Depressed, CensorshipLevel.Partial, 0.3),
            new Definition(71, 100, HeartbeatState.Stable, CensorshipLevel.None, 0.0),
            new Definition(101, 150, HeartbeatState.Excited, CensorshipLevel.Partial, 0.3),
            new Definition(151, 190, HeartbeatState.VeryExcited, CensorshipLevel.Full, 0.5),
            new Definition(191, 200, HeartbeatState.Fatal, CensorshipLevel.Full, 0.0)
        };

        private readonly IReadOnlyList<Definition> _boundaries;

        /// <summary>Fatal이 아닌 구간들 중 가장 작은 Min. 키 구역 등 생존 가능 범위가 필요한 곳에서 쓴다.</summary>
        public int SurvivableMin { get; }

        /// <summary>Fatal이 아닌 구간들 중 가장 큰 Max.</summary>
        public int SurvivableMax { get; }

        public HeartbeatZone(IReadOnlyList<Definition> boundaries = null)
        {
            _boundaries = boundaries ?? DefaultBoundaries;

            var survivable = _boundaries.Where(b => b.State != HeartbeatState.Fatal).ToList();
            if (survivable.Count == 0)
                throw new ArgumentException("Fatal이 아닌 구간이 최소 하나는 있어야 합니다.", nameof(boundaries));

            SurvivableMin = survivable.Min(b => b.Min);
            SurvivableMax = survivable.Max(b => b.Max);
        }

        public Definition Resolve(int heartbeatValue)
        {
            foreach (var boundary in _boundaries)
                if (boundary.Contains(heartbeatValue))
                    return boundary;

            throw new ArgumentOutOfRangeException(nameof(heartbeatValue),
                $"심박수 {heartbeatValue}에 해당하는 구간이 정의되어 있지 않습니다.");
        }

        public HeartbeatState StateOf(int heartbeatValue) => Resolve(heartbeatValue).State;
        public CensorshipLevel CensorshipOf(int heartbeatValue) => Resolve(heartbeatValue).Censorship;
        public double ComplexSpawnChanceOf(int heartbeatValue) => Resolve(heartbeatValue).ComplexSpawnChance;
        public bool IsFatal(int heartbeatValue) => StateOf(heartbeatValue) == HeartbeatState.Fatal;
    }
}
