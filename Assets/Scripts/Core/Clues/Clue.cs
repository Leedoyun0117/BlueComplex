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
        /// <summary>시간 태그 전부(정의에 적힌 순서). 없으면 빈 목록.</summary>
        public IReadOnlyList<TimeTag> Times { get; }

        /// <summary>대표 시간 태그 — 첫 번째, 없으면 None. 태그가 여럿인 단서는 <see cref="Times"/>를 본다.</summary>
        public TimeTag Time => Times.Count > 0 ? Times[0] : TimeTag.None;

        public IReadOnlyList<PersonTag> Persons { get; }
        public IReadOnlyList<EmotionTag> Emotions { get; }

        /// <summary>한 번에 손패로 들어오는 단서 묶음(set)의 id. 같은 값을 가진 정의끼리 한 set이고, null이면 set에 속하지 않는다.
        /// set에서 하나가 뽑히면 나머지도 함께 뽑히고, 3개 이상인 set은 그중 <see cref="SetPickCount"/>개만 제공된다(<see cref="CluePool.TryDrawGroup"/>).</summary>
        public string SetId { get; }

        /// <summary>set 하나가 손패에 한 번에 내놓는 단서 수. set 크기가 이보다 크면 그중 이만큼만 랜덤으로 제공한다.</summary>
        public const int SetPickCount = 2;

        public ClueDefinition(string id,
                              string displayName,
                              string story,
                              TimeTag time,
                              IReadOnlyList<PersonTag> persons,
                              IReadOnlyList<EmotionTag> emotions,
                              string setId = null)
            : this(id, displayName, story,
                   time == TimeTag.None ? Array.Empty<TimeTag>() : new[] { time },
                   persons, emotions, setId)
        {
        }

        /// <summary>시간 태그가 여럿인 단서(예: 스테이지 2 "과거 + 현재")나 시간 태그가 아예 없는 단서.</summary>
        public ClueDefinition(string id,
                              string displayName,
                              string story,
                              IReadOnlyList<TimeTag> times,
                              IReadOnlyList<PersonTag> persons,
                              IReadOnlyList<EmotionTag> emotions,
                              string setId = null)
        {
            Id = id;
            DisplayName = displayName;
            Story = story;
            Times = times;
            Persons = persons;
            Emotions = emotions;
            SetId = setId;
        }

        public TagSet CreateOriginalTagSet() => new(Times, Persons, Emotions);
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
