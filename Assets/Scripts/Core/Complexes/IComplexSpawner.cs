using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Complexes
{
    /// <summary>발현이 결정된 턴에 어떤 컴플렉스를 붙일지 정한다. 기본 구현은 무작위(<see cref="ComplexSpawner"/>), 튜토리얼은 정해진 순서(<see cref="ScriptedComplexSchedule"/>).</summary>
    public interface IComplexSpawner
    {
        bool TrySpawn(ComplexBoard board, out ComplexInstance spawned);
    }
}
