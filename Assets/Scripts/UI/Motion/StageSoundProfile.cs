using BlueComplex.Core.Stage;

namespace BlueComplex.UI.Motion
{
    /// <summary>한 스테이지가 쓰는 배경음 큐 묶음.</summary>
    public readonly struct StageSoundProfile
    {
        /// <summary>안정 구간의 기본 배경음(심박수 배경음 채널). 침체·흥분 배경음은 스테이지 공통이라 여기 없다 — 이 곡과 크로스페이드로 오간다.</summary>
        public UiSoundCue BaseBed { get; }

        /// <summary>플레이 내내 깔리는 상시 배경음(별도 채널). null이면 이 스테이지는 상시 배경음이 없다.</summary>
        public UiSoundCue? Ambient { get; }

        public StageSoundProfile(UiSoundCue baseBed, UiSoundCue? ambient)
        {
            BaseBed = baseBed;
            Ambient = ambient;
        }
    }
}
