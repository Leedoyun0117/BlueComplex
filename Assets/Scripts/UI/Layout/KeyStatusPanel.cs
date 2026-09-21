using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 키 표시 창(UI 가이드 11번) — 목업의 살짝 기울어진 흰 카드. "키" 글자 아래에 쿼터 수만큼의 칸이 항상 떠 있고, 칸 i는 (i+1)쿼터의 키 획득 여부를 보여준다.
    /// 못 얻었으면 회색, 얻었으면 파란 아이콘. 표시만 한다 — 판정 없음.
    /// </summary>
    public sealed class KeyStatusPanel : MonoBehaviour
    {
        private static readonly Color EmptyKeyColor = new Color32(110, 110, 114, 255);
        private static readonly Color ObtainedKeyColor = new Color32(24, 143, 204, 255);

        private const float CardTiltDegrees = -6f;

        /// <summary>칸마다 종이를 손으로 붙인 것처럼 살짝씩 다르게 기울인다(카드 기울기에 더해진다).</summary>
        private static readonly float[] SlotTilts = { -3f, 2f, -2f };

        private KeyGlyph[] _glyphs;

        public static KeyStatusPanel Create(Transform parent, int quarterCount, Vector2 anchorMin, Vector2 anchorMax,
            TMP_FontAsset font)
        {
            var rect = RuntimeUi.CreateRect(parent, "Key Status Panel", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            rect.localRotation = Quaternion.Euler(0f, 0f, CardTiltDegrees);
            var panel = rect.gameObject.AddComponent<KeyStatusPanel>();
            panel.Build(quarterCount, font);
            return panel;
        }

        /// <summary>results[i] == true인 칸만 파랗게 칠한다(false=실패, null=판정 전은 둘 다 회색).</summary>
        public void SetResults(IReadOnlyList<bool?> results)
        {
            for (var i = 0; i < _glyphs.Length; i++)
                _glyphs[i].SetColor(i < results.Count && results[i] == true ? ObtainedKeyColor : EmptyKeyColor);
        }

        /// <summary>방금 얻은 칸을 잠깐 부풀린다.</summary>
        public void Pop(int index)
        {
            if (index >= 0 && index < _glyphs.Length) _glyphs[index].Pop();
        }

        private void Build(int quarterCount, TMP_FontAsset font)
        {
            var card = gameObject.AddComponent<Image>();
            card.color = MockupStyle.Paper;
            card.raycastTarget = false;
            MockupStyle.AddPaperEdge(gameObject);

            var header = RuntimeUi.CreateText(transform, "Header", "키", font, 28f, MockupStyle.Ink, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 0.76f), new Vector2(0.40f, 0.98f));
            header.fontStyle = FontStyles.Bold;

            var row = RuntimeUi.CreateRect(transform, "Slots", new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.76f),
                Vector2.zero, Vector2.zero);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 26f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _glyphs = new KeyGlyph[quarterCount];
            for (var i = 0; i < quarterCount; i++)
            {
                var slot = RuntimeUi.CreateImage(row, $"Key Slot {i + 1}", MockupStyle.Card, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero);
                slot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, SlotTilts[i % SlotTilts.Length]);
                MockupStyle.AddPaperEdge(slot.gameObject, shadow: false);

                var inner = RuntimeUi.CreateRect(slot.transform, "Inner", Vector2.zero, Vector2.one,
                    new Vector2(12f, 12f), new Vector2(-12f, -12f));
                _glyphs[i] = new KeyGlyph(inner, "Key");
                _glyphs[i].SetColor(EmptyKeyColor);
            }
        }

        private void OnDisable()
        {
            if (_glyphs == null) return;
            foreach (var glyph in _glyphs) glyph.Kill();
        }
    }
}
