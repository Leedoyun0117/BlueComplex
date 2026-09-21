using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    public interface IComplexEffect
    {
        void Apply(ComplexContext context);
    }

    /// <summary>조건이 매칭한 감정들을 지정 감정으로 변환. (예: 행복/사랑 → 혐오)</summary>
    public sealed class ConvertMatchedEmotionsTo : IComplexEffect
    {
        private readonly EmotionTag _target;
        public ConvertMatchedEmotionsTo(EmotionTag target) => _target = target;

        public void Apply(ComplexContext context)
        {
            foreach (var emotion in context.MatchedEmotions)
            {
                if (emotion == _target) continue;
                context.Tags.ReplaceEmotion(emotion, _target);
            }
        }
    }

    /// <summary>조건이 매칭한 인물들을 지정 인물로 변환. (예: 친구 → 연인, 연인/친구 → 타인)</summary>
    public sealed class ConvertMatchedPersonsTo : IComplexEffect
    {
        private readonly PersonTag _target;
        public ConvertMatchedPersonsTo(PersonTag target) => _target = target;

        public void Apply(ComplexContext context)
        {
            foreach (var person in context.MatchedPersons)
            {
                if (person == _target) continue;
                context.Tags.ReplacePerson(person, _target);
            }
        }
    }

    /// <summary>지금 붙어 있는 감정 종류마다 하나씩 더 붙인다. (예: 되새김 — 과거의 감정을 한 번 더 느낀다)</summary>
    public sealed class RepeatEachEmotion : IComplexEffect
    {
        public void Apply(ComplexContext context)
        {
            foreach (var emotion in context.Tags.Emotions.Keys.ToList())
                context.Tags.AddEmotion(emotion);
        }
    }

    public sealed class AddEmotion : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly int _amount;

        public AddEmotion(EmotionTag emotion, int amount = 1)
        {
            _emotion = emotion;
            _amount = amount;
        }

        public void Apply(ComplexContext context) => context.Tags.AddEmotion(_emotion, _amount);
    }

    /// <summary>이미 존재하는 감정만 증폭한다. (예: 슬픔이 있다면 슬픔 +2)</summary>
    public sealed class AmplifyEmotion : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly int _amount;

        public AmplifyEmotion(EmotionTag emotion, int amount)
        {
            _emotion = emotion;
            _amount = amount;
        }

        public void Apply(ComplexContext context)
        {
            if (!context.Tags.HasEmotion(_emotion)) return;
            context.Tags.AddEmotion(_emotion, _amount);
        }
    }

    public sealed class ShiftTime : IComplexEffect
    {
        private readonly TimeTag _time;
        public ShiftTime(TimeTag time) => _time = time;

        public void Apply(ComplexContext context) => context.Tags.SetTime(_time);
    }
}
