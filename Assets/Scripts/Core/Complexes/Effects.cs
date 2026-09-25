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

    /// <summary>인물 태그를 amount개 더한다. 변형(<see cref="ConvertMatchedPersonsTo"/>)과 달리 있던 태그는 그대로 두고 더하기만 한다. (스테이지 2 합리화 "타인 +1", 전이 "가족 +1")</summary>
    public sealed class AddPerson : IComplexEffect
    {
        private readonly PersonTag _person;
        private readonly int _amount;

        public AddPerson(PersonTag person, int amount = 1)
        {
            _person = person;
            _amount = amount;
        }

        public void Apply(ComplexContext context) => context.Tags.AddPerson(_person, _amount);
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

    /// <summary>시간 태그를 하나 더 붙인다. 기존 시간 태그는 그대로다. (스테이지 2 과한 기대 — "시간 태그 '미래' 추가")</summary>
    public sealed class AddTime : IComplexEffect
    {
        private readonly TimeTag _time;
        public AddTime(TimeTag time) => _time = time;

        public void Apply(ComplexContext context) => context.Tags.AddTime(_time);
    }

    /// <summary>감정 태그를 amount개 뺀다. 없으면 아무 일도 없다. (스테이지 2 과거 부정 "슬픔 -1", 피해 망상 "행복 -1")</summary>
    public sealed class RemoveEmotion : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly int _amount;

        public RemoveEmotion(EmotionTag emotion, int amount = 1)
        {
            _emotion = emotion;
            _amount = amount;
        }

        public void Apply(ComplexContext context) => context.Tags.RemoveEmotion(_emotion, _amount);
    }

    /// <summary>붙어 있는 감정 태그를 전부 배로 만든다(중첩 포함, 슬픔 ×2 → ×4). (스테이지 2 사고 과다 "감정 * 2")</summary>
    public sealed class DoubleEmotions : IComplexEffect
    {
        public void Apply(ComplexContext context)
        {
            foreach (var pair in context.Tags.Emotions.ToList())
                context.Tags.AddEmotion(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// 해당 극성 감정 태그를 하나하나 한 개씩 더 붙인다 — 중첩된 태그도 낱개마다 세므로 슬픔 ×2는 ×4가 된다.
    /// (스테이지 2 자책 — "동일한 침체 감정 태그 모두 +1씩 추가 (중복 포함)")
    /// </summary>
    public sealed class RepeatEachEmotionOfPolarity : IComplexEffect
    {
        private readonly Polarity _polarity;
        private readonly IEmotionPolarityTable _polarityTable;

        public RepeatEachEmotionOfPolarity(Polarity polarity, IEmotionPolarityTable polarityTable)
        {
            _polarity = polarity;
            _polarityTable = polarityTable;
        }

        public void Apply(ComplexContext context)
        {
            foreach (var pair in context.Tags.Emotions.ToList())
            {
                if (_polarityTable.GetPolarity(pair.Key) != _polarity) continue;
                context.Tags.AddEmotion(pair.Key, pair.Value);
            }
        }
    }

    /// <summary>
    /// 해당 극성 감정 태그의 개수(중첩 포함)에 배수를 곱한 만큼 지정 감정을 더한다. 개수는 효과를 적용하기 전 값으로 센다.
    /// (스테이지 2 자기 분노 — "감정 태그 '행복'을 단서의 흥분 태그 갯수 * 2만큼 추가")
    /// </summary>
    public sealed class AddEmotionPerEmotionOfPolarity : IComplexEffect
    {
        private readonly EmotionTag _emotion;
        private readonly Polarity _polarity;
        private readonly int _multiplier;
        private readonly IEmotionPolarityTable _polarityTable;

        public AddEmotionPerEmotionOfPolarity(EmotionTag emotion, Polarity polarity, int multiplier, IEmotionPolarityTable polarityTable)
        {
            _emotion = emotion;
            _polarity = polarity;
            _multiplier = multiplier;
            _polarityTable = polarityTable;
        }

        public void Apply(ComplexContext context)
        {
            var count = context.Tags.Emotions
                .Where(pair => _polarityTable.GetPolarity(pair.Key) == _polarity)
                .Sum(pair => pair.Value);
            if (count > 0) context.Tags.AddEmotion(_emotion, count * _multiplier);
        }
    }

    /// <summary>지정한 감정을 뺀 나머지 감정 태그를 전부 지운다. (스테이지 2 과대 해석 — "다른 감정 태그 모두 제거")</summary>
    public sealed class KeepOnlyEmotion : IComplexEffect
    {
        private readonly EmotionTag _keep;
        public KeepOnlyEmotion(EmotionTag keep) => _keep = keep;

        public void Apply(ComplexContext context)
        {
            foreach (var emotion in context.Tags.Emotions.Keys.ToList())
            {
                if (emotion == _keep) continue;
                context.Tags.RemoveEmotion(emotion, context.Tags.CountOf(emotion));
            }
        }
    }

    /// <summary>시간 태그에 따라 갈라지는 효과. 지정한 시간이 붙어 있으면 <c>ifPresent</c>, 아니면 <c>otherwise</c>. (스테이지 2 사고 과다)</summary>
    public sealed class ByTime : IComplexEffect
    {
        private readonly TimeTag _time;
        private readonly IComplexEffect _ifPresent;
        private readonly IComplexEffect _otherwise;

        public ByTime(TimeTag time, IComplexEffect ifPresent, IComplexEffect otherwise)
        {
            _time = time;
            _ifPresent = ifPresent;
            _otherwise = otherwise;
        }

        public void Apply(ComplexContext context) =>
            (context.Tags.HasTime(_time) ? _ifPresent : _otherwise).Apply(context);
    }
}
