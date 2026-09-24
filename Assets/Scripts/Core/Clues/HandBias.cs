using System;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
    /// <summary>쿼터 손패를 채울 때 강제로 넣을 단서의 조건: <see cref="Qualifies"/>를 만족하는 단서가 손패에 <see cref="MinClues"/>장 이상 있어야 한다.</summary>
    public sealed class HandBias
    {
        public int MinClues { get; }
        public Func<ClueDefinition, bool> Qualifies { get; }

        public HandBias(int minClues, Func<ClueDefinition, bool> qualifies)
        {
            MinClues = minClues;
            Qualifies = qualifies ?? throw new ArgumentNullException(nameof(qualifies));
        }
    }

    /// <summary>스테이지가 켜는 키 구역 손패 편향의 값(<see cref="StageConfig.KeyHandBias"/>). 꺼 두려면 null.</summary>
    public sealed class KeyHandBiasSettings
    {
        /// <summary>'침체'/'흥분' 구간 목표일 때, 목표 구역 쪽 감정을 충분히 가진 단서를 손패에 최소 몇 장 넣을지.</summary>
        public int MinClues { get; }

        /// <summary>'매우 침체'/'매우 흥분' 구간 목표일 때의 같은 값.</summary>
        public int MinCluesVery { get; }

        /// <summary>'침체'/'흥분' 구간 목표일 때, 단서 하나가 목표 쪽 감정 태그를 최소 몇 개 가져야 조건 충족인지.</summary>
        public int MinTagsNormal { get; }

        /// <summary>'매우 침체'/'매우 흥분' 구간 목표일 때의 같은 값.</summary>
        public int MinTagsVery { get; }

        /// <param name="minCluesVery">null이면 <paramref name="minClues"/>와 같다(매우 구간도 같은 장수).</param>
        public KeyHandBiasSettings(int minClues, int minTagsNormal, int minTagsVery, int? minCluesVery = null)
        {
            if (minClues < 1) throw new ArgumentOutOfRangeException(nameof(minClues));
            if (minCluesVery.HasValue && minCluesVery.Value < 1) throw new ArgumentOutOfRangeException(nameof(minCluesVery));
            if (minTagsNormal < 1) throw new ArgumentOutOfRangeException(nameof(minTagsNormal));
            if (minTagsVery < 1) throw new ArgumentOutOfRangeException(nameof(minTagsVery));
            MinClues = minClues;
            MinCluesVery = minCluesVery ?? minClues;
            MinTagsNormal = minTagsNormal;
            MinTagsVery = minTagsVery;
        }
    }

    /// <summary>
    /// 현재 쿼터의 키 목표 구역이 다섯 구간(매우 침체/침체/안정/흥분/매우 흥분) 중 어디인지 판정해 <see cref="HandBias"/>를 만든다.
    /// 구역의 구간은 구역 한가운데 칸의 구간이다(구역 폭이 구간 경계를 걸칠 수 있다). 침체 쪽이면 침체 감정, 흥분 쪽이면 흥분 감정을 가진 단서가
    /// 조건 충족이고, 안정 구간이면 편향이 없다(null). 감정 태그가 침체/흥분 어느 쪽인지는 극성 표를 따른다.
    /// </summary>
    public sealed class KeyZoneHandBiasRule
    {
        private readonly HeartbeatZone _zone;
        private readonly IEmotionPolarityTable _polarity;
        private readonly KeyHandBiasSettings _settings;

        public KeyZoneHandBiasRule(HeartbeatZone zone, IEmotionPolarityTable polarity, KeyHandBiasSettings settings)
        {
            _zone = zone;
            _polarity = polarity;
            _settings = settings;
        }

        public HandBias For(KeyZone target)
        {
            var center = target.StartSlot + target.Width / 2;
            var state = _zone.StateOf(center);

            Polarity side;
            int minTags;
            int minClues;
            switch (state)
            {
                case HeartbeatState.VeryDepressed: side = Polarity.Depressed; minTags = _settings.MinTagsVery; minClues = _settings.MinCluesVery; break;
                case HeartbeatState.Depressed: side = Polarity.Depressed; minTags = _settings.MinTagsNormal; minClues = _settings.MinClues; break;
                case HeartbeatState.Excited: side = Polarity.Excited; minTags = _settings.MinTagsNormal; minClues = _settings.MinClues; break;
                case HeartbeatState.VeryExcited: side = Polarity.Excited; minTags = _settings.MinTagsVery; minClues = _settings.MinCluesVery; break;
                default: return null;
            }

            return new HandBias(minClues, definition => TagsOnSide(definition, side) >= minTags);
        }

        private int TagsOnSide(ClueDefinition definition, Polarity side)
        {
            var count = 0;
            foreach (var emotion in definition.Emotions)
                if (_polarity.GetPolarity(emotion) == side) count++;
            return count;
        }
    }
}
