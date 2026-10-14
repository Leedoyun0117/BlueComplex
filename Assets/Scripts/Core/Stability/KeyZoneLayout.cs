using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>
    /// 키 구역의 물리적 규격. 배치 정책은 이 규격 안에서 offset만 고른다.
    /// Min/Max는 키가 놓일 수 있는 생존 가능 범위(양 끝을 포함)로, 항상 <see cref="HeartbeatZone"/> 에서
    /// 가져와야 한다 — Fatal 구간과 겹치는 키 구역이 생기면 도달 자체가 불가능해지기 때문이다.
    /// </summary>
    public sealed class KeyZoneLayout
    {
        /// <summary>10칸 기준 양끝 3칸을 200 스케일로 환산한 값(3 × 20).</summary>
        public const int DefaultEdgeWidth = 60;

        /// <summary>생존 구간(폭 181) 대비 원래 비율(10칸 중 2칸)을 유지한 값(181 × 0.2 ≈ 36).</summary>
        public const int DefaultKeyWidth = 36;

        /// <summary>키가 놓일 수 있는 생존 구간의 최솟값(포함).</summary>
        public int Min { get; }

        /// <summary>키가 놓일 수 있는 생존 구간의 최댓값(포함).</summary>
        public int Max { get; }

        public int EdgeWidth { get; }
        public int KeyWidth { get; }

        /// <summary>생존 구간 전체 칸 수.</summary>
        public int Slots => Max - Min + 1;

        /// <summary>한쪽 끝 구역 안에서 키 시작 칸의 후보 개수.</summary>
        public int OffsetCount => EdgeWidth - KeyWidth + 1;

        public KeyZoneLayout(int min, int max, int edgeWidth = DefaultEdgeWidth, int keyWidth = DefaultKeyWidth)
        {
            if (max < min)
                throw new ArgumentException($"max({max})는 min({min}) 이상이어야 합니다.", nameof(max));
            if (keyWidth > edgeWidth)
                throw new ArgumentException($"keyWidth({keyWidth})는 edgeWidth({edgeWidth}) 이하여야 합니다.", nameof(keyWidth));
            if (edgeWidth > max - min + 1)
                throw new ArgumentException($"edgeWidth({edgeWidth})가 생존 구간 폭({max - min + 1})보다 큽니다.", nameof(edgeWidth));

            Min = min;
            Max = max;
            EdgeWidth = edgeWidth;
            KeyWidth = keyWidth;
        }

        /// <summary>생존 구간을 아는 <see cref="HeartbeatZone"/> 으로부터 레이아웃을 만든다.</summary>
        public KeyZoneLayout(HeartbeatZone zone, int edgeWidth = DefaultEdgeWidth, int keyWidth = DefaultKeyWidth)
            : this(zone.SurvivableMin, zone.SurvivableMax, edgeWidth, keyWidth)
        {
        }

        public KeyZone LeftZone(int offset) => new(Min + offset, KeyWidth);

        public KeyZone RightZone(int offset) => new(Max - EdgeWidth + 1 + offset, KeyWidth);
    }
}
