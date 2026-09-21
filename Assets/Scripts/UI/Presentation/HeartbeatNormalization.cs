namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 심박수를 -1(침체 끝) ~ 0(중앙) ~ +1(흥분 끝)로 정규화한다.
    /// CRT와 램프처럼 같은 심박수 시각 피드백을 쓰는 컴포넌트들이 같은 규칙을 공유한다.
    /// </summary>
    public static class HeartbeatNormalization
    {
        /// <summary>
        /// center를 0으로 두고 lower / upper를 각각 -1 / +1로 매핑한다.
        /// 안정 구간이 중앙 기준 비대칭(-9/+20)이므로 양방향을 따로 계산한다.
        /// </summary>
        public static float Normalize(int value, int center, int lower, int upper)
        {
            if (value == center) return 0f;

            return value < center
                ? (value - center) / (float)(center - lower)
                : (value - center) / (float)(upper - center);
        }
    }
}
