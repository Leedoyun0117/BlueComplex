using System.Collections.Generic;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 키 표시 창(UI 가이드 11번) — 목업의 살짝 기울어진 흰 카드. 위쪽 두 모서리에 테이프로 붙어 있고, "키" 글자 아래에 쿼터 수만큼의 칸이 항상 떠 있다.
    /// 칸 i는 (i+1)쿼터의 키 획득 여부를 보여준다. 못 얻었으면 회색 열쇠 실루엣, 얻었으면 파란 열쇠.
    /// 키를 얻는 순간에는 빈 칸에 열쇠가 위에서 떨어져 "탁" 찍힌다(<see cref="KeyGlyph.PlayStamp"/>). 표시만 한다 — 판정 없음.
    /// (스테이지 클리어 연출 — 자물쇠에 키가 들어가며 열림 — 은 이 카드 다음 단계라 아직 없다.)
    /// </summary>
    public sealed class KeyStatusPanel : MonoBehaviour
    {
        private static readonly Color EmptyKeyColor = new Color32(110, 110, 114, 255);
        private static readonly Color ObtainedKeyColor = new Color32(24, 143, 204, 255);
        private static readonly Color FlashKeyColor = new Color32(214, 240, 255, 255);
        private static readonly Color TapeColor = new Color32(232, 222, 176, 205);

        private const float CardTiltDegrees = -6f;

        /// <summary>칸마다 종이를 손으로 붙인 것처럼 살짝씩 다르게 기울인다(카드 기울기에 더해진다).</summary>
        private static readonly float[] SlotTilts = { -3f, 2f, -2f };

        private KeyGlyph[] _glyphs;
        private Image[] _slotImages;
        private bool[] _obtained;

        public static KeyStatusPanel Create(Transform parent, int quarterCount, Vector2 anchorMin, Vector2 anchorMax,
            TMP_FontAsset font)
        {
            var rect = RuntimeUi.CreateRect(parent, "Key Status Panel", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            rect.localRotation = Quaternion.Euler(0f, 0f, CardTiltDegrees);
            var panel = rect.gameObject.AddComponent<KeyStatusPanel>();
            panel.Build(quarterCount, font);
            return panel;
        }

        /// <summary>results[i] == true인 칸만 파랗게 칠한다(false=실패, null=판정 전은 둘 다 회색).
        /// animate이고 그 칸이 방금 처음 획득된 것이면 파랗게 바뀌는 대신 열쇠가 찍히는 연출을 한다. 그 외 변화는 바로 반영한다(되돌리기 포함).</summary>
        public void SetResults(IReadOnlyList<bool?> results, bool animate)
        {
            for (var i = 0; i < _glyphs.Length; i++)
            {
                var obtained = i < results.Count && results[i] == true;
                if (obtained == _obtained[i]) continue;

                _obtained[i] = obtained;

                if (obtained && animate)
                {
                    Stamp(i);
                    continue;
                }

                _glyphs[i].Kill();
                _glyphs[i].SetColor(obtained ? ObtainedKeyColor : EmptyKeyColor);
            }
        }

        private void Stamp(int index)
        {
            var slot = _slotImages[index];
            _glyphs[index].PlayStamp(ObtainedKeyColor, FlashKeyColor, () =>
            {
                UiSoundHooks.Play(UiSoundCue.Key);
                FlashSlot(slot);
            });
        }

        /// <summary>열쇠가 닿는 순간 칸 종이도 살짝 밝아졌다 돌아온다.</summary>
        private static void FlashSlot(Image slot)
        {
            if (slot == null) return;

            slot.DOKill();
            slot.color = Color.white;
            slot.DOColor(MockupStyle.Card, UiMotion.Settings.keyFlash).SetEase(Ease.OutQuad).SetUpdate(true);
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
            _slotImages = new Image[quarterCount];
            _obtained = new bool[quarterCount];
            for (var i = 0; i < quarterCount; i++)
            {
                var slot = RuntimeUi.CreateImage(row, $"Key Slot {i + 1}", MockupStyle.Card, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero);
                slot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, SlotTilts[i % SlotTilts.Length]);
                MockupStyle.AddPaperEdge(slot.gameObject, shadow: false);
                _slotImages[i] = slot;

                var inner = RuntimeUi.CreateRect(slot.transform, "Inner", Vector2.zero, Vector2.one,
                    new Vector2(12f, 12f), new Vector2(-12f, -12f));
                _glyphs[i] = new KeyGlyph(inner, "Key");
                _glyphs[i].SetColor(EmptyKeyColor);
            }

            // 테이프는 칸 위에 얹혀야 하므로 마지막에 만든다.
            BuildTape("Tape Left", new Vector2(0.005f, 0.985f), 38f);
            BuildTape("Tape Right", new Vector2(0.995f, 0.985f), -38f);
        }

        /// <summary>카드 위쪽 모서리에 비스듬히 붙은 반투명 테이프 조각.</summary>
        private void BuildTape(string name, Vector2 anchor, float degrees)
        {
            var tape = RuntimeUi.CreateImage(transform, name, TapeColor, anchor, anchor, Vector2.zero, Vector2.zero);
            var rect = tape.rectTransform;
            rect.sizeDelta = new Vector2(78f, 26f);
            rect.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        private void OnDisable()
        {
            if (_glyphs == null) return;

            foreach (var glyph in _glyphs) glyph.Kill();
            foreach (var slot in _slotImages)
            {
                if (slot == null) continue;
                slot.DOKill();
                slot.color = MockupStyle.Card;
            }
        }
    }
}
