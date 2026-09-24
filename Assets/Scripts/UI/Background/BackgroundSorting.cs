using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 배경 리그 안에 끼워 넣는 파티클(컵 김, 램프 연기)의 그리기 순서.
    /// 배경 레이어 quad는 <c>sortingOrder</c>(리그의 레이어 순번)로 순서가 정해지고, 그게 머티리얼 렌더 큐보다 우선한다.
    /// 파티클 렌더러를 0인 채로 두면 모든 레이어(순번 1~)보다 먼저 그려져서 통째로 가려진다.
    /// </summary>
    internal static class BackgroundSorting
    {
        /// <summary>BackgroundSetupTool의 기본 레이어 순서에서 Things의 순번(BG0 Window1 Clock2 Min3 Hour4 Lamp5 Things6 인물7·8 그림자9).</summary>
        private const int DefaultThingsOrder = 6;

        /// <summary>Things 레이어 quad의 sortingOrder. 같은 값이면 같은 큐 안에서 카메라에 더 가까운 쪽이 Things 위에 얹히고,
        /// 그보다 큰 순번의 인물·그림자 오버레이보다는 뒤에 그려진다. 리그를 못 찾으면 기본 순번.</summary>
        public static int ThingsLayerOrder(Component from)
        {
            var rig = from.GetComponentInParent<BackgroundLayerRig>();
            if (rig == null) return DefaultThingsOrder;

            var things = rig.transform.Find("Layers/Things/Quad") ?? rig.transform.Find("Things/Quad");
            var thingsRenderer = things != null ? things.GetComponent<Renderer>() : null;
            return thingsRenderer != null ? thingsRenderer.sortingOrder : DefaultThingsOrder;
        }
    }
}
