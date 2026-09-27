using UnityEngine;
using UnityEngine.Playables;

namespace KTH
{
    /// <summary>
    /// 컷씬에서 효과음을 내는 연결 지점. 실제 재생은 <see cref="KTH_Sfx"/>가 맡는다.
    /// - 인스펙터 이벤트(Signal Receiver 리액션, 버튼 등)에서 <see cref="PlaySfx"/>에 클립을 넘겨 부를 수 있다.
    /// - 타임라인의 <see cref="KTH_SfxMarker"/>도 받는다(Signal Track이 바인딩된 Signal 오브젝트에 붙어 있을 때).
    /// 코드에서는 이 컴포넌트 없이 KTH_Sfx.Play(clip)를 바로 불러도 된다.
    /// </summary>
    public class KTH_TimelineSfxReceiver : MonoBehaviour, INotificationReceiver
    {
        [Tooltip("이 컷씬 효과음 전체에 곱해지는 볼륨. 플레이어 옵션 볼륨은 믹서가 따로 곱한다.")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        public void PlaySfx(AudioClip clip) => KTH_Sfx.Play(clip, volume);

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is KTH_SfxMarker marker)
                KTH_Sfx.Play(marker.Clip, marker.Volume * volume);
        }
    }
}
