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
            if (context.Tags.Time != _time) return false;
            context.MarkTime(_time);
            return true;
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
}
