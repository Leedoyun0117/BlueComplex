using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Esc
{
    /// <summary>
    /// 설정창의 "메인 메뉴로 나가기" 줄과 확인 팝업. 볼륨/밝기 슬라이더처럼 프리팹에 심지 않고 런타임에 짓는다 —
    /// <see cref="LSO_EscSilhouette"/>와 같은 이유(프리팹을 손으로 고치다 오브젝트가 날아간 적이 있다, LSO_EscBackdrop 문서 참고).
    /// <see cref="LSO_EscPanel.Start"/>에서 <see cref="Ensure"/>로 한 번 짓는다.
    ///
    /// 눌리면 확인 팝업(딤 + 문구 + 나가기/취소)을 띄운다. "나가기"를 누르면 창을 닫고
    /// <see cref="StageBootstrapper.ExitToMainMenu"/>를 불러 진행 중이던 스테이지/튜토리얼/컷신/대사를 정리하고 메인 화면으로 돌아간다.
    /// 팝업은 창이 닫히면(<see cref="LSO_EscPanel.Closed"/>) 함께 숨는다 — ESC로 창째로 닫아도 팝업만 남지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_EscExitButton : MonoBehaviour
    {
        private const string RowName = "ExitToMenu";
        private const float RowHeight = 44f;

        /// <summary>다른 설정 줄(Master/Bgm/Sfx/Brightness)과 같은 폭 — 그 줄들은 늘어나는 앵커가 아니라
        /// 점 앵커 + 고정 sizeDelta로 폭을 가진다(VerticalLayoutGroup이 폭은 건드리지 않는다). 늘어나는 앵커를 쓰면 폭이 0으로 읽힌다.</summary>
        private const float RowWidth = 320f;

        private LSO_EscPanel _panel;
        private RectTransform _confirmRoot;
        private TMP_FontAsset _font;

        /// <summary>줄이 아직 없으면(재진입이면 있다) settingsLayout(볼륨·밝기 슬라이더가 쌓이는 곳) 맨 아래에 짓는다.</summary>
        internal static void Ensure(LSO_EscPanel panel)
        {
            if (panel.Content == null) return;

            var settingsLayout = panel.Content.Find("Elements/SettingsLayout");
            if (settingsLayout == null)
            {
                Debug.LogWarning("[LSO_EscExitButton] Elements/SettingsLayout을 찾지 못했다 — 나가기 버튼을 만들지 않는다.", panel);
                return;
            }
            if (settingsLayout.Find(RowName) != null) return;

            // "Elements"(SettingsLayout의 부모)는 라벨-왼쪽/슬라이더-오른쪽인 다른 설정 줄들의 시각적 균형을 위해
            // x로 조금 밀려 있다(anchoredPosition.x). 좌우 대칭인 이 버튼은 그 오프셋을 그대로 물려받으면
            // 화면 중앙이 아니라 오른쪽으로 치우쳐 보이므로, Build에서 그 값만큼 반대로 되돌린다.
            var elementsOffsetX = (settingsLayout.parent as RectTransform)?.anchoredPosition.x ?? 0f;

            var row = new GameObject(RowName, typeof(RectTransform)) { layer = panel.gameObject.layer };
            var rect = (RectTransform)row.transform;
            rect.SetParent(settingsLayout, false);
            // 다른 설정 줄과 같은 앵커/크기 방식(점 앵커 + 고정 sizeDelta) — VerticalLayoutGroup이 세로 자리만 잡아 준다.
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(RowWidth, RowHeight);

            row.AddComponent<LSO_EscExitButton>().Build(panel, rect, elementsOffsetX);
        }

        /// <summary>줄(row)은 다른 설정 줄과 같은 폭(320)으로 VerticalLayoutGroup 자리만 차지하고,
        /// 실제 보이는 사각형 버튼은 그 안에 더 좁게 중앙 정렬로 따로 둔다 — SettingsLayout의 정렬이
        /// 왼쪽 기준(UpperLeft)이라 row 폭 그대로 그리면 폭이 좁아지는 순간 왼쪽으로 쏠린다.</summary>
        private const float ButtonWidth = 220f;
        private const float ButtonHeight = 40f;

        private void Build(LSO_EscPanel panel, RectTransform row, float elementsOffsetX)
        {
            _panel = panel;
            _font = RuntimeUi.GameFont;

            var layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = RowHeight;
            layoutElement.minHeight = RowHeight;

            var button = RuntimeUi.CreateRect(row, "Button", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            button.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            button.anchoredPosition = new Vector2(-elementsOffsetX, 0f); // Elements의 x 오프셋을 상쇄해 화면 중앙에 맞춘다.

            var background = RuntimeUi.CreateImage(button, "Background", new Color(1f, 1f, 1f, 0.08f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, raycastTarget: true);
            WireButton(background, HandleClick, new Color(1f, 1f, 1f, 0.18f), new Color(1f, 1f, 1f, 0.28f));

            RuntimeUi.CreateText(button, "Label", "메인 메뉴로 나가기", _font, 22f, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            panel.Closed += HideConfirm;
        }

        private void OnDestroy()
        {
            if (_panel != null) _panel.Closed -= HideConfirm;
        }

        private void HandleClick()
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            ShowConfirm();
        }

        private void ShowConfirm()
        {
            if (_confirmRoot == null) BuildConfirm();
            _confirmRoot.gameObject.SetActive(true);
            _confirmRoot.SetAsLastSibling(); // 다른 설정 줄·실루엣보다 위에 그린다.
        }

        private void HideConfirm()
        {
            if (_confirmRoot != null) _confirmRoot.gameObject.SetActive(false);
        }

        /// <summary>패널 전체(EscPanel)를 덮는 확인 팝업 — 딤 배경 + 문구 + 나가기/취소 두 버튼. 처음 열 때 한 번만 짓는다.</summary>
        private void BuildConfirm()
        {
            _confirmRoot = RuntimeUi.CreateStretched(_panel.Content, "ExitConfirm");
            RuntimeUi.CreateImage(_confirmRoot, "Dim", new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, raycastTarget: true); // 뒤 슬라이더·버튼 클릭을 막는다.

            var box = RuntimeUi.CreateRect(_confirmRoot, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            box.sizeDelta = new Vector2(560f, 220f);
            RuntimeUi.CreateImage(box, "Panel", new Color(0.12f, 0.12f, 0.14f, 0.97f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, raycastTarget: true);

            var message = RuntimeUi.CreateText(box, "Message",
                "진행 중인 내용은 저장되지 않습니다.\n메인 메뉴로 나가시겠습니까?", _font, 24f, Color.white,
                TextAlignmentOptions.Center, new Vector2(0f, 0.4f), new Vector2(1f, 1f));
            message.textWrappingMode = TextWrappingModes.Normal;

            BuildConfirmButton(box, "ConfirmBtn", "나가기", new Vector2(0.1f, 0.1f), new Vector2(0.46f, 0.32f), ConfirmExit);
            BuildConfirmButton(box, "CancelBtn", "취소", new Vector2(0.54f, 0.1f), new Vector2(0.9f, 0.32f), HideConfirm);
        }

        private void BuildConfirmButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            UnityEngine.Events.UnityAction onConfirm)
        {
            var background = RuntimeUi.CreateImage(parent, name, new Color(1f, 1f, 1f, 0.12f), anchorMin, anchorMax,
                Vector2.zero, Vector2.zero, raycastTarget: true);
            WireButton(background, () =>
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                onConfirm();
            }, new Color(1f, 1f, 1f, 0.24f), new Color(1f, 1f, 1f, 0.34f));

            RuntimeUi.CreateText(background.transform, "Label", label, _font, 22f, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        }

        private static void WireButton(Image background, UnityEngine.Events.UnityAction onClick, Color highlighted, Color pressed)
        {
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            button.colors = colors;
            button.onClick.AddListener(onClick);
        }

        /// <summary>"나가기" 확정: 팝업을 닫고 창을 닫은 뒤(<see cref="LSO_EscPanel.Close"/>) 부트스트래퍼에 나가기를 맡긴다.</summary>
        private void ConfirmExit()
        {
            HideConfirm();
            _panel.Close();

            var bootstrapper = FindFirstObjectByType<StageBootstrapper>();
            if (bootstrapper == null)
            {
                Debug.LogWarning("[LSO_EscExitButton] StageBootstrapper를 찾지 못했다 — 메인 화면으로 나갈 수 없다.");
                return;
            }
            bootstrapper.ExitToMainMenu();
        }
    }
}
