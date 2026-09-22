using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Traits
{
    /// <summary>일반 특성은 아이템 사용으로 발현되고 정해진 턴 동안만 간다. 특수 특성은 컴플렉스가 최대 중첩을 넘쳐 발현될 때 붙고, 안정 구간에 들어설 때까지 간다.</summary>
    public enum TraitKind
    {
        Normal,
        Special
    }

    /// <summary>
    /// 특성 한 종류의 데이터. 효과는 숫자 필드로만 적혀 있고 <see cref="TraitBoard"/>와 평가기가 그 값을 모아 쓴다 — 특성 이름으로 갈라지는 코드는 없다.
    /// 기본값(1, false)은 "영향 없음"이라 필요한 필드만 채우면 된다.
    /// </summary>
    public sealed class TraitDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public TraitKind Kind { get; }

        /// <summary>일반 특성이 붙어 있는 턴 수(이 값 이후 만료). 특수 특성은 무시된다 — 치유될 때까지 간다.</summary>
        public int DefaultDuration { get; init; } = 1;

        /// <summary>심박수 변화량에 곱하는 배율(예민 ×3, 무력 ×1/2). 여러 특성이면 곱한다.</summary>
        public double HeartbeatMultiplier { get; init; } = 1.0;

        /// <summary>단서의 원래 감정 개수에 곱하는 배율(과대 망상 ×2). 컴플렉스 해석 <b>전</b>에 적용된다.</summary>
        public int EmotionCountMultiplier { get; init; } = 1;

        /// <summary>침체 감정과 흥분 감정을 반대로 받아들인다(환각) — 심박수가 원래와 반대로 변한다.</summary>
        public bool InvertsPolarity { get; init; }

        /// <summary>흥분 감정 한 개의 영향력에 곱하는 배율(고기능 우울증 ×1/2). 환각이 있으면 뒤집힌 뒤의 극성 기준이다.</summary>
        public double ExcitedInfluence { get; init; } = 1.0;

        /// <summary>침체 감정 한 개의 영향력에 곱하는 배율(과흥분 ×1/2).</summary>
        public double DepressedInfluence { get; init; } = 1.0;

        /// <summary>특수 특성이 어느 쪽 구간에서 컴플렉스가 넘칠 때 붙는지(고기능 우울증 = 침체, 과흥분 = 흥분). 일반 특성은 null.</summary>
        public Polarity? OverflowSide { get; init; }

        public TraitDefinition(string id, string displayName, string description, TraitKind kind)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Kind = kind;
        }
    }

    public sealed class TraitInstance
    {
        public TraitDefinition Definition { get; }

        /// <summary>남은 턴. 특수 특성은 턴으로 줄지 않으므로 항상 <see cref="TraitBoard.UntilCured"/>다.</summary>
        public int RemainingTurns { get; private set; }

        public bool IsExpired => Definition.Kind == TraitKind.Normal && RemainingTurns <= 0;

        public TraitInstance(TraitDefinition definition, int duration)
        {
            Definition = definition;
            RemainingTurns = definition.Kind == TraitKind.Special ? TraitBoard.UntilCured : duration;
        }

        public void Tick()
        {
            if (Definition.Kind == TraitKind.Normal) RemainingTurns--;
        }
    }

    /// <summary>
    /// 지금 붙어 있는 특성들과, 그 효과를 모은 값. 효과 적용 순서는 <see cref="TraitAwareEmotionEvaluator"/>와 TurnRunner.PlayClue 주석에 있다.
    /// 특성 목록(카탈로그)은 스테이지 설정이 준다 — 아이템·컴플렉스 초과가 id나 방향으로 특성을 찾는다.
    /// </summary>
    public sealed class TraitBoard
    {
        /// <summary>특수 특성의 남은 턴 표시값(턴으로 줄지 않고 치유될 때까지 간다).</summary>
        public const int UntilCured = int.MaxValue;

        private readonly List<TraitInstance> _traits = new();
        private readonly IReadOnlyList<TraitDefinition> _catalog;

        public IReadOnlyList<TraitInstance> Traits => _traits;
        public IReadOnlyList<TraitDefinition> Catalog => _catalog;

        public event Action<TraitInstance> Granted;

        /// <summary>일반 특성의 지속 턴이 끝나거나 특수 특성이 치유되면 발생한다.</summary>
        public event Action<TraitInstance> Expired;

        public TraitBoard(IReadOnlyList<TraitDefinition> catalog = null) =>
            _catalog = catalog ?? Array.Empty<TraitDefinition>();

        public bool Has(string traitId) => _traits.Any(t => t.Definition.Id == traitId);
        public bool Has(TraitDefinition definition) => Has(definition.Id);
        public bool HasSpecial => _traits.Any(t => t.Definition.Kind == TraitKind.Special);

        public TraitDefinition Find(string traitId) => _catalog.FirstOrDefault(t => t.Id == traitId);

        // ------------------------------------------------------------------
        // 효과 집계
        // ------------------------------------------------------------------

        /// <summary>심박수 변화량 배율(예민 ×3, 무력 ×1/2 — 둘 다면 ×1.5).</summary>
        public double HeartbeatMultiplier => _traits.Aggregate(1.0, (product, t) => product * t.Definition.HeartbeatMultiplier);

        /// <summary>단서 원래 감정 개수 배율(과대 망상 ×2).</summary>
        public int EmotionCountMultiplier => _traits.Aggregate(1, (product, t) => product * t.Definition.EmotionCountMultiplier);

        /// <summary>환각이 붙어 있으면 true — 침체/흥분 감정을 반대로 받아들인다.</summary>
        public bool InvertsPolarity => _traits.Count(t => t.Definition.InvertsPolarity) % 2 == 1;

        /// <summary>받아들이는 극성(환각이 뒤집은 뒤)이 <paramref name="perceived"/>인 감정 하나의 영향력 배율(특수 특성).</summary>
        public double InfluenceMultiplier(Polarity perceived) => _traits.Aggregate(1.0, (product, t) =>
            product * (perceived == Polarity.Excited ? t.Definition.ExcitedInfluence : t.Definition.DepressedInfluence));

        /// <summary>단서의 원래 태그에 특성을 적용한 사본(과대 망상 = 감정 개수 ×N). 원본은 건드리지 않는다. 컴플렉스 해석 전에 부른다.</summary>
        public TagSet ApplyToOriginal(TagSet original)
        {
            var multiplier = EmotionCountMultiplier;
            var copy = original.Clone();
            if (multiplier <= 1) return copy;

            foreach (var pair in original.Emotions)
                copy.AddEmotion(pair.Key, pair.Value * (multiplier - 1));
            return copy;
        }

        // ------------------------------------------------------------------
        // 발현 / 만료
        // ------------------------------------------------------------------

        /// <summary>특성을 붙인다. 같은 특성이 이미 있으면 새로 붙인 것으로 갈아끼운다(지속 턴이 다시 시작된다).</summary>
        public TraitInstance Grant(TraitDefinition definition, int? duration = null)
        {
            var existing = _traits.FirstOrDefault(t => t.Definition.Id == definition.Id);
            if (existing != null) _traits.Remove(existing);

            var trait = new TraitInstance(definition, duration ?? definition.DefaultDuration);
            _traits.Add(trait);
            Granted?.Invoke(trait);
            return trait;
        }

        public TraitInstance Grant(string traitId, int? duration = null)
        {
            var definition = Find(traitId) ?? throw new InvalidOperationException($"특성 카탈로그에 '{traitId}'이(가) 없습니다.");
            return Grant(definition, duration);
        }

        /// <summary>컴플렉스가 최대 중첩을 넘쳐 발현됐을 때 그 구간 쪽의 특수 특성을 붙인다. 그 쪽 특수 특성이 카탈로그에 없으면 null.</summary>
        public TraitInstance GrantOverflow(Polarity side)
        {
            var definition = _catalog.FirstOrDefault(t => t.Kind == TraitKind.Special && t.OverflowSide == side);
            return definition == null ? null : Grant(definition);
        }

        /// <summary>일반 특성만 한 턴 줄이고 끝난 것은 뗀다. 특수 특성은 턴으로 만료되지 않는다.</summary>
        public void TickDurations()
        {
            foreach (var trait in _traits) trait.Tick();

            foreach (var expired in _traits.Where(t => t.IsExpired).ToList())
            {
                _traits.Remove(expired);
                Expired?.Invoke(expired);
            }
        }

        /// <summary>붙어 있는 특수 특성을 전부 치유한다(안정 구간에 들어서는 즉시 TurnRunner가 부른다). 치유된 특성 수를 돌려준다.</summary>
        public int CureSpecial()
        {
            var cured = _traits.Where(t => t.Definition.Kind == TraitKind.Special).ToList();
            foreach (var trait in cured)
            {
                _traits.Remove(trait);
                Expired?.Invoke(trait);
            }

            return cured.Count;
        }
    }
}
