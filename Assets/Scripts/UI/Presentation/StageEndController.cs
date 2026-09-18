using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Layout;
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
            _panel.RestartSameSeedButton.onClick.AddListener(() => Bootstrapper.RestartWithSameSeed());
            _panel.RestartNewSeedButton.onClick.AddListener(() => Bootstrapper.RestartWithNewSeed());
            base.Awake();
        }

        protected override void Subscribe(StageSession session) => session.Runner.StageEnded += OnStageEnded;
        protected override void Unsubscribe(StageSession session) => session.Runner.StageEnded -= OnStageEnded;
        protected override void Render() => _panel.Hide();

        private void OnStageEnded(StageOutcome outcome)
        {
            // Ledger.CommitRun()이 있어야 다음 런에 해금이 반영된다. Ledger는 재시작해도
            // StageBootstrapper가 계속 재사용하므로 여기서 새로 만들거나 참조를 바꾸지 않는다.
            Session.Ledger.CommitRun();
            _panel.Show(KoreanLabels.Outcome(outcome));
        }
    }
}
