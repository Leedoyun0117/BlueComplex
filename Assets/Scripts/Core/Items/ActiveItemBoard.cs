using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Items
{
    /// <summary>
    /// 지속 중인 아이템 효과를 모아 컴플렉스 필터와 결과 보정으로 노출한다.
    /// 아이템 개별 규칙은 각 Behaviour가 갖고, 여기서는 집계만 한다. 필터·보정은 사용마다 따로 들고 있어서 아이템이 겹쳐도 만료된 것만 빠진다.
    /// </summary>
    public sealed class ActiveItemBoard : IComplexFilter, IResultModifier
    {
        private readonly List<ActiveItem> _active = new();

        public IReadOnlyList<ActiveItem> Active => _active;

        public event Action<ActiveItem> Expired;

        public void Register(ActiveItem item) => _active.Add(item);

        public bool ShouldIgnore(ComplexInstance complex) =>
            _active.Any(item => item.Filters.Any(filter => filter.ShouldIgnore(complex)));

        public void Modify(TagSet finalTags)
        {
            foreach (var item in _active)
                foreach (var modifier in item.Modifiers)
                    modifier.Modify(finalTags);
        }

        public void TickDurations()
        {
            foreach (var item in _active) item.Tick();

            foreach (var expired in _active.Where(a => a.IsExpired).ToList())
            {
                _active.Remove(expired);
                Expired?.Invoke(expired);
            }
        }

        /// <summary>앞서 떠 둔 목록(<see cref="Active"/>의 사본)으로 되돌린다 — 그 뒤에 켠 효과만 거둔다(튜토리얼의 오답 되돌리기). 만료가 아니므로 <see cref="Expired"/>는 쏘지 않는다.</summary>
        public void RestoreTo(IReadOnlyList<ActiveItem> snapshot)
        {
            _active.Clear();
            _active.AddRange(snapshot);
        }
    }
}
