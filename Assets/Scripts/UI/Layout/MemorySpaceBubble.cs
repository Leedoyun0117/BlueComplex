using System.Collections.Generic;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 생각 공간(말풍선). 손으로 그린 흰 선(채우지 않은 열린 곡선)만 그리고, 평소엔 흐리게 있다가 단서를 집으면(드래그 시작) 또렷해진다.
    /// 놓으면 다시 흐려지지만, 놓은 단서로 턴 연출이 시작됐다면 연출이 끝날 때까지(Presenter가 <see cref="SetEngaged"/>로 알린다) 또렷하게 둔다.
    /// 턴 결과 후에는 남은 감정 태그가 위로 떠오르며 사라진다. UI 디자인 가이드 원문: "태그는 위로 올라가며 서서히
    /// 사라지며, 그와 동시에 인디케이터가 움직인다" — 이 클래스는 자기 애니메이션만 알고, 심박수
    /// 이동과 나란히 맞추는 건 3단계 CinematicTurnResultPresenter가 쥔다.
    /// </summary>
    public sealed class MemorySpaceBubble : MonoBehaviour
    {
        private const float RiseDistance = 70f;
        private const float RiseDuration = 0.9f;
        private const float StaggerPerTag = 0.08f;
        private const float TagSpacingX = 90f;
        private const float FadeDuration = 0.2f;

        private static readonly Color IdleColor = new(1f, 1f, 1f, 0.55f);
        private static readonly Color EngagedColor = new(1f, 1f, 1f, 1f);

        [SerializeField] private Image _bubbleBackground;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TMP_Text _summaryText;

        private ITurnResultPresenter _presenter;
        private ClueCardTray _tray;
        private Tween _fadeTween;

        public RectTransform Root => (RectTransform)transform;
        public Image BubbleBackground => _bubbleBackground;

        /// <summary>단서를 집은 동안(또는 그 단서의 턴 연출이 도는 동안) true.</summary>
        public bool IsEngaged { get; private set; }

        private void Awake()
        {
            // 색으로 채운 사각형이 아니라 손그림 선으로 그린다. 스프라이트를 프리팹에 굽지 않고 런타임에 만든다(RuntimeUi.Circle과 같은 방식).
            if (_bubbleBackground != null)
            {
                _bubbleBackground.sprite = RuntimeUi.HandDrawnLoop;
                _bubbleBackground.type = Image.Type.Simple;
                _bubbleBackground.raycastTarget = false; // 드롭은 DropZone이 받는다.
                _bubbleBackground.color = IdleColor;
            }

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

        private void OnDestroy()
        {
            _fadeTween?.Kill();
            UnsubscribeFromClueDrag();
        }

        /// <summary>또렷하게(true) 또는 흐리게(false). 이미 그 상태면 아무것도 안 한다.</summary>
        public void SetEngaged(bool engaged)
        {
            if (IsEngaged == engaged) return;
            IsEngaged = engaged;

            if (_bubbleBackground == null) return;

            _fadeTween?.Kill();
            _fadeTween = _bubbleBackground.DOColor(engaged ? EngagedColor : IdleColor, FadeDuration);
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
        /// 남은 감정 태그(한글 표시 문자열, 호출자가 KoreanLabels로 변환해서 넘긴다)를 하나씩 살짝
        /// 시차를 두고 띄워 위로 떠오르며 사라지게 한다. 완료된 태그 GameObject는 스스로 정리한다.
        /// </summary>
        public Sequence PlayRemainingTags(IReadOnlyList<string> labels)
        {
            var sequence = DOTween.Sequence();
            if (labels == null || labels.Count == 0) return sequence;

            for (var i = 0; i < labels.Count; i++)
            {
                var offsetX = (i - (labels.Count - 1) / 2f) * TagSpacingX;
                var label = CreateTagLabel(labels[i], offsetX);
                var rt = (RectTransform)label.transform;
                var canvasGroup = label.gameObject.AddComponent<CanvasGroup>();
                var startY = rt.anchoredPosition.y;
                var delay = i * StaggerPerTag;

                sequence.Insert(delay, rt.DOAnchorPosY(startY + RiseDistance, RiseDuration).SetEase(Ease.OutQuad));
                sequence.Insert(delay, canvasGroup.DOFade(0f, RiseDuration).SetEase(Ease.InQuad));
                sequence.InsertCallback(delay + RiseDuration, () => Destroy(label.gameObject));
            }

            return sequence;
        }

        /// <summary>마지막 턴의 최종 감정을 고정 표시한다. PlayRemainingTags의 상승/소멸 연출과는
        /// 별개로 존재해서, 연출이 끝난 뒤에도 계속 보인다. 다음 턴 결과가 나오면 이 호출로 갱신된다.
        /// 호출 시점(연출과 같은 프레임에 갱신할지 등)은 Presenter가 쥔다 — 여기선 표시만 한다.</summary>
        public void SetPersistentSummary(string text)
        {
            if (_summaryText != null) _summaryText.text = text;
        }

        private TMP_Text CreateTagLabel(string text, float offsetX)
        {
            // 레이어를 부모에서 물려받아야 UI 카메라가 그린다(RuntimeUi 문서 참고) — new GameObject는 기본 레이어(0)로 만든다.
            var go = new GameObject("Tag", typeof(RectTransform)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(offsetX, 0f);
            rt.sizeDelta = new Vector2(120f, 36f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            if (_font != null) tmp.font = _font;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return tmp;
        }
    }
}
