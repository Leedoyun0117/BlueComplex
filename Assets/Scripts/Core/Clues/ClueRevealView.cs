using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
    /// <summary>단서 한 장의 태그를 지금 지식으로 가린 모습 — 해금 안 된 칸은 <c>Revealed=false</c>("?"로 그린다).
    /// 칸 수와 순서는 단서 정의 그대로다(몇 칸이 가려져 있는지는 보인다).</summary>
    public sealed class ClueRevealView
    {
        public ClueDefinition Definition { get; }
        public bool TimeRevealed { get; }
        public IReadOnlyList<(PersonTag Tag, bool Revealed)> Persons { get; }
        public IReadOnlyList<(EmotionTag Tag, bool Revealed)> Emotions { get; }

        public ClueRevealView(ClueDefinition definition, bool timeRevealed,
            IReadOnlyList<(PersonTag, bool)> persons, IReadOnlyList<(EmotionTag, bool)> emotions)
        {
            Definition = definition;
            TimeRevealed = timeRevealed;
            Persons = persons;
            Emotions = emotions;
        }
    }
}
