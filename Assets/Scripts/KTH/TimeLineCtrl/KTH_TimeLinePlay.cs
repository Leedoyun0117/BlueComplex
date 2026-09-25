using System;
using UnityEngine;
using UnityEngine.Playables;

namespace KTH
{
    [RequireComponent(typeof(PlayableDirector))]
    public class KTH_TimeLinePlay : MonoBehaviour, ITimeLinePlayer
    {
        // 타임라인 에셋(.playable) 자체를 키로 사용 → 문자열/채널 에셋 불필요
        private static event Action<PlayableAsset> PlayRequested;

        [SerializeField] private bool playOnce = true;
        [Tooltip("시작 시 꺼두고 타임라인 재생이 시작되면 켜는 오브젝트 (타임라인이 끝나면 다시 끔)")]
        [SerializeField] private GameObject[] activateOnPlay;

        private PlayableDirector pd;
        private bool hasPlayed;

        public static void Request(PlayableAsset timeLine)
        {
            PlayRequested?.Invoke(timeLine);
        }

        private void Awake()
        {
            pd = GetComponent<PlayableDirector>();
            SetActivateTargets(false);
        }

        private void OnEnable()
        {
            PlayRequested += OnPlayRequested;
            pd.played += OnPlayed;
            pd.stopped += OnStopped;
        }

        private void OnDisable()
        {
            PlayRequested -= OnPlayRequested;
            pd.played -= OnPlayed;
            pd.stopped -= OnStopped;
        }

        private void OnPlayRequested(PlayableAsset timeLine)
        {
            if (timeLine == pd.playableAsset) Play();
        }

        private void OnPlayed(PlayableDirector director)
        {
            SetActivateTargets(true);
        }

        // 타임라인이 끝나면 켰던 오브젝트(캔버스 등)를 다시 끈다
        private void OnStopped(PlayableDirector director)
        {
            SetActivateTargets(false);
        }

        private void SetActivateTargets(bool active)
        {
            if (activateOnPlay == null) return;
            foreach (var go in activateOnPlay)
            {
                if (go != null) go.SetActive(active);
            }
        }

        public void Play()
        {
            if (playOnce && hasPlayed) return;
            hasPlayed = true;
            pd.Play();
        }
    }
}
