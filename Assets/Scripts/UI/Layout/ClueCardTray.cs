using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stability;
using BlueComplex.UI.Presentation;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>단서 카드 4장의 트레이. 표시만 한다 — 판정 없음, 코어 이벤트도 직접 구독하지 않는다.</summary>
    public sealed class ClueCardTray : MonoBehaviour
    {
        [SerializeField] private ClueCardView[] _cards;

        public RectTransform Root => (RectTransform)transform;
        public int CardCount => _cards?.Length ?? 0;

        public ClueCardView GetCard(int index) => _cards[index];

        /// <summary>손패 전체를 현재 해금/검열 상태로 다시 그린다. 남는 슬롯은 비운다.</summary>
        public void RefreshAll(IReadOnlyList<ClueInstance> handCards, ClueKnowledgeLedger ledger, CensorshipLevel censorship)
        {
            for (var i = 0; i < _cards.Length; i++)
            {
                if (i < handCards.Count)
                {
                    var card = handCards[i];
                    _cards[i].Render(card, ClueCardFormatter.Format(card, ledger, censorship));
                }
                else
                {
                    _cards[i].SetEmpty();
                }
            }
        }
    }
}
