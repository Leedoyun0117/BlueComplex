using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// "대화와 반응" 노션 문서 "2. 결과에 따른 대사" 표 — 한 턴의 최종 감정 태그 조합(종류만, 개수는 무시)에 맞는
    /// 유키의 결과 대사. 기존 <see cref="ComplexReactionLines"/>(컴플렉스 발동 대사)와는 별개다: 이건 컴플렉스가 아니라
    /// 그 턴의 최종 결과 전체에 붙는다. 표에 없는 조합(태그가 없거나, 침체/흥분 감정이 섞여 있는 등)은 대사가 없다 —
    /// 호출자가 null을 받아 그 턴엔 결과 대사를 건너뛴다.
    /// </summary>
    public sealed class ResultTagLines : ScriptableObject
    {
        public const string ResourcePath = "ResultTagLines";

        [Serializable]
        public struct Entry
        {
            /// <summary>이 대사가 뜨는 감정 태그 조합. 개수는 무시하고 "이 태그들이 전부, 그리고 이 태그들만" 있어야 매칭된다.</summary>
            public EmotionTag[] emotions;

            /// <summary>인스펙터에서 알아보기 위한 이름표("침체 / 슬픔" 등). 조회에는 쓰지 않는다.</summary>
            public string label;

            public string line;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        /// <summary>finalTags에 있는 감정 태그 종류의 집합과 정확히 일치하는 표 항목을 찾는다. 없으면 null.</summary>
        public string Pick(TagSet finalTags)
        {
            if (finalTags == null) return null;

            var present = new HashSet<EmotionTag>(finalTags.Emotions.Keys);
            if (present.Count == 0) return null;

            foreach (var entry in _entries)
            {
                if (entry.emotions == null || string.IsNullOrWhiteSpace(entry.line)) continue;
                if (entry.emotions.Length != present.Count) continue;
                if (entry.emotions.All(present.Contains)) return entry.line;
            }

            return null;
        }
    }

    /// <summary>결과 대사 조회. 에셋이 없거나 일치하는 조합이 없으면 null.</summary>
    public static class ResultTagReactions
    {
        private static ResultTagLines _lines;

        public static string Pick(TagSet finalTags)
        {
            if (_lines == null) _lines = Resources.Load<ResultTagLines>(ResultTagLines.ResourcePath);
            return _lines != null ? _lines.Pick(finalTags) : null;
        }
    }
}
