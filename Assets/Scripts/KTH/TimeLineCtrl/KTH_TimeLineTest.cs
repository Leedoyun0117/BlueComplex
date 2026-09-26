using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace KTH
{
    // 테스트용: 키 입력으로 타임라인 재생 요청
    public class KTH_TimeLineTest : MonoBehaviour
    {
        [SerializeField] private PlayableAsset timeLine; 
        private Key playKey = Key.T;

        private void Update()
        {
            if (timeLine == null || Keyboard.current == null) return;
            if (!Keyboard.current[playKey].wasPressedThisFrame) return;

            KTH_TimeLinePlay.Request(timeLine);
        }
    }
}
