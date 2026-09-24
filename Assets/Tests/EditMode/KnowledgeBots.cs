using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 로그라이트 곡선 측정용 — 단서 속성 해금 상태(<see cref="ClueKnowledgeLedger"/>)를 아는 만큼만 보는 봇과, 런을 이어 붙여 해금을 쌓는 실행기.
    ///
    /// 주의: <see cref="QuarterBots.ChooseHeuristicCard"/>는 카드의 진짜 태그로 미리 계산하는 봇이라 해금 상태와 무관하게 "태그 다 앎" 봇이다.
    /// 여기 봇은 카드 UI가 실제로 보여 주는 것만 쓴다 — 해금된 시간·인물·감정 태그, 그리고 안 밝혀진 칸의 개수("?"의 수).
    /// 안 밝혀진 칸은 액자식 스토리로 추측해야 하는데, 추측은 모델링할 수 없어서 칸마다 <c>guessAccuracy</c> 확률로 맞힌다고 본다
    /// (틀리면 그 칸을 무시한 것과 같다). 0 = 밝혀진 것만 믿는다, 1 = 완전 정보(=QuarterBots 휴리스틱과 같은 판단).
    /// 컴플렉스·심박수·구역은 화면에 다 보이므로 진짜 값을 쓴다.
    /// </summary>
    public static class KnowledgeBots
    {
        /// <summary>모든 단서의 시간·인물·감정 태그를 전부 아는 장부(여러 런을 거친 최종 상태).</summary>
        public static ClueKnowledgeLedger FullyRevealedLedger(IEnumerable<ClueDefinition> clues)
        {
            var ledger = new ClueKnowledgeLedger();
            foreach (var clue in clues)
            {
                var knowledge = ledger.GetKnowledge(clue.Id);
                if (clue.Times.Count > 0) knowledge.RevealTime();
                foreach (var person in clue.Persons) knowledge.RevealPerson(person);
                foreach (var emotion in clue.Emotions) knowledge.RevealEmotion(emotion);
            }

            return ledger;
        }

        /// <summary>전체 단서의 (시간 1칸 + 인물 칸 + 감정 칸) 중 밝혀진 칸의 비율.</summary>
        public static double RevealedFraction(ClueKnowledgeLedger ledger, IEnumerable<ClueDefinition> clues)
        {
            int total = 0, revealed = 0;
            foreach (var clue in clues)
            {
                var knowledge = ledger.GetKnowledge(clue.Id);
                if (clue.Times.Count > 0) { total++; if (knowledge.TimeRevealed) revealed++; }
                foreach (var person in clue.Persons) { total++; if (knowledge.IsPersonRevealed(person)) revealed++; }
                foreach (var emotion in clue.Emotions) { total++; if (knowledge.IsEmotionRevealed(emotion)) revealed++; }
            }

            return total == 0 ? 1.0 : (double)revealed / total;
        }

        /// <summary>
        /// 봇이 이 카드에 대해 믿는 원본 태그. 해금된 칸은 그대로, 안 밝혀진 칸은 guessAccuracy 확률로 맞히고 못 맞히면 빠진다.
        /// </summary>
        public static TagSet BelievedTags(ClueDefinition clue, ClueKnowledge knowledge, double guessAccuracy, Random guess)
        {
            bool Guessed() => guessAccuracy >= 1.0 || (guessAccuracy > 0.0 && guess.NextDouble() < guessAccuracy);

            var times = clue.Times.Count == 0 || knowledge.TimeRevealed || Guessed()
                ? clue.Times
                : Array.Empty<TimeTag>();
            var persons = clue.Persons.Where(p => knowledge.IsPersonRevealed(p) || Guessed()).ToList();
            var emotions = clue.Emotions.Where(e => knowledge.IsEmotionRevealed(e) || Guessed()).ToList();

            return new TagSet(times, persons, emotions);
        }

        /// <summary>해금 상태만 아는 휴리스틱 봇. 카드마다 한 번만 추측하고(같은 카드는 계산마다 같은 믿음), 런마다 새로 만든다.</summary>
        public sealed class BeliefChooser
        {
            private readonly ClueKnowledgeLedger _ledger;
            private readonly double _guessAccuracy;
            private readonly Random _guess;
            private readonly Dictionary<ClueInstance, TagSet> _beliefs = new();

            public BeliefChooser(ClueKnowledgeLedger ledger, double guessAccuracy, int seed)
            {
                _ledger = ledger;
                _guessAccuracy = guessAccuracy;
                _guess = new Random(seed * 31 + 7);
            }

            public TagSet Believe(ClueInstance card)
            {
                if (!_beliefs.TryGetValue(card, out var tags))
                {
                    tags = BelievedTags(card.Definition, _ledger.GetKnowledge(card.Definition.Id), _guessAccuracy, _guess);
                    _beliefs[card] = tags;
                }

                return tags;
            }

            public ClueInstance Choose(StageSession session) => QuarterBots.ChooseHeuristicCard(session, Believe);
        }

        public readonly struct RunResult
        {
            public StageOutcome Outcome { get; }
            public int KeysCollected { get; }
            public int ItemsUsed { get; }

            public RunResult(StageOutcome outcome, int keysCollected, int itemsUsed)
            {
                Outcome = outcome;
                KeysCollected = keysCollected;
                ItemsUsed = itemsUsed;
            }

            public bool Cleared => Outcome == StageOutcome.Cleared;
        }

        /// <summary>
        /// 한 런을 끝까지 돌린다. 장부는 그대로 넘겨받아 쓰고, 런이 끝나면 CommitRun()으로 이번 런의 관찰을 확정한다(다음 런부터 해금).
        /// <paramref name="useItems"/>가 켜지면 카드를 내기 전에 ItemBots가 쓸 만한 아이템을 쓴다(완전 정보 규칙).
        /// </summary>
        public static RunResult RunOnce(StageConfig config, int seed, ClueKnowledgeLedger ledger,
                                        Func<StageSession, ClueInstance> choose, bool useItems = false)
        {
            var random = new SystemRandomSource(seed);
            var session = StageFactory.Create(config, random, ledger, new DefaultEmotionPolarityTable());
            session.Runner.StartStage();

            var itemsUsed = 0;
            var guard = 0;
            while (session.Runner.Outcome == StageOutcome.InProgress && guard < config.TotalTurns)
            {
                guard++;
                if (session.Hand.Cards.Count == 0) break;

                if (useItems) itemsUsed += ItemBots.UseHelpfulItems(session, config).Count;
                if (session.Runner.Outcome != StageOutcome.InProgress || session.Hand.Cards.Count == 0) break;

                session.Runner.PlayClue(choose(session));
            }

            ledger.CommitRun();
            return new RunResult(session.Runner.Outcome, session.Keys.Collected, itemsUsed);
        }
    }
}
