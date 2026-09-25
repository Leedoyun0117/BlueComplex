using System.Linq;

namespace BlueComplex.Core.Items
{
    /// <summary>논리적 설득의 특성 제거 조각 — 붙어 있는 특성(일반·특수 가리지 않고) 중 하나를 랜덤으로 지운다. 붙어 있는 특성이 없으면 아무 일도 없다(아이템은 그래도 쓸 수 있다).</summary>
    public sealed class RemoveRandomTrait : IItemBehaviour
    {
        public void OnActivate(ItemActivationContext context) => context.Traits.RemoveRandom();
    }

    /// <summary>행동 조각 여러 개를 적은 순서대로 이어 실행한다(예: 논리적 설득 = 중복 감정 정리 + 랜덤 특성 제거). 대상이 필요한 조각은 하나로 감싸지 않는다 — 대상 검증(<see cref="IItemTargeting"/>)은 이 조합을 거치지 않는다.</summary>
    public sealed class CombinedBehaviour : IItemBehaviour
    {
        private readonly IItemBehaviour[] _parts;

        public CombinedBehaviour(params IItemBehaviour[] parts) => _parts = parts;

        public void OnActivate(ItemActivationContext context)
        {
            foreach (var part in _parts) part.OnActivate(context);
        }
    }
}
