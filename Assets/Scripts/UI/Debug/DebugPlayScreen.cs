using BlueComplex.UI.Bootstrap;
using UnityEngine;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>
    /// 디버그 플레이 화면 루트. uGUI 계층을 코드로 조립하고 각 화면 요소를 StageBootstrapper에 묶는다.
    /// 정식 UI(Assets/Scripts/UI/)와는 완전히 분리되어 있으며, 이 폴더를 통째로 지워도 게임 로직에는 영향이 없다.
    ///
    /// 씬 배선: 빈 GameObject를 하나 만들고 이 컴포넌트를 붙이기만 하면 된다.
    /// StageBootstrapper 참조를 비워두면 씬에서 자동으로 찾는다.
    /// </summary>
    public sealed class DebugPlayScreen : MonoBehaviour
    {
        [SerializeField] private StageBootstrapper _bootstrapper;

        private void Awake()
        {
            if (_bootstrapper == null)
                _bootstrapper = FindFirstObjectByType<StageBootstrapper>();

            if (_bootstrapper == null)
            {
                Debug.LogError("DebugPlayScreen: 씬에서 StageBootstrapper를 찾을 수 없습니다.");
                return;
            }

            Build();
        }

        private void Build()
        {
            var canvas = DebugUIFactory.CreateRootCanvas("DebugPlayCanvas");
            canvas.transform.SetParent(transform, false);
            var canvasRoot = (RectTransform)canvas.transform;

            // 전체를 덮는 배경을 진하게 깔면 CRT 카메라 효과가 그 밑에 가려 보이지 않는다.
            // ScreenSpaceOverlay 캔버스는 항상 카메라 렌더링 위에 그려지므로, 여기서는 살짝만 어둡게 해 가독성만 보탠다.
            var background = DebugUIFactory.CreatePanel(canvasRoot, "Background", new Color(0.02f, 0.02f, 0.03f, 0.28f));
            DebugUIFactory.StretchFull(background);

            var mainLayout = DebugUIFactory.CreateEmpty(canvasRoot, "MainLayout");
            DebugUIFactory.StretchFull(mainLayout);
            DebugUIFactory.AddVerticalLayout(mainLayout.gameObject, spacing: 10, expandWidth: true, expandHeight: true);

            BindRow<DebugHeaderView>(mainLayout, "Header", minHeight: 28);
            BindRow<DebugKeyZoneView>(mainLayout, "KeyZone", minHeight: 60);

            var middleRow = DebugUIFactory.CreateEmpty(mainLayout, "MiddleRow");
            DebugUIFactory.AddHorizontalLayout(middleRow.gameObject, spacing: 10, expandWidth: true, expandHeight: true);
            DebugUIFactory.AddLayoutElement(middleRow.gameObject, flexibleHeight: 1);

            var complexGo = new GameObject("Complexes", typeof(RectTransform));
            complexGo.transform.SetParent(middleRow, false);
            DebugUIFactory.AddLayoutElement(complexGo, minWidth: 260, flexibleHeight: 1);
            complexGo.AddComponent<DebugComplexListView>().Bind(_bootstrapper);

            var logGo = new GameObject("Log", typeof(RectTransform));
            logGo.transform.SetParent(middleRow, false);
            DebugUIFactory.AddLayoutElement(logGo, flexibleWidth: 1, flexibleHeight: 1);
            logGo.AddComponent<DebugLogView>().Bind(_bootstrapper);

            BindRow<DebugHandView>(mainLayout, "Hand", minHeight: 150);
            BindRow<DebugItemView>(mainLayout, "Items", minHeight: 80);

            var endGo = new GameObject("StageEnd", typeof(RectTransform));
            endGo.transform.SetParent(canvasRoot, false);
            DebugUIFactory.StretchFull((RectTransform)endGo.transform);
            endGo.AddComponent<DebugStageEndView>().Bind(_bootstrapper);
        }

        private void BindRow<T>(Transform parent, string name, float minHeight) where T : DebugSessionView
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            DebugUIFactory.AddLayoutElement(go, minHeight: minHeight);
            go.AddComponent<T>().Bind(_bootstrapper);
        }
    }
}
