using System;
using System.Collections.Generic;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
    /// <summary>저작 데이터. 변하지 않는 원본 정의.</summary>
    public sealed class ClueDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Story { get; }
        public TimeTag Time { get; }
        public IReadOnlyList<PersonTag> Persons { get; }
        public IReadOnlyList<EmotionTag> Emotions { get; }

        public ClueDefinition(string id,
                              string displayName,
                              string story,
                              TimeTag time,
                              IReadOnlyList<PersonTag> persons,
                              IReadOnlyList<EmotionTag> emotions)
        {
            Id = id;
            DisplayName = displayName;
            Story = story;
            Time = time;
            Persons = persons;
            Emotions = emotions;
        }

        public TagSet CreateOriginalTagSet() => new(Time, Persons, Emotions);
    }

    /// <summary>런 중 손에 들고 있는 단서. 한 번 내면 손패에서 사라진다(사용 횟수 제한 없음 — 영구 소멸은 ClueHand.Use).</summary>
    public sealed class ClueInstance
    {
        public ClueDefinition Definition { get; }

        public ClueInstance(ClueDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
}
