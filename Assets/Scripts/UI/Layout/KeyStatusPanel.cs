using System.Collections.Generic;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 키 표시 창(UI 가이드 11번) — 목업의 살짝 기울어진 흰 카드. 위쪽 두 모서리에 테이프로 붙어 있고, "키" 글자 아래에 쿼터 수만큼의 칸이 항상 떠 있다.
    /// 칸 i는 (i+1)쿼터의 키 획득 여부를 보여준다. 못 얻었으면 회색 열쇠 실루엣, 얻었으면 파란 열쇠.
    /// 키를 얻는 순간에는 빈 칸에 열쇠가 위에서 떨어져 "탁" 찍힌다(<see cref="KeyGlyph.PlayStamp"/>). 표시만 한다 — 판정 없음.
    /// (스테이지 클리어 연출 — 자물쇠에 키가 들어가며 열림 — 은 이 카드 다음 단계라 아직 없다.)
    ///
    /// 표시 타이밍(기획 문서에 없는 새 규칙): 평소엔 단서 패널 뒤에 접혀 있고 윗변만 살짝(<see cref="PeekFraction"/>) 삐져나와 있다.
    /// 열림 영역(단서 패널의 제목 줄)이나 삐져나온 카드 끝에 포인터를 올리면 패널 윗변에서 서랍처럼 미끄러져 올라온다. 카드는 RectMask2D 서랍(<see cref="Drawer"/>) 안에 있어
    /// 단서 패널 윗변 아래로는 잘려서 접힌 동안 삐져나온 끝 말고는 보이지 않는다. 포인터가 열림 영역과 카드를 모두 벗어나면 짧은 유예 뒤 다시 들어간다.
    /// 키를 얻은 순간엔 열쇠 찍히는 연출이 안 보이면 안 되므로 스스로 잠깐 열렸다 닫힌다(<see cref="UiMotionSettings.keyPeekHold"/>).
    /// </summary>
    public sealed class KeyStatusPanel : MonoBehaviour
    {
        private static readonly Color EmptyKeyColor = new Color32(110, 110, 114, 255);
        private static readonly Color ObtainedKeyColor = new Color32(24, 143, 204, 255);
        private static readonly Color FlashKeyColor = new Color32(214, 240, 255, 255);
        private static readonly Color TapeColor = new Color32(232, 222, 176, 205);

        private const float CardTiltDegrees = -6f;

        /// <summary>서랍 마스크가 카드 좌우로 더 넓게 잡는 폭(화면 비율) — 기울어진 카드 모서리·테이프가 잘리지 않게.</summary>
        private const float DrawerSideMargin = 0.06f;

        /// <summary>접었을 때 카드 윗변 가운데가 서랍 아래 끝(단서 패널 윗변) 위로 삐져나와 있는 정도(카드 높이 대비). 카드가 기울어 있어 왼쪽 끝은 더 올라온다.</summary>
        private const float PeekFraction = 0.15f;

        /// <summary>칸마다 종이를 손으로 붙인 것처럼 살짝씩 다르게 기울인다(카드 기울기에 더해진다).</summary>
        private static readonly float[] SlotTilts = { -3f, 2f, -2f };

        private KeyGlyph[] _glyphs;
        private Image[] _slotImages;
        private bool[] _obtained;

        private RectTransform _rect;
        private Vector2 _openMin;
        private Vector2 _openMax;
        private float _closedShift;
        private float _openness;
        private Tween _slide;
        private Tween _autoClose;
        private int _hovering;

        /// <summary>카드를 자르는 서랍(마스크). 형제 순서를 옮기거나 지울 땐 카드 대신 이걸 쓴다.</summary>
        public RectTransform Drawer { get; private set; }

        /// <summary>열림 영역(투명 히트 영역). <see cref="AttachHotspot"/> 뒤에만 있다.</summary>
        public RectTransform Hotspot { get; private set; }

        public bool IsOpen => _openness > 0.5f;

        /// <param name="anchorMin">열렸을 때 카드 위치(캔버스 비율, 아래쪽 원점).</param>
        /// <param name="clipBottom">서랍 마스크의 아래 끝(캔버스 비율) — 단서 패널의 윗변. 카드는 이 선 아래로는 안 보인다.</param>
        public static KeyStatusPanel Create(Transform parent, int quarterCount, Vector2 anchorMin, Vector2 anchorMax,
            TMP_FontAsset font, float clipBottom)
        {
            var drawerMin = new Vector2(Mathf.Max(0f, anchorMin.x - DrawerSideMargin), clipBottom);
            var drawerMax = new Vector2(Mathf.Min(1f, anchorMax.x + DrawerSideMargin), 1f);
            var drawer = RuntimeUi.CreateRect(parent, "Key Drawer", drawerMin, drawerMax, Vector2.zero, Vector2.zero);
            drawer.gameObject.AddComponent<RectMask2D>();

            // 카드의 앵커는 서랍 안 좌표(서랍 폭·높이 대비)로 바꿔 둔다 — 서랍이 캔버스 일부만 덮기 때문.
            var size = drawerMax - drawerMin;
            var localMin = new Vector2((anchorMin.x - drawerMin.x) / size.x, (anchorMin.y - drawerMin.y) / size.y);
            var localMax = new Vector2((anchorMax.x - drawerMin.x) / size.x, (anchorMax.y - drawerMin.y) / size.y);

            var rect = RuntimeUi.CreateRect(drawer, "Key Status Panel", localMin, localMax, Vector2.zero, Vector2.zero);
            rect.localRotation = Quaternion.Euler(0f, 0f, CardTiltDegrees);
            var panel = rect.gameObject.AddComponent<KeyStatusPanel>();
            panel.Drawer = drawer;
            panel._rect = rect;
            panel._openMin = localMin;
            panel._openMax = localMax;
            panel._closedShift = localMax.y - PeekFraction * (localMax.y - localMin.y); // 카드 윗변이 서랍 아래 끝 위로 PeekFraction만큼만 남는 위치.
            panel.Build(quarterCount, font);
            panel.ApplyOpenness(0f);
            return panel;
        }

        /// <summary>열림 영역을 만든다: 캔버스 위 투명 히트 영역(포인터를 올리면 카드가 열린다). 단서 패널 위에 얹히므로 그 밑의 것을 가린다 —
        /// 제목 줄처럼 상호작용이 없는 자리에만 잡을 것.</summary>
        public void AttachHotspot(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var image = RuntimeUi.CreateImage(parent, "Key Drawer Hotspot", Color.clear, anchorMin, anchorMax,
                Vector2.zero, Vector2.zero, raycastTarget: true);
            Hotspot = image.rectTransform;
            image.gameObject.AddComponent<HoverRelay>().Bind(OnHoverChanged, ignoreWhileDragging: true);
        }

        /// <summary>서랍을 열거나 닫는다(미끄러짐). 열림 영역·카드 위 호버, 키 획득 시 자동 열림이 이걸 부른다.</summary>
        public void SetOpen(bool open)
        {
            _autoClose?.Kill();
            _slide?.Kill();

            var motion = UiMotion.Settings;
            _slide = DOTween.To(() => _openness, ApplyOpenness, open ? 1f : 0f, open ? motion.keyDrawerOpen : motion.keyDrawerClose)
                .SetEase(open ? Ease.OutCubic : Ease.InCubic).SetUpdate(true).SetTarget(this);
        }

        private void ApplyOpenness(float openness)
        {
            _openness = openness;
            var shift = _closedShift * (1f - openness);
            _rect.anchorMin = new Vector2(_openMin.x, _openMin.y - shift);
            _rect.anchorMax = new Vector2(_openMax.x, _openMax.y - shift);
        }

        private void OnHoverChanged(bool inside)
        {
            _hovering = Mathf.Max(0, _hovering + (inside ? 1 : -1));
            if (_hovering > 0)
            {
                _autoClose?.Kill();
                if (_openness < 1f && (_slide == null || !_slide.IsActive())) SetOpen(true);
                return;
            }

            ScheduleClose(UiMotion.Settings.keyDrawerCloseDelay);
        }

        private void ScheduleClose(float delay)
        {
            _autoClose?.Kill();
            _autoClose = DOVirtual.DelayedCall(delay, () =>
            {
                if (_hovering == 0) SetOpen(false);
            }).SetUpdate(true).SetTarget(this);
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
                    PeekAndStamp(i);
                    continue;
                }

                _glyphs[i].Kill();
                _glyphs[i].SetColor(obtained ? ObtainedKeyColor : EmptyKeyColor);
            }
        }

        /// <summary>카드가 접혀 있어도 열쇠가 찍히는 순간은 보여야 한다 — 먼저 열고, 다 열린 뒤 찍고, 잠깐 보여준 뒤 접는다(포인터가 올라와 있으면 유지).</summary>
        private void PeekAndStamp(int index)
        {
            var motion = UiMotion.Settings;
            SetOpen(true);
            DOVirtual.DelayedCall(motion.keyDrawerOpen, () => Stamp(index)).SetUpdate(true).SetTarget(this);

            // 찍히는 데 걸리는 시간(낙하+튕김)이 지난 뒤부터 보여주는 시간을 센다.
            var stampSeconds = motion.keyFall + motion.keyBounce;
            _autoClose?.Kill();
            _autoClose = DOVirtual.DelayedCall(motion.keyDrawerOpen + stampSeconds + motion.keyPeekHold, () =>
            {
                if (_hovering == 0) SetOpen(false);
            }).SetUpdate(true).SetTarget(this);
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
            card.raycastTarget = true; // 열려 있는 카드 위에 포인터가 있는 동안에도 닫히지 않게(접힌 카드는 삐져나온 끝만 마스크를 통과해 반응한다).
            PaperPanel.Skin(card, PaperKind.Panel);
            MockupStyle.AddPaperEdge(gameObject);
            gameObject.AddComponent<HoverRelay>().Bind(OnHoverChanged, ignoreWhileDragging: true); // 삐져나온 끝이 있어, 단서 카드를 끌고 지나가기만 해선 안 열리게.

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
                PaperPanel.Skin(slot, PaperKind.Card);
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
            _slide?.Kill();
            _autoClose?.Kill();
            _hovering = 0;
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

    /// <summary>포인터 진입/이탈을 콜백으로 넘기는 작은 릴레이. 끌기(단서 카드 드래그) 중의 진입은 무시할 수 있다 —
    /// 카드를 끌고 지나가는 것만으로 서랍이 열리지 않게. 이탈은 진입을 받아들인 경우에만 알린다.</summary>
    internal sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private System.Action<bool> _changed;
        private bool _ignoreWhileDragging;
        private bool _inside;

        public void Bind(System.Action<bool> changed, bool ignoreWhileDragging)
        {
            _changed = changed;
            _ignoreWhileDragging = ignoreWhileDragging;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inside || _ignoreWhileDragging && eventData.pointerDrag != null) return;

            _inside = true;
            _changed?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_inside) return;

            _inside = false;
            _changed?.Invoke(false);
        }

        private void OnDisable()
        {
            if (!_inside) return;

            _inside = false;
            _changed?.Invoke(false);
        }
    }
}
