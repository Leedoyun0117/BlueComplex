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

    /// <summary>
    /// 단서 노트(시작 화면)와 단서 카드가 함께 쓰는 판정: 어떤 단서를 목록에 올리는가, 각 칸을 보여 주는가.
    /// 표시 문자열은 UI(ClueCardFormatter)가 만든다.
    /// </summary>
    public static class ClueNote
    {
        /// <summary>한 번이라도 본(손패에 들어왔거나 낸) 단서만, 카탈로그 순서 그대로.</summary>
        public static List<ClueDefinition> SeenClues(IEnumerable<ClueDefinition> catalog, ClueKnowledgeLedger ledger) =>
            catalog.Where(def => ledger.IsSeen(def.Id)).ToList();

        public static ClueRevealView Reveal(ClueDefinition definition, ClueKnowledgeLedger ledger)
        {
            var knowledge = ledger.GetKnowledge(definition.Id);
            var persons = definition.Persons.Select(p => (p, knowledge.IsPersonRevealed(p))).ToList();
            var emotions = definition.Emotions.Select(e => (e, knowledge.IsEmotionRevealed(e))).ToList();
            return new ClueRevealView(definition, knowledge.TimeRevealed, persons, emotions);
        }
    }
}
