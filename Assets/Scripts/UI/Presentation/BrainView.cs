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

        /// <summary>뇌 그림(과 영역 글자)을 프레임 안에서 위로 올리는 양 — 판 안쪽(Content) 높이에 대한 비율(0.1 = 10%). 유키 초상화·프레임은 움직이지 않고 뇌만 머리 위쪽으로 간다
        /// (전엔 뇌 아래쪽이 유키 눈을 덮었다). 기본 자리(프리팹의 BrainArea 앵커) 기준이라 0이면 예전 자리. Play 중 인스펙터에서 바꾸면 바로 반영된다. 2026-09-28 사용자 요청.</summary>
        [SerializeField, Range(0f, 0.4f), Tooltip("뇌를 위로 올리는 양(판 안쪽 높이 비율). 0 = 원래 자리. Play 중 바로 반영된다.")]
        private float _lift = DefaultLift;

        public const float DefaultLift = 0.17f;

        [SerializeField] private BrainRegionView[] _regions;
        [SerializeField] private TooltipPopup _tooltip;

        public int RegionCount => _regions?.Length ?? 0;

        private RectTransform _area;
        private Vector2 _baseAnchorMin, _baseAnchorMax;

        protected override void Awake()
        {
            foreach (var region in _regions) region.Init(_tooltip);
            CacheArea();
            ApplyLift();
            base.Awake();
        }

        /// <summary>뇌를 올리는 양(판 안쪽 높이 비율). 검증·튜닝용.</summary>
        public float Lift
        {
            get => _lift;
            set
            {
                _lift = value;
                ApplyLift();
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying) ApplyLift();
        }

        /// <summary>올릴 대상은 이 뇌의 부모(BrainArea) — 뇌 아트와 영역 글자(Labels)가 같이 그 아래에 있어 함께 올라간다. 원래 앵커는 처음 한 번만 기억한다(다시 적용해도 쌓이지 않게).</summary>
        private void CacheArea()
        {
            if (_area != null) return;

            _area = transform.parent as RectTransform;
            if (_area == null) return;

            _baseAnchorMin = _area.anchorMin;
            _baseAnchorMax = _area.anchorMax;
        }

        private void ApplyLift()
        {
            CacheArea();
            if (_area == null) return;

            var up = new Vector2(0f, _lift);
            _area.anchorMin = _baseAnchorMin + up;
            _area.anchorMax = _baseAnchorMax + up;
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

        public void Refresh(IReadOnlyList<ComplexInstance> complexes) => Place(complexes, keepShown: false);

        /// <summary>턴 연출의 발광 단계용 배치: 이미 그 컴플렉스가 들어 있는 영역은 건드리지 않는다. 코어는 해석 직후 남은 턴을 이미 줄여 두어서
        /// 다시 할당하면 "N턴" 글자가 포스트잇보다 먼저 바뀐다 — 최신 상태는 포스트잇이 갱신되는 순간 <see cref="Refresh"/>가 맞춘다.</summary>
        public void Arrange(IReadOnlyList<ComplexInstance> complexes) => Place(complexes, keepShown: true);

        private void Place(IReadOnlyList<ComplexInstance> complexes, bool keepShown)
        {
            for (var slot = 0; slot < _regions.Length; slot++)
            {
                var regionIndex = slot < FillOrder.Length ? FillOrder[slot] : slot;
                if (regionIndex >= _regions.Length) continue;

                var complex = slot < complexes.Count ? complexes[slot] : null;
                if (keepShown && _regions[regionIndex].Complex == complex) continue;

                _regions[regionIndex].Assign(complex);
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
