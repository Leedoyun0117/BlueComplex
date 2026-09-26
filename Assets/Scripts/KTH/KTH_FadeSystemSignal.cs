using UnityEngine;
using DG.Tweening;

namespace KTH
{
    public class KTH_FadeSystemSignal : MonoBehaviour
    {
        [Tooltip("시그널마다 순서대로 사용할 캔버스 그룹. 마지막 이후로는 마지막 그룹을 계속 사용")]
        [SerializeField] private CanvasGroup[] canvasGroups;
        [SerializeField] private float fadeDuration = 0.5f;

        private int index;

        public void FadeIn()
        {
            Fade(0f);
        }

        public void FadeOut()
        {
            Fade(1f);
        }

        // 현재 그룹을 페이드하고 다음 그룹으로 넘어감
        private void Fade(float alpha)
        {
            if (canvasGroups == null || canvasGroups.Length == 0) return;

            var group = canvasGroups[index];
            if (group != null)
            {
                // 다른 그룹의 페이드는 끊지 않도록 해당 그룹의 트윈만 정리
                group.DOKill();
                group.DOFade(alpha, fadeDuration);
            }

            if (index < canvasGroups.Length - 1) index++;
        }
    }
}
