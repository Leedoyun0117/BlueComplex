using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Clues
{
    /// <summary>한 단서에 대해 지금까지 밝혀진 속성.</summary>
    public sealed class ClueKnowledge
    {
        public bool TimeRevealed { get; private set; }
        private readonly HashSet<PersonTag> _persons = new();
        private readonly HashSet<EmotionTag> _emotions = new();

        public IReadOnlyCollection<PersonTag> RevealedPersons => _persons;
        public IReadOnlyCollection<EmotionTag> RevealedEmotions => _emotions;

        public void RevealTime() => TimeRevealed = true;
        public void RevealPerson(PersonTag person) => _persons.Add(person);
        public void RevealEmotion(EmotionTag emotion) => _emotions.Add(emotion);

        public bool IsPersonRevealed(PersonTag person) => _persons.Contains(person);
        public bool IsEmotionRevealed(EmotionTag emotion) => _emotions.Contains(emotion);
    }

    /// <summary>
    /// 런 도중 관찰한 내용을 모았다가, 런 종료 시점에 영구 지식으로 승격한다.
    /// 그래서 해금된 정보는 다음 런부터 노트에 보인다.
    /// </summary>
    public sealed class ClueKnowledgeLedger
    {
        private readonly Dictionary<string, ClueKnowledge> _persistent = new();
        private readonly List<(string ClueId, InterpretationStep Step)> _pending = new();

        public ClueKnowledge GetKnowledge(string clueId)
        {
            if (!_persistent.TryGetValue(clueId, out var knowledge))
            {
                knowledge = new ClueKnowledge();
                _persistent[clueId] = knowledge;
            }
            return knowledge;
        }

        /// <summary>실제로 발동한 첫 컴플렉스가 참조한 태그만 관찰로 인정한다.</summary>
        public void RecordInterpretation(string clueId, InterpretationResult result)
        {
            var firstTriggered = result.Steps.FirstOrDefault(step => step.Triggered);
            if (firstTriggered.Complex == null) return;

            _pending.Add((clueId, firstTriggered));
        }

        /// <summary>런 종료 후 분석 — 여기서 비로소 노트의 빈칸이 채워진다.</summary>
        public void CommitRun()
        {
            foreach (var (clueId, step) in _pending)
            {
                var knowledge = GetKnowledge(clueId);

                if (step.ObservedTime != TimeTag.None) knowledge.RevealTime();
                foreach (var person in step.ObservedPersons) knowledge.RevealPerson(person);
                foreach (var emotion in step.ObservedEmotions) knowledge.RevealEmotion(emotion);
            }

            _pending.Clear();
        }
    }
}
