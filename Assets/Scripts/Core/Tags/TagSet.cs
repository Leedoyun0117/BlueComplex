using System.Collections.Generic;
using System.Linq;

namespace BlueComplex.Core.Tags
{
    /// <summary>
    /// 시간 1개, 인물 N개, 감정 N개(중복 누적 가능)를 담는다.
    /// 감정은 "슬픔 +2" 같은 증폭을 표현해야 하므로 개수를 센다.
    /// </summary>
    public sealed class TagSet
    {
        private readonly HashSet<PersonTag> _persons = new();
        private readonly Dictionary<EmotionTag, int> _emotions = new();

        public TimeTag Time { get; private set; }

        public IReadOnlyCollection<PersonTag> Persons => _persons;
        public IReadOnlyDictionary<EmotionTag, int> Emotions => _emotions;

        public TagSet(TimeTag time = TimeTag.None,
                      IEnumerable<PersonTag> persons = null,
                      IEnumerable<EmotionTag> emotions = null)
        {
            Time = time;
            if (persons != null)
                foreach (var p in persons) _persons.Add(p);
            if (emotions != null)
                foreach (var e in emotions) AddEmotion(e);
        }

        public void SetTime(TimeTag time) => Time = time;

        public bool HasPerson(PersonTag person) => _persons.Contains(person);
        public bool HasAnyPerson() => _persons.Count > 0;

        public bool HasEmotion(EmotionTag emotion) => _emotions.ContainsKey(emotion);
        public int CountOf(EmotionTag emotion) => _emotions.TryGetValue(emotion, out var n) ? n : 0;

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
            var clone = new TagSet(Time, _persons);
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
