using BlueComplex.Core.Stability;
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
    ///
    /// "표정과 반응" 기획표의 분노/슬픔/기쁨은 <see cref="SetMood"/>가 담당한다 — Flash와는 별개 레이어다: Flash는
    /// 컴플렉스가 발동한 그 순간만 잠깐 튀는 반응이고, SetMood는 그 턴의 최종 태그로 정해지는 "바탕 표정"이라
    /// Flash가 끝나면 무표정이 아니라 이 바탕 표정으로 돌아간다. 분노·슬픔·기쁨 스프라이트는 프리팹에 안 구워져 있으면
    /// (Assets/Resources/UI/Portraits/Yuki/yuki_angry·yuki_sad·yuki_joy) 스스로 찾아 쓰고, 그마저 없으면 무표정으로 대신한다.
    /// </summary>
    public sealed class PortraitXrayView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _neutral;
        [SerializeField] private Sprite _reactive;
        [SerializeField] private Sprite _angry;
        [SerializeField] private Sprite _sad;
        [SerializeField] private Sprite _joy;

        private Tween _flashTween;

        /// <summary>지금 이 턴의 바탕 표정(기본은 _neutral). Flash가 끝나면 여기로 돌아간다.</summary>
        private Sprite _moodSprite;

        /// <summary>프리팹에 구워진 스케일. 초상화는 좌우 반전(x = -1)돼 있을 수 있어서, 반응 연출이 끝나고 돌아갈 자리를
        /// <c>Vector3.one</c>으로 잡으면 반전이 풀려 버린다 — 시작할 때 원래 값을 기억해 두고 항상 이 값으로 돌아간다.</summary>
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            _baseScale = transform.localScale;

            if (_angry == null) _angry = Resources.Load<Sprite>("UI/Portraits/Yuki/yuki_angry");
            if (_sad == null) _sad = Resources.Load<Sprite>("UI/Portraits/Yuki/yuki_sad");
            if (_joy == null) _joy = Resources.Load<Sprite>("UI/Portraits/Yuki/yuki_joy");

            ResetToNeutral();
        }

        /// <summary>표정을 무표정으로 되돌린다. 판넬이 열릴 때마다 부른다 — 지난 턴의 바탕 표정이 다음 턴까지 남아 있지 않게.</summary>
        public void ResetToNeutral()
        {
            _flashTween?.Kill();
            _moodSprite = _neutral;
            ApplyMood();
            transform.localScale = _baseScale;
        }

        /// <summary>이 턴의 최종 태그·심박수로 정해진 바탕 표정을 적용한다. Flash가 재생 중이면(반응 표정이 떠 있는 동안) 화면은
        /// 안 바꾸고 되돌아갈 표정만 갱신해 둔다 — 안 그러면 반응 표정 위로 바탕 표정이 끼어들어 깜빡인다.</summary>
        public void SetMood(YukiReaction mood)
        {
            _moodSprite = mood switch
            {
                YukiReaction.Anger => _angry != null ? _angry : _neutral,
                YukiReaction.Sadness => _sad != null ? _sad : _neutral,
                YukiReaction.Joy => _joy != null ? _joy : _neutral,
                _ => _neutral
            };

            if (_flashTween == null || !_flashTween.IsActive()) ApplyMood();
        }

        private void ApplyMood()
        {
            if (_image != null && _moodSprite != null) _image.sprite = _moodSprite;
        }

        /// <summary>컴플렉스가 발동한 순간 잠깐 반응 표정으로 바뀌었다가(살짝 커지며) 바탕 표정으로 돌아온다. 이미지·반응 스프라이트가 없으면 조용히 아무것도 안 한다.</summary>
        public void Flash()
        {
            if (_image == null || _reactive == null || _moodSprite == null) return;

            _flashTween?.Kill();
            var half = Mathf.Max(0.05f, UiMotion.Settings.portraitFlash * 0.5f);

            _image.sprite = _reactive;
            transform.localScale = _baseScale * 1.05f;
            _flashTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .AppendInterval(half)
                .AppendCallback(ApplyMood)
                .Append(transform.DOScale(_baseScale, half).SetEase(Ease.OutQuad));
        }

        private void OnDisable() => DOTween.Kill(this);
    }
}
