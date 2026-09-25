using System.Collections;
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
    ///
    /// 바탕 표정이 "유지 중"인 동안에는 그 표정의 숨쉬기 루프(<see cref="YukiBreathSet"/>, 4프레임 1→2→3→4→3→2)가 계속 돈다 —
    /// 표정이 바뀌면(SetMood) 새 표정의 숨쉬기로 바로 갈아탄다. Flash가 떠 있는 동안은 숨쉬기를 멈췄다가 바탕 표정으로 돌아올 때 다시 시작한다.
    /// 숨쉬기 시트가 없으면 예전처럼 정지 스프라이트를 보여준다.
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
        private YukiBreathSet _breathSet;
        private Coroutine _breathRoutine;
        private Sprite[] _playingBreath;

        /// <summary>지금 바탕 표정의 숨쉬기 프레임(4장). 시트가 없거나 프레임이 모자라면 null — 정지 스프라이트로 대신한다.</summary>
        private Sprite[] _moodBreath;

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
            _breathSet = YukiBreathSet.Load();

            ResetToNeutral();
        }

        /// <summary>표정을 무표정으로 되돌린다. 판넬이 열릴 때마다 부른다 — 지난 턴의 바탕 표정이 다음 턴까지 남아 있지 않게.</summary>
        public void ResetToNeutral()
        {
            _flashTween?.Kill();
            _moodSprite = _neutral;
            _moodBreath = BreathFor(YukiReaction.Normal);
            ApplyMood();
            transform.localScale = _baseScale;
        }

        /// <summary>이 턴의 최종 태그·심박수로 정해진 바탕 표정을 적용한다. Flash가 재생 중이면(반응 표정이 떠 있는 동안) 화면은
        /// 안 바꾸고 되돌아갈 표정만 갱신해 둔다 — 안 그러면 반응 표정 위로 바탕 표정이 끼어들어 깜빡인다.</summary>
        public void SetMood(YukiReaction mood)
        {
            _moodBreath = BreathFor(mood);
            _moodSprite = mood switch
            {
                YukiReaction.Anger => _angry != null ? _angry : _neutral,
                YukiReaction.Sadness => _sad != null ? _sad : _neutral,
                YukiReaction.Joy => _joy != null ? _joy : _neutral,
                _ => _neutral
            };

            if (_flashTween == null || !_flashTween.IsActive()) ApplyMood();
        }

        private Sprite[] BreathFor(YukiReaction mood)
        {
            var frames = _breathSet != null ? _breathSet.For(mood) : null;
            return frames != null && frames.Length >= 4 ? frames : null;
        }

        /// <summary>바탕 표정을 화면에 올린다: 정지 스프라이트를 먼저 깔고(숨쉬기를 못 돌릴 때의 대비), 돌릴 수 있으면 숨쉬기 루프를 (다시) 시작한다.</summary>
        private void ApplyMood()
        {
            // 같은 표정이 이미 숨쉬는 중이면(턴마다 같은 SetMood가 온다) 루프를 끊지 않는다 — 끊으면 매 턴 숨이 1번 프레임으로 튄다.
            if (_moodBreath != null && _breathRoutine != null && ReferenceEquals(_playingBreath, _moodBreath)) return;

            StopBreath();
            if (_image != null && _moodSprite != null) _image.sprite = _moodSprite;
            if (_moodBreath != null && _image != null && isActiveAndEnabled)
            {
                _playingBreath = _moodBreath;
                _breathRoutine = StartCoroutine(BreathLoop(_moodBreath));
            }
        }

        private void StopBreath()
        {
            if (_breathRoutine != null) StopCoroutine(_breathRoutine);
            _breathRoutine = null;
            _playingBreath = null;
        }

        private IEnumerator BreathLoop(Sprite[] frames)
        {
            var cycle = _breathSet.Cycle();
            while (true)
            {
                foreach (var (frame, seconds) in cycle)
                {
                    _image.sprite = frames[frame];
                    yield return new WaitForSecondsRealtime(seconds);
                }
            }
        }

        private void OnEnable()
        {
            // 비활성 중엔 코루틴이 못 도니 다시 켜질 때 이어 돌린다. Flash가 떠 있는 동안이면 Flash가 끝나며 ApplyMood가 돌린다.
            if (_moodBreath != null && (_flashTween == null || !_flashTween.IsActive())) ApplyMood();
        }

        /// <summary>컴플렉스가 발동한 순간 잠깐 반응 표정으로 바뀌었다가(살짝 커지며) 바탕 표정으로 돌아온다. 이미지·반응 스프라이트가 없으면 조용히 아무것도 안 한다.</summary>
        public void Flash()
        {
            if (_image == null || _reactive == null || _moodSprite == null) return;

            _flashTween?.Kill();
            StopBreath();
            var half = Mathf.Max(0.05f, UiMotion.Settings.portraitFlash * 0.5f);

            _image.sprite = _reactive;
            transform.localScale = _baseScale * 1.05f;
            _flashTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .AppendInterval(half)
                .AppendCallback(ApplyMood)
                .Append(transform.DOScale(_baseScale, half).SetEase(Ease.OutQuad));
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            // 코루틴은 비활성화로 이미 멈췄다 — 핸들만 비워야 다시 켜질 때 ApplyMood의 "이미 숨쉬는 중" 판정에 안 걸린다.
            _breathRoutine = null;
            _playingBreath = null;
        }
    }
}
