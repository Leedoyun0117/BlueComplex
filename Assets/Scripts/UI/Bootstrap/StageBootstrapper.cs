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

        [Header("스테이지 (임시 디버그 선택)")]
        [Tooltip("시작할 스테이지 번호(1 또는 2). Play 중에는 F1/F2 키로 해당 스테이지를 새로 시작한다.")]
        [SerializeField, Range(1, 2)] private int _stageNumber = 1;

        [Header("시드")]
        [SerializeField] private int _seed = 20260916;

        /// <summary>플레이할 수 있는 마지막 스테이지 번호. 이보다 큰 스테이지는 없다 — 클리어 뒤 이어질 다음 스테이지가 있는지 가리는 기준이다.</summary>
        public const int LastStageNumber = 2;

        public StageSession Session { get; private set; }

        /// <summary>지금 돌고 있는(또는 마지막으로 시작한) 스테이지 번호.</summary>
        public int StageNumber => _stageNumber;

        /// <summary>클리어하면 이어서 시작할 다음 스테이지가 있는가. 튜토리얼은 스테이지 번호 흐름 밖이라 없다.</summary>
        public bool HasNextStage => !_isTutorial && _stageNumber < LastStageNumber;

        /// <summary>지금 돌고 있는 것이 튜토리얼인가(<see cref="TutorialContent.StageId"/>). 스테이지 번호(<see cref="StageNumber"/>)는 마지막으로 시작한 본편 스테이지 그대로다.</summary>
        public bool IsTutorial => _isTutorial;

        /// <summary>튜토리얼을 클리어한 뒤 종료 연출이 끝나면 한 번 알린다. 본편 시작으로 넘어가는 연결은 이 이벤트를 구독하는 쪽이 정한다(구독이 없으면 결과 패널만 남는다).</summary>
        public event Action TutorialCompleted;

        /// <summary>튜토리얼 종료 연출이 끝났음을 알린다(<see cref="StageEndController"/>가 부른다).</summary>
        public void NotifyTutorialCompleted() => TutorialCompleted?.Invoke();

        /// <summary>지금 돌고 있는 스테이지의 저작 설정 — 스테이지 이름 표시 같은 UI가 읽는다. 세션이 바뀌면 함께 바뀐다.</summary>
        public StageConfig Config { get; private set; }
        public int CurrentSeed { get; private set; }

        /// <summary>전체 화면 오버레이 같은 UI가 게임 입력을 잠글 때 켠다 — 그동안 디버그 숫자키가 카드를 내지 않는다.
        /// (마우스 입력은 오버레이가 레이캐스트로 막는다.)</summary>
        public bool InputBlocked { get; set; }

        /// <summary>새 StageSession이 만들어질 때마다(최초 시작 포함) 알린다. Ledger는 세션 간에 계속 유지된다.</summary>
        public event Action<StageSession> SessionStarted;

        private ClueKnowledgeLedger _ledger;

        /// <summary>튜토리얼 전용 해금 장부 — 튜토리얼을 시작할 때마다 새로 만든다. 본편 장부(<see cref="_ledger"/>)와 완전히 분리되어, 튜토리얼에서 밝혀진 단서 속성이 본편에 새어 들지 않는다.</summary>
        private ClueKnowledgeLedger _tutorialLedger;

        private bool _isTutorial;
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
            if (!_enableKeyboardInput || Session == null || Keyboard.current == null) return;

            // 스테이지 선택은 대화 재생 중(InputBlocked)에도 통한다 — BeginNewSession이 재생 중인 대화를 끊는다.
            if (Keyboard.current.f1Key.wasPressedThisFrame) StartStage(1);
            else if (Keyboard.current.f2Key.wasPressedThisFrame) StartStage(2);
            else if (Keyboard.current.f4Key.wasPressedThisFrame) StartTutorial();

            if (InputBlocked) return;

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

            var card = Session.Hand.Cards[index];
            var verdict = Session.CheckPlay(card);
            if (!verdict.Allowed)
            {
                PlayGateFeedback.Show(FindCanvasRoot(), verdict, Session.Runner.CurrentTurn);
                return;
            }

            Session.Runner.PlayClue(card);
        }

        /// <summary>스테이지 번호(1 또는 2)를 골라 새 무작위 시드로 시작한다. 해금 지식(Ledger)은 유지된다. 임시 디버그 선택용.</summary>
        public void StartStage(int stageNumber)
        {
            if (stageNumber < 1 || stageNumber > LastStageNumber)
            {
                Debug.LogWarning($"스테이지 {stageNumber}는 없습니다. (1~{LastStageNumber})");
                return;
            }

            _isTutorial = false;
            _stageNumber = stageNumber;
            BeginNewSession(Environment.TickCount);
        }

        /// <summary>튜토리얼을 시작한다(새 시드, 새 튜토리얼 장부). 스테이지 번호 흐름과 무관하다 — 클리어해도 다음 스테이지로 이어지지 않는다(<see cref="TutorialCompleted"/>).
        /// 시작 컷신은 <see cref="StageFlowHooks.PlayTutorialIntro"/> 훅으로 걸린다. 임시 디버그 시작은 F4.</summary>
        public void StartTutorial()
        {
            _isTutorial = true;
            BeginNewSession(Environment.TickCount);
        }

        [ContextMenu("Start Tutorial")]
        private void StartTutorialFromInspector() => StartTutorial();

        /// <summary>다음 스테이지를 새 무작위 시드로 시작한다(스테이지 클리어 연출이 컷신 뒤에 부른다). 다음 스테이지가 없으면 아무 일도 안 한다.</summary>
        public void StartNextStage()
        {
            if (HasNextStage) StartStage(_stageNumber + 1);
        }

        [ContextMenu("Start Stage 1")]
        private void StartStage1FromInspector() => StartStage(1);

        [ContextMenu("Start Stage 2")]
        private void StartStage2FromInspector() => StartStage(2);

        private StageConfig CreateConfig() => _stageNumber == 2
            ? Stage2Content.Stage2(_polarityTable)
            : PrototypeContent.PrototypeStage(_polarityTable);

        /// <summary>같은 시드로 스테이지를 재시작한다. 해금 지식(Ledger)은 그대로 유지된다.</summary>
        public void RestartWithSameSeed() => BeginNewSession(CurrentSeed);

        /// <summary>새 무작위 시드로 스테이지를 재시작한다. 해금 지식(Ledger)은 그대로 유지된다.</summary>
        public void RestartWithNewSeed() => BeginNewSession(Environment.TickCount);

        private void BeginNewSession(int seed)
        {
            _logger?.Dispose();

            // 끝나지 않은 채 버려지는 판(F1/F2 등 StageEnded를 안 거친 재시작)의 미확정 관찰은 다음 판에 딸려 가지 않게 버린다.
            // 결과 패널 경로는 StageEnded에서 이미 CommitRun했으므로 여기서 비는 게 정상이다.
            _ledger.DiscardPending();
            _tutorialLedger?.DiscardPending();

            // 재시작이 대화 재생 도중이면 그 코루틴을 끊고 막을 치운다 — InputBlocked도 켜진 채로 남지 않게.
            StopAllCoroutines();
            InputBlocked = false;
            _pendingQuarterDialogue.Clear();
            _quarterDialoguePlaying = false;

            CurrentSeed = seed;
            var random = new SystemRandomSource(seed);

            // 튜토리얼은 본편과 다른 장부·다른 조립 경로(스크립트 손패·고정 키 구역·정해진 컴플렉스)를 쓴다. 그 뒤의 흐름(뷰 연결, 시작 대화, 쿼터 대화)은 똑같다.
            if (_isTutorial)
            {
                _tutorialLedger = new ClueKnowledgeLedger();
                Session = TutorialContent.CreateSession(_polarityTable, random, _tutorialLedger);
            }
            else
            {
                Session = StageFactory.Create(CreateConfig(), random, _ledger, _polarityTable);
            }

            var config = Session.Config;
            Config = config;
            _logger = new StageTurnLogger(Session);

            if (_crtEffectDriver != null)
                _crtEffectDriver.Bind(Session.Heartbeat);

            if (_lampLightDriver != null)
                _lampLightDriver.Bind(Session.Heartbeat);

            var canvasRoot = FindCanvasRoot();
            StageDialoguePlayer.GetOrCreate(canvasRoot)?.ResetNow();

            // 튜토리얼 가이드(청장의 말풍선·강조·클릭 막)는 튜토리얼 세션에만 붙는다 — 첫 턴이 시작될 때(시작 대화 뒤) 안내가 시작된다. 다른 세션이면 남은 것을 치운다.
            if (_isTutorial) TutorialGuide.GetOrCreate(canvasRoot)?.Begin(Session);
            else TutorialGuide.Find(canvasRoot)?.ResetNow();
            BranchSceneDirector.GetOrCreate(canvasRoot)?.ResetNow(); // 분기 대사 장면 도중이었으면 게임 UI를 되돌린다.

            // 상시 배경음은 스테이지(재시작 포함)가 시작될 때 처음부터 — 시작 대화 재생 중에도 이미 깔려 있다.
            var ambient = StageSounds.For(config).Ambient;
            if (ambient.HasValue) UiSoundHooks.StartAmbient(ambient.Value);
            else UiSoundHooks.StopAmbient();

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
            // 튜토리얼의 시작 컷신 훅(컷신 담당 영역이 채운다). 걸려 있지 않으면 건너뛴다.
            if (_isTutorial)
            {
                var cutscene = StageFlowHooks.PlayTutorialIntro?.Invoke();
                if (cutscene != null)
                {
                    InputBlocked = true;
                    yield return cutscene;
                    InputBlocked = false;
                }
            }

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

            // 분기 대사 장면: 게임 UI가 빠지고 배경과 대사만 남는다(스테이지 시작·클리어 대화는 그대로 어두운 막 위에서 한다).
            InputBlocked = true;
            yield return BranchSceneDirector.GetOrCreate(canvasRoot).Play(variant.Value);
            InputBlocked = false;
        }

        /// <summary>StageBootstrapper가 MainHud 캔버스의 자식이라는 보장이 없어(씬 배치에 따라 다르다) 대화 오버레이를 지을 캔버스를 직접 찾는다.
        /// 대사 오버레이처럼 자기 캔버스를 가진 중첩 캔버스가 먼저 잡히지 않게, 부모 쪽에 다른 캔버스가 없는 최상위 캔버스만 고른다
        /// (꺼져 있는 중첩 캔버스는 rootCanvas가 자기 자신을 돌려주므로 그걸 믿지 않는다).</summary>
        private static Transform FindCanvasRoot()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                var parent = canvas.transform.parent;
                if (parent == null || parent.GetComponentInParent<Canvas>(true) == null) return canvas.transform;
            }

            return null;
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
