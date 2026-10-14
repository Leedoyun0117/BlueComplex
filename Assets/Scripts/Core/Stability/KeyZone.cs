using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>심박수 축 위에 나타나는 키 구역. 폭(Width)만큼의 심박수 구간을 차지한다.</summary>
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
}
