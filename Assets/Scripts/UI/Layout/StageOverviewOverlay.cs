using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 전체 스테이지 오버레이(UI 가이드 "쿼터 및 스테이지 UI"). 쿼터 진행 창을 클릭하면 열리고, 빈 곳 아무데나 클릭하면 닫힌다.
    /// 반투명 판넬이 플레이 화면을 덮고 그 위에 쿼터 블록이 쿼터 수만큼 나열된다 — 블록마다 쿼터 번호, 턴 칸(현재 쿼터엔 하얀 점),
    /// 목표 심박수 숫자, 키 획득 여부(파란 아이콘 / 빈칸). 현재 쿼터의 블록은 크게, 나머지는 작고 흐리게 그린다.
    ///
    /// 아이템·단서·키 UI는 판넬에 가려지지 않는다: 열릴 때 그 오브젝트들을 판넬 위로 올려 두고(닫으면 원래 순서로 되돌린다),
    /// 동시에 레이캐스트를 꺼서 판넬 밑에 있는 것처럼 클릭·드래그·호버가 먹지 않게 한다 — 보이기만 하고 클릭하면 닫힘으로 이어진다.
    /// 표시만 한다 — 판정 없음.
    /// </summary>
    public sealed class StageOverviewOverlay : MonoBehaviour, IPointerClickHandler
    {
        private const float CurrentScale = 1f;
        private const float OtherScale = 0.78f;
        private const float OtherAlpha = 0.7f;
        private const float ScaleDuration = 0.2f;

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color BlockColor = new Color32(24, 24, 30, 240);
        private static readonly Color CurrentFrameColor = Color.white;
        private static readonly Color IdleFrameColor = new Color(1f, 1f, 1f, 0f);
        private static readonly Color LabelColor = new Color32(170, 170, 180, 255);
        private static readonly Color ObtainedKeyColor = new Color32(70, 150, 255, 255);

        private sealed class QuarterBlock
        {
            public RectTransform Visual;
            public CanvasGroup Group;
            public Image Frame;
            public TurnTrack Track;
            public TMP_Text Target;
            public KeyGlyph Key;
            public bool IsCurrent;
        }

        private readonly struct RaisedElement
        {
            public readonly Transform Transform;
            public readonly int SiblingIndex;
            public readonly CanvasGroup Group;
            public readonly bool BlockedRaycasts;

            public RaisedElement(Transform transform, int siblingIndex, CanvasGroup group)
            {
                Transform = transform;
                SiblingIndex = siblingIndex;
                Group = group;
                BlockedRaycasts = group.blocksRaycasts;
            }
        }

        private readonly List<QuarterBlock> _blocks = new();
        private readonly List<RaisedElement> _raised = new();
        private Transform[] _keepVisible = Array.Empty<Transform>();

        public bool IsOpen => gameObject.activeSelf;

        public event Action Opened;
        public event Action Closed;

        /// <param name="keepVisible">열려 있는 동안 판넬 위에 남아야 하는 오브젝트(아이템·단서·키 UI). 오버레이보다 먼저
        /// 만들어진(형제 순서가 앞선) 것이어야 닫을 때 원래 순서가 정확히 복원된다.</param>
        public static StageOverviewOverlay Create(Transform canvasRoot, TMP_FontAsset font, int quarterCount, int turnsPerQuarter,
            IEnumerable<Transform> keepVisible)
        {
            var rect = RuntimeUi.CreateStretched(canvasRoot, "Stage Overview Overlay");
            var overlay = rect.gameObject.AddComponent<StageOverviewOverlay>();
            overlay.Build(font, quarterCount, turnsPerQuarter);

            var keep = new List<Transform>();
            foreach (var element in keepVisible)
                if (element != null) keep.Add(element);
            overlay._keepVisible = keep.ToArray();

            rect.gameObject.SetActive(false);
            return overlay;
        }

        /// <summary>현재 상태를 블록에 반영한다. 열려 있든 닫혀 있든 부르면 된다(닫힌 동안은 보이지 않을 뿐).</summary>
        /// <param name="targetTexts">쿼터별 목표 심박수 문구(인덱스 0 = 1쿼터).</param>
        /// <param name="results">쿼터별 키 결과(true=획득). 획득한 쿼터만 파란 아이콘이 켜진다.</param>
        public void Refresh(int currentQuarter, int turnInQuarter, IReadOnlyList<string> targetTexts,
            IReadOnlyList<bool?> results, bool animate)
        {
            for (var i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                var isCurrent = i + 1 == currentQuarter;

                ApplyEmphasis(block, isCurrent, animate);
                block.Track.SetTurn(isCurrent ? turnInQuarter : 0, animate);
                block.Target.text = i < targetTexts.Count ? targetTexts[i] : "-";
                block.Key.SetVisible(i < results.Count && results[i] == true);
            }
        }

        public void Show()
        {
            if (IsOpen) return;

            // 판넬(자신)을 맨 위로 올린 뒤 그 위로 아이템·단서·키 UI를 다시 올린다. 원래 자리는 닫을 때 복원한다.
            _raised.Clear();
            foreach (var element in _keepVisible)
            {
                if (!element.TryGetComponent<CanvasGroup>(out var group))
                    group = element.gameObject.AddComponent<CanvasGroup>();
                _raised.Add(new RaisedElement(element, element.GetSiblingIndex(), group));
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            foreach (var raised in _raised)
            {
                raised.Group.blocksRaycasts = false;
                raised.Transform.SetAsLastSibling();
            }

            Opened?.Invoke();
        }

        public void Hide()
        {
            if (!IsOpen) return;

            // 원래 형제 인덱스가 작은 것부터 되꽂아야 뒤쪽 인덱스가 어긋나지 않는다.
            _raised.Sort((a, b) => a.SiblingIndex.CompareTo(b.SiblingIndex));
            foreach (var raised in _raised)
            {
                if (raised.Transform == null) continue;
                raised.Transform.SetSiblingIndex(raised.SiblingIndex);
                raised.Group.blocksRaycasts = raised.BlockedRaycasts;
            }

            _raised.Clear();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>판넬 어디를 눌러도 닫힌다 — 내용물은 전부 레이캐스트를 받지 않으므로 클릭은 항상 여기로 온다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            Hide();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Hide();
        }

        private void Build(TMP_FontAsset font, int quarterCount, int turnsPerQuarter)
        {
            var backdrop = gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            // 심박수 모니터(맨 위)는 판넬 밑으로 흐리게라도 보이도록 띠 위로 비워 둔다 — 목표 숫자를 현재 심박수와 견줘볼 수 있다.
            // 판넬 위로 올라오는 아이템(우측 열)·키 카드(x 0.61~0.85, y 0.29~0.44)·단서(y 0.05~0.26)와 겹치지 않도록 왼쪽 가운데에 둔다.
            var content = RuntimeUi.CreateRect(transform, "Content", new Vector2(0.14f, 0.46f), new Vector2(0.58f, 0.80f),
                Vector2.zero, Vector2.zero);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (var quarter = 1; quarter <= quarterCount; quarter++)
                _blocks.Add(BuildBlock(content, font, quarter, turnsPerQuarter));
        }

        private static QuarterBlock BuildBlock(Transform parent, TMP_FontAsset font, int quarter, int turnsPerQuarter)
        {
            // 슬롯은 레이아웃이 크기를 정하는 고정 칸이고, 실제로 그려지는 Visual만 스케일로 키우고 줄인다 —
            // 글자까지 함께 커지고, 강조가 바뀌어도 이웃 블록이 밀리지 않는다.
            var slot = RuntimeUi.CreateStretched(parent, $"Quarter Slot {quarter}");
            var visual = RuntimeUi.CreateStretched(slot, "Visual");
            var group = visual.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var frame = RuntimeUi.CreateImage(visual, "Frame", IdleFrameColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RuntimeUi.CreateImage(visual, "Body", BlockColor, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

            RuntimeUi.CreateText(visual, "Title", $"{quarter}분기점", font, 30f, Color.white, TextAlignmentOptions.Center,
                new Vector2(0f, 0.80f), new Vector2(1f, 0.98f)).fontStyle = FontStyles.Bold;

            var track = new TurnTrack(visual, "Turn Track", new Vector2(0.10f, 0.62f), new Vector2(0.90f, 0.78f), turnsPerQuarter, 16f);

            RuntimeUi.CreateText(visual, "Target Label", "목표 심박수", font, 16f, LabelColor, TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.61f));
            var target = RuntimeUi.CreateText(visual, "Target", "-", font, 44f, Color.white, TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.50f));
            target.fontStyle = FontStyles.Bold;

            // 키 칸: 어두운 빈칸은 항상 있고, 획득했을 때만 그 안에 파란 아이콘이 켜진다.
            var keySlot = RuntimeUi.CreateImage(visual, "Key Slot", RuntimeUi.SlotColor,
                new Vector2(0.38f, 0.04f), new Vector2(0.62f, 0.25f), Vector2.zero, Vector2.zero);
            var key = new KeyGlyph(keySlot.transform, "Key");
            key.SetColor(ObtainedKeyColor);
            key.SetVisible(false);

            return new QuarterBlock { Visual = visual, Group = group, Frame = frame, Track = track, Target = target, Key = key };
        }

        private static void ApplyEmphasis(QuarterBlock block, bool isCurrent, bool animate)
        {
            var scale = isCurrent ? CurrentScale : OtherScale;
            block.Frame.color = isCurrent ? CurrentFrameColor : IdleFrameColor;
            block.Group.alpha = isCurrent ? 1f : OtherAlpha;

            block.Visual.DOKill();
            if (animate && block.IsCurrent != isCurrent)
                block.Visual.DOScale(scale, ScaleDuration).SetEase(Ease.OutQuad);
            else
                block.Visual.localScale = Vector3.one * scale;

            block.IsCurrent = isCurrent;
        }

        private void OnDisable()
        {
            foreach (var block in _blocks)
            {
                block.Visual.DOKill();
                block.Track.Kill();
                block.Key.Kill();
            }
        }
    }
}
