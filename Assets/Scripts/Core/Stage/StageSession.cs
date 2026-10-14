using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Items;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    public sealed class StageSession
    {
        /// <summary>이 세션을 만든 스테이지 설정 — 스테이지별로 달라지는 UI(배경음 등)가 id를 여기서 읽는다.</summary>
        public StageConfig Config { get; init; }
        public TurnRunner Runner { get; init; }
        public ClueHand Hand { get; init; }
        public ComplexBoard Complexes { get; init; }
        public Heartbeat Heartbeat { get; init; }
        public HeartbeatZone Zone { get; init; }
        public ItemInventory Items { get; init; }
        public TraitBoard Traits { get; init; }
        public KeyProgress Keys { get; init; }
        public ClueKnowledgeLedger Ledger { get; init; }
        public ActiveItemBoard ActiveItems { get; init; }

        /// <summary>단서를 내기 전에 끼어드는 규칙(튜토리얼). null이면 언제나 허용한다. 게이트가 세션의 다른 부분(컴플렉스 보드, 특성)을 봐야 해서 세션이 만들어진 뒤에 붙인다.</summary>
        public IPlayGate PlayGate { get; set; }

        /// <summary>단서를 내도 되는지 — <see cref="TurnRunner.PlayClue"/> 전에 반드시 이걸 거친다(드롭 영역·디버그 입력 모두).</summary>
        public PlayVerdict CheckPlay(ClueInstance card) => PlayGate?.Check(card) ?? PlayVerdict.Allow;

        /// <summary>결과를 보여 준 거절(<see cref="PlayVerdict.IsTrial"/>)의 연출이 끝난 뒤 부른다 — 이번 턴이 시작된 상태로 되돌린다. 게이트가 없으면 아무 일도 없다.</summary>
        public void RollBackTrial()
        {
            PlayGate?.RollBackTrial();
            TrialRolledBack?.Invoke();
        }

        /// <summary>결과를 보여 준 오답을 되돌린 직후 — 가이드가 "아이템을 쓰는" 단계로 되돌아가는 데 쓴다.</summary>
        public event Action TrialRolledBack;
    }
}
