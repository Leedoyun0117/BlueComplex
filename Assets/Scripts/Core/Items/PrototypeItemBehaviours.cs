using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Items
{
    /// <summary>감정적 설득 — '침체' 감정을 건드리는 컴플렉스를 지속 시간 동안 무시한다.</summary>
    public sealed class EmotionalPersuasion : IItemBehaviour, IComplexFilter
    {
        private readonly IComplexDepressionTagger _tagger;

        public EmotionalPersuasion(IComplexDepressionTagger tagger) => _tagger = tagger;

        public void OnActivate(ItemActivationContext context) => context.ActiveItems.AddFilter(this);

        public bool ShouldIgnore(ComplexInstance complex) => _tagger.AffectsDepression(complex.Definition);
    }

    /// <summary>
    /// 어떤 컴플렉스가 '침체' 감정에 관여하는지 판단하는 책임.
    /// 조건/효과를 뜯어보는 대신 저작 단계에서 표시하는 편이 안정적이므로 분리해 둔다.
    /// </summary>
    public interface IComplexDepressionTagger
    {
        bool AffectsDepression(ComplexDefinition definition);
    }

    public sealed class IdListDepressionTagger : IComplexDepressionTagger
    {
        private readonly string[] _ids;
        public IdListDepressionTagger(params string[] ids) => _ids = ids;
        public bool AffectsDepression(ComplexDefinition definition) => _ids.Contains(definition.Id);
    }

    /// <summary>기억 공감 — 결과에서 공포/슬픔을 1씩 덜어내고, 대신 예민 특성을 얻는다.</summary>
    public sealed class MemoryEmpathy : IItemBehaviour, IResultModifier
    {
        private readonly int _sensitiveDuration;

        public MemoryEmpathy(int sensitiveDuration = 3) => _sensitiveDuration = sensitiveDuration;

        public void OnActivate(ItemActivationContext context)
        {
            context.ActiveItems.AddModifier(this);
            context.Traits.Grant(TraitType.Sensitive, _sensitiveDuration);
        }

        public void Modify(TagSet finalTags)
        {
            finalTags.RemoveEmotion(EmotionTag.Fear);
            finalTags.RemoveEmotion(EmotionTag.Sadness);
        }
    }

    /// <summary>회상 — 손패를 전부 풀에 되돌리고 다시 뽑는다.</summary>
    public sealed class Recollection : IItemBehaviour
    {
        public void OnActivate(ItemActivationContext context) => context.Hand.RedrawAll();
    }

    /// <summary>완화제 — 중복된 감정을 하나씩만 남긴다.</summary>
    public sealed class Sedative : IItemBehaviour, IResultModifier
    {
        public void OnActivate(ItemActivationContext context) => context.ActiveItems.AddModifier(this);

        public void Modify(TagSet finalTags) => finalTags.CollapseDuplicates();
    }
}
