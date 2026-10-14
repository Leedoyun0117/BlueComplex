using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>한 컴플렉스가 단서를 해석한 결과 한 줄. UI 연출(순차 발광)에 그대로 쓴다.</summary>
    public readonly struct InterpretationStep
    {
        public ComplexInstance Complex { get; }
        public bool Triggered { get; }
        public TagSet Snapshot { get; }
        public IReadOnlyList<EmotionTag> ObservedEmotions { get; }
        public TimeTag ObservedTime { get; }
        public IReadOnlyList<PersonTag> ObservedPersons { get; }

        public InterpretationStep(ComplexInstance complex,
                                  bool triggered,
                                  TagSet snapshot,
                                  TimeTag observedTime,
                                  IReadOnlyList<PersonTag> observedPersons,
                                  IReadOnlyList<EmotionTag> observedEmotions)
        {
            Complex = complex;
            Triggered = triggered;
            Snapshot = snapshot;
            ObservedTime = observedTime;
            ObservedPersons = observedPersons;
            ObservedEmotions = observedEmotions;
        }
    }
}
