using System.Collections.Generic;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Traits;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 특성 표시(UI 가이드 "추가 : 특성 UI"): 엑스레이 판넬이 펼쳐졌을 때 판넬 위에 특성이 뱃지로 나란히 뜬다. 특성이 없으면 아무것도 안 보인다(글자 자리표시도 없다).
    /// 이 컴포넌트는 판넬의 Content(펼침/접힘에 따라 같이 나타나고 사라지는 CanvasGroup) 안에 있어서 판넬이 접혀 있으면 뱃지도 안 보이고 호버도 안 받는다.
    /// 뱃지마다 0.25초 호버 팝업이 있다(<see cref="TraitBadgeView"/>).
    ///
    /// 특성은 턴 안에서(아이템 사용, 지속 턴 감소) 바뀌므로 갱신 시점은 컴플렉스 상시 표시와 같은 규칙이다:
    /// 세션 시작은 직접 그리고, 턴 안의 변화는 ITurnResultPresenter가 연출 순서에 맞춰 <see cref="Refresh"/>를 부른다.
    /// Granted/Expired는 표시만 해 두고 연출 중이 아닐 때만 그린다(아이템처럼 턴 밖에서 특성이 바뀌는 경우는 이 경로가 받는다).
    /// </summary>
    public sealed class TraitStatusView : SessionBoundView
    {
        private const float BadgeGap = 8f;

        /// <summary>뱃지가 들어가는 컨테이너(왼쪽 위 기준으로 뱃지를 직접 배치한다).</summary>
        [SerializeField] private RectTransform _container;

        /// <summary>복제 원본 뱃지(비활성). 뱃지 모양은 프리팹이 쥐고 여기서는 개수만 맞춘다.</summary>
        [SerializeField] private TraitBadgeView _badgeTemplate;

        [SerializeField] private TooltipPopup _tooltip;

        private readonly List<TraitBadgeView> _badges = new();
        private ITurnResultPresenter _presenter;
        private bool _dirty;

        protected override void Awake()
        {
            if (_badgeTemplate != null) _badgeTemplate.gameObject.SetActive(false);
            base.Awake();
        }

        protected override void Subscribe(StageSession session)
        {
            session.Traits.Granted += OnBoardChanged;
            session.Traits.Expired += OnBoardChanged;
        }

        protected override void Unsubscribe(StageSession session)
        {
            session.Traits.Granted -= OnBoardChanged;
            session.Traits.Expired -= OnBoardChanged;
        }

        protected override void Render() => Refresh();

        public void Refresh()
        {
            _dirty = false;
            if (_container == null || _badgeTemplate == null) return;

            var traits = Session != null ? Session.Traits.Traits : System.Array.Empty<TraitInstance>();

            // 뱃지는 재사용한다 — 모자라면 원본을 복제하고, 남으면 숨긴다(특성이 없으면 전부 숨겨 아무것도 안 보인다).
            while (_badges.Count < traits.Count)
                _badges.Add(Instantiate(_badgeTemplate, _container, false));

            for (var i = 0; i < _badges.Count; i++)
            {
                var badge = _badges[i];
                var active = i < traits.Count;
                badge.gameObject.SetActive(active);
                if (active) badge.Bind(traits[i], _tooltip);
            }

            LayoutBadges();
        }

        /// <summary>뱃지를 왼쪽 위부터 나란히 놓고, 컨테이너 폭을 넘으면 다음 줄로 넘긴다(LayoutGroup은 넘칠 때 줄바꿈이 없어 뱃지가 찌그러진다).
        /// 크기는 뱃지 자신의 레이아웃(글자 폭 + 여백)이 주는 선호 크기를 쓴다.</summary>
        private void LayoutBadges()
        {
            if (_container == null) return;

            var maxWidth = _container.rect.width;
            float x = 0f, y = 0f, rowHeight = 0f;
            foreach (var badge in _badges)
            {
                if (!badge.gameObject.activeSelf) continue;

                var rect = (RectTransform)badge.transform;

                // 방금 켜지거나 글자가 바뀐 뱃지는 레이아웃 캐시가 옛 값(0)이다 — 재계산을 강제한 뒤에 잰다.
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                var size =new Vector2(LayoutUtility.GetPreferredWidth(rect), LayoutUtility.GetPreferredHeight(rect));
                if (x > 0f && x + size.x > maxWidth)
                {
                    x = 0f;
                    y += rowHeight + BadgeGap;
                    rowHeight = 0f;
                }

                rect.anchoredPosition = new Vector2(x, -y);
                rect.sizeDelta = size;
                x += size.x + BadgeGap;
                rowHeight = Mathf.Max(rowHeight, size.y);
            }
        }

        // 판넬이 접힘/펼침에 맞춰 크기가 바뀌거나(ComplexXrayPanel.ApplyPose) 세션보다 늦게 자리가 잡히면 줄바꿈 기준 폭이 달라진다.
        private void OnRectTransformDimensionsChange()
        {
            if (_badges.Count > 0) LayoutBadges();
        }

        private void OnBoardChanged(TraitInstance trait) => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty) return;

            _presenter ??= transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_presenter != null && _presenter.IsPresenting) return;

            Refresh();
        }
    }
}
