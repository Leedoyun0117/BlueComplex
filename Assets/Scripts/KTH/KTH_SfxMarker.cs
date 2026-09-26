using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace KTH
{
    /// <summary>
    /// 타임라인에 찍는 효과음 마커. 클립을 마커에 직접 넣으므로 효과음마다 시그널 에셋을 만들 필요가 없다.
    /// Signal Track 위에서 우클릭 → Add Sfx Marker 로 추가하고, 인스펙터에서 클립/볼륨을 지정한다.
    /// 재생은 같은 오브젝트(Signal)에 붙은 <see cref="KTH_TimelineSfxReceiver"/>가 맡는다.
    /// </summary>
    [DisplayName("Sfx Marker")]
    public class KTH_SfxMarker : Marker, INotification, INotificationOptionProvider
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Tooltip("타임라인을 이 마커 뒤에서 시작해도 재생할지")]
        [SerializeField] private bool retroactive;

        [Tooltip("타임라인이 반복돼도 한 번만 재생할지")]
        [SerializeField] private bool emitOnce;

        public AudioClip Clip => clip;
        public float Volume => volume;

        public PropertyName id => new PropertyName(nameof(KTH_SfxMarker));

        NotificationFlags INotificationOptionProvider.flags =>
            (retroactive ? NotificationFlags.Retroactive : default) |
            (emitOnce ? NotificationFlags.TriggerOnce : default);
    }
}
