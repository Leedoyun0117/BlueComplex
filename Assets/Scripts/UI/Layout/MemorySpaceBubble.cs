using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 생각 공간(말풍선). 평소엔 드롭 판정 영역 표시일 뿐이고, 턴 결과 후 남은 감정 태그가 위로
    /// 떠오르며 사라지는 연출을 재생한다. UI 디자인 가이드 원문: "태그는 위로 올라가며 서서히
    /// 사라지며, 그와 동시에 인디케이터가 움직인다" — 이 클래스는 자기 애니메이션만 알고, 심박수
    /// 이동과 나란히 맞추는 건 3단계 CinematicTurnResultPresenter가 쥔다.
    /// </summary>
    public sealed class MemorySpaceBubble : MonoBehaviour
    {
        private const float RiseDistance = 70f;
        private const float RiseDuration = 0.9f;
        private const float StaggerPerTag = 0.08f;
        private const float TagSpacingX = 90f;

        [SerializeField] private Image _bubbleBackground;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TMP_Text _summaryText;

        public RectTransform Root => (RectTransform)transform;
        public Image BubbleBackground => _bubbleBackground;

        private void Awake()
        {
            // BuildMemorySpaceBubble(UiLayoutSetupTool.cs)은 폰트를 직렬화해서 넘기지 않는다 —
            // 이미 구워진 프리팹을 재생성하지 않아도 되도록, 같은 MainHud 아래 이미 한글 폰트가
            // 물려 있는 아무 텍스트(예: DialogueText)에서 빌려온다.
            if (_font == null)
            {
                var anyLabel = transform.root.GetComponentInChildren<TMP_Text>(true);
                if (anyLabel != null) _font = anyLabel.font;
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
            var go = new GameObject("Tag", typeof(RectTransform));
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
