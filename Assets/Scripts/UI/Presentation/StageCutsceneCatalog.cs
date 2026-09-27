using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 종료 컷신 번호(<see cref="BlueComplex.Core.Stage.StageCutscenePlan"/>) → 컷신 프리팹(Assets/TimeLine/Prefabs/NN_이름) 표.
    /// 프리팹 이름 앞의 두 자리 번호가 컷신 번호다. Resources에 두어 런타임에 <see cref="StageCutsceneHost"/>가 읽고 — 프리팹을 씬에 미리 올려 두지 않고 재생할 때마다 새로 만든다.
    /// 자산은 에디터 도구(BlueComplex/Cutscene/Rebuild Cutscene Catalog)가 폴더를 훑어 다시 만든다.
    /// </summary>
    public sealed class StageCutsceneCatalog : ScriptableObject
    {
        public const string ResourcePath = "StageCutsceneCatalog";

        [Serializable]
        public struct Entry
        {
            public int number;
            public GameObject prefab;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        public static StageCutsceneCatalog Load() => Resources.Load<StageCutsceneCatalog>(ResourcePath);

        /// <summary>번호의 컷신 프리팹. 표에 없으면 null.</summary>
        public GameObject Find(int number)
        {
            foreach (var entry in _entries)
                if (entry.number == number && entry.prefab != null) return entry.prefab;

            return null;
        }

        /// <summary>프리팹 이름("07_Yuki_Requiem")의 앞 숫자를 컷신 번호로 읽는다. 숫자로 시작하지 않으면 false.</summary>
        public static bool TryParseNumber(string prefabName, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(prefabName)) return false;

            var digits = 0;
            while (digits < prefabName.Length && char.IsDigit(prefabName[digits])) digits++;
            return digits > 0 && int.TryParse(prefabName.Substring(0, digits), out number);
        }

#if UNITY_EDITOR
        public void SetEntries(Entry[] entries) => _entries = entries ?? Array.Empty<Entry>();
#endif
    }
}
