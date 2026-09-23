using UnityEngine;
using UnityEngine.Playables;

namespace KTH
{
    /// <summary>PlayableDirector의 재생 속도를 조절한다. 1 = 기본, 0.5 = 절반 속도.</summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class KTH_TimelineSpeed : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 3f)] private float speed = 1f;

        private PlayableDirector director;

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
            director.played += _ => ApplySpeed();
        }

        private void Start()
        {
            ApplySpeed();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) ApplySpeed();
        }

        public void SetSpeed(float value)
        {
            speed = value;
            ApplySpeed();
        }

        private void ApplySpeed()
        {
            if (director == null || !director.playableGraph.IsValid()) return;
            director.playableGraph.GetRootPlayable(0).SetSpeed(speed);
        }
    }
}
