using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public interface IComplexCondition
    {
        bool Evaluate(ComplexContext context);
    }

    public sealed class TimeIs : IComplexCondition
    {
        private readonly TimeTag _time;
        public TimeIs(TimeTag time) => _time = time;

        public bool Evaluate(ComplexContext context)
        {
            if (!context.Tags.HasTime(_time)) return false;
            context.MarkTime(_time);
            return true;
        }
    }

    /// <summary>지정된 시간 중 하나라도 붙어 있으면 성립. (예: 회피 — '현재 또는 미래') 여러 개가 붙어 있으면 후보 순서상 첫 번째만 기록한다.</summary>
    public sealed class TimeIsAnyOf : IComplexCondition
    {
        private readonly IReadOnlyList<TimeTag> _candidates;
        public TimeIsAnyOf(params TimeTag[] candidates) => _candidates = candidates;

        public bool Evaluate(ComplexContext context)
        {
            foreach (var candidate in _candidates)
            {
                if (!context.Tags.HasTime(candidate)) continue;
                context.MarkTime(candidate);
                return true;
            }

            return false;
        }
    }

    public sealed class HasAnyPerson : IComplexCondition
    {
        public bool Evaluate(ComplexContext context)
        {
            if (!context.Tags.HasAnyPerson()) return false;
            foreach (var person in context.Tags.Persons) context.MarkPerson(person);
            return true;
        }
    }

    public sealed class HasPerson : IComplexCondition
    {
        private readonly PersonTag _person;
        public HasPerson(PersonTag person) => _person = person;

        public bool Evaluate(ComplexContext context)
        {
            if (!context.Tags.HasPerson(_person)) return false;
            context.MarkPerson(_person);
            return true;
        }
    }

    /// <summary>지정된 인물 중 하나라도 있으면 성립. 매칭된 인물만 기록한다.</summary>
    public sealed class HasAnyPersonOf : IComplexCondition
    {
        private readonly IReadOnlyList<PersonTag> _candidates;
        public HasAnyPersonOf(params PersonTag[] candidates) => _candidates = candidates;

        public bool Evaluate(ComplexContext context)
        {
            var matched = _candidates.Where(context.Tags.HasPerson).ToList();
            if (matched.Count == 0) return false;
            foreach (var person in matched) context.MarkPerson(person);
            return true;
        }
    }

    /// <summary>지정된 감정 중 하나라도 있으면 성립. 매칭된 감정만 기록한다.</summary>
    public sealed class HasAnyEmotion : IComplexCondition
    {
        private static readonly EmotionTag[] AllEmotions = (EmotionTag[])System.Enum.GetValues(typeof(EmotionTag));

        private readonly IReadOnlyList<EmotionTag> _candidates;
        public HasAnyEmotion(params EmotionTag[] candidates) => _candidates = candidates;

        /// <summary>종류를 가리지 않고 감정 태그가 하나라도 붙어 있으면 성립. (예: "감정 태그가 붙어있다면")</summary>
        public static HasAnyEmotion Any() => new(AllEmotions);

        public bool Evaluate(ComplexContext context)
        {
            var matched = _candidates.Where(context.Tags.HasEmotion).ToList();
            if (matched.Count == 0) return false;
            foreach (var emotion in matched) context.MarkEmotion(emotion);
            return true;
        }
    }

    /// <summary>침체/흥분 극성에 해당하는 감정이 하나라도 있으면 성립.</summary>
    public sealed class HasEmotionOfPolarity : IComplexCondition
    {
        private readonly Polarity _polarity;
        private readonly IEmotionPolarityTable _polarityTable;

        public HasEmotionOfPolarity(Polarity polarity, IEmotionPolarityTable polarityTable)
        {
            _polarity = polarity;
            _polarityTable = polarityTable;
        }

        public bool Evaluate(ComplexContext context)
        {
            var matched = context.Tags.Emotions.Keys
                .Where(e => _polarityTable.GetPolarity(e) == _polarity)
                .ToList();
            if (matched.Count == 0) return false;
            foreach (var emotion in matched) context.MarkEmotion(emotion);
            return true;
        }
    }

    /// <summary>서로 다른 인물 태그가 count개 이상 붙어 있으면 성립. (스테이지 2 의식 분산 — "인물 태그 2개 이상")</summary>
    public sealed class PersonCountAtLeast : IComplexCondition
    {
        private readonly int _count;
        public PersonCountAtLeast(int count) => _count = count;

        public bool Evaluate(ComplexContext context)
        {
            if (context.Tags.Persons.Count < _count) return false;
            foreach (var person in context.Tags.Persons) context.MarkPerson(person);
            return true;
        }
    }

    /// <summary>지정한 인물 태그가 count개 이상 붙어 있으면 성립(같은 종류가 더해져 겹친 개수까지 센다). (스테이지 2 가족애 — "가족 + 2")</summary>
    public sealed class PersonCountOfAtLeast : IComplexCondition
    {
        private readonly PersonTag _person;
        private readonly int _count;

        public PersonCountOfAtLeast(PersonTag person, int count)
        {
            _person = person;
            _count = count;
        }

        public bool Evaluate(ComplexContext context)
        {
            if (context.Tags.CountOfPerson(_person) < _count) return false;
            context.MarkPerson(_person);
            return true;
        }
    }

    /// <summary>서로 다른 감정 종류가 count개 이상 붙어 있으면 성립. 같은 감정이 중첩된 것(슬픔 ×2)은 한 종류로 센다. (스테이지 2 복합 감정)</summary>
    public sealed class EmotionKindCountAtLeast : IComplexCondition
    {
        private readonly int _count;
        public EmotionKindCountAtLeast(int count) => _count = count;

        public bool Evaluate(ComplexContext context)
        {
            if (context.Tags.Emotions.Count < _count) return false;
            foreach (var emotion in context.Tags.Emotions.Keys) context.MarkEmotion(emotion);
            return true;
        }
    }

    /// <summary>침체 감정과 흥분 감정이 동시에 하나 이상씩 붙어 있으면 성립. (스테이지 2 자기 분노 — "감정 태그 침체 + 흥분")</summary>
    public sealed class HasBothPolarities : IComplexCondition
    {
        private readonly IEmotionPolarityTable _polarityTable;
        public HasBothPolarities(IEmotionPolarityTable polarityTable) => _polarityTable = polarityTable;

        public bool Evaluate(ComplexContext context)
        {
            var emotions = context.Tags.Emotions.Keys.ToList();
            var hasDepressed = emotions.Any(e => _polarityTable.GetPolarity(e) == Polarity.Depressed);
            var hasExcited = emotions.Any(e => _polarityTable.GetPolarity(e) == Polarity.Excited);
            if (!hasDepressed || !hasExcited) return false;
            foreach (var emotion in emotions) context.MarkEmotion(emotion);
            return true;
        }
    }
}
