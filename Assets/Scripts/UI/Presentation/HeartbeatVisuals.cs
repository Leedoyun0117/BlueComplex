using BlueComplex.Core.Stability;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 심박수 구간별 색상표. 침체(차가운 청보라 계열) ↔ 안정(청록, 중립) ↔ 흥분(따뜻한 주황 계열) ↔ 즉사(붉음, 위험)
    /// 순서로 의미가 읽히게 고정한다. 모니터의 BPM 숫자·상태 배지·파형 선 색이 같은 표를 쓴다.
    /// </summary>
    public static class HeartbeatVisuals
    {
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
