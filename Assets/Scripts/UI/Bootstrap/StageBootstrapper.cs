using System;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Debugging;
using BlueComplex.UI.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueComplex.UI.Bootstrap
{
    /// <summary>
    /// 씬에서 프로토타입 스테이지를 직접 돌려보기 위한 임시 디버그 부트스트래퍼.
    /// 세션 조립과 입력을 PlayClue 호출로 중계하는 것만 담당한다 — 판정/연출 로직은 갖지 않는다.
    /// 정식 UI가 붙으면 이 컴포넌트는 걷어낸다.
    /// </summary>
    public sealed class StageBootstrapper : MonoBehaviour
    {
        [SerializeField] private CrtEffectDriver _crtEffectDriver;

        [Header("디버그 입력")]
        [Tooltip("숫자키 1~4로 해당 인덱스의 손패 카드를 즉시 낸다.")]
        [SerializeField] private bool _enableKeyboardInput = true;

        [Tooltip("인스펙터에서 낼 카드의 손패 인덱스. 컨텍스트 메뉴 'Play Selected Card'로 실행.")]
        [SerializeField] private int _selectedCardIndex;

        [Header("시드")]
        [SerializeField] private int _seed = 20260916;

        public StageSession Session { get; private set; }
        public int CurrentSeed { get; private set; }

        /// <summary>새 StageSession이 만들어질 때마다(최초 시작 포함) 알린다. Ledger는 세션 간에 계속 유지된다.</summary>
        public event Action<StageSession> SessionStarted;

        private ClueKnowledgeLedger _ledger;
        private IEmotionPolarityTable _polarityTable;
        private StageTurnLogger _logger;

        private void Start()
        {
            _polarityTable = new DefaultEmotionPolarityTable();
            _ledger = new ClueKnowledgeLedger();

            BeginNewSession(_seed);
        }

        private void OnDestroy()
        {
            _logger?.Dispose();
        }

        private void Update()
        {
            if (!_enableKeyboardInput || Session == null || Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) PlayCardAtIndex(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) PlayCardAtIndex(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) PlayCardAtIndex(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame) PlayCardAtIndex(3);
        }

        [ContextMenu("Play Selected Card")]
        private void PlaySelectedCardFromInspector() => PlayCardAtIndex(_selectedCardIndex);

        /// <summary>손패의 index번째 카드를 낸다. 입력을 TurnRunner.PlayClue 호출로 그대로 중계한다.</summary>
        public void PlayCardAtIndex(int index)
        {
            if (Session == null || Session.Runner.Outcome != StageOutcome.InProgress) return;
            if (index < 0 || index >= Session.Hand.Cards.Count)
            {
                Debug.LogWarning($"손패 인덱스 {index}는 유효하지 않습니다. (현재 손패 {Session.Hand.Cards.Count}장)");
                return;
            }

            Session.Runner.PlayClue(Session.Hand.Cards[index]);
        }

        /// <summary>같은 시드로 스테이지를 재시작한다. 해금 지식(Ledger)은 그대로 유지된다.</summary>
        public void RestartWithSameSeed() => BeginNewSession(CurrentSeed);

        /// <summary>새 무작위 시드로 스테이지를 재시작한다. 해금 지식(Ledger)은 그대로 유지된다.</summary>
        public void RestartWithNewSeed() => BeginNewSession(Environment.TickCount);

        private void BeginNewSession(int seed)
        {
            _logger?.Dispose();

            CurrentSeed = seed;
            var config = PrototypeContent.PrototypeStage(_polarityTable);
            var random = new SystemRandomSource(seed);

            Session = StageFactory.Create(config, random, _ledger, _polarityTable);
            _logger = new StageTurnLogger(Session);

            if (_crtEffectDriver != null)
                _crtEffectDriver.Bind(Session.Heartbeat);

            // StartStage()가 첫 TurnBegan을 곧바로 쏘아 올리므로, 구독자는 그 전에 새 세션을 받아야 한다.
            SessionStarted?.Invoke(Session);
            Session.Runner.StartStage();
        }
    }
}
