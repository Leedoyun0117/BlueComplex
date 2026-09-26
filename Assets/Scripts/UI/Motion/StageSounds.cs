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

    /// <summary>
    /// 스테이지별 배경음 지정. Core(StageConfig)는 UI 큐를 모르므로, StageDialogues처럼 스테이지 id로 UI 쪽에서 고른다.
    /// 새 스테이지는 여기 한 줄을 더하고 SoundSetupTool/SoundLibrary에 그 곡의 큐를 등록하면 된다. 목록에 없는 스테이지는 스테이지 1과 같은 소리를 쓴다.
    /// </summary>
    public static class StageSounds
    {
        public static readonly StageSoundProfile Stage1 = new(UiSoundCue.HeartbeatBase, UiSoundCue.AmbientNotes);

        /// <summary>스테이지 2("천사"): 기본 배경음이 ClockTower로 바뀌고 상시 배경음(Fragile Notes)은 없다. 침체/흥분 배경음·전환음은 스테이지 1과 공통.</summary>
        public static readonly StageSoundProfile Stage2 = new(UiSoundCue.HeartbeatBaseStage2, null);

        /// <summary>스테이지 3("무제"): 기본 배경음이 Faded Scribbles로 바뀌고 상시 배경음(Fragile Notes)은 없다(스테이지 2와 같다). 침체/흥분 배경음·전환음은 공통.</summary>
        public static readonly StageSoundProfile Stage3 = new(UiSoundCue.HeartbeatBaseStage3, null);

        public static StageSoundProfile For(StageConfig config) => config?.Id switch
        {
            "stage_2" => Stage2,
            "stage_3" => Stage3,
            _ => Stage1,
        };
    }
}
