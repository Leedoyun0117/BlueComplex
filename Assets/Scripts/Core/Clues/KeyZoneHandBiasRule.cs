using System;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
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
