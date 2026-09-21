using UnityEngine;

namespace BlueComplex.UI.Motion
{
    /// <summary>
    /// 책상 위 종이 연출의 시간·크기 값 전부. 인스펙터에서 한 곳(Assets/Resources/UiMotionSettings.asset)만 고치면 된다 —
    /// 키 카드·쿼터 창처럼 코드로 짓는 UI는 프리팹이 없어 필드를 직렬화해 둘 곳이 없어서 Resources의 ScriptableObject로 모았다
    /// (UiIconCatalog와 같은 방식). 에셋이 없으면 아래 기본값으로 동작한다. 모든 시간은 초.
    /// </summary>
    [CreateAssetMenu(fileName = "UiMotionSettings", menuName = "BlueComplex/UI Motion Settings")]
    public sealed class UiMotionSettings : ScriptableObject
    {
        public const string ResourcePath = "UiMotionSettings";

        [Header("심박수 모니터")]
        [Tooltip("심박수가 바뀔 때 파형 높이·속도·BPM 숫자가 새 값으로 넘어가는 시간. 턴 결과 연출의 태그 상승(0.9초)과 맞춘다.")]
        public float heartTransition = 0.9f;
        [Tooltip("쿼터가 바뀌어 목표 띠가 새 자리로 옮겨 가는 시간.")]
        public float bandMove = 0.6f;
        [Tooltip("키 턴에 목표 띠가 강조되며 맥동하는 한 주기.")]
        public float keyBandPulse = 0.7f;
        [Tooltip("키 판정 결과(성공/실패)가 띠에 번쩍이는 시간.")]
        public float bandResultFlash = 0.5f;
        [Tooltip("심박수 구간이 바뀔 때 상태 배지가 깜빡이며 교체되는 전체 시간.")]
        public float badgeFlicker = 0.5f;

        [Header("키 카드")]
        [Tooltip("열쇠가 찍히기 시작하는 높이(캔버스 픽셀).")]
        public float keyDropHeight = 90f;
        [Tooltip("열쇠가 위에서 떨어지는 시간.")]
        public float keyFall = 0.16f;
        [Tooltip("닿은 뒤 작게 튀었다 가라앉는 시간.")]
        public float keyBounce = 0.22f;
        [Tooltip("닿는 순간 밝아졌다가 원래 색으로 돌아오는 시간.")]
        public float keyFlash = 0.38f;

        [Header("단서 카드")]
        public float clueHover = 0.14f;
        [Tooltip("호버 시 떠오르는 높이(캔버스 픽셀).")]
        public float clueHoverLift = 6f;
        public float clueHoverScale = 1.04f;
        [Tooltip("집을 때 더 크게 들리는 시간.")]
        public float cluePickup = 0.12f;
        public float cluePickupScale = 1.16f;
        [Tooltip("집었을 때 기울어지는 각도(도).")]
        public float cluePickupTilt = 4f;
        [Tooltip("놓기 실패: 원위치로 날아가는 시간.")]
        public float clueReturnFly = 0.3f;
        [Tooltip("놓기 실패: 도착 뒤 종이처럼 흔들리며 안착하는 시간.")]
        public float clueReturnSettle = 0.42f;
        [Tooltip("놓기 성공: 기억 풍선으로 빨려 들어가는 시간.")]
        public float clueSuck = 0.38f;

        [Header("기억 풍선")]
        [Tooltip("선이 한 바퀴 돌며 그려지는 시간.")]
        public float bubbleDraw = 0.55f;
        [Tooltip("지우개로 지우듯 사라지는 시간.")]
        public float bubbleErase = 0.65f;

        [Header("대사창")]
        [Tooltip("한 글자가 나오는 간격.")]
        public float dialogueSecondsPerChar = 0.03f;
        [Tooltip("타이핑 소리를 몇 글자마다 한 번 낼지(1 = 매 글자).")]
        [Min(1)] public int dialogueSoundEvery = 1;
        [Tooltip("우하단 ▼가 한 번 어두워지는 시간(왕복은 두 배).")]
        public float dialogueNextBlink = 0.9f;

        [Header("아이템 패널")]
        [Tooltip("사용한 카드가 구겨져 사라지는 시간.")]
        public float itemUse = 0.5f;
        [Tooltip("새 카드가 빈 칸에 끼워지는 시간.")]
        public float itemInsert = 0.42f;
    }

    /// <summary>모션 값 조회. 에셋이 없으면 기본값 인스턴스를 만들어 쓴다.</summary>
    public static class UiMotion
    {
        private static UiMotionSettings _settings;

        public static UiMotionSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;

                _settings = Resources.Load<UiMotionSettings>(UiMotionSettings.ResourcePath);
                if (_settings != null) return _settings;

                _settings = ScriptableObject.CreateInstance<UiMotionSettings>();
                _settings.hideFlags = HideFlags.HideAndDontSave;
                return _settings;
            }
        }
    }
}
