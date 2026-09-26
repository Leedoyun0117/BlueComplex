using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Tags
{
    /// <summary>
    /// 시간 N개(스테이지 1은 늘 1개, 스테이지 2는 "과거 + 현재"처럼 여러 개), 인물 N개, 감정 N개(중복 누적 가능)를 담는다.
    /// 감정은 "슬픔 +2" 같은 증폭을 표현해야 하므로 개수를 센다. 인물도 "타인 +1"처럼 더해질 수 있어 종류마다 개수를 센다
    /// (단서 원본은 종류마다 1개이고, 컴플렉스가 더할 때만 2 이상이 된다).
    /// </summary>
    public sealed class TagSet
    {
        private readonly List<TimeTag> _times = new();
        private readonly Dictionary<PersonTag, int> _persons = new();
        private readonly Dictionary<EmotionTag, int> _emotions = new();

        /// <summary>붙어 있는 시간 태그(정의에 적힌 순서). 없으면 빈 목록.</summary>
        public IReadOnlyList<TimeTag> Times => _times;

        /// <summary>시간 태그가 하나뿐이라고 보는 곳을 위한 대표값 — 첫 시간 태그, 없으면 None. 여러 개인 단서는 <see cref="Times"/>/<see cref="HasTime"/>을 쓴다.</summary>
        public TimeTag Time => _times.Count > 0 ? _times[0] : TimeTag.None;

        /// <summary>붙어 있는 인물 태그의 종류(개수와 무관하게 종류당 하나). 개수는 <see cref="CountOfPerson"/>.</summary>
        public IReadOnlyCollection<PersonTag> Persons => _persons.Keys;
        public IReadOnlyDictionary<EmotionTag, int> Emotions => _emotions;

        public TagSet(TimeTag time = TimeTag.None,
                      IEnumerable<PersonTag> persons = null,
                      IEnumerable<EmotionTag> emotions = null)
        {
            AddTime(time);
            AddPersonsAndEmotions(persons, emotions);
        }

        public TagSet(IEnumerable<TimeTag> times,
                      IEnumerable<PersonTag> persons = null,
                      IEnumerable<EmotionTag> emotions = null)
        {
            if (times != null)
                foreach (var t in times) AddTime(t);
            AddPersonsAndEmotions(persons, emotions);
        }

        private void AddPersonsAndEmotions(IEnumerable<PersonTag> persons, IEnumerable<EmotionTag> emotions)
        {
            if (persons != null)
                foreach (var p in persons) _persons.TryAdd(p, 1);
            if (emotions != null)
                foreach (var e in emotions) AddEmotion(e);
        }

        public bool HasTime(TimeTag time) => time != TimeTag.None && _times.Contains(time);

        /// <summary>시간 태그를 전부 지우고 이것 하나로 바꾼다. (스테이지 1 회피: "→ 과거")</summary>
        public void SetTime(TimeTag time)
        {
            _times.Clear();
            AddTime(time);
        }

        /// <summary>시간 태그를 하나 더 붙인다. 이미 있으면 그대로. (스테이지 2 과한 기대: "시간 태그 '미래' 추가")</summary>
        public void AddTime(TimeTag time)
        {
            if (time == TimeTag.None || _times.Contains(time)) return;
            _times.Add(time);
        }

        public bool HasPerson(PersonTag person) => _persons.ContainsKey(person);
        public int CountOfPerson(PersonTag person) => _persons.TryGetValue(person, out var n) ? n : 0;
        public bool HasAnyPerson() => _persons.Count > 0;

        public bool HasEmotion(EmotionTag emotion) => _emotions.ContainsKey(emotion);
        public int CountOf(EmotionTag emotion) => _emotions.TryGetValue(emotion, out var n) ? n : 0;

        /// <summary>인물 태그를 다른 인물 태그로 바꾼다(변형). 이미 to가 있으면 하나로 합쳐진다(개수는 둘 중 큰 쪽).</summary>
        public void ReplacePerson(PersonTag from, PersonTag to)
        {
            if (!_persons.Remove(from, out var moved)) return;
            _persons[to] = Math.Max(CountOfPerson(to), moved);
        }

        /// <summary>인물 태그를 amount개 더한다(변형이 아니라 추가 — 있던 태그는 그대로다). 없던 종류면 새로 붙는다. (스테이지 2 합리화 "타인 +1", 전이 "가족 +1")</summary>
        public void AddPerson(PersonTag person, int amount = 1)
        {
            if (person == PersonTag.None || amount <= 0) return;
            _persons[person] = CountOfPerson(person) + amount;
        }

        /// <summary>인물 태그를 amount개 뺀다. 0개가 되면 그 종류는 사라진다. 없으면 아무 일도 없다. (스테이지 3 밀착 "타인 -1")</summary>
        public void RemovePerson(PersonTag person, int amount = 1)
        {
            if (!_persons.TryGetValue(person, out var n) || amount <= 0) return;
            n -= amount;
            if (n <= 0) _persons.Remove(person);
            else _persons[person] = n;
        }

        /// <summary>시간 태그 하나를 뗀다. 없으면 아무 일도 없다. (스테이지 3 시간 혼합 "과거 -1")</summary>
        public void RemoveTime(TimeTag time) => _times.Remove(time);

        public void AddEmotion(EmotionTag emotion, int amount = 1)
        {
            if (amount <= 0) return;
            _emotions[emotion] = CountOf(emotion) + amount;
        }

        public void RemoveEmotion(EmotionTag emotion, int amount = 1)
        {
            if (!_emotions.TryGetValue(emotion, out var n)) return;
            n -= amount;
            if (n <= 0) _emotions.Remove(emotion);
            else _emotions[emotion] = n;
        }

        public void ReplaceEmotion(EmotionTag from, EmotionTag to)
        {
            var n = CountOf(from);
            if (n == 0) return;
            _emotions.Remove(from);
            AddEmotion(to, n);
        }

        /// <summary>중복 감정을 1개씩만 남긴다. (아이템 '완화제')</summary>
        public void CollapseDuplicates()
        {
            foreach (var key in _emotions.Keys.ToList())
                _emotions[key] = 1;
        }

        public TagSet Clone()
        {
            var clone = new TagSet(_times);
            foreach (var pair in _persons) clone._persons[pair.Key] = pair.Value;
            foreach (var pair in _emotions) clone.AddEmotion(pair.Key, pair.Value);
            return clone;
        }

        public IEnumerable<EmotionTag> EnumerateEmotionsFlat()
        {
            foreach (var pair in _emotions)
                for (var i = 0; i < pair.Value; i++)
                    yield return pair.Key;
        }
    }
}
