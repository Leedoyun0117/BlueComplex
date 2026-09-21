using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stage;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 컴플렉스 인터페이스의 뇌(엑스레이 판넬 안). 뇌를 세 부분(엽)으로 나누어 각 부분에 컴플렉스를 하나씩 할당하고, 단서가 들어오면
    /// 우선순위 순서대로 해당 부분이 빛난다(<see cref="PlayGlow"/>) — 발동하지 않은 컴플렉스의 부분은 빛나지 않는다.
    ///
    /// 코어 <c>ComplexBoard.MaxSlots</c>는 4인데 기획서의 뇌는 3분할이라 4번째 컴플렉스를 놓을 곳이 기획서에 없다. 임시로 도트 뇌의 <b>뇌간</b>
    /// (뇌 아래 줄기)을 네 번째 영역으로 쓴다 — 최대 중첩을 3으로 줄일지는 기획자 결정이고, 그때는 <see cref="_regions"/>의 마지막 영역만 빼면 된다.
    ///
    /// 우선순위 순서(<c>InPriorityOrder</c>)의 i번째 컴플렉스가 i번째 영역을 받는다 — 컴플렉스가 만료돼 목록이 줄면 뒤의 컴플렉스가 앞 영역으로 옮겨 온다.
    /// 갱신 시점은 Presenter가 쥔다(<see cref="Refresh"/>). 세션 시작(스냅)과 판넬이 열릴 때만 스스로 코어 상태를 읽는다.
    /// </summary>
    public sealed class BrainView : SessionBoundView
    {
        [SerializeField] private BrainRegionView[] _regions;
        [SerializeField] private TooltipPopup _tooltip;

        public int RegionCount => _regions?.Length ?? 0;

        protected override void Awake()
        {
            foreach (var region in _regions) region.Init(_tooltip);
            base.Awake();
        }

        protected override void Subscribe(StageSession session) { }

        protected override void Unsubscribe(StageSession session) { }

        protected override void Render() => SyncFromSession();

        /// <summary>코어의 현재 컴플렉스 목록으로 다시 그린다 — 턴 연출 밖(세션 시작, 판넬이 그냥 열릴 때)에서만 부른다.</summary>
        public void SyncFromSession()
        {
            if (Session == null) return;
            Refresh(Session.Complexes.InPriorityOrder().ToList());
        }

        public void Refresh(IReadOnlyList<ComplexInstance> complexes)
        {
            for (var i = 0; i < _regions.Length; i++)
                _regions[i].Assign(i < complexes.Count ? complexes[i] : null);
        }

        /// <summary>이 컴플렉스가 할당된 영역을 한 번 빛나게 한다. 영역이 없으면(같은 턴에 만료돼 이미 빠졌다) false — 호출자는 조용히 건너뛴다.</summary>
        public bool PlayGlow(ComplexInstance complex)
        {
            foreach (var region in _regions)
            {
                if (region.Complex != complex) continue;

                region.PlayGlow();
                return true;
            }

            return false;
        }
    }
}
