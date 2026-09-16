using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;

namespace BlueComplex.Core.Stability
{
    /// <summary>인디케이터 위에 나타나는 키 구역. 2칸을 차지한다.</summary>
    public readonly struct KeyZone
    {
        public int StartSlot { get; }
        public int Width { get; }

        public KeyZone(int startSlot, int width)
        {
            StartSlot = startSlot;
            Width = width;
        }

        public bool Contains(int position) => position >= StartSlot && position < StartSlot + Width;
    }

    /// <summary>키 구역의 물리적 규격. 배치 정책은 이 규격 안에서 offset만 고른다.</summary>
    public sealed class KeyZoneLayout
    {
        public int Slots { get; }
        public int EdgeWidth { get; }
        public int KeyWidth { get; }

        /// <summary>한쪽 끝 구역 안에서 키 시작 칸의 후보 개수.</summary>
        public int OffsetCount => EdgeWidth - KeyWidth + 1;

        public KeyZoneLayout(int slots = 10, int edgeWidth = 3, int keyWidth = 2)
        {
            Slots = slots;
            EdgeWidth = edgeWidth;
            KeyWidth = keyWidth;
        }

        public KeyZone LeftZone(int offset) => new(offset, KeyWidth);

        public KeyZone RightZone(int offset) => new(Slots - EdgeWidth + offset, KeyWidth);
    }

    /// <summary>키 구역을 어디에 열지 결정하는 정책. 보정판 구현을 교체할 수 있도록 인터페이스로 분리했다.</summary>
    public interface IKeyZonePlacer
    {
        KeyZone Place(int indicatorPosition, IReadOnlyList<ClueInstance> hand);
    }

    /// <summary>기본 정책. 좌우와 구역 내 offset을 모두 무작위로 고른다.</summary>
    public sealed class RandomKeyZonePlacer : IKeyZonePlacer
    {
        private readonly IRandomSource _random;
        private readonly KeyZoneLayout _layout;

        public RandomKeyZonePlacer(IRandomSource random, KeyZoneLayout layout = null)
        {
            _random = random;
            _layout = layout ?? new KeyZoneLayout();
        }

        public KeyZone Place(int indicatorPosition, IReadOnlyList<ClueInstance> hand)
        {
            var useLeft = _random.Range(0, 2) == 0;
            var offset = _random.Range(0, _layout.OffsetCount);
            return useLeft ? _layout.LeftZone(offset) : _layout.RightZone(offset);
        }
    }

    /// <summary>키 획득 진행 상황. 목표 개수를 채우면 스테이지 클리어.</summary>
    public sealed class KeyProgress
    {
        private readonly HashSet<int> _keyTurns;

        public int Required { get; }
        public int Collected { get; private set; }
        public KeyZone? ActiveZone { get; private set; }

        public bool IsComplete => Collected >= Required;

        public event Action<KeyZone> ZoneOpened;
        public event Action<int> KeyCollected;
        public event Action ZoneMissed;

        public KeyProgress(int required, IEnumerable<int> keyTurns)
        {
            Required = required;
            _keyTurns = keyTurns.ToHashSet();
        }

        public bool IsKeyTurn(int turn) => _keyTurns.Contains(turn);

        public void OpenZone(KeyZone zone)
        {
            ActiveZone = zone;
            ZoneOpened?.Invoke(zone);
        }

        /// <summary>턴 종료 시점의 인디케이터 위치로 획득 여부를 판정한다.</summary>
        public void Judge(int indicatorPosition)
        {
            if (ActiveZone == null) return;

            if (ActiveZone.Value.Contains(indicatorPosition))
            {
                Collected++;
                KeyCollected?.Invoke(Collected);
            }
            else
            {
                ZoneMissed?.Invoke();
            }

            ActiveZone = null;
        }
    }
}
