using UnityEngine;
using DG.Tweening;

namespace KTH
{
    public class KTH_FadeSystemSignal : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private Sequence seq;

        public void FadeIn()
        {
            seq?.Kill();
            seq = DOTween.Sequence();
            seq.Append(canvasGroup.DOFade(0f, fadeDuration));
        }

        public void FadeOut()
        {
            seq?.Kill();
            seq = DOTween.Sequence();
            seq.Append(canvasGroup.DOFade(1f, fadeDuration));
        }
    }
}