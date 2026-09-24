using System;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Stability
{
    /// <summary>유키의 턴 종료 표정. Normal(평소)을 제외한 셋은 "표정과 반응" 기획표의 반응 기준을 그대로 옮긴 것이다.</summary>
    public enum YukiReaction
    {
        Normal,
        Anger,
        Sadness,
        Joy
    }

    /// <summary>나츠의 표정 상태. Fury는 당황이다. Focus(키 턴)는 심박수가 아니라 턴 종류로 정해지므로 <see cref="PortraitReactionRules.ClassifyNatsu"/>는 돌려주지 않는다.</summary>
    public enum NatsuExpression
    {
        Normal,
        Fury,
        Focus,
        Relief
    }

    /// <summary>
    /// "표정과 반응" 기획서(유키/나츠)의 순수 판정 로직. UI 쪽 포트레이트 뷰는 이 판정 결과로 스프라이트만 고르고,
    /// 판정 자체는 여기서만 한다(Unity 의존 없음, EditMode 테스트로 검증 가능).
    /// </summary>
    public static class PortraitReactionRules
    {
        private static readonly EmotionTag[] AllEmotions = (EmotionTag[])Enum.GetValues(typeof(EmotionTag));

        /// <summary>
        /// 유키의 턴 종료 표정. 기획표 순서(분노 → 슬픔 → 기쁨) 그대로 우선순위를 매겼다 — 스펙 원문에 세 조건이
        /// 동시에 성립할 때의 우선순위가 명시돼 있지 않아, 표에 적힌 순서를 가장 문자 그대로의 해석으로 삼았다.
        /// finalTags가 null이면(손패가 비어 넘어간 턴) Normal.
        /// </summary>
        public static YukiReaction ClassifyYuki(TagSet finalTags, HeartbeatState heartbeatState)
        {
            if (finalTags == null) return YukiReaction.Normal;

            if (IsPlurality(finalTags, EmotionTag.Anger)) return YukiReaction.Anger;
            if (heartbeatState is HeartbeatState.Depressed or HeartbeatState.VeryDepressed) return YukiReaction.Sadness;
            if (IsPluralityCombined(finalTags, EmotionTag.Happiness, EmotionTag.Love)) return YukiReaction.Joy;

            return YukiReaction.Normal;
        }

        /// <summary>나츠가 당황하는 조건 — 매우 침체 또는 매우 흥분.</summary>
        public static bool IsNatsuFlustered(HeartbeatState heartbeatState) =>
            heartbeatState is HeartbeatState.VeryDepressed or HeartbeatState.VeryExcited;

        /// <summary>
        /// 심박수 구간이 바뀔 때의 나츠 표정: 매우 침체/매우 흥분이면 당황(Fury), 안정 구간에 새로 들어섰으면 안도(Relief),
        /// 그 외엔 Normal. Normal은 "지금 표정을 평소로 되돌려라"는 뜻이지, 안도 연출이 도는 중에 끊으라는 뜻이 아니다(호출한 쪽이 판단).
        /// </summary>
        public static NatsuExpression ClassifyNatsu(HeartbeatState previous, HeartbeatState current)
        {
            if (IsNatsuFlustered(current)) return NatsuExpression.Fury;
            if (current == HeartbeatState.Stable && previous != HeartbeatState.Stable) return NatsuExpression.Relief;
            return NatsuExpression.Normal;
        }

        /// <summary>emotion 하나의 개수가 다른 모든 감정 태그 각각보다 많은가("다른 태그보다 많을 시"의 문자 그대로 해석 — 동률이면 아니다).</summary>
        private static bool IsPlurality(TagSet tags, EmotionTag emotion)
        {
            var count = tags.CountOf(emotion);
            return count > 0 && AllEmotions.Where(e => e != emotion).All(e => count > tags.CountOf(e));
        }

        private static bool IsPluralityCombined(TagSet tags, EmotionTag a, EmotionTag b)
        {
            var count = tags.CountOf(a) + tags.CountOf(b);
            return count > 0 && AllEmotions.Where(e => e != a && e != b).All(e => count > tags.CountOf(e));
        }
    }
}
