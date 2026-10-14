using System;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
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
}
