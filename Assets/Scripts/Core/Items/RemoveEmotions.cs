using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>기억 공감 — 결과에서 지정한 감정들을 amount개씩 제거한다(기획: 슬픔 1씩).</summary>
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
}
