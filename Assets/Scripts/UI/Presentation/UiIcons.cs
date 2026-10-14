using System;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
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
