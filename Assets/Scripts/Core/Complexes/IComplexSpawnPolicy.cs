using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Complexes
{
    public interface IComplexSpawnPolicy
    {
        bool ShouldSpawn(int heartbeatValue);
    }
}
