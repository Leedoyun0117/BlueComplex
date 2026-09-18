using BlueComplex.Core.Stability;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 심박수 구간별 색상표. 침체(파랑 계열) ↔ 안정(초록, 목표) ↔ 흥분(주황 계열) ↔ 즉사(검붉음, 위험)
    /// 순서로 의미가 읽히게 고정한다. 에디터 셋업 도구(UiLayoutSetupTool, 세그먼트/탭을 굽는다)와
    /// 런타임(HeartRateController, BPM 숫자 색) 양쪽이 같은 표를 쓰기 위해 공유한다.
    /// </summary>
    public static class HeartbeatVisuals
    {
        public static Color SegmentColor(HeartbeatState state) => state switch
        {
            HeartbeatState.Fatal => new Color32(40, 12, 12, 235),
            HeartbeatState.VeryDepressed => new Color32(60, 108, 150, 220),
            HeartbeatState.Depressed => new Color32(96, 150, 176, 200),
            HeartbeatState.Stable => new Color32(70, 190, 112, 220),
            HeartbeatState.Excited => new Color32(210, 150, 80, 200),
            HeartbeatState.VeryExcited => new Color32(214, 100, 55, 220),
            _ => Color.gray
        };

        public static Color TextColor(HeartbeatState state) => state switch
        {
            HeartbeatState.Fatal => new Color32(255, 95, 95, 255),
            HeartbeatState.VeryDepressed => new Color32(150, 200, 230, 255),
            HeartbeatState.Depressed => new Color32(180, 218, 232, 255),
            HeartbeatState.Stable => new Color32(160, 245, 190, 255),
            HeartbeatState.Excited => new Color32(240, 195, 135, 255),
            HeartbeatState.VeryExcited => new Color32(255, 155, 105, 255),
            _ => Color.white
        };
    }
}
