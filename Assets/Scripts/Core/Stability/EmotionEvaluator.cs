using System;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Stability
{
    public interface IEmotionEvaluator
    {
        int Evaluate(TagSet finalTags);
    }

    /// <summary>흥분 태그 +TagMagnitude, 침체 태그 -TagMagnitude. 중복 감정은 개수만큼 누적된다.</summary>
    public sealed class EmotionEvaluator : IEmotionEvaluator
    {
        /// <summary>태그 하나가 심박수에 미치는 영향력. 기본값은 200 스케일 기준 ±10.</summary>
        public const int DefaultTagMagnitude = 10;

        private readonly IEmotionPolarityTable _polarityTable;
        private readonly int _tagMagnitude;

        public EmotionEvaluator(IEmotionPolarityTable polarityTable, int tagMagnitude = DefaultTagMagnitude)
        {
            _polarityTable = polarityTable;
            _tagMagnitude = tagMagnitude;
        }

        public int Evaluate(TagSet finalTags) =>
            finalTags.EnumerateEmotionsFlat().Sum(e => (int)_polarityTable.GetPolarity(e)) * _tagMagnitude;
    }

    /// <summary>
    /// 특성을 반영해 심박수 변화량을 계산한다. 이 평가기가 맡는 단계의 순서(전체 파이프라인은 TurnRunner.PlayClue 주석):
    /// 1) 환각 — 감정 하나하나의 극성을 뒤집는다(침체 ↔ 흥분).
    /// 2) 특수 특성 — 뒤집힌 뒤의 극성 기준으로 감정 하나의 영향력을 줄인다(고기능 우울증 = 흥분 ×1/2, 과흥분 = 침체 ×1/2).
    /// 3) 예민/무력 — 합계에 배율을 곱한다(×3, ×1/2).
    /// 4) 반올림 — 위 계산은 실수로 하고, 마지막에 한 번만 반올림한다(0.5는 0에서 먼 쪽으로). 2)와 3)은 곱셈이라 순서를 바꿔도 값이 같다.
    /// 과대 망상(감정 개수 ×2)은 이 평가기 앞, 컴플렉스 해석 전에 TraitBoard.ApplyToOriginal이 처리한다.
    /// </summary>
    public sealed class TraitAwareEmotionEvaluator : IEmotionEvaluator
    {
        private readonly IEmotionPolarityTable _polarityTable;
        private readonly TraitBoard _traits;
        private readonly int _tagMagnitude;

        public TraitAwareEmotionEvaluator(IEmotionPolarityTable polarityTable, TraitBoard traits,
                                          int tagMagnitude = EmotionEvaluator.DefaultTagMagnitude)
        {
            _polarityTable = polarityTable;
            _traits = traits;
            _tagMagnitude = tagMagnitude;
        }

        public int Evaluate(TagSet finalTags)
        {
            var invert = _traits.InvertsPolarity;
            var sum = 0.0;

            foreach (var emotion in finalTags.EnumerateEmotionsFlat())
            {
                var perceived = _polarityTable.GetPolarity(emotion);
                if (invert) perceived = perceived == Polarity.Excited ? Polarity.Depressed : Polarity.Excited;

                sum += (int)perceived * _tagMagnitude * _traits.InfluenceMultiplier(perceived);
            }

            return (int)Math.Round(sum * _traits.HeartbeatMultiplier, MidpointRounding.AwayFromZero);
        }
    }
}
