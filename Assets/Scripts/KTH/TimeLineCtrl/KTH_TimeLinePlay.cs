using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace KTH
{
    [RequireComponent(typeof(PlayableDirector))]
    public class KTH_TimeLinePlay : MonoBehaviour, ITimeLinePlayer
    {
        // 켜져 있는 재생기들. 타임라인 에셋(.playable) 자체를 키로 사용 → 문자열/채널 에셋 불필요
        private static readonly List<KTH_TimeLinePlay> Players = new();

        /// <summary>타임라인 하나가 끝났을 때(끝까지 재생됐거나 멈췄을 때) 불린다. 인자는 끝난 타임라인 에셋. 게임 흐름이 컷신 종료를 알 때 쓴다.</summary>
        public static event Action<PlayableAsset> Finished;

        [SerializeField] private bool playOnce = true;
        [Tooltip("시작 시 꺼두고 타임라인 재생이 시작되면 켜는 오브젝트 (타임라인이 끝나면 다시 끔)")]
        [SerializeField] private GameObject[] activateOnPlay;

        private PlayableDirector pd;
        private bool hasPlayed;

        /// <summary>이 타임라인을 재생하도록 요청한다. 실제로 재생을 시작한 재생기가 하나라도 있으면 true —
        /// 켜진 재생기가 없거나(오브젝트가 비활성/씬에 없음) <c>playOnce</c>라 이미 한 번 재생했으면 false(이때는 <see cref="Finished"/>도 오지 않는다).</summary>
        public static bool Request(PlayableAsset timeLine)
        {
            var started = false;
            // Play()가 재생기 목록을 건드리지 않지만, 시작 시점의 콜백이 목록을 바꿔도 안전하도록 복사본을 돈다.
            foreach (var player in Players.ToArray())
            {
                if (player != null && timeLine == player.pd.playableAsset && player.Play()) started = true;
            }

            return started;
        }

        private void Awake()
        {
            pd = GetComponent<PlayableDirector>();
            SetActivateTargets(false);
        }

        private void OnEnable()
        {
            Players.Add(this);
            pd.played += OnPlayed;
            pd.stopped += OnStopped;
        }

        private void OnDisable()
        {
            Players.Remove(this);
            pd.played -= OnPlayed;
            pd.stopped -= OnStopped;
        }

        private void OnPlayed(PlayableDirector director)
        {
            SetActivateTargets(true);
        }

        // 타임라인이 끝나면 켰던 오브젝트(캔버스 등)를 다시 끄고, 끝났음을 알린다
        private void OnStopped(PlayableDirector director)
        {
            SetActivateTargets(false);
            Finished?.Invoke(director.playableAsset);
        }

        private void SetActivateTargets(bool active)
        {
            if (activateOnPlay == null) return;
            foreach (var go in activateOnPlay)
            {
                if (go != null) go.SetActive(active);
            }
        }

        /// <summary>재생을 시작했으면 true. <c>playOnce</c>이고 이미 재생했으면 false.</summary>
        public bool Play()
        {
            if (playOnce && hasPlayed) return false;
            hasPlayed = true;
            pd.Play();
            return true;
        }

        // ITimeLinePlayer.Play() 시그니처(void)를 지키는 명시적 구현.
        void ITimeLinePlayer.Play() => Play();
    }
}
