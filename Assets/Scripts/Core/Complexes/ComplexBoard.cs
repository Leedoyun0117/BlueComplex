using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Complexes
{
    /// <summary>
    /// 현재 상대에게 붙어 있는 컴플렉스 집합.
    /// 보유와 우선순위 정렬만 책임지고, 해석 실행은 ComplexResolver가 맡는다.
    /// </summary>
    public sealed class ComplexBoard
    {
        /// <summary>스테이지 설정이 따로 정하지 않았을 때의 최대 중첩. 뇌 인터페이스가 3분할이라 3이다(StageConfig.MaxComplexSlots의 기본값).</summary>
        public const int DefaultMaxSlots = 3;

        private readonly List<ComplexInstance> _slots = new();

        /// <summary>이 보드에 동시에 붙을 수 있는 컴플렉스 수. 스테이지 설정(<c>StageConfig.MaxComplexSlots</c>)에서 온다.</summary>
        public int MaxSlots { get; }

        public ComplexBoard(int maxSlots = DefaultMaxSlots)
        {
            if (maxSlots < 1) throw new ArgumentOutOfRangeException(nameof(maxSlots), "최대 중첩은 1 이상이어야 합니다.");
            MaxSlots = maxSlots;
        }

        public IReadOnlyList<ComplexInstance> Slots => _slots;
        public bool IsFull => _slots.Count >= MaxSlots;

        public event Action<ComplexInstance> Attached;
        public event Action<ComplexInstance> Expired;

        /// <summary>붙어 있는 컴플렉스의 남은 지속 시간이 아이템(극복)으로 바뀌었을 때 발생한다. 턴마다 줄어드는 Tick은 알리지 않는다(Presenter가 안다).</summary>
        public event Action<ComplexInstance> DurationChanged;

        /// <summary>슬롯이 가득 차면 신규 컴플렉스는 거부된다.</summary>
        public bool TryAttach(ComplexInstance instance)
        {
            if (IsFull) return false;
            if (_slots.Any(s => s.Definition.Id == instance.Definition.Id)) return false;

            _slots.Add(instance);
            Attached?.Invoke(instance);
            return true;
        }

        public IEnumerable<ComplexInstance> InPriorityOrder() =>
            _slots.OrderBy(s => s.Priority).ToList();

        /// <summary>
        /// 해석(평가) 순서. 기본은 <see cref="InPriorityOrder"/>와 같고, <see cref="ComplexDefinition.EvaluatedRightAfterId"/>가 있는 컴플렉스만 앵커 컴플렉스 바로 뒤로 옮긴다.
        /// 옮겨지는 컴플렉스 말고는 서로의 상대 순서가 그대로다. UI(뇌 영역 배치·목록)는 이 순서가 아니라 <see cref="InPriorityOrder"/>를 쓴다.
        /// </summary>
        public IReadOnlyList<ComplexInstance> InEvaluationOrder()
        {
            var ordered = InPriorityOrder().ToList();
            foreach (var follower in ordered.Where(c => c.Definition.EvaluatedRightAfterId != null).ToList())
            {
                var anchorId = follower.Definition.EvaluatedRightAfterId;
                if (!ordered.Any(c => c.Definition.Id == anchorId)) continue;

                ordered.Remove(follower);
                ordered.Insert(ordered.FindIndex(c => c.Definition.Id == anchorId) + 1, follower);
            }

            return ordered;
        }

        public void TickDurations()
        {
            foreach (var slot in _slots) slot.Tick();

            foreach (var expired in _slots.Where(s => s.IsExpired).ToList())
            {
                _slots.Remove(expired);
                Expired?.Invoke(expired);
            }
        }

        /// <summary>붙어 있는 컴플렉스의 남은 지속 시간을 절반으로 줄인다(아이템 '극복'). 줄었으면 <see cref="DurationChanged"/>를 알린다.</summary>
        public bool HalveRemainingTurns(ComplexInstance complex)
        {
            if (!_slots.Contains(complex)) return false;
            if (!complex.HalveRemainingTurns()) return false;

            DurationChanged?.Invoke(complex);
            return true;
        }

        public void Clear() => _slots.Clear();
    }
}
