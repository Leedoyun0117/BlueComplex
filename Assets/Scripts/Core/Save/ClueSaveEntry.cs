using System;
using System.Collections.Generic;

namespace BlueComplex.Core.Save
{
    /// <summary>단서 하나의 영구 지식 — 본 적 있는가, 그리고 확정된(CommitRun을 거친) 해금 태그.</summary>
    [Serializable]
    public sealed class ClueSaveEntry
    {
        public string Id;
        public bool Seen;
        public bool TimeRevealed;
        public List<string> Persons = new();
        public List<string> Emotions = new();
    }
}
