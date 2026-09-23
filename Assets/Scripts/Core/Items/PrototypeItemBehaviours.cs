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

    /// <summary>
    /// 감정적 설득 — 스테이지가 지정한 컴플렉스(예: '침체' 감정에 영향을 주는 것들)를 지속 시간 동안 무시한다.
    /// 어떤 컴플렉스인지는 스테이지마다 다르므로 <see cref="StageParameterKey"/>로 StageConfig.ItemParameters에서 읽는다.
    /// </summary>
    public sealed class IgnoreStageComplexes : IItemBehaviour
    {
        public string StageParameterKey { get; }

        public IgnoreStageComplexes(string stageParameterKey) => StageParameterKey = stageParameterKey;

        public void OnActivate(ItemActivationContext context) =>
            context.AddFilter(new IdSetFilter(context.Parameter(StageParameterKey)));

        private sealed class IdSetFilter : IComplexFilter
        {
            private readonly string[] _ids;
            public IdSetFilter(System.Collections.Generic.IReadOnlyList<string> ids) => _ids = ids.ToArray();
            public bool ShouldIgnore(ComplexInstance complex) => _ids.Contains(complex.Definition.Id);
        }
    }

    /// <summary>기억 공감 — 결과에서 지정한 감정들을 amount개씩 제거한다(기획: 공포·슬픔 1씩).</summary>
    public sealed class RemoveEmotions : IItemBehaviour, IResultModifier
    {
        private readonly EmotionTag[] _emotions;
        private readonly int _amount;

        public RemoveEmotions(int amount, params EmotionTag[] emotions)
        {
            _amount = amount;
            _emotions = emotions;
        }

        public void OnActivate(ItemActivationContext context) => context.AddModifier(this);

        public void Modify(TagSet finalTags)
        {
            foreach (var emotion in _emotions) finalTags.RemoveEmotion(emotion, _amount);
        }
    }

    /// <summary>회상 — 손패를 전부 풀에 되돌리고 다시 뽑는다.</summary>
    public sealed class RedrawHand : IItemBehaviour
    {
        public void OnActivate(ItemActivationContext context) => context.Hand.RedrawAll();
    }

    /// <summary>논리적 설득 — 결과에서 중복된 감정을 하나씩만 남긴다.</summary>
    public sealed class CollapseDuplicateEmotions : IItemBehaviour, IResultModifier
    {
        public void OnActivate(ItemActivationContext context) => context.AddModifier(this);

        public void Modify(TagSet finalTags) => finalTags.CollapseDuplicates();
    }

    /// <summary>착한 사마리아인 — 심박수가 <see cref="Side"/> 구간에 있을 때만 <see cref="Amount"/>만큼 올린다(기획: 침체면 +10). 아니면 심박수는 그대로다.</summary>
    public sealed class RaiseHeartbeatInZone : IItemBehaviour
    {
        public Polarity Side { get; }
        public int Amount { get; }

        public RaiseHeartbeatInZone(Polarity side, int amount)
        {
            Side = side;
            Amount = amount;
        }

        public void OnActivate(ItemActivationContext context)
        {
            if (context.Zone.PolarityOf(context.Heartbeat.Value) == Side) context.Heartbeat.Change(Amount);
        }
    }

    /// <summary>선택적 기억 — 고른 보유 단서를 풀에서 뽑은 랜덤 단서로 바꾼다(고른 단서는 풀로 돌아간다). 풀이 비어 있으면 바꿀 수 없다.</summary>
    public sealed class ReplaceClue : IItemBehaviour, IItemTargeting
    {
        public bool IsValidTarget(ItemTarget target, ItemActivationContext context) =>
            target is ClueTarget { Card: var card } && context.Hand.Cards.Contains(card) && context.Hand.CanReplaceOne;

        public void OnActivate(ItemActivationContext context)
        {
            var target = (ClueTarget)context.Target;
            if (!context.Hand.TryReplaceRandom(target.Card, out _))
                throw new InvalidOperationException("바꿀 단서가 풀에 없습니다.");
        }
    }
}
