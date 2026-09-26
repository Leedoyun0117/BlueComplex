using System;
using System.Collections.Generic;

namespace BlueComplex.Core.Complexes
{
    /// <summary>
    /// 정해진 턴이 끝날 때 정해진 컴플렉스를 붙이는 발현 규칙(튜토리얼). 확률도 난수도 쓰지 않는다.
    /// 발현 여부(<see cref="IComplexSpawnPolicy"/>)와 무엇을 붙일지(<see cref="IComplexSpawner"/>)를 한 객체가 함께 맡아, 같은 표를 두 곳에서 따로 들지 않게 한다.
    /// 턴 번호는 스테이지가 만들어진 뒤에야 알 수 있어(턴 러너가 이 객체를 받아 만들어진다) <see cref="BindTurn"/>으로 나중에 연결한다.
    /// 붙는 컴플렉스의 우선순위와 지속 시간은 <see cref="ComplexSpawner"/>와 같다(기본 100 + 붙어 있던 수) — 먼저 붙은 것이 먼저 해석된다.
    /// </summary>
    public sealed class ScriptedComplexSchedule : IComplexSpawnPolicy, IComplexSpawner
    {
        private const int DefaultPriority = 100;

        private readonly IReadOnlyDictionary<int, ComplexDefinition> _afterTurn;
        private Func<int> _currentTurn;

        /// <param name="afterTurn">턴 번호 → 그 턴이 끝날 때 붙는 컴플렉스. 표에 없는 턴에는 아무것도 붙지 않는다.</param>
        public ScriptedComplexSchedule(IReadOnlyDictionary<int, ComplexDefinition> afterTurn)
        {
            _afterTurn = afterTurn ?? throw new ArgumentNullException(nameof(afterTurn));
        }

        /// <summary>지금 진행 중인 턴 번호를 읽는 함수를 연결한다(보통 <c>() =&gt; session.Runner.CurrentTurn</c>).</summary>
        public void BindTurn(Func<int> currentTurn) => _currentTurn = currentTurn;

        public bool ShouldSpawn(int heartbeatValue) => Scheduled() != null;

        public bool TrySpawn(ComplexBoard board, out ComplexInstance spawned)
        {
            spawned = null;
            var definition = Scheduled();
            if (definition == null || board.IsFull) return false;

            var instance = new ComplexInstance(definition, DefaultPriority + board.Slots.Count);
            if (!board.TryAttach(instance)) return false;

            spawned = instance;
            return true;
        }

        private ComplexDefinition Scheduled()
        {
            if (_currentTurn == null)
                throw new InvalidOperationException("BindTurn으로 턴 번호를 연결해야 합니다.");

            return _afterTurn.TryGetValue(_currentTurn(), out var definition) ? definition : null;
        }
    }
}
