using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Traits
{
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

        /// <summary>일반 특성의 지속 턴이 끝나거나, 특수 특성이 치유되거나, 아이템이 특성을 지우면(<see cref="RemoveRandom"/>) 발생한다.</summary>
        public event Action<TraitInstance> Expired;

        /// <summary>아이템이 특성을 지웠을 때만 발생한다(<see cref="Expired"/>도 함께 발생) — 지속 턴 만료·치유와 구분해야 하는 쪽(결과 화면의 "새로 발현된 특성")이 쓴다.</summary>
        public event Action<TraitInstance> Removed;

        private readonly IRandomSource _random;

        /// <param name="random">특성을 랜덤으로 지우는 아이템(논리적 설득)이 쓴다. 없으면 <see cref="RemoveRandom"/>은 쓸 수 없다.</param>
        public TraitBoard(IReadOnlyList<TraitDefinition> catalog = null, IRandomSource random = null)
        {
            _catalog = catalog ?? Array.Empty<TraitDefinition>();
            _random = random;
        }

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

            var trait = new TraitInstance(definition, duration ?? definition.DefaultDuration) { RenewedExisting = existing != null };
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

        /// <summary>붙어 있는 특성(일반·특수 가리지 않고) 중 하나를 랜덤으로 지운다. 지운 특성을 돌려주고, 붙어 있는 특성이 없으면 아무것도 안 하고 null을 돌려준다.</summary>
        public TraitInstance RemoveRandom()
        {
            if (_traits.Count == 0) return null;
            if (_random == null) throw new InvalidOperationException("랜덤 원천 없이 만든 TraitBoard라 특성을 랜덤으로 지울 수 없습니다.");

            var removed = _traits[_random.Range(0, _traits.Count)];
            _traits.Remove(removed);
            Removed?.Invoke(removed);
            Expired?.Invoke(removed);
            return removed;
        }

        /// <summary>
        /// 앞서 떠 둔 목록(<see cref="Traits"/>의 사본)으로 되돌린다(튜토리얼의 오답 되돌리기). 그 뒤에 붙은 특성은 아이템이 지운 것처럼 뗀다(<see cref="Removed"/> + <see cref="Expired"/> —
        /// 턴 결과의 "새로 발현된 특성"에서도 빠진다). 그 사이 갈아끼워져 사라진 옛 특성은 조용히 다시 붙인다(떠 둔 시점에 이미 걸려 있던 것이라 새로 발현한 게 아니다).
        /// </summary>
        public void RestoreTo(IReadOnlyList<TraitInstance> snapshot)
        {
            var removed = _traits.Where(t => !snapshot.Contains(t)).ToList();

            _traits.Clear();
            _traits.AddRange(snapshot);

            foreach (var trait in removed)
            {
                Removed?.Invoke(trait);
                Expired?.Invoke(trait);
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
