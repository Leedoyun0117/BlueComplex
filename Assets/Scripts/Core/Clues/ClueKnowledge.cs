using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Save;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
    /// <summary>한 단서에 대해 지금까지 밝혀진 속성.</summary>
    public sealed class ClueKnowledge
    {
        public bool TimeRevealed { get; private set; }
        private readonly HashSet<PersonTag> _persons = new();
        private readonly HashSet<EmotionTag> _emotions = new();

        public IReadOnlyCollection<PersonTag> RevealedPersons => _persons;
        public IReadOnlyCollection<EmotionTag> RevealedEmotions => _emotions;

        /// <summary>새로 밝혀졌으면 true(이미 밝혀져 있었으면 false).</summary>
        public bool RevealTime()
        {
            var isNew = !TimeRevealed;
            TimeRevealed = true;
            return isNew;
        }

        public bool RevealPerson(PersonTag person) => _persons.Add(person);
        public bool RevealEmotion(EmotionTag emotion) => _emotions.Add(emotion);

        public bool IsPersonRevealed(PersonTag person) => _persons.Contains(person);
        public bool IsEmotionRevealed(EmotionTag emotion) => _emotions.Contains(emotion);
    }
}
