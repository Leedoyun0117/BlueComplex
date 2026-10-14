using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Stability
{
    /// <summary>시작 위치에서 moves번 움직였을 때 심박수가 닿을 수 있는 범위(양 끝 포함).</summary>
    public interface IReachModel
    {
        (int Low, int High) Range(int startPosition, int moves);
    }
}
