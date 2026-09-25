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

        /// <summary>아직 <see cref="CommitRun"/>으로 확정되지 않은 이번 런의 관찰 수(검사·디버그용).</summary>
        public int PendingCount => _pending.Count;

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

        /// <summary>런이 끝나지 않은 채 버려질 때(StageEnded를 거치지 않는 재시작) 아직 확정 안 된 관찰을 버린다 — 안 그러면 다음 런의 CommitRun에 딸려 들어간다.
        /// 이미 확정된 영구 지식(<see cref="CommitRun"/>)은 건드리지 않는다.</summary>
        public void DiscardPending() => _pending.Clear();
    }
}
