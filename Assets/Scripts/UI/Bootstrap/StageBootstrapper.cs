using System;
using System.Collections;
using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Background;
using BlueComplex.UI.Debugging;
using BlueComplex.UI.Motion;
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

        /// <summary>쿼터가 끝나 재생을 기다리는 결과들. 한 번의 PlayClue 호출 안에서 TurnResolved가 연달아 올 수 있어
        /// (CinematicTurnResultPresenter 문서 참고) 큐에 쌓아 순서대로 재생한다.</summary>
        private readonly Queue<TurnReport> _pendingQuarterDialogue = new();
        private bool _quarterDialoguePlaying;

        /// <summary>이 세션(앱 실행)에서 이미 들어온 적 있는 스테이지 id — "처음" 시작 대사는 스테이지별로 딱 한 번만 나온다.
        /// 재시작(RestartWithSameSeed/NewSeed)은 여기서 지우지 않는다: 같은 스테이지를 다시 들어오는 것도 "재진입"이다.</summary>
        private readonly HashSet<string> _stagesEnteredBefore = new();

        private void Start()
        {
            _polarityTable = new DefaultEmotionPolarityTable();
            _ledger = new ClueKnowledgeLedger();

            BeginNewSession(_seed);
        }

        private void OnDestroy()
        {
            _logger?.Dispose();
            UiSoundHooks.StopAmbient();
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

            // 재시작이 대화 재생 도중이면 그 코루틴을 끊고 막을 치운다 — InputBlocked도 켜진 채로 남지 않게.
            StopAllCoroutines();
            InputBlocked = false;
            _pendingQuarterDialogue.Clear();
            _quarterDialoguePlaying = false;

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

            var canvasRoot = FindCanvasRoot();
            StageDialoguePlayer.GetOrCreate(canvasRoot)?.ResetNow();

            // 상시 배경음은 스테이지(재시작 포함)가 시작될 때 처음부터 — 시작 대화 재생 중에도 이미 깔려 있다.
            UiSoundHooks.StartAmbient(UiSoundCue.AmbientNotes);

            // StartStage()가 첫 TurnBegan을 곧바로 쏘아 올리므로, 구독자는 그 전에 새 세션을 받아야 한다.
            RaiseSessionStarted();

            // 뷰들(CinematicTurnResultPresenter 포함)이 방금 RaiseSessionStarted에서 TurnResolved를 구독했다 —
            // 우리 구독은 그 뒤에 걸어야, 쿼터 마지막 턴이 끝날 때 턴 결과 연출(Present)이 먼저 시작되고 나서
            // 우리 핸들러가 불린다(그래야 IsPresenting이 그새 true가 되어 아래에서 정확히 기다릴 수 있다).
            Session.Runner.TurnResolved += OnTurnResolvedForQuarterDialogue;

            // "처음" 시작 대사는 이 스테이지에 이 세션에서 정말 처음 들어올 때만 — 그 뒤로는(재시작 포함) "그 후" 풀에서 무작위로 고른다.
            var isFirstEntry = !_stagesEnteredBefore.Contains(config.Id);
            _stagesEnteredBefore.Add(config.Id);

            // 스테이지 시작 대화가 끝나야 StartStage()를 부른다 — 그 전엔 손패가 비어 있어 카드가 없다.
            StartCoroutine(BeginStageAfterIntro(canvasRoot, config.Id, isFirstEntry));
        }

        /// <summary>스테이지 시작 대화(있으면) 재생 → 입력 잠금 해제 → StartStage(). 대화가 없으면 그대로 바로 시작한다.</summary>
        private IEnumerator BeginStageAfterIntro(Transform canvasRoot, string stageId, bool isFirstEntry)
        {
            var variant = StageDialogues.PickStageStart(stageId, isFirstEntry);
            if (variant.HasValue && canvasRoot != null)
            {
                InputBlocked = true;
                yield return StageDialoguePlayer.GetOrCreate(canvasRoot).Play(variant.Value);
                InputBlocked = false;
            }

            Session.Runner.StartStage();
        }

        /// <summary>쿼터의 마지막 턴이 끝났으면(스테이지가 그대로 계속되는 경우만) 대화 재생 큐에 넣는다.
        /// 스테이지가 같은 턴에 끝났으면(Cleared/Failed) 여기서는 재생하지 않는다 — 클리어 대사는 StageEndController가 맡는다.</summary>
        private void OnTurnResolvedForQuarterDialogue(TurnReport report)
        {
            if (report.Outcome != StageOutcome.InProgress) return;
            if (Session == null || !Session.Runner.Schedule.IsQuarterEnd(report.Turn)) return;

            _pendingQuarterDialogue.Enqueue(report);
            if (_quarterDialoguePlaying) return;

            _quarterDialoguePlaying = true;
            StartCoroutine(DrainQuarterDialogue());
        }

        private IEnumerator DrainQuarterDialogue()
        {
            while (_pendingQuarterDialogue.Count > 0)
                yield return PlayQuarterEndDialogue(_pendingQuarterDialogue.Dequeue());

            _quarterDialoguePlaying = false;
        }

        /// <summary>이 턴의 결과 연출(CinematicTurnResultPresenter)이 다 끝날 때까지 기다린 뒤, 그 시점 심박수 구간에 맞는 대사를 재생한다.</summary>
        private IEnumerator PlayQuarterEndDialogue(TurnReport report)
        {
            var canvasRoot = FindCanvasRoot();
            if (canvasRoot == null) yield break;

            var presenter = canvasRoot.GetComponentInChildren<ITurnResultPresenter>(true);
            if (presenter != null) yield return new WaitUntil(() => !presenter.IsPresenting);

            var mood = StageDialogueMoodClassifier.Classify(Session.Zone, report.HeartbeatValue);
            var variant = StageDialogues.PickQuarterEnd(Config.Id, mood);
            if (!variant.HasValue) yield break;

            InputBlocked = true;
            yield return StageDialoguePlayer.GetOrCreate(canvasRoot).Play(variant.Value);
            InputBlocked = false;
        }

        /// <summary>StageBootstrapper가 MainHud 캔버스의 자식이라는 보장이 없어(씬 배치에 따라 다르다) 대화 오버레이를 지을 캔버스를 직접 찾는다.</summary>
        private static Transform FindCanvasRoot()
        {
            var canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            return canvas != null ? canvas.transform : null;
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
