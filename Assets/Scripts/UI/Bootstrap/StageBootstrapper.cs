using System;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Background;
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
        [SerializeField] private LampLightDriver _lampLightDriver;

        [Header("디버그 입력")]
        [Tooltip("숫자키 1~4로 해당 인덱스의 손패 카드를 즉시 낸다.")]
        [SerializeField] private bool _enableKeyboardInput = true;

        [Tooltip("인스펙터에서 낼 카드의 손패 인덱스. 컨텍스트 메뉴 'Play Selected Card'로 실행.")]
        [SerializeField] private int _selectedCardIndex;

        [Header("시드")]
        [SerializeField] private int _seed = 20260916;

        public StageSession Session { get; private set; }

        /// <summary>지금 돌고 있는 스테이지의 저작 설정 — 스테이지 이름 표시 같은 UI가 읽는다. 세션이 바뀌면 함께 바뀐다.</summary>
        public StageConfig Config { get; private set; }
        public int CurrentSeed { get; private set; }

        /// <summary>전체 화면 오버레이 같은 UI가 게임 입력을 잠글 때 켠다 — 그동안 디버그 숫자키가 카드를 내지 않는다.
        /// (마우스 입력은 오버레이가 레이캐스트로 막는다.)</summary>
        public bool InputBlocked { get; set; }

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
            if (!_enableKeyboardInput || InputBlocked || Session == null || Keyboard.current == null) return;

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

            Config = config;
            Session = StageFactory.Create(config, random, _ledger, _polarityTable);
            _logger = new StageTurnLogger(Session);

            if (_crtEffectDriver != null)
                _crtEffectDriver.Bind(Session.Heartbeat);

            if (_lampLightDriver != null)
                _lampLightDriver.Bind(Session.Heartbeat);

            // StartStage()가 첫 TurnBegan을 곧바로 쏘아 올리므로, 구독자는 그 전에 새 세션을 받아야 한다.
            RaiseSessionStarted();
            Session.Runner.StartStage();
        }

        /// <summary>구독자마다 따로 부른다 — 뷰 하나의 초기화가 예외로 죽어도(참조가 끊긴 프리팹 등) 뒤의 뷰들이 초기화를 못 받아 화면이 통째로 비는 일이 없게 한다.
        /// 예외는 삼키지 않고 콘솔에 그대로 남긴다.</summary>
        private void RaiseSessionStarted()
        {
            var handlers = SessionStarted;
            if (handlers == null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<StageSession>)handler)(Session);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
