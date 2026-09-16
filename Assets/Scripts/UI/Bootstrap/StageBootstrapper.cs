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

        private StageTurnLogger _logger;

        private void Start()
        {
            var polarityTable = new DefaultEmotionPolarityTable();
            var config = PrototypeContent.PrototypeStage(polarityTable);
            var random = new SystemRandomSource(_seed);
            var ledger = new ClueKnowledgeLedger();

            Session = StageFactory.Create(config, random, ledger, polarityTable);
            _logger = new StageTurnLogger(Session);

            if (_crtEffectDriver != null)
                _crtEffectDriver.Bind(Session.Heartbeat);

            Session.Runner.StartStage();
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
    }
}
