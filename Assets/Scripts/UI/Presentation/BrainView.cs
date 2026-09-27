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
    /// 뇌의 세 부분과 컴플렉스 슬롯(코어 최대 중첩, <c>StageConfig.MaxComplexSlots</c> — 기본 3)은 1:1이다. 슬롯 수가 영역 수와 다르면 세션이 시작될 때 경고한다.
    ///
    /// 우선순위 순서(<c>InPriorityOrder</c>)의 컴플렉스는 영역 배열(<c>_regions</c>, 순서상 Left/Middle/Right)이 아니라
    /// <see cref="FillOrder"/>가 정한 순서(Left → Right → Middle)로 영역을 받는다 — 컴플렉스가 만료돼 목록이 줄면 뒤의 컴플렉스가 그 다음 순서 영역으로 옮겨 온다.
    /// 갱신 시점은 Presenter가 쥔다(<see cref="Refresh"/>). 세션 시작(스냅)과 판넬이 열릴 때만 스스로 코어 상태를 읽는다.
    /// </summary>
    public sealed class BrainView : SessionBoundView
    {
        /// <summary>우선순위 i번째 컴플렉스가 받는 영역 인덱스 — 뇌 영역 배열은 Left/Middle/Right(0/1/2) 순이지만 채워지는 순서는 Left(0) → Right(2) → Middle(1)이다.</summary>
        private static readonly int[] FillOrder = { 0, 2, 1 };

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

        protected override void Render()
        {
            if (Session != null && Session.Complexes.MaxSlots != RegionCount)
                Debug.LogWarning($"[BrainView] 컴플렉스 최대 중첩({Session.Complexes.MaxSlots})과 뇌 영역 수({RegionCount})가 다르다 — " +
                                 "뇌의 세 부분과 슬롯은 1:1이어야 한다.", this);

            SyncFromSession();
        }

        /// <summary>코어의 현재 컴플렉스 목록으로 다시 그린다 — 턴 연출 밖(세션 시작, 판넬이 그냥 열릴 때)에서만 부른다.</summary>
        public void SyncFromSession()
        {
            if (Session == null) return;
            Refresh(Session.Complexes.InPriorityOrder().ToList());
        }

        public void Refresh(IReadOnlyList<ComplexInstance> complexes)
        {
            for (var slot = 0; slot < _regions.Length; slot++)
            {
                var regionIndex = slot < FillOrder.Length ? FillOrder[slot] : slot;
                if (regionIndex >= _regions.Length) continue;

                _regions[regionIndex].Assign(slot < complexes.Count ? complexes[slot] : null);
            }
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
