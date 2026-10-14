using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 튜토리얼 가이드의 진행 상태. 지금 어느 단계이고 무엇이 일어나면 다음 단계로 넘어가는지만 안다 — 말풍선·강조·클릭 막은 UI(TutorialGuide)가 이 상태를 읽어 그린다.
    /// 화면과 무관해서 EditMode로 검증할 수 있다. 단계에 맞지 않는 사건은 무시한다(예: 책이 열리기 전에 메뉴얼 탭이 알려져도 넘어가지 않는다).
    /// </summary>
    public sealed class TutorialGuideFlow
    {
        private readonly IReadOnlyList<TutorialGuideStep> _steps;
        private int _index = -1;

        /// <summary>지금 단계에서 기다리던 사건이 몇 번 일어났는지(<see cref="TutorialGuideStep.RequiredCount"/>까지 센다). 단계가 바뀌면 0.</summary>
        private int _count;

        public TutorialGuideFlow(IReadOnlyList<TutorialGuideStep> steps)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }

        public IReadOnlyList<TutorialGuideStep> Steps => _steps;

        /// <summary>지금 단계. 시작 전이거나 끝났으면 null.</summary>
        public TutorialGuideStep Current => _index >= 0 && _index < _steps.Count ? _steps[_index] : null;

        public bool IsStarted => _index >= 0;
        public bool IsFinished => _index >= _steps.Count;

        /// <summary>단계가 바뀔 때마다(시작 포함) 새 단계로 알린다.</summary>
        public event Action<TutorialGuideStep> StepChanged;

        /// <summary>마지막 단계가 끝났을 때.</summary>
        public event Action Finished;

        /// <summary>처음 단계로 시작한다. 이미 시작했으면 아무 일도 없다.</summary>
        public void Begin()
        {
            if (IsStarted) return;
            _index = 0;
            Announce();
        }

        /// <summary>지금 단계가 <paramref name="stepId"/>보다 뒤라면 그 단계로 되돌아가 다시 안내한다(그 단계 이후에 걸린 상태가 되돌려졌을 때). 이미 그 단계이거나 앞이면 아무 일도 없다.</summary>
        public bool Rewind(string stepId)
        {
            var target = -1;
            for (var i = 0; i < _steps.Count; i++)
                if (_steps[i].Id == stepId) target = i;

            if (target < 0 || _index <= target) return false;

            _index = target;
            _count = 0;
            Announce();
            return true;
        }

        /// <summary>지금 단계가 끝나려면 기다리던 사건이 몇 번 더 일어나야 하는지. 시작 전이거나 끝났으면 0.</summary>
        public int RemainingCount => Current == null ? 0 : Current.RequiredCount - _count;

        /// <summary>일어난 일을 알린다. 지금 단계가 기다리던 것이고 필요한 횟수(<see cref="TutorialGuideStep.RequiredCount"/>)를 채웠으면 다음 단계로 넘어가고 true.
        /// <paramref name="turn"/>은 <see cref="GuideAdvance.TurnResolved"/>일 때 결산된 턴 번호.</summary>
        public bool Notify(GuideAdvance happened, int turn = 0)
        {
            var current = Current;
            if (current == null || current.Advance != happened) return false;
            if (happened == GuideAdvance.TurnResolved && turn < current.WaitTurn) return false;
            if (++_count < current.RequiredCount) return false;

            _index++;
            _count = 0;
            Announce();
            return true;
        }

        private void Announce()
        {
            var current = Current;
            if (current != null) StepChanged?.Invoke(current);
            else Finished?.Invoke();
        }
    }
}
