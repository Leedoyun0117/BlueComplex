using System;

namespace BlueComplex.UI.Motion
{
    public enum UiSoundCue
    {
        /// <summary>종이가 스치거나 놓이는 소리(카드 집기·안착, 아이템 삽입·구겨짐).</summary>
        Paper,

        /// <summary>압정·테이프로 붙이는 소리(포스트잇·키 카드가 붙을 때).</summary>
        Pin,

        /// <summary>타자기 한 글자.</summary>
        Type,

        /// <summary>펜으로 선을 긋거나 지우개로 지우는 소리(기억 풍선).</summary>
        Pen,

        /// <summary>열쇠가 찍히는 소리.</summary>
        Key,
    }

    /// <summary>
    /// 사운드 훅. 오디오 에셋이 아직 없어서 소리는 내지 않고, 연출이 "지금 이 소리가 나야 한다"는 시점만 알린다.
    /// 오디오 담당은 <see cref="Cue"/>를 구독해 클립을 재생하면 된다(구독이 없으면 아무 일도 없다).
    /// </summary>
    public static class UiSoundHooks
    {
        public static event Action<UiSoundCue> Cue;

        public static void Play(UiSoundCue cue) => Cue?.Invoke(cue);
    }
}
