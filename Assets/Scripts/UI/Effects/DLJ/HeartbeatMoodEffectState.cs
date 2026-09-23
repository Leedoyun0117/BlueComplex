using System;
using BlueComplex.Core.Stability;

namespace BlueComplex.UI.Effects.DLJ
{
    /// <summary>DLJ 연출 전용 상태/시간. Unity 프레임과 분리해 진입 효과 종료·복귀·재진입을 검증한다.</summary>
    public sealed class HeartbeatMoodEffectState
    {
        private enum Mood { Stable, Depressed, Excited }
        private Mood _mood;
        private float _fromDepressed;
        private float _fromExcited;
        private float _toDepressed;
        private float _toExcited;
        private float _elapsed;

        public float TransitionSeconds { get; set; } = 0.65f;
        public float NormalStateStrength { get; set; } = 0.75f;
        public float GlitchSeconds { get; set; } = 2f;
        public float ChromaticSeconds { get; set; } = 3.5f;
        public float ChromaticFadeSeconds { get; set; } = 1f;
        public float Depressed { get; private set; }
        public float Excited { get; private set; }
        public float GlitchRemaining { get; private set; }
        public float ChromaticRemaining { get; private set; }
        public float ChromaticBurst
        {
            get
            {
                if (ChromaticRemaining <= 0f) return 0f;
                var fade = Math.Min(ChromaticSeconds, ChromaticFadeSeconds);
                var t = fade <= 0f ? 1f : Math.Clamp(ChromaticRemaining / fade, 0f, 1f);
                return t * t * (3f - 2f * t);
            }
        }
        public int DisplayedHeartbeat { get; private set; } = 80;

        public void Show(int value, HeartbeatZone zone, bool snap)
        {
            value = Math.Clamp(value, 0, 200);
            DisplayedHeartbeat = value;
            var state = zone.StateOf(value);
            var next = state switch
            {
                HeartbeatState.Depressed or HeartbeatState.VeryDepressed => Mood.Depressed,
                HeartbeatState.Excited or HeartbeatState.VeryExcited => Mood.Excited,
                HeartbeatState.Fatal => value < 80 ? Mood.Depressed : Mood.Excited,
                _ => Mood.Stable
            };
            var strong = state is HeartbeatState.VeryDepressed or HeartbeatState.VeryExcited or HeartbeatState.Fatal;
            var strength = strong ? 1f : Math.Clamp(NormalStateStrength, 0f, 1f);
            _fromDepressed = Depressed;
            _fromExcited = Excited;
            _toDepressed = next == Mood.Depressed ? strength : 0f;
            _toExcited = next == Mood.Excited ? strength : 0f;
            _elapsed = 0f;
            // 흥분 -> 매우 흥분은 같은 계열. 기존 타이머를 연장하지 않는다.
            if (snap || next != Mood.Excited)
                GlitchRemaining = ChromaticRemaining = 0f;
            else if (_mood != Mood.Excited)
            {
                GlitchRemaining = Math.Max(0f, GlitchSeconds);
                ChromaticRemaining = Math.Max(0f, ChromaticSeconds);
            }
            _mood = next;
            if (snap)
            {
                Depressed = _fromDepressed = _toDepressed;
                Excited = _fromExcited = _toExcited;
            }
        }

        public void Advance(float unscaledDeltaTime)
        {
            var dt = Math.Max(0f, unscaledDeltaTime);
            _elapsed += dt;
            var t = TransitionSeconds <= 0f ? 1f : Math.Clamp(_elapsed / TransitionSeconds, 0f, 1f);
            var eased = t * t * (3f - 2f * t);
            Depressed = _fromDepressed + (_toDepressed - _fromDepressed) * eased;
            Excited = _fromExcited + (_toExcited - _fromExcited) * eased;
            GlitchRemaining = Math.Max(0f, GlitchRemaining - dt);
            ChromaticRemaining = Math.Max(0f, ChromaticRemaining - dt);
        }

        public void Reset()
        {
            _mood = Mood.Stable;
            Depressed = Excited = GlitchRemaining = ChromaticRemaining = 0f;
            _fromDepressed = _fromExcited = _toDepressed = _toExcited = _elapsed = 0f;
            DisplayedHeartbeat = 80;
        }
    }
}
