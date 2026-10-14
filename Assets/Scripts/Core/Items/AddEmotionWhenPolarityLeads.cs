using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>
    /// 명상 — 결과 감정 중 <see cref="Leading"/> 극성(기획: 흥분)의 개수가 반대 극성보다 <b>많을 때만</b> 지정한 감정(기획: 슬픔)을 amount개 더한다. 같거나 적으면 그대로다.
    /// 개수는 겹친 태그까지 센다(심박수 계산이 감정 태그 하나하나를 더하는 것과 같은 단위). 극성은 환각이 뒤집기 전의 원래 극성이다(보정 단계가 환각 앞이다).
    /// </summary>
    public sealed class AddEmotionWhenPolarityLeads : IItemBehaviour, IResultModifier
    {
        public Polarity Leading { get; }
        private readonly EmotionTag _emotion;
        private readonly int _amount;
        private readonly IEmotionPolarityTable _polarityTable;

        public AddEmotionWhenPolarityLeads(Polarity leading, EmotionTag emotion, int amount, IEmotionPolarityTable polarityTable)
        {
            Leading = leading;
            _emotion = emotion;
            _amount = amount;
            _polarityTable = polarityTable;
        }

        public void OnActivate(ItemActivationContext context) => context.AddModifier(this);

        public void Modify(TagSet finalTags)
        {
            var leading = 0;
            var other = 0;
            foreach (var (emotion, count) in finalTags.Emotions)
            {
                if (_polarityTable.GetPolarity(emotion) == Leading) leading += count;
                else other += count;
            }

            if (leading > other) finalTags.AddEmotion(_emotion, _amount);
        }
    }
}
