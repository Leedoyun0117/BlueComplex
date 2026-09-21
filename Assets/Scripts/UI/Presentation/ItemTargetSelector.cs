using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Items;
using BlueComplex.UI.Layout;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>대상 선택 모드에서 강조되는 뷰(컴플렉스 행·뇌 영역·단서 카드). 강조를 켜고 끄기만 한다 — 클릭은 뷰가 <see cref="ItemTargetSelector.TryPick(ComplexInstance)"/>로 넘긴다.</summary>
    public interface ITargetHighlight
    {
        void SetTargetable(bool on);
    }

    /// <summary>
    /// 아이템 대상 선택 모드: 아이템 클릭 → 고를 수 있는 대상이 강조됨 → 대상 클릭 → 사용. 취소는 우클릭 / Esc / 같은 아이템 다시 클릭.
    /// 코어의 <c>TurnRunner.GetItemTargets</c>가 돌려준 대상만 강조되고 클릭으로 고를 수 있다 — 무엇이 유효한 대상인지는 UI가 아니라 코어(아이템 행동)가 정한다.
    ///
    /// 대상을 고르면 <c>onPicked</c>로 알릴 뿐 아이템 사용(<c>UseItem</c>)은 호출자(ItemController)가 한다. 모드 중에는 단서를 끌 수 없고(ClueCardDragHandler),
    /// 단서·컴플렉스 클릭은 상세 팝업이 아니라 대상 선택으로 처리된다(각 뷰가 <see cref="TryPick(ClueInstance)"/> 등을 먼저 부른다).
    /// 화면 위쪽에 "대상을 고르세요" 안내 한 줄이 뜬다. 스크립트는 런타임에 ItemController가 붙인다(프리팹 배선 없음).
    /// </summary>
    public sealed class ItemTargetSelector : MonoBehaviour
    {
        /// <summary>대상을 고르는 중이면 그 선택기, 아니면 null.</summary>
        public static ItemTargetSelector Active { get; private set; }

        private ItemDefinition _item;
        private IReadOnlyList<ItemTarget> _targets = Array.Empty<ItemTarget>();
        private ItemSlotView _slot;
        private Action<ItemTarget> _onPicked;
        private readonly List<ITargetHighlight> _highlighted = new();

        private RectTransform _hint;
        private TMP_Text _hintText;
        private Tween _hintTween;

        public bool IsSelecting => _item != null;
        public ItemDefinition Item => _item;

        // 정적 진입점 — 뷰(컴플렉스 행, 단서 카드, 뇌 영역)의 클릭 핸들러가 부른다. true면 클릭을 선택기가 먹었으니 원래 동작(상세 팝업 등)은 하지 않는다.
        public static bool TryPick(ComplexInstance complex) => Active != null && Active.Pick(complex);
        public static bool TryPick(ClueInstance card) => Active != null && Active.Pick(card);

        /// <summary>선택 모드를 시작한다. <paramref name="slot"/>은 선택된 아이템 칸(강조용, null 가능).</summary>
        public void Begin(ItemDefinition item, IReadOnlyList<ItemTarget> targets, ItemSlotView slot, Action<ItemTarget> onPicked)
        {
            Cancel();

            _item = item;
            _targets = targets;
            _slot = slot;
            _onPicked = onPicked;
            Active = this;

            if (_slot != null) _slot.SetSelected(true);
            HighlightViews();
            ShowHint($"\"{item.DisplayName}\" — 대상 {TargetName(item.TargetKind)}를 고르세요  ·  우클릭 / Esc로 취소", persistent: true);
        }

        public void Cancel()
        {
            if (_item == null) return;

            ClearHighlights();
            if (_slot != null) _slot.SetSelected(false);
            _item = null;
            _slot = null;
            _onPicked = null;
            _targets = Array.Empty<ItemTarget>();
            if (Active == this) Active = null;
            HideHint();
        }

        /// <summary>안내 한 줄을 잠깐(또는 취소될 때까지) 띄운다 — 사용할 수 없는 아이템을 눌렀을 때의 이유 표시에도 쓴다.</summary>
        public void ShowMessage(string message) => ShowHint(message, persistent: false);

        private void Update()
        {
            if (_item == null) return;

            var cancelPressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                                || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
            if (cancelPressed) Cancel();
        }

        private void OnDisable() => Cancel();

        private bool Pick(ComplexInstance complex)
        {
            var target = _targets.OfType<ComplexTarget>().FirstOrDefault(t => t.Complex == complex);
            return Choose(target, ItemTargetKind.Complex);
        }

        private bool Pick(ClueInstance card)
        {
            var target = _targets.OfType<ClueTarget>().FirstOrDefault(t => t.Card == card);
            return Choose(target, ItemTargetKind.Clue);
        }

        private bool Choose(ItemTarget target, ItemTargetKind clickedKind)
        {
            if (_item == null) return false;

            // 다른 종류의 것을 눌렀으면(예: 컴플렉스를 골라야 하는데 단서를 눌렀다) 모드를 유지한 채 이유만 알린다.
            if (target == null)
            {
                ShowMessage(clickedKind == _item.TargetKind
                    ? "그 대상은 고를 수 없습니다 — 강조된 것 중에서 고르세요"
                    : $"지금은 {TargetName(_item.TargetKind)}를 골라야 합니다");
                return true;
            }

            var picked = _onPicked;
            Cancel();
            picked?.Invoke(target);
            return true;
        }

        private static string TargetName(ItemTargetKind kind) => kind switch
        {
            ItemTargetKind.Complex => "컴플렉스",
            ItemTargetKind.Clue => "단서",
            _ => "것"
        };

        // ------------------------------------------------------------------
        // 강조
        // ------------------------------------------------------------------

        private void HighlightViews()
        {
            var root = transform.root;
            foreach (var target in _targets)
            {
                switch (target)
                {
                    case ComplexTarget complexTarget:
                        foreach (var row in root.GetComponentsInChildren<ComplexRowView>(true))
                            if (row.Complex == complexTarget.Complex) Highlight(row);
                        foreach (var region in root.GetComponentsInChildren<BrainRegionView>(true))
                            if (region.Complex == complexTarget.Complex) Highlight(region);
                        break;

                    case ClueTarget clueTarget:
                        foreach (var view in root.GetComponentsInChildren<ClueCardView>(true))
                            if (view.Card == clueTarget.Card) Highlight(view);
                        break;
                }
            }
        }

        private void Highlight(ITargetHighlight view)
        {
            view.SetTargetable(true);
            _highlighted.Add(view);
        }

        private void ClearHighlights()
        {
            foreach (var view in _highlighted)
            {
                // 대상이 사용으로 파괴됐을 수 있다(컴플렉스 행·단서 카드가 다시 그려지는 등).
                if (view is UnityEngine.Object unityObject && unityObject == null) continue;
                view.SetTargetable(false);
            }

            _highlighted.Clear();
        }

        // ------------------------------------------------------------------
        // 안내 한 줄
        // ------------------------------------------------------------------

        private void ShowHint(string message, bool persistent)
        {
            EnsureHint();
            _hintTween?.Kill();
            _hintText.text = message;
            _hint.gameObject.SetActive(true);

            var group = _hint.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            if (persistent) return;

            _hintTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .AppendInterval(1.6f)
                .Append(group.DOFade(0f, 0.3f))
                .OnComplete(() => { if (!IsSelecting) _hint.gameObject.SetActive(false); });
        }

        private void HideHint()
        {
            if (_hint == null) return;

            _hintTween?.Kill();
            _hint.gameObject.SetActive(false);
        }

        private void EnsureHint()
        {
            if (_hint != null) return;

            var canvasRoot = transform.root;
            var font = RuntimeUi.FindFont(canvasRoot);

            // 모니터 왼쪽 아래, 쿼터 진행 포스트잇 왼쪽의 빈 자리(화면 비율).
            _hint = RuntimeUi.CreateRect(canvasRoot, "Item Target Hint", new Vector2(0.20f, 0.815f), new Vector2(0.565f, 0.865f), Vector2.zero, Vector2.zero);
            _hint.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            var background = _hint.gameObject.AddComponent<Image>();
            background.sprite = RuntimeUi.RoundedRect;
            background.type = Image.Type.Sliced;
            background.color = new Color32(20, 26, 32, 230);
            background.raycastTarget = false;

            _hintText = RuntimeUi.CreateText(_hint, "Text", string.Empty, font, 24f, new Color32(255, 226, 120, 255),
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            _hintText.textWrappingMode = TextWrappingModes.NoWrap;
            _hint.gameObject.SetActive(false);
        }
    }
}
