using System.Collections.Generic;
using System.Linq;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 생각 공간(말풍선). 손으로 그린 흰 선(<see cref="HandDrawnStrokeGraphic"/>, 채우지 않은 열린 곡선)만 그리고, 평소엔 보이지 않는다.
    /// 단서를 집으면(드래그 시작) 선이 한 바퀴 돌며 그려지듯 나타나고(그릴 때마다 모양이 조금씩 다르다), 놓으면 지우개로 지우듯 사라진다.
    /// 놓은 단서로 턴 연출이 시작됐다면 연출이 끝날 때까지(Presenter가 <see cref="SetEngaged"/>로 알린다) 선을 그대로 둔다.
    /// 시퀀스가 끝나면 남은 감정 태그가 풍선 안에 색 칩으로 나타나고(감정별 색), 가만히 읽힌 뒤 위로 떠오르며 사라진다. UI 디자인 가이드 원문: "태그는 위로 올라가며 서서히
    /// 사라지며, 그와 동시에 인디케이터가 움직인다" — 이 클래스는 자기 애니메이션만 알고, 심박수
    /// 이동과 나란히 맞추는 건 3단계 CinematicTurnResultPresenter가 쥔다.
    /// 드롭 판정은 이 선이 아니라 프리팹의 DropZone(선보다 사방 32px 넉넉한 사각형)이 받는다 — 선 모양은 판정과 무관하다.
    /// </summary>
    public sealed class MemorySpaceBubble : MonoBehaviour
    {

        [SerializeField] private Image _bubbleBackground;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TMP_Text _summaryText;

        private ITurnResultPresenter _presenter;
        private ClueCardTray _tray;
        private HandDrawnStrokeGraphic _stroke;

        public RectTransform Root => (RectTransform)transform;

        /// <summary>단서를 집은 동안(또는 그 단서의 턴 연출이 도는 동안) true.</summary>
        public bool IsEngaged { get; private set; }

        private void Awake()
        {
            BuildStroke();
            SubscribeToClueDrag();

            // BuildMemorySpaceBubble(UiLayoutSetupTool.cs)은 폰트를 직렬화해서 넘기지 않는다 —
            // 이미 구워진 프리팹을 재생성하지 않아도 되도록, 같은 MainHud 아래 이미 한글 폰트가
            // 물려 있는 아무 텍스트(예: DialogueText)에서 빌려온다.
            if (_font == null)
            {
                var anyLabel = transform.root.GetComponentInChildren<TMP_Text>(true);
                if (anyLabel != null) _font = anyLabel.font;
            }
        }

        /// <summary>구워진 Bubble 이미지는 색 사각형이라 끄고, 같은 자리에 손그림 선을 런타임에 짓는다(스프라이트를 프리팹에 굽지 않는다).</summary>
        private void BuildStroke()
        {
            if (_bubbleBackground == null) return;

            _bubbleBackground.enabled = false;
            _bubbleBackground.raycastTarget = false; // 드롭은 DropZone이 받는다.

            var rect = RuntimeUi.CreateStretched(_bubbleBackground.transform, "Stroke");
            _stroke = rect.gameObject.AddComponent<HandDrawnStrokeGraphic>();
            _stroke.color = Color.white;
        }

        private void OnDestroy() => UnsubscribeFromClueDrag();

        /// <summary>선을 그려 넣는다(true) 또는 지운다(false). 이미 그 상태면 아무것도 안 한다.</summary>
        public void SetEngaged(bool engaged)
        {
            if (IsEngaged == engaged) return;
            IsEngaged = engaged;

            if (_stroke == null) return;

            var motion = UiMotion.Settings;
            UiSoundHooks.Play(UiSoundCue.Pen);

            if (engaged)
            {
                _stroke.Regenerate(0);
                _stroke.Draw(motion.bubbleDraw);
            }
            else
            {
                _stroke.Erase(motion.bubbleErase);
            }
        }

        private void SubscribeToClueDrag() => ForEachCardDragHandler(h =>
        {
            h.DragStarted += OnClueDragStarted;
            h.DragEnded += OnClueDragEnded;
        });

        private void UnsubscribeFromClueDrag() => ForEachCardDragHandler(h =>
        {
            h.DragStarted -= OnClueDragStarted;
            h.DragEnded -= OnClueDragEnded;
        });

        // 연출이 도는 중에 집은 단서는 어차피 못 낸다(MemorySpaceDropZone) — 그때 켜면 꺼줄 시점이 없다.
        private void OnClueDragStarted()
        {
            if (!IsTurnPresenting) SetEngaged(true);
        }

        /// <summary>드롭은 OnEndDrag보다 먼저 처리되므로 단서를 냈다면 이 시점에 이미 연출이 시작돼 있다 —
        /// 그때는 끄는 시점을 Presenter에게 맡긴다.</summary>
        private void OnClueDragEnded()
        {
            if (!IsTurnPresenting) SetEngaged(false);
        }

        private bool IsTurnPresenting
        {
            get
            {
                _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
                return _presenter != null && _presenter.IsPresenting;
            }
        }

        private void ForEachCardDragHandler(System.Action<ClueCardDragHandler> apply)
        {
            _tray ??= transform.root.GetComponentInChildren<ClueCardTray>(true);
            if (_tray == null) return;

            for (var i = 0; i < _tray.CardCount; i++)
            {
                var drag = _tray.GetCard(i).GetComponent<ClueCardDragHandler>();
                if (drag != null) apply(drag);
            }
        }

        /// <summary>
        /// 결과 태그 칩 하나: 표시 글자 + 감정 색(침체 쪽 청록 / 흥분 쪽 주황 — <see cref="EmotionVisuals"/>).
        /// </summary>
        public readonly struct ResultTag
        {
            public string Text { get; }
            public Color Color { get; }

            public ResultTag(string text, Color color)
            {
                Text = text;
                Color = color;
            }
        }

        private const float ChipHeight = 72f;
        private const float ChipPadding = 52f;
        private const float ChipGap = 14f;
        private const float ChipFontSize = 40f;
        private const float RiseDistance = 110f;

        private readonly List<RectTransform> _chips = new();

        /// <summary>지금 풍선 안에 결과 태그가 떠 있으면 true.</summary>
        public bool HasResultTags => _chips.Count > 0;

        /// <summary>
        /// 시퀀스가 끝난 뒤 남은 감정 태그를 풍선 <b>안</b>에 칩으로 하나씩 나타낸다(기획서 생각 공간 이미지의 색 칩). 칩은 읽을 수 있게 크고(40pt) 불투명하며,
        /// 다 나타난 뒤 <c>tagHold</c>초 가만히 있는 건 호출자(Presenter)가 정한다 — 이 메서드는 나타나는 움직임만 돌려준다.
        /// 남아 있는 칩은 <see cref="RiseResultTags"/>가 올려 보낸다.
        /// </summary>
        public Sequence ShowResultTags(IReadOnlyList<ResultTag> tags)
        {
            ClearResultTags();

            var sequence = DOTween.Sequence().SetUpdate(true);
            if (tags == null || tags.Count == 0) return sequence;

            var motion = UiMotion.Settings;
            var area = ChipArea();

            // 폭이 모자라면 다음 줄로 — 줄마다 가운데 정렬하고, 전체를 영역 세로 중앙에 놓는다.
            var widths = new float[tags.Count];
            for (var i = 0; i < tags.Count; i++)
            {
                var label = CreateChip(tags[i], i, out var chip);
                widths[i] = label.GetPreferredValues(tags[i].Text).x + ChipPadding;
                chip.sizeDelta = new Vector2(widths[i], ChipHeight);
                _chips.Add(chip);
            }

            var rows = new List<List<int>> { new() };
            var rowWidth = 0f;
            for (var i = 0; i < tags.Count; i++)
            {
                var needed = widths[i] + (rows[rows.Count - 1].Count > 0 ? ChipGap : 0f);
                if (rows[rows.Count - 1].Count > 0 && rowWidth + needed > area.width)
                {
                    rows.Add(new List<int>());
                    rowWidth = 0f;
                    needed = widths[i];
                }

                rows[rows.Count - 1].Add(i);
                rowWidth += needed;
            }

            var totalHeight = rows.Count * ChipHeight + (rows.Count - 1) * ChipGap;
            var y = area.center.y + totalHeight * 0.5f - ChipHeight * 0.5f;
            var order = 0;
            foreach (var row in rows)
            {
                var rowTotal = row.Sum(i => widths[i]) + (row.Count - 1) * ChipGap;
                var x = area.center.x - rowTotal * 0.5f;
                foreach (var i in row)
                {
                    var chip = _chips[i];
                    chip.anchoredPosition = new Vector2(x + widths[i] * 0.5f, y);
                    x += widths[i] + ChipGap;

                    var group = chip.GetComponent<CanvasGroup>();
                    group.alpha = 0f;
                    chip.localScale = Vector3.one * 0.4f;

                    var delay = order++ * 0.07f;
                    sequence.Insert(delay, group.DOFade(1f, motion.tagPop * 0.6f));
                    sequence.Insert(delay, chip.DOScale(1f, motion.tagPop).SetEase(Ease.OutBack, 2f));
                }

                y -= ChipHeight + ChipGap;
            }

            UiSoundHooks.Play(UiSoundCue.Paper);
            return sequence;
        }

        /// <summary>떠 있는 결과 태그 칩을 위로 올려 보내며 서서히 사라지게 한다(<c>tagRise</c>초). 다 사라진 칩은 스스로 정리한다.
        /// 심박수 모니터 전환과 같은 프레임에 시작하는 건 Presenter가 정한다.</summary>
        public Sequence RiseResultTags()
        {
            var sequence = DOTween.Sequence().SetUpdate(true);
            var rise = UiMotion.Settings.tagRise;

            for (var i = 0; i < _chips.Count; i++)
            {
                var chip = _chips[i];
                if (chip == null) continue;

                var group = chip.GetComponent<CanvasGroup>();
                var delay = i * 0.06f;
                sequence.Insert(delay, chip.DOAnchorPosY(chip.anchoredPosition.y + RiseDistance, rise).SetEase(Ease.OutQuad));
                // 처음 40%는 또렷하게 떠 올라가다가 그 뒤로 서서히 사라진다.
                sequence.Insert(delay + rise * 0.4f, group.DOFade(0f, rise * 0.6f).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(ClearResultTags);
            return sequence;
        }

        private void ClearResultTags()
        {
            foreach (var chip in _chips)
            {
                if (chip == null) continue;
                chip.DOKill();
                chip.GetComponent<CanvasGroup>()?.DOKill();
                Destroy(chip.gameObject);
            }

            _chips.Clear();
        }

        /// <summary>칩이 놓이는 영역(풍선 루트 기준 픽셀): 선 안쪽 가운데. 최종 감정 요약 글자(아래쪽 띠)와 겹치지 않게 위로 올려 잡는다.</summary>
        private Rect ChipArea()
        {
            var rect = Root.rect;
            var width = rect.width * 0.74f;
            var height = rect.height * 0.46f;
            return new Rect(-width * 0.5f, rect.height * 0.08f - height * 0.5f, width, height);
        }

        private TMP_Text CreateChip(ResultTag tag, int index, out RectTransform chip)
        {
            // 레이어를 부모에서 물려받아야 UI 카메라가 그린다(RuntimeUi 문서 참고) — new GameObject는 기본 레이어(0)로 만든다.
            var go = new GameObject($"Result Tag {index}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image))
            {
                layer = gameObject.layer
            };
            go.transform.SetParent(transform, false);

            chip = (RectTransform)go.transform;
            chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(0.5f, 0.5f);
            chip.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-2.5f, 2.5f, Mathf.Repeat(index * 0.618f, 1f)));

            var image = go.GetComponent<Image>();
            image.sprite = RuntimeUi.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = tag.Color;
            image.raycastTarget = false;
            MockupStyle.AddShadow(go);

            var text = RuntimeUi.CreateText(chip, "Text", tag.Text, _font, ChipFontSize, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one);
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>마지막 턴의 최종 감정을 고정 표시한다. 결과 태그(ShowResultTags/RiseResultTags)의 상승/소멸 연출과는
        /// 별개로 존재해서, 연출이 끝난 뒤에도 계속 보인다. 다음 턴 결과가 나오면 이 호출로 갱신된다.
        /// 호출 시점(연출과 같은 프레임에 갱신할지 등)은 Presenter가 쥔다 — 여기선 표시만 한다.</summary>
        public void SetPersistentSummary(string text)
        {
            if (_summaryText != null) _summaryText.text = text;
        }

    }
}
