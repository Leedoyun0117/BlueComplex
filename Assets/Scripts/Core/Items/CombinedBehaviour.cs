using System.Linq;

namespace BlueComplex.Core.Items
{
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
