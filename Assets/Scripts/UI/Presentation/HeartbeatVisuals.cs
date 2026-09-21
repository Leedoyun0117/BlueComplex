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

        /// <summary>모니터(숫자·상태 배지·심전도 선)의 색. 안정은 목업의 청록이고, 침체는 그보다 푸른 쪽, 흥분은 주황 쪽으로 갈라진다 —
        /// 청록과 침체가 헷갈리지 않게 침체 계열은 청록에서 확실히 떨어진 청보라로 잡는다.</summary>
        public static Color TextColor(HeartbeatState state) => state switch
        {
            HeartbeatState.Fatal => new Color32(255, 95, 95, 255),
            HeartbeatState.VeryDepressed => new Color32(132, 150, 255, 255),
            HeartbeatState.Depressed => new Color32(150, 186, 255, 255),
            HeartbeatState.Stable => new Color32(72, 222, 234, 255),
            HeartbeatState.Excited => new Color32(255, 196, 120, 255),
            HeartbeatState.VeryExcited => new Color32(255, 140, 96, 255),
            _ => Color.white
        };
    }
}
