using System;
using System.Collections.Generic;

namespace BlueComplex.Core.Save
{
    /// <summary>
    /// 세이브 파일 한 개의 내용. 지금은 단서 지식(<see cref="Clues"/>)만 담는다 — 스테이지 진행도 같은 것을 저장하게 되면
    /// 여기에 필드를 더한다(옛 파일에는 그 필드가 없어 기본값으로 읽힌다). 형식을 깨는 변경이면 <see cref="Version"/>을 올리고 읽을 때 옮긴다.
    ///
    /// JsonUtility로 직렬화하므로 공개 필드와 [Serializable]만 쓴다(프로퍼티·Dictionary는 저장되지 않는다).
    /// 태그는 enum 숫자가 아니라 이름 문자열로 둔다 — enum 순서가 바뀌어도 옛 세이브가 엉뚱한 태그로 풀리지 않게.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public List<ClueSaveEntry> Clues = new();
    }

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
