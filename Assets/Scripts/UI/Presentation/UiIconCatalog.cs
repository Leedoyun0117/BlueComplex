using System;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// UI 아이콘 id → 스프라이트 표. 아이템/단서/키 아이콘이 여기 모인다 — 아트를 바꾸려면 PNG(Assets/Art/UI/Icons)만 교체하고
    /// 메뉴 BlueComplex/UI/Rebuild Icon Catalog를 다시 돌리면 된다. id는 파일 이름이다(아이템 id, 단서 id, "key", "placeholder").
    /// Resources에 두는 이유: 코드로 짓는 UI(키 카드 등)도 프리팹 배선 없이 같은 표를 읽게 하려는 것이다.
    /// </summary>
    public sealed class UiIconCatalog : ScriptableObject
    {
        public const string ResourcePath = "UiIconCatalog";
        public const string PlaceholderId = "placeholder";
        public const string KeyId = "key";

        [Serializable]
        public struct Entry
        {
            public string Id;
            public Sprite Sprite;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public Sprite Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var entry in _entries)
                if (entry.Id == id) return entry.Sprite;
            return null;
        }

#if UNITY_EDITOR
        public void SetEntries(Entry[] entries) => _entries = entries;
#endif
    }

    /// <summary>카탈로그 조회. 도장 없는 id는 placeholder 아이콘으로 돌려준다 — 아트가 아직 없는 단서/아이템도 빈 칸으로 보이지 않게.</summary>
    public static class UiIcons
    {
        private static UiIconCatalog _catalog;

        private static UiIconCatalog Catalog
        {
            get
            {
                if (_catalog == null) _catalog = Resources.Load<UiIconCatalog>(UiIconCatalog.ResourcePath);
                return _catalog;
            }
        }

        /// <summary>id의 아이콘, 없으면 placeholder, 카탈로그 자체가 없으면 null(호출자는 아이콘 이미지를 감춘다).</summary>
        public static Sprite Get(string id)
        {
            var catalog = Catalog;
            if (catalog == null) return null;
            return catalog.Find(id) ?? catalog.Find(UiIconCatalog.PlaceholderId);
        }

        /// <summary>placeholder로 대체하지 않는 조회 — 키 아이콘처럼 "없으면 도형으로 그린다"가 필요한 곳.</summary>
        public static Sprite GetExact(string id) => Catalog != null ? Catalog.Find(id) : null;
    }
}
