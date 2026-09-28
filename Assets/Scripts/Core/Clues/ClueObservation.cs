using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Save;
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

        /// <summary>새로 밝혀졌으면 true(이미 밝혀져 있었으면 false).</summary>
        public bool RevealTime()
        {
            var isNew = !TimeRevealed;
            TimeRevealed = true;
            return isNew;
        }

        public bool RevealPerson(PersonTag person) => _persons.Add(person);
        public bool RevealEmotion(EmotionTag emotion) => _emotions.Add(emotion);

        public bool IsPersonRevealed(PersonTag person) => _persons.Contains(person);
        public bool IsEmotionRevealed(EmotionTag emotion) => _emotions.Contains(emotion);
    }

    /// <summary>
    /// 런 도중 관찰한 내용을 모았다가, 런 종료 시점에 영구 지식으로 승격한다.
    /// 그래서 해금된 정보는 다음 런부터 노트에 보인다.
    ///
    /// 따로 "본 적 있는 단서"도 기록한다(<see cref="MarkSeen"/>) — 손패에 한 번이라도 들어온 단서. 시작 화면의 단서 노트가
    /// 이것으로 목록을 거른다. 이건 분석 결과가 아니라 사실이라 런 종료를 기다리지 않고 곧바로 영구 기록이 된다.
    /// </summary>
    public sealed class ClueKnowledgeLedger
    {
        private readonly Dictionary<string, ClueKnowledge> _persistent = new();
        private readonly List<(string ClueId, InterpretationStep Step)> _pending = new();
        private readonly HashSet<string> _seen = new();

        /// <summary>아직 <see cref="CommitRun"/>으로 확정되지 않은 이번 런의 관찰 수(검사·디버그용).</summary>
        public int PendingCount => _pending.Count;

        /// <summary><see cref="CommitRun"/>이 관찰을 확정한 직후 — 세이브가 여기에 건다.</summary>
        public event Action Committed;

        /// <summary>손패에 한 번이라도 들어왔거나 낸 적 있는 단서 id.</summary>
        public IReadOnlyCollection<string> SeenClueIds => _seen;

        public bool IsSeen(string clueId) => _seen.Contains(clueId);

        /// <summary>단서가 손패에 들어왔다(StageFactory가 ClueHand.CardAdded에 건다). 즉시 영구 기록이다.</summary>
        public void MarkSeen(string clueId)
        {
            if (_seen.Add(clueId)) HasNewNoteInfo = true;
        }

        /// <summary>마지막으로 단서 노트를 연 뒤에 처음 본 단서가 생겼거나, 본 단서의 "?"였던 칸이 새로 해금됐는가 —
        /// 메인 화면 "단서 노트" 버튼의 빨간 점. 세이브에 실린다(<see cref="RestoreNoteAlert"/>). 세이브 초기화(<see cref="Clear"/>)는 끈다.</summary>
        public bool HasNewNoteInfo { get; private set; }

        /// <summary>단서 노트를 열었다 — 새 정보 표시를 끈다.</summary>
        public void AcknowledgeNote() => HasNewNoteInfo = false;

        /// <summary>세이브에서 읽은 표시 상태를 되살린다(옛 세이브엔 필드가 없어 false로 읽힌다).</summary>
        public void RestoreNoteAlert(bool hasNew) => HasNewNoteInfo = hasNew;

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
            MarkSeen(clueId); // 낸 단서는 손패를 거쳤다 — 손패 기록이 빠진 경로(테스트 조립 등)에서도 본 것으로 친다.

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

                if (step.ObservedTime != TimeTag.None && knowledge.RevealTime()) HasNewNoteInfo = true;
                foreach (var person in step.ObservedPersons) if (knowledge.RevealPerson(person)) HasNewNoteInfo = true;
                foreach (var emotion in step.ObservedEmotions) if (knowledge.RevealEmotion(emotion)) HasNewNoteInfo = true;
            }

            _pending.Clear();
            Committed?.Invoke();
        }

        /// <summary>런이 끝나지 않은 채 버려질 때(StageEnded를 거치지 않는 재시작) 아직 확정 안 된 관찰을 버린다 — 안 그러면 다음 런의 CommitRun에 딸려 들어간다.
        /// 이미 확정된 영구 지식(<see cref="CommitRun"/>)은 건드리지 않는다.</summary>
        public void DiscardPending() => _pending.Clear();

        /// <summary>확정 지식·미확정 관찰·본 단서를 전부 비운다 — 세이브 초기화 버튼이 메모리 상태까지 처음 하는 사람으로 되돌릴 때 쓴다.</summary>
        public void Clear()
        {
            _persistent.Clear();
            _pending.Clear();
            _seen.Clear();
            HasNewNoteInfo = false;
        }

        // ── 세이브 ──────────────────────────────────────────────────────────

        /// <summary>확정된 지식과 본 단서를 세이브 항목으로 뜬다. 미확정 관찰(pending)은 넣지 않는다 — 런이 끝나야 지식이다.
        /// 아무것도 없는 항목(카드 표시 때 GetKnowledge가 만들어 둔 빈 칸)은 뺀다. 순서는 id 순(파일 비교가 쉽게).</summary>
        public List<ClueSaveEntry> ToSaveEntries()
        {
            var ids = new SortedSet<string>(_seen, StringComparer.Ordinal);
            foreach (var pair in _persistent)
            {
                var k = pair.Value;
                if (k.TimeRevealed || k.RevealedPersons.Count > 0 || k.RevealedEmotions.Count > 0) ids.Add(pair.Key);
            }

            var entries = new List<ClueSaveEntry>();
            foreach (var id in ids)
            {
                _persistent.TryGetValue(id, out var k);
                entries.Add(new ClueSaveEntry
                {
                    Id = id,
                    Seen = _seen.Contains(id),
                    TimeRevealed = k != null && k.TimeRevealed,
                    Persons = k == null ? new List<string>() : k.RevealedPersons.Select(p => p.ToString()).OrderBy(s => s, StringComparer.Ordinal).ToList(),
                    Emotions = k == null ? new List<string>() : k.RevealedEmotions.Select(e => e.ToString()).OrderBy(s => s, StringComparer.Ordinal).ToList()
                });
            }
            return entries;
        }

        /// <summary>세이브 항목을 지식에 더한다(지우지 않는다 — 게임 시작 시 빈 장부에 한 번 부른다).
        /// 이름을 모르는 태그(태그가 이름이 바뀌거나 빠진 뒤의 옛 세이브)는 건너뛴다.</summary>
        public void Restore(IEnumerable<ClueSaveEntry> entries)
        {
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id)) continue;
                if (entry.Seen) _seen.Add(entry.Id);

                var k = GetKnowledge(entry.Id);
                if (entry.TimeRevealed) k.RevealTime();
                if (entry.Persons != null)
                    foreach (var name in entry.Persons)
                        if (Enum.TryParse<PersonTag>(name, out var person)) k.RevealPerson(person);
                if (entry.Emotions != null)
                    foreach (var name in entry.Emotions)
                        if (Enum.TryParse<EmotionTag>(name, out var emotion)) k.RevealEmotion(emotion);
            }
        }
    }
}
