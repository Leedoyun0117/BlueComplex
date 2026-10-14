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
    /// 아이템 한 종류의 데이터. 행동(<see cref="Behaviour"/>)은 숫자·id를 인자로 받는 범용 조각이고, 이 클래스가 무엇을 조합하는지 적는다 —
    /// 대상 종류, 지속 시간, 부여하는 특성(<see cref="GrantedTraitId"/>)이 전부 데이터다.
    /// </summary>
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }

        /// <summary>지속 효과(컴플렉스 무시, 결과 보정)가 붙어 있는 턴 수. 0이면 즉시 효과만 있다.</summary>
        public int Duration { get; }
        public IItemBehaviour Behaviour { get; }

        /// <summary>사용 전에 고르는 대상의 종류. None이면 클릭 한 번으로 바로 쓴다.</summary>
        public ItemTargetKind TargetKind { get; }

        /// <summary>사용하면 함께 붙는 특성의 id(없으면 null). 특성 자체의 지속 턴과 효과는 TraitDefinition이 쥔다.</summary>
        public string GrantedTraitId { get; }

        public ItemDefinition(string id, string displayName, string description, int duration, IItemBehaviour behaviour,
                              ItemTargetKind targetKind = ItemTargetKind.None, string grantedTraitId = null)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Duration = duration;
            Behaviour = behaviour;
            TargetKind = targetKind;
            GrantedTraitId = grantedTraitId;
        }
    }
}
