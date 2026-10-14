using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    // 아이템 행동 조각들. 숫자·id·감정 목록은 전부 생성자 인자다 — 어떤 아이템이 어떤 조각을 어떤 값으로 쓰는지는 PrototypeContent(데이터)가 적는다.
    // 특성 부여는 행동이 아니라 ItemDefinition.GrantedTraitId가 맡는다.

    /// <summary>극복 — 고른 컴플렉스의 남은 지속 시간을 절반으로 줄인다(올림). 남은 턴이 1이면 줄일 게 없어서 대상이 아니다.</summary>
    public sealed class HalveComplexDuration : IItemBehaviour, IItemTargeting
    {
        public bool IsValidTarget(ItemTarget target, ItemActivationContext context) =>
            target is ComplexTarget { Complex: var complex }
            && context.Complexes != null
            && context.Complexes.Slots.Contains(complex)
            && complex.RemainingTurns >= 2;

        public void OnActivate(ItemActivationContext context)
        {
            var target = (ComplexTarget)context.Target;
            context.Complexes.HalveRemainingTurns(target.Complex);
        }
    }
}
