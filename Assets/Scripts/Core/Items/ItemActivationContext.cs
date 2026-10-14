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
    /// <summary>아이템이 손댈 수 있는 것들만 모은 좁은 창구.</summary>
    public sealed class ItemActivationContext
    {
        public ClueHand Hand { get; }
        public TraitBoard Traits { get; }
        public ActiveItemBoard ActiveItems { get; }
        public ComplexBoard Complexes { get; }
        public Heartbeat Heartbeat { get; }
        public HeartbeatZone Zone { get; }

        /// <summary>스테이지가 아이템에 넘기는 값(예: 감정적 설득이 무시할 컴플렉스 id 목록). StageConfig.ItemParameters에서 온다.</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> StageParameters { get; }

        /// <summary>사용자가 고른 대상. 대상이 필요 없는 아이템은 null.</summary>
        public ItemTarget Target { get; }

        /// <summary>이 사용으로 만들어지는 지속 효과 상자(지속 시간이 0이면 null — 즉시 효과만 낼 수 있다).</summary>
        internal ActiveItem Active { get; set; }

        public ItemActivationContext(ClueHand hand,
                                     TraitBoard traits,
                                     ActiveItemBoard activeItems,
                                     ComplexBoard complexes = null,
                                     Heartbeat heartbeat = null,
                                     HeartbeatZone zone = null,
                                     IReadOnlyDictionary<string, IReadOnlyList<string>> stageParameters = null,
                                     ItemTarget target = null)
        {
            Hand = hand;
            Traits = traits;
            ActiveItems = activeItems;
            Complexes = complexes;
            Heartbeat = heartbeat;
            Zone = zone;
            StageParameters = stageParameters;
            Target = target;
        }

        /// <summary>스테이지 파라미터 하나. 없으면 빈 목록.</summary>
        public IReadOnlyList<string> Parameter(string key) =>
            StageParameters != null && StageParameters.TryGetValue(key, out var values) ? values : Array.Empty<string>();

        /// <summary>이 사용이 살아 있는 동안 컴플렉스를 무시하는 필터를 건다.</summary>
        public void AddFilter(IComplexFilter filter) => RequireActive().Filters.Add(filter);

        /// <summary>이 사용이 살아 있는 동안 최종 결과에 걸리는 보정을 건다.</summary>
        public void AddModifier(IResultModifier modifier) => RequireActive().Modifiers.Add(modifier);

        private ActiveItem RequireActive() =>
            Active ?? throw new InvalidOperationException("지속 시간이 0인 아이템은 지속 효과(필터/보정)를 걸 수 없습니다.");
    }
}
