using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
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
