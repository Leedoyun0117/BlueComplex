using BlueComplex.Core.Stability;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 심박수 모니터의 세로축 — <b>심박수 → 모니터 높이</b> 매핑이 사는 유일한 곳이다.
    /// 파형 봉우리(R파)와 목표 띠의 위아래 경계가 둘 다 <see cref="ToY"/>를 부른다. 매핑이 두 군데 있으면
    /// "띠 안에 들어간 것처럼 보이는데 실패" 같은 불일치가 생기므로, 이 구조체 밖에서 BPM을 높이로 바꾸지 않는다.
    ///
    /// 0 BPM이 기준선이다: 높이 = 기준선 + (BPM / TopBpm)^<see cref="SizeContrastExponent"/> × (맨 위 - 기준선). TopBpm은 생존
    /// 구간의 위쪽 끝(기본 190)이라 생존 구간(10~190)이 기준선 바로 위부터 사용 가능한 세로 영역의 맨 위(190 BPM = 100%)까지 펴진다.
    /// 10 이상이 생존이므로 봉우리는 기준선과 겹치지 않는다. 즉사 위쪽(191~200)은 맨 위에 붙는다.
    /// 지수가 1보다 작아 곡선이 저심박 쪽에서 더 가파르다 — 그래야 침체될 때 봉우리(따라서 목표 띠도 같이) 높이가
    /// 눈에 띄게 작아진다. 파형이 흐르는 속도(<see cref="EcgWaveGraphic"/>의 박 간격)는 이미 BPM에 따라 바뀌는데,
    /// 진폭이 선형이면 그 변화가 상대적으로 묻혀 "느려지고 빨라지기만" 하는 것처럼 보였다.
    /// 반환값은 모니터 화면(파형 그래픽) 높이 대비 0~1이다.
    /// </summary>
    public readonly struct HeartbeatMonitorScale
    {
        /// <summary>0 BPM(기준선)이 놓이는 높이. 모니터 화면 하단 근처 — Q·S파가 기준선 아래로 살짝 내려갈 자리를 둔다.</summary>
        public const float BaselineY = 0.08f;

        /// <summary>TopBpm이 놓이는 높이(맨 위 여백을 남긴다).</summary>
        public const float TopY = 0.95f;

        /// <summary>정규화된 BPM 비율(0~1)에 거는 지수. 1이면 선형(예전 동작). 1보다 작을수록 저심박 쪽 기울기가
        /// 가팔라져 같은 BPM 차이라도 봉우리·목표 띠의 크기 차이가 더 커 보인다.</summary>
        private const float SizeContrastExponent = 0.6f;

        public int TopBpm { get; }

        public HeartbeatMonitorScale(int topBpm) => TopBpm = Mathf.Max(1, topBpm);

        /// <summary>코어의 심박수 구간표에서 생존 구간의 위쪽 끝을 읽는다 — 구간표가 바뀌어도 UI가 따로 숫자를 들고 있지 않는다.</summary>
        public static HeartbeatMonitorScale FromZone(HeartbeatZone zone) => new(zone.SurvivableMax);

        public static HeartbeatMonitorScale Default => FromZone(new HeartbeatZone());

        /// <summary>BPM(정수든 트윈 중인 소수든)의 높이. 화면 높이 대비 0~1.</summary>
        public float ToY(float bpm)
        {
            var fraction = Mathf.Pow(Mathf.Clamp01(bpm / TopBpm), SizeContrastExponent);
            return Mathf.Lerp(BaselineY, TopY, fraction);
        }

        /// <summary>기준선 위로 올라간 정도만(R파 진폭). 화면 높이 대비 0~1.</summary>
        public float AboveBaseline(float bpm) => ToY(bpm) - BaselineY;
    }
}
