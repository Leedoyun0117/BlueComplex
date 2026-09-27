using System.Collections;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 종료 연출을 코어에 연결하고 순서를 정한다. StageEnded는 1회성 이벤트라 Presenter 대상이
    /// 아니다(Assets/Scripts/UI/Debug/DebugStageEndView.cs와 동일한 패턴) — 직접 구독한다.
    ///
    /// 순서("연출 목록 — 스테이지 클리어 연출"): 마지막 턴 연출이 끝나면 어두운 화면에 자물쇠가 나타나 키 수만큼 열린다(<see cref="StageClearDirector"/>) →
    /// 전부 열렸으면 클리어 대사가 나오며 화면이 밝아지고 → 컷신(<see cref="StageFlowHooks.PlayCutscene"/>, 내용은 이 클래스가 모른다) → 화면이 어두워지고 → 다음 스테이지가 시작되며 밝아진다.
    /// 다음 스테이지가 없으면 컷신 뒤 결과 패널. 못 열었으면 자물쇠도 어두워지며 완전한 암전 → 시작 화면 복귀(<see cref="StageFlowHooks.ReturnToStart"/>, 연결이 없으면 결과 패널).
    /// 컷신은 번호 순서대로 시퀀서(<see cref="CutsceneSequencer"/>, 번호·순서는 <see cref="StageCutscenePlan"/>)가 훅을 부르며 재생한다 — 스테이지 1·2는 성공/실패와 무관하게 재생하고,
    /// 마지막 스테이지(3)는 클리어하면 컷신 → 엔딩 시퀀스, 실패하면 컷신 없이 암전 → 같은 스테이지 재시도.
    /// 어느 경로로 갈지는 코어의 <see cref="StageEndPlan"/>이 정한다.
    /// </summary>
    public sealed class StageEndController : SessionBoundView
    {
        [SerializeField] private StageEndPanel _panel;

        /// <summary>이 컨트롤러가 다음 스테이지를 시작하는 동안 true — 그때 오는 Render(새 세션 알림)는 이 연출을 끊으면 안 된다.</summary>
        private bool _advancing;

        protected override void Awake()
        {
            _panel.RestartSameSeedButton.onClick.AddListener(() =>
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                Bootstrapper.RestartWithSameSeed();
            });
            _panel.RestartNewSeedButton.onClick.AddListener(() =>
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                Bootstrapper.RestartWithNewSeed();
            });
            base.Awake();
        }

        protected override void Subscribe(StageSession session) => session.Runner.StageEnded += OnStageEnded;
        protected override void Unsubscribe(StageSession session) => session.Runner.StageEnded -= OnStageEnded;
        protected override void Render()
        {
            _panel.Hide();
            if (_advancing) return; // 이 연출이 스스로 다음 스테이지를 시작한 것 — 계속 이어 간다.

            // 재시작이 종료 연출 도중이면(디버그 경로로만 가능) 그 코루틴을 끊는다 — 옛 세션 결과로 패널이 뒤늦게 뜨지 않게. 막·자물쇠도 치운다.
            StopAllCoroutines();
            transform.root.GetComponentInChildren<StageClearDirector>(true)?.ResetNow();
        }

        private void OnStageEnded(StageOutcome outcome)
        {
            // 판정 결과(태그·컴플렉스 효과·해금)는 이 순간 그대로 확정한다 — 마지막 턴의 연출이 아직 도는 중이어도 바뀌지 않는다.
            // Ledger.CommitRun()이 있어야 다음 런에 해금이 반영된다. Ledger는 재시작해도
            // StageBootstrapper가 계속 재사용하므로 여기서 새로 만들거나 참조를 바꾸지 않는다.
            Session.Ledger.CommitRun();

            UiSoundHooks.StopAmbient(); // 상시 배경음은 스테이지가 끝나면 페이드아웃. 재시작하면 처음부터 다시 시작한다.

            // StageEnded는 TurnResolved 직후 같은 호출 안에서 오므로 마지막 턴의 결과 연출(Presenter)은 이제 막 시작한 참이다 —
            // 그 연출이 다 끝난 뒤에야 종료 연출/결과 패널(=재시작 버튼)을 띄운다. 안 그러면 연출 도중 재시작이 가능해진다.
            StartCoroutine(RunEnding(outcome));
        }

        private IEnumerator RunEnding(StageOutcome outcome)
        {
            var presenter = transform.root.GetComponentInChildren<ITurnResultPresenter>(true);
            if (presenter != null) yield return new WaitUntil(() => !presenter.IsPresenting);

            // 튜토리얼은 자물쇠 연출·본편 클리어 대사·다음 스테이지 흐름을 타지 않는다 — 간단한 완료 메시지만.
            if (Bootstrapper.IsTutorial)
            {
                yield return PlayTutorialEnding(outcome);
                yield break;
            }

            // 자물쇠 그림이 없으면(카탈로그 미빌드) 연출 없이 예전처럼 대사 → 패널로 간다 — 종료 화면이 통째로 비면 안 된다.
            if (!StageLockView.Available)
            {
                yield return PlayLegacyEnding(outcome);
                yield break;
            }

            var plan = StageEndPlan.Create(outcome, Session.Keys.Required, Session.Keys.Collected, Bootstrapper.HasNextStage, Bootstrapper.IsFinalStage);
            var director = StageClearDirector.GetOrCreate(transform.root);

            Bootstrapper.InputBlocked = true;
            yield return director.PlayLocks(plan);

            if (plan.Route == StageEndRoute.FailToStart || plan.Route == StageEndRoute.FailRetry)
                yield return PlayFailure(director, plan, outcome);
            else
                yield return PlayClear(director, plan, outcome);

            // 다음 스테이지·재시도가 시작됐다면 입력 잠금은 그 세션(시작 대화)이 이미 새로 정했다 — 여기서 풀면 대화 도중 입력이 열린다.
            if (plan.Route != StageEndRoute.ClearToNextStage && plan.Route != StageEndRoute.FailRetry) Bootstrapper.InputBlocked = false;
        }

        /// <summary>클리어: 대사(화면이 밝아짐) → 컷신 → (다음 스테이지가 있으면) 어두워짐 → 다음 스테이지 시작 + 밝아짐 / (마지막 스테이지면) 엔딩 시퀀스 → 결과 패널 / (없으면) 결과 패널.</summary>
        private IEnumerator PlayClear(StageClearDirector director, StageEndPlan plan, StageOutcome outcome)
        {
            var stageId = Bootstrapper.Config.Id;
            var mood = StageDialogueMoodClassifier.Classify(Session.Zone, Session.Heartbeat.Value);
            yield return director.PlayClearDialogue(StageDialogues.PickStageClear(stageId, mood));

            // 컷신은 이 클래스가 내용을 모른다 — 번호 순서대로 훅을 불러 끝날 때까지 기다리기만 한다(내용이 없는 번호는 건너뜀).
            yield return PlayCutscenes(outcome);

            if (plan.Route == StageEndRoute.ClearToEnding)
            {
                yield return Bootstrapper.PlayEndingSequence();
                _panel.Show(KoreanLabels.Outcome(outcome));
                yield break;
            }

            if (plan.Route != StageEndRoute.ClearToNextStage)
            {
                _panel.Show(KoreanLabels.Outcome(outcome));
                yield break;
            }

            yield return director.FadeToBlack();

            // 새 세션이 시작되면 뷰들이 Render로 막을 치우므로(재시작 처리) 시작 직후 같은 프레임에 어둠을 되살리고 밝아진다 — 화면은 한 프레임도 안 밝아진다.
            _advancing = true;
            Bootstrapper.StartNextStage();
            _advancing = false;
            director.SnapBlack();
            director.FadeFromBlack();
        }

        /// <summary>실패: 자물쇠도 어두워지며 완전한 암전 → (컷신이 있으면 밝아져 재생 → 다시 암전) → 시작 화면 복귀(훅, 없으면 결과 패널) / 마지막 스테이지면 같은 스테이지를 새 시드로 재시도.</summary>
        private IEnumerator PlayFailure(StageClearDirector director, StageEndPlan plan, StageOutcome outcome)
        {
            yield return director.PlayBlackout();

            // 컷신 훅은 밝은 화면에서 시작해 밝은 화면으로 끝난다 — 암전 상태에서 걷어 올렸다가 끝나면 다시 어둡게 한다. 재생할 컷신이 없으면 화면은 그대로 어둡다.
            var steps = StageCutscenePlan.For(Bootstrapper.StageNumber, outcome);
            if (steps.Count > 0 && StageFlowHooks.PlayCutscene != null)
            {
                yield return director.FadeFromBlack().WaitForCompletion(true);
                yield return PlayCutscenes(outcome);
                yield return director.FadeToBlack();
            }

            if (plan.Route == StageEndRoute.FailRetry)
            {
                // 다음 스테이지 진입과 같은 방식: 새 세션이 막을 치우므로 같은 프레임에 어둠을 되살리고 밝아진다.
                _advancing = true;
                Bootstrapper.RestartWithNewSeed();
                _advancing = false;
                director.SnapBlack();
                director.FadeFromBlack();
                yield break;
            }

            var returnToStart = StageFlowHooks.ReturnToStart?.Invoke();
            if (returnToStart != null)
            {
                yield return returnToStart;
                yield break;
            }

            _panel.Show(KoreanLabels.Outcome(outcome));
            director.FadeFromBlack();
        }

        /// <summary>지금 스테이지 종료에 정해진 컷신을 번호 순서대로 재생한다. 호출·건너뜀은 로그로 남는다.</summary>
        private IEnumerator PlayCutscenes(StageOutcome outcome) =>
            CutsceneSequencer.Play(StageCutscenePlan.For(Bootstrapper.StageNumber, outcome), number => StageFlowHooks.PlayCutscene?.Invoke(number), Debug.Log);

        /// <summary>튜토리얼 클리어 뒤 청장의 마지막 대사(노션 원문).</summary>
        private static readonly DialogueVariant TutorialClearMessage = new()
        {
            lines = new[] { new DialogueLine { speaker = DialogueSpeaker.Chief, text = TutorialGuideContent.ClearLine } }
        };

        /// <summary>튜토리얼 종료: 클리어면 완료 메시지 → 완료 알림 → 결과 패널, 실패면 바로 결과 패널(다시 시작).</summary>
        private IEnumerator PlayTutorialEnding(StageOutcome outcome)
        {
            TutorialGuide.Find(transform.root)?.ResetNow(); // 남은 말풍선·강조·클릭 막을 치운다.

            if (outcome == StageOutcome.Cleared)
            {
                Bootstrapper.InputBlocked = true;
                yield return StageDialoguePlayer.GetOrCreate(transform.root).Play(TutorialClearMessage);
                Bootstrapper.InputBlocked = false;
                Bootstrapper.NotifyTutorialCompleted();
            }

            _panel.Show(KoreanLabels.Outcome(outcome));
        }

        /// <summary>자물쇠 연출이 없던 때의 종료: 클리어는 대사 → 패널, 실패는 바로 패널.</summary>
        private IEnumerator PlayLegacyEnding(StageOutcome outcome)
        {
            if (outcome == StageOutcome.Cleared)
            {
                var mood = StageDialogueMoodClassifier.Classify(Session.Zone, Session.Heartbeat.Value);
                var variant = StageDialogues.PickStageClear(Bootstrapper.Config.Id, mood);
                if (variant.HasValue)
                {
                    Bootstrapper.InputBlocked = true;
                    yield return StageDialoguePlayer.GetOrCreate(transform.root).Play(variant.Value);
                    Bootstrapper.InputBlocked = false;
                }
            }

            _panel.Show(KoreanLabels.Outcome(outcome));
        }
    }
}
