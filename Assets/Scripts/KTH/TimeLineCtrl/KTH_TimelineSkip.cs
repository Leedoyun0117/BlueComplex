using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace KTH
{
    /// <summary>
    /// 재생 중인 컷씬을 스킵 키로 바로 끝낸다. PlayableDirector와 같은 오브젝트에 붙인다.
    /// Stop으로 끝내므로 끝까지 재생됐을 때처럼 stopped 이벤트가 나간다(KTH_TimeLinePlay의 정리도 그대로 동작).
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class KTH_TimelineSkip : MonoBehaviour
    {
        [SerializeField] private Key skipKey = Key.Space;

        private PlayableDirector director;
        private int playedFrame = -1;

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
        }

        private void OnEnable() => director.played += OnPlayed;
        private void OnDisable() => director.played -= OnPlayed;

        private void OnPlayed(PlayableDirector _) => playedFrame = Time.frameCount;

        private void Update()
        {
            if (director.state != PlayState.Playing) return;
            // ESC 일시정지(timeScale=0) 중에는 스킵하지 않는다.
            if (Time.timeScale <= 0f) return;
            // 컷씬을 시작시킨 같은 키 입력으로 곧바로 스킵되지 않게 시작 프레임은 건너뛴다.
            if (Time.frameCount <= playedFrame) return;
            if (Keyboard.current == null || !Keyboard.current[skipKey].wasPressedThisFrame) return;

            Skip();
        }

        /// <summary>
        /// 컷씬을 바로 끝낸다. 건너뛴 구간의 시그널은 불리지 않으므로
        /// SoundOff/StopRepeat는 여기서 대신 호출한다.
        /// </summary>
        public void Skip()
        {
            if (director.state != PlayState.Playing) return;

            StopSignalSounds();
            director.Stop();
        }

        // 시그널 트랙에 바인딩된 SignalReceiver의 반응 대상 중 켜져 있는 사운드를 끈다.
        private void StopSignalSounds()
        {
            if (director.playableAsset == null) return;

            foreach (var output in director.playableAsset.outputs)
            {
                var go = director.GetGenericBinding(output.sourceObject) switch
                {
                    GameObject g => g,
                    Component c => c.gameObject,
                    _ => null,
                };
                if (go == null) continue;

                foreach (var receiver in go.GetComponents<SignalReceiver>())
                {
                    for (int i = 0; i < receiver.Count(); i++)
                    {
                        var reaction = receiver.GetReactionAtIndex(i);
                        if (reaction == null) continue;

                        for (int j = 0; j < reaction.GetPersistentEventCount(); j++)
                        {
                            switch (reaction.GetPersistentTarget(j))
                            {
                                case KTH_TimelineAudioSignal audio when audio.IsOn:
                                    audio.SoundOff();
                                    break;
                                case KTH_RepeatSfx repeat:
                                    repeat.StopRepeat();
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
