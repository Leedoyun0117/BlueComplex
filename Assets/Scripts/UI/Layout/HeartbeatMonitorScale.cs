using BlueComplex.Core.Stability;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 심박수 모니터의 세로축 — <b>심박수 → 모니터 높이</b> 매핑이 사는 유일한 곳이다.
    /// 파형 봉우리(R파)와 목표 띠의 위아래 경계가 둘 다 <see cref="ToY"/>를 부른다. 매핑이 두 군데 있으면
    /// "띠 안에 들어간 것처럼 보이는데 실패" 같은 불일치가 생기므로, 이 구조체 밖에서 BPM을 높이로 바꾸지 않는다.
    ///
    /// 선형이고 0 BPM이 기준선이다: 높이 = 기준선 + BPM / TopBpm × (맨 위 - 기준선). TopBpm은 생존 구간의 위쪽 끝(기본 190)이라
    /// 생존 구간(10~190)이 기준선 바로 위(10 BPM = 5.3%)부터 사용 가능한 세로 영역의 맨 위(190 BPM = 100%)까지 정확히 펴진다.
    /// 10 이상이 생존이므로 봉우리는 기준선과 겹치지 않는다. 즉사 위쪽(191~200)은 맨 위에 붙는다.
    /// 반환값은 모니터 화면(파형 그래픽) 높이 대비 0~1이다.
    /// </summary>
    public readonly struct HeartbeatMonitorScale
    {
        /// <summary>0 BPM(기준선)이 놓이는 높이. 모니터 화면 하단 근처 — Q·S파가 기준선 아래로 살짝 내려갈 자리를 둔다.</summary>
        public const float BaselineY = 0.08f;

        /// <summary>TopBpm이 놓이는 높이(맨 위 여백을 남긴다).</summary>
        public const float TopY = 0.95f;

        public int TopBpm { get; }

        public HeartbeatMonitorScale(int topBpm) => TopBpm = Mathf.Max(1, topBpm);

        /// <summary>코어의 심박수 구간표에서 생존 구간의 위쪽 끝을 읽는다 — 구간표가 바뀌어도 UI가 따로 숫자를 들고 있지 않는다.</summary>
        public static HeartbeatMonitorScale FromZone(HeartbeatZone zone) => new(zone.SurvivableMax);

        public static HeartbeatMonitorScale Default => FromZone(new HeartbeatZone());

        /// <summary>BPM(정수든 트윈 중인 소수든)의 높이. 화면 높이 대비 0~1.</summary>
        public float ToY(float bpm) => Mathf.Lerp(BaselineY, TopY, Mathf.Clamp01(bpm / TopBpm));

        /// <summary>기준선 위로 올라간 정도만(R파 진폭). 화면 높이 대비 0~1.</summary>
        public float AboveBaseline(float bpm) => ToY(bpm) - BaselineY;
    }
}
