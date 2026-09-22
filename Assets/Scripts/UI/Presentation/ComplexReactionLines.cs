using System;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 컴플렉스 발동 대사 표(컴플렉스 id → 유키의 짧은 대사 목록). 대사는 코드가 아니라 이 에셋(Assets/Resources/ComplexReactionLines)에 있다 —
    /// 기획서의 "반응 대사"/"바리에이션" 표를 그대로 옮기는 자리라 인스펙터에서 컴플렉스마다 줄을 늘리면 된다.
    /// 한 컴플렉스에 대사가 둘 이상이면 발동할 때마다 그중 하나를 무작위로 뽑는다(하나면 늘 그 대사).
    /// Resources에 두는 이유는 <see cref="UiIconCatalog"/>와 같다: 프리팹 배선 없이 어디서든 같은 표를 읽는다.
    /// </summary>
    public sealed class ComplexReactionLines : ScriptableObject
    {
        public const string ResourcePath = "ComplexReactionLines";

        [Serializable]
        public struct Entry
        {
            public string complexId;

            /// <summary>인스펙터에서 어느 컴플렉스인지 알아보기 위한 이름표. 조회에는 쓰지 않는다.</summary>
            public string label;

            public string[] lines;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        /// <summary>이 컴플렉스의 대사 하나. 표에 없거나 비어 있으면 null — 호출자가 공통 문구로 대체한다.</summary>
        /// <param name="range">[0, count) 범위의 인덱스를 고르는 함수. null이면 UnityEngine.Random(검증용으로 바꿔 끼울 수 있게 열어 둔다).</param>
        public string Pick(string complexId, Func<int, int> range = null)
        {
            if (string.IsNullOrEmpty(complexId)) return null;

            foreach (var entry in _entries)
            {
                if (entry.complexId != complexId) continue;

                return PickLine(entry.lines, range);
            }

            return null;
        }

        private static string PickLine(string[] lines, Func<int, int> range)
        {
            if (lines == null) return null;

            // 인스펙터에서 늘린 빈 칸은 대사가 아니다.
            var count = 0;
            foreach (var line in lines)
                if (!string.IsNullOrWhiteSpace(line)) count++;
            if (count == 0) return null;

            var pick = count == 1 ? 0 : (range ?? DefaultRange)(count);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (pick-- == 0) return line.Trim();
            }

            return null;
        }

        private static int DefaultRange(int count) => UnityEngine.Random.Range(0, count);
    }

    /// <summary>발동 대사 조회. 에셋이 없으면(또는 그 컴플렉스 칸이 비어 있으면) null.</summary>
    public static class ComplexReactions
    {
        private static ComplexReactionLines _lines;

        public static string Pick(string complexId, Func<int, int> range = null)
        {
            if (_lines == null) _lines = Resources.Load<ComplexReactionLines>(ComplexReactionLines.ResourcePath);
            return _lines != null ? _lines.Pick(complexId, range) : null;
        }
    }
}
