using System.Collections;
using UnityEngine;

namespace KTH
{
    /// <summary>
    /// 켜져 있는 동안 효과음을 일정 간격(실제 초)으로 반복 재생한다. 예: 파도 소리를 8초마다.
    /// Signal Receiver의 SoundOn/SoundOff 반응에 StartRepeat/StopRepeat를 같이 걸어 BGM과 함께 켜고 끈다.
    /// </summary>
    public class KTH_RepeatSfx : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Tooltip("재생 간격(초). 타임라인 배속과 무관한 실제 시간이다.")]
        [SerializeField, Min(0.1f)] private float interval = 8f;

        [Tooltip("StartRepeat 후 첫 재생까지 기다리는 시간(초).")]
        [SerializeField, Min(0f)] private float startDelay;

        private Coroutine routine;

        public void StartRepeat()
        {
            StopRepeat();
            if (clip != null && isActiveAndEnabled) routine = StartCoroutine(Repeat());
        }

        public void StopRepeat()
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }

        private void OnDisable() => StopRepeat();

        private IEnumerator Repeat()
        {
            if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);
            while (true)
            {
                KTH_Sfx.Play(clip, volume);
                yield return new WaitForSecondsRealtime(interval);
            }
        }
    }
}
