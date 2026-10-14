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
}
