using System.Text;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>
    /// 해금 상태(ClueKnowledgeLedger)와 검열 수준을 반영해 단서 카드 텍스트를 만든다.
    /// 표시만 담당하고 판정에는 관여하지 않는다.
    /// </summary>
    internal static class ClueCardFormatter
    {
        private const string CensorBlock = "▓▓▓▓▓▓▓▓";

        public static string Format(ClueInstance card, ClueKnowledgeLedger ledger, CensorshipLevel censorship)
        {
            var def = card.Definition;
            var sb = new StringBuilder();
            sb.AppendLine(def.DisplayName);

            if (censorship == CensorshipLevel.Full)
            {
                sb.Append(CensorBlock);
                return sb.ToString();
            }

            var knowledge = ledger.GetKnowledge(def.Id);

            sb.AppendLine($"시간: {(knowledge.TimeRevealed ? DebugKoreanLabels.Times(def.Times) : "?")}");
            sb.AppendLine($"인물: {FormatPersons(def, knowledge)}");
            sb.AppendLine($"감정: {FormatEmotions(def, knowledge)}");
            sb.Append(censorship == CensorshipLevel.Partial ? CensorBlock : $"\"{def.Story}\"");

            return sb.ToString();
        }

        private static string FormatPersons(ClueDefinition def, ClueKnowledge knowledge)
        {
            var parts = new string[def.Persons.Count];
            for (var i = 0; i < def.Persons.Count; i++)
            {
                var person = def.Persons[i];
                parts[i] = knowledge.IsPersonRevealed(person) ? DebugKoreanLabels.Person(person) : "?";
            }
            return string.Join(", ", parts);
        }

        private static string FormatEmotions(ClueDefinition def, ClueKnowledge knowledge)
        {
            var parts = new string[def.Emotions.Count];
            for (var i = 0; i < def.Emotions.Count; i++)
            {
                var emotion = def.Emotions[i];
                parts[i] = knowledge.IsEmotionRevealed(emotion) ? DebugKoreanLabels.Emotion(emotion) : "?";
            }
            return string.Join(", ", parts);
        }
    }
}
