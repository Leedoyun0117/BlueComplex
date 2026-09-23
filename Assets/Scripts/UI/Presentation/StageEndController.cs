using System.Collections;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 종료 오버레이를 코어에 연결한다. StageEnded는 1회성 이벤트라 Presenter 대상이
    /// 아니다(Assets/Scripts/UI/Debug/DebugStageEndView.cs와 동일한 패턴) — 직접 구독한다.
    /// </summary>
    public sealed class StageEndController : SessionBoundView
    {
        [SerializeField] private StageEndPanel _panel;

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
            // 재시작이 클리어 대사 재생 도중이면(디버그 경로로만 가능) 그 코루틴을 끊는다 — 옛 세션 결과로 패널이 뒤늦게 뜨지 않게.
            StopAllCoroutines();
            _panel.Hide();
        }

        private void OnStageEnded(StageOutcome outcome)
        {
            // Ledger.CommitRun()이 있어야 다음 런에 해금이 반영된다. Ledger는 재시작해도
            // StageBootstrapper가 계속 재사용하므로 여기서 새로 만들거나 참조를 바꾸지 않는다.
            Session.Ledger.CommitRun();

            // "스테이지 시작 대화" 기획: 클리어 대사는 최종 심박수 구간에 따라 갈리고, 결과 패널이 뜨기 전에 먼저 나온다.
            // 실패는 클리어 대사가 없으므로(기획표에 없음) 바로 보여준다.
            if (outcome == StageOutcome.Cleared) StartCoroutine(PlayClearDialogueThenShow(outcome));
            else _panel.Show(KoreanLabels.Outcome(outcome));
        }

        private IEnumerator PlayClearDialogueThenShow(StageOutcome outcome)
        {
            var mood = StageDialogueMoodClassifier.Classify(Session.Zone, Session.Heartbeat.Value);
            var variant = StageDialogues.PickStageClear(Bootstrapper.Config.Id, mood);
            if (variant.HasValue)
            {
                Bootstrapper.InputBlocked = true;
                yield return StageDialoguePlayer.GetOrCreate(transform.root).Play(variant.Value);
                Bootstrapper.InputBlocked = false;
            }

            _panel.Show(KoreanLabels.Outcome(outcome));
        }
    }
}
