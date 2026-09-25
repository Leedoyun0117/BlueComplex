using System.Collections.Generic;
using BlueComplex.Core.Complexes;
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

        /// <param name="fresh">이번에 새로 발현된 컴플렉스(있으면 그 행에 "NEW" 표시가 준비된다). null이면 표시 없음.</param>
        public void Refresh(IReadOnlyList<ComplexInstance> complexes, ICollection<ComplexInstance> fresh = null)
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                if (i < complexes.Count) _rows[i].Render(complexes[i], fresh != null && fresh.Contains(complexes[i]));
                else _rows[i].SetEmpty();
            }
        }

        /// <summary>준비된 "NEW" 표시를 드러낸다 — 포스트잇이 다 붙은 뒤 Presenter가 부른다.</summary>
        public void RevealNewMarks()
        {
            foreach (var row in _rows) row.RevealNewMark();
        }

        /// <summary>
        /// 이 컴플렉스의 행을 한 번 빛나게 한다. 행을 못 찾으면(같은 턴에 만료돼 이미 행이 없다) false — 호출자는 조용히 건너뛴다.
        /// 어느 컴플렉스를 어떤 순서(평가 순서, InterpretationResult.Steps가 이미 그 순서다)로 빛낼지, 그 사이에 무엇을 보여줄지(이벤트 대사)는
        /// Presenter가 쥔다. TickDurations/스폰이 Resolve 이후에 일어나므로 행 배치는 Steps가 계산됐을 때와 다를 수 있어 인덱스가 아니라
        /// ComplexInstance 참조로 찾는다 — 호출 전에 Refresh()로 최신 보드 상태를 먼저 반영해 둬야 한다.
        /// </summary>
        public bool PlayGlow(ComplexInstance complex)
        {
            var row = FindRow(complex);
            if (row == null) return false;

            row.PlayGlow();
            return true;
        }

        private ComplexRowView FindRow(ComplexInstance complex)
        {
            foreach (var row in _rows)
                if (row.Complex == complex) return row;
            return null;
        }
    }
}
