using System.Collections.Generic;
using BlueComplex.Core.Complexes;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 붙어 있는 컴플렉스 목록. Refresh는 Presenter만 호출한다 — ComplexBoard.Attached/Expired를
    /// 직접 구독하지 않는다(3단계에서 순차 발광 연출이 들어갈 자리라 순서 제어권을 Presenter가 쥔다).
    /// </summary>
    public sealed class ComplexListView : MonoBehaviour
    {
        [SerializeField] private ComplexRowView[] _rows;
        [SerializeField] private TooltipPopup _tooltip;

        private void Awake()
        {
            foreach (var row in _rows) row.Init(_tooltip);
        }

        public void Refresh(IReadOnlyList<ComplexInstance> complexes)
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                if (i < complexes.Count) _rows[i].Render(complexes[i]);
                else _rows[i].SetEmpty();
            }
        }

        /// <summary>
        /// 발동한(Triggered) 컴플렉스만, 우선순위 순서(Steps가 이미 그 순서다) 그대로 한 번에 하나씩
        /// 빛난다. TickDurations/스폰이 Resolve 이후에 일어나므로 이 시점의 행 배치는 Steps가 계산됐을
        /// 때와 다를 수 있다 — 그래서 인덱스가 아니라 ComplexInstance 참조로 행을 찾는다. 같은 턴에
        /// 만료돼 이미 행이 없는 컴플렉스는 조용히 건너뛴다.
        ///
        /// 호출 전에 Refresh()로 최신 보드 상태를 먼저 반영해 둬야 한다 — 순서 제어(재생 시점)는
        /// Presenter가 쥐고, 이 메서드는 반환한 Sequence를 재생할지 말지도 관여하지 않는다.
        /// </summary>
        public Sequence PlaySequence(InterpretationResult result)
        {
            var sequence = DOTween.Sequence();

            foreach (var step in result.Steps)
            {
                if (!step.Triggered) continue;

                var row = FindRow(step.Complex);
                if (row == null) continue;

                sequence.AppendCallback(row.PlayGlow);
                sequence.AppendInterval(ComplexRowView.GlowDuration);
            }

            return sequence;
        }

        private ComplexRowView FindRow(ComplexInstance complex)
        {
            foreach (var row in _rows)
                if (row.Complex == complex) return row;
            return null;
        }
    }
}
