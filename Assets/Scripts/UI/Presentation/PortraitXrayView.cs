using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 엑스레이 판넬 안, 뇌 위쪽에 붙는 유키의 얼굴 사진(Assets/Art/UI/Portraits/Yuki). 판정에 관여하지 않는 순수 장식이다 —
    /// 평소엔 무표정이고, 컴플렉스가 발동해 뇌의 해당 부분이 빛날 때마다 잠깐 반응 표정으로 바뀌었다가 되돌아온다.
    /// 표시 시점은 이 컴포넌트가 스스로 정하지 않는다: <see cref="Flash"/>는 CinematicTurnResultPresenter가
    /// BrainView.PlayGlow와 같은 자리에서 부르고, <see cref="ResetToNeutral"/>은 ComplexXrayPanel이 판넬을 열 때마다 부른다.
    /// </summary>
    public sealed class PortraitXrayView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _neutral;
        [SerializeField] private Sprite _reactive;

        private Tween _flashTween;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            ResetToNeutral();
        }

        /// <summary>표정을 무표정으로 되돌린다. 판넬이 열릴 때마다 부른다 — 지난 반응 표정이 다음 반응까지 남아 있지 않게.</summary>
        public void ResetToNeutral()
        {
            _flashTween?.Kill();
            if (_image != null && _neutral != null) _image.sprite = _neutral;
            transform.localScale = Vector3.one;
        }

        /// <summary>컴플렉스가 발동한 순간 잠깐 반응 표정으로 바뀌었다가(살짝 커지며) 무표정으로 돌아온다. 이미지·반응 스프라이트가 없으면 조용히 아무것도 안 한다.</summary>
        public void Flash()
        {
            if (_image == null || _reactive == null || _neutral == null) return;

            _flashTween?.Kill();
            var half = Mathf.Max(0.05f, UiMotion.Settings.portraitFlash * 0.5f);

            _image.sprite = _reactive;
            transform.localScale = Vector3.one * 1.05f;
            _flashTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .AppendInterval(half)
                .AppendCallback(() => { if (_image != null) _image.sprite = _neutral; })
                .Append(transform.DOScale(1f, half).SetEase(Ease.OutQuad));
        }

        private void OnDisable() => DOTween.Kill(this);
    }
}
