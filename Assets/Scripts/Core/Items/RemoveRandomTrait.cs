using System.Linq;

namespace BlueComplex.Core.Items
{
    /// <summary>논리적 설득의 특성 제거 조각 — 붙어 있는 특성(일반·특수 가리지 않고) 중 하나를 랜덤으로 지운다. 붙어 있는 특성이 없으면 아무 일도 없다(아이템은 그래도 쓸 수 있다).</summary>
    public sealed class RemoveRandomTrait : IItemBehaviour
    {
        public void OnActivate(ItemActivationContext context) => context.Traits.RemoveRandom();
    }
}
