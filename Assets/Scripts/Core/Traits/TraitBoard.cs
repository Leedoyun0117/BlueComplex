using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Traits
{
    public enum TraitType
    {
        /// <summary>예민 — 인디케이터 이동 감도 3배.</summary>
        Sensitive
    }

    public sealed class TraitInstance
    {
        public TraitType Type { get; }
        public int RemainingTurns { get; private set; }
        public bool IsExpired => RemainingTurns <= 0;

        public TraitInstance(TraitType type, int duration)
        {
            Type = type;
            RemainingTurns = duration;
        }

        public void Tick() => RemainingTurns--;
    }

    public sealed class TraitBoard
    {
        private readonly List<TraitInstance> _traits = new();

        public IReadOnlyList<TraitInstance> Traits => _traits;

        public event Action<TraitInstance> Granted;
        public event Action<TraitInstance> Expired;

        public int SensitivityMultiplier => Has(TraitType.Sensitive) ? 3 : 1;

        public bool Has(TraitType type) => _traits.Any(t => t.Type == type);

        public void Grant(TraitType type, int duration)
        {
            var existing = _traits.FirstOrDefault(t => t.Type == type);
            if (existing != null) _traits.Remove(existing);

            var trait = new TraitInstance(type, duration);
            _traits.Add(trait);
            Granted?.Invoke(trait);
        }

        public void TickDurations()
        {
            foreach (var trait in _traits) trait.Tick();

            foreach (var expired in _traits.Where(t => t.IsExpired).ToList())
            {
                _traits.Remove(expired);
                Expired?.Invoke(expired);
            }
        }
    }
}
