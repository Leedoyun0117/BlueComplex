using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Items;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>튜토리얼 한 턴의 진행 규칙. 턴은 1부터 센다.</summary>
    public sealed class TutorialStep
    {
        public int Turn { get; }

        /// <summary>이 턴이 시작될 때 손패를 통째로 이 단서들로 바꾼다. null이면 손패를 그대로 둔다. 빈 목록이면 손패를 비운다 —
        /// 손패가 비면 코어가 그 턴을 그냥 넘긴다(<see cref="TurnReport.IsPass"/>), 그래서 "아무 것도 내지 않고 시간만 흐르는 턴"이 된다.</summary>
        public IReadOnlyList<ClueDefinition> Hand { get; }

        /// <summary>이 턴에 낸 단서의 최종 결과가 향해야 하는 방향. null이면 어떤 카드든 낼 수 있다.</summary>
        public Polarity? TargetPolarity { get; }

        /// <summary>이 턴에 카드를 내려면 모두 켜져 있어야 하는 아이템 id들. 비어 있으면 상관없다.</summary>
        public IReadOnlyList<string> RequiredActiveItemIds { get; }

        /// <summary>이 턴이 시작될 때 손에 쥐여 주는 아이템들(빈 칸이 있을 때만, 순서대로). 비어 있으면 아무것도 주지 않는다.
        /// 아이템을 언제 처음 만나게 할지는 여기서 정한다 — 그 전에는 아이템이 손에 없어서 미리 쓸 수 없다.</summary>
        public IReadOnlyList<ItemDefinition> GrantItems { get; }

        /// <summary>
        /// true면 이 턴의 오답(이 카드를 내면 쿼터의 키를 못 얻는 수)을 내기 전에 막지 않는다 — 결과를 끝까지 보여 준 뒤 이 턴이 시작된 상태로 되돌린다(<see cref="PlayVerdict.IsTrial"/>).
        /// 방향(<see cref="TargetPolarity"/>) 대신 키 판정으로 정답을 가린다. 키 턴(쿼터의 마지막 턴)에서만 뜻이 있다.
        /// </summary>
        public bool ShowMissThenRollBack { get; }

        public TutorialStep(int turn,
                            IReadOnlyList<ClueDefinition> hand = null,
                            Polarity? targetPolarity = null,
                            IReadOnlyList<string> requiredActiveItemIds = null,
                            IReadOnlyList<ItemDefinition> grantItems = null,
                            bool showMissThenRollBack = false)
        {
            Turn = turn;
            Hand = hand;
            TargetPolarity = targetPolarity;
            RequiredActiveItemIds = requiredActiveItemIds ?? Array.Empty<string>();
            GrantItems = grantItems ?? Array.Empty<ItemDefinition>();
            ShowMissThenRollBack = showMissThenRollBack;
        }
    }
}
