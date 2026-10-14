using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using UnityEngine;

namespace BlueComplex.Audio
{
    /// <summary>
    /// <see cref="UiSoundCue"/> 하나에 재생할 클립들을 묶어 둔 항목. 클립을 여러 개 넣으면 재생할 때마다 그중 하나를
    /// 무작위로 고른다(같은 소리가 반복돼 단조롭지 않게). 새 소리를 추가할 때는 코드를 건드릴 필요 없이
    /// 인스펙터에서 이 배열에 항목을 늘리거나 기존 항목의 Clips에 파일을 끌어넣으면 된다.
    /// </summary>
    [Serializable]
    public sealed class SoundEntry
    {
        public UiSoundCue cue;
        public AudioClip[] clips = Array.Empty<AudioClip>();

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("재생할 때마다 이 범위 안에서 피치를 무작위로 살짝 바꿔 반복 재생이 기계적으로 들리지 않게 한다.")]
        public Vector2 pitchRange = new(0.97f, 1.03f);

        [Tooltip("0이면 공용 보이스를 돌려 쓴다. 1 이상이면 이 큐만 쓰는 전용 보이스를 그 수만큼 둔다 — 긴 클립(뉴스 잡음·빛 번짐)이 다른 소리에 끊기지 않고, " +
                 "1이면 다시 불렀을 때 이전 소리를 끊고 처음부터 다시 울린다(겹쳐 쌓이지 않는다). 짧게 연달아 울리는 소리(타자음)는 3~5.")]
        [Min(0)] public int dedicatedVoices;

        [Tooltip("같은 큐가 이 시간(초) 안에 다시 오면 무시한다. 0(비워 둠)이면 SoundManager의 기본값을 쓴다. 글자마다 울리는 타자음처럼 촘촘한 큐는 짧게 둔다.")]
        [Min(0f)] public float minInterval;

        [Tooltip("0(비워 둠)이면 클립을 끝까지 울린다. 0보다 크면 이 시간(초)까지만 쓰고 끝의 0.25초를 페이드아웃해 자른다 — 클립이 너무 길 때(빛 번짐 7초 중 3초만).")]
        [Min(0f)] public float maxSeconds;
    }
}
