using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>
    /// 스테이지 종료(Cleared/Failed) 표시와 재시작 버튼.
    /// 종료 시 ClueKnowledgeLedger.CommitRun()을 호출해 해금을 확정한다.
    /// 이 컴포넌트 자신은 항상 활성 상태를 유지하고, 자식 오버레이 패널만 켜고 끈다
    /// (자신을 비활성화하면 OnDisable에서 이벤트 구독이 풀려버리기 때문).
    /// </summary>
    internal sealed class DebugStageEndView : DebugSessionView
    {
        private GameObject _overlay;
        private Text _resultText;

        protected override void BuildUI()
        {
            var overlayRt = DebugUIFactory.CreatePanel(transform, "Overlay", new Color(0f, 0f, 0f, 0.75f));
            DebugUIFactory.StretchFull(overlayRt);
            _overlay = overlayRt.gameObject;

            var box = DebugUIFactory.CreatePanel(overlayRt, "Box", new Color(0.15f, 0.15f, 0.2f));
            box.anchorMin = new Vector2(0.5f, 0.5f);
            box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(440, 220);
            DebugUIFactory.AddVerticalLayout(box.gameObject, spacing: 16, expandWidth: true, expandHeight: true);

            _resultText = DebugUIFactory.CreateText(box, "Result", 26, TextAnchor.MiddleCenter);
            DebugUIFactory.AddLayoutElement(_resultText.gameObject, minHeight: 60, flexibleHeight: 1);

            var buttonRow = DebugUIFactory.CreateEmpty(box, "Buttons");
            DebugUIFactory.AddHorizontalLayout(buttonRow.gameObject, spacing: 12, expandWidth: true);
            DebugUIFactory.AddLayoutElement(buttonRow.gameObject, minHeight: 48);

            var sameSeedButton = DebugUIFactory.CreateButton(buttonRow, "SameSeed", out var sameLabel);
            sameLabel.text = "같은 시드로 재시작";
            sameLabel.alignment = TextAnchor.MiddleCenter;
            DebugUIFactory.AddLayoutElement(sameSeedButton.gameObject, minWidth: 180, flexibleWidth: 1);
            sameSeedButton.onClick.AddListener(() => Bootstrapper.RestartWithSameSeed());

            var newSeedButton = DebugUIFactory.CreateButton(buttonRow, "NewSeed", out var newLabel);
            newLabel.text = "새 시드로 재시작";
            newLabel.alignment = TextAnchor.MiddleCenter;
            DebugUIFactory.AddLayoutElement(newSeedButton.gameObject, minWidth: 180, flexibleWidth: 1);
            newSeedButton.onClick.AddListener(() => Bootstrapper.RestartWithNewSeed());

            _overlay.SetActive(false);
        }

        protected override void SubscribeSession(StageSession session)
        {
            session.Runner.StageEnded += OnStageEnded;
        }

        protected override void UnsubscribeSession(StageSession session)
        {
            session.Runner.StageEnded -= OnStageEnded;
        }

        private void OnStageEnded(StageOutcome outcome)
        {
            Session.Ledger.CommitRun();

            _resultText.text = DebugKoreanLabels.Outcome(outcome);
            _overlay.SetActive(true);
        }

        protected override void Render()
        {
            _overlay.SetActive(false);
        }
    }
}
