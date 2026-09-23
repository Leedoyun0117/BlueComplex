using System.Collections.Generic;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>
    /// 한 컴플렉스가 한 단서를 해석하는 동안의 작업 공간.
    /// 조건은 매칭한 태그를 여기 기록하고, 효과는 그 기록을 보고 대상을 정한다.
    /// 기록된 태그는 그대로 단서 속성 해금의 '관찰' 근거가 된다.
    /// </summary>
    public sealed class ComplexContext
    {
        private readonly List<PersonTag> _matchedPersons = new();
        private readonly List<EmotionTag> _matchedEmotions = new();

        public TagSet Tags { get; }
        public TimeTag MatchedTime { get; private set; } = TimeTag.None;
        public IReadOnlyList<PersonTag> MatchedPersons => _matchedPersons;
        public IReadOnlyList<EmotionTag> MatchedEmotions => _matchedEmotions;

        public ComplexContext(TagSet tags) => Tags = tags;

        public void MarkTime(TimeTag time) => MatchedTime = time;

        public void MarkPerson(PersonTag person)
        {
            if (!_matchedPersons.Contains(person)) _matchedPersons.Add(person);
        }

        public void MarkEmotion(EmotionTag emotion)
        {
            if (!_matchedEmotions.Contains(emotion)) _matchedEmotions.Add(emotion);
        }

        public void ClearMatches()
        {
            MatchedTime = TimeTag.None;
            _matchedPersons.Clear();
            _matchedEmotions.Clear();
        }
    }
}
