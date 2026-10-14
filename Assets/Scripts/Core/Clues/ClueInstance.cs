using System;
using System.Collections.Generic;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
    /// <summary>런 중 손에 들고 있는 단서. 한 번 내면 손패에서 사라진다(사용 횟수 제한 없음 — 영구 소멸은 ClueHand.Use).</summary>
    public sealed class ClueInstance
    {
        public ClueDefinition Definition { get; }

        public ClueInstance(ClueDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
}
