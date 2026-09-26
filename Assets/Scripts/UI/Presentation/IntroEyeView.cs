using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신 2막: 고래의 눈. 전용 픽셀아트(<see cref="IntroCutsceneArt"/>) 320x180 한 장이 화면 전체(정수 배 확대)이고, 눈꺼풀 둘이 눈을 덮는다.
    ///
    /// 층 순서(뒤 → 앞): EyeEffect(불이 켜진 눈, 처음엔 투명) · BlackEye(검은 동공 + 흰 테두리) · 아래 눈꺼풀 · 위 눈꺼풀.
    /// BlackEye가 EyeEffect 위에 있어야 눈이 밝아진 뒤에도 동공이 보인다 — EyeEffect는 회색 홍채 원이라 동공이 없고, 그 위에 BlackEye가 동공 자리를 채운다.
    ///
    ///  (a) 감긴 눈: 위·아래 눈꺼풀 1번이 대각선 이음매에서 맞닿아 화면을 덮는다(이음매가 눈 한가운데를 지난다).
    ///  (b) 눈이 뜨인다: 깜빡임 두 번 → 활짝.
    ///      깜빡임 A: 눈꺼풀 1이 살짝 벌어졌다가(느리게) 빠르게 닫힌다. 깜빡임 B: 눈꺼풀 2/3(S자)가 서로 겹쳐 덮은 상태에서 절반쯤 벌어졌다가 빠르게 닫힌다.
    ///      그다음 S자 눈꺼풀이 활짝 벌어지고 EyeEffect가 밝아진 뒤, 위·아래로 더 물러난다.
    ///      S자 눈꺼풀은 처음부터 3번 그림을 쓴다 — 3은 2와 모양이 같고 캔버스만 위/아래로 80px씩 넓어서, 두 눈꺼풀이 서로 겹쳐 덮도록 밀어도 화면 가장자리가 드러나지 않는다(2로는 드러난다).
    ///      눈꺼풀은 "간격(gap)" 하나로 움직인다: 위는 위로 gap, 아래는 아래로 gap(프레임 픽셀, 정수 단위로 끊어서 도트 느낌). 음수면 서로 겹쳐 덮는다.
    ///  (c) 동공 확대: 동공 중심이 화면 한가운데로 오면서 화면 전체가 확대되어 빨려 들어가고, 끝에서 검게 사라진다.
    /// </summary>
    internal sealed class IntroEyeView : MonoBehaviour
    {
        private const float FrameWidth = 320f;
        private const float FrameHeight = 180f;

        /// <summary>눈 중심(동공)의 프레임 좌표(픽셀, 왼쪽 위 기준) — BlackEye 그림의 중심이다(EyeEffect의 홍채도 같은 점).</summary>
        private static readonly Vector2 EyeCenter = new(168.5f, 82.5f);

        /// <summary>BlackEye의 검은 원(테두리 안쪽) 반지름(프레임 픽셀). 확대가 끝났을 때 화면이 이 검정으로 가득 차도록 배율을 정한다.</summary>
        private const float PupilRadius = 36f;

        // ── 눈꺼풀 간격(프레임 픽셀). 눈꺼풀 1은 0 = 닫힘, 눈꺼풀 2는 -55 = 서로 겹쳐 덮음(눈꺼풀 2의 모양이 중앙에서 맞닿는 값), 0 = 원래 벌어진 모양. ──
        private const float BlinkASlit = 14f;
        private const float Lids2Covered = -58f;
        private const float BlinkBHalf = -22f;
        private const float LidSlide = 50f;

        // ── 깜빡임 시간(초, introEyeOpen = BaseOpenTotal일 때). 닫히는 건 빠르고 벌어지는 건 살짝 느리다. ──
        private const float AOpen = 0.26f, AHoldOpen = 0.08f, AClose = 0.09f, AHoldClosed = 0.18f;
        private const float BOpen = 0.30f, BHoldOpen = 0.10f, BClose = 0.10f, BHoldClosed = 0.16f;
        private const float FinalOpen = 0.50f, FinalSlide = 0.45f;
        private const float BaseOpenTotal = AOpen + AHoldOpen + AClose + AHoldClosed + BOpen + BHoldOpen + BClose + BHoldClosed + FinalOpen + FinalSlide;

        private RectTransform _frame;
        private CanvasGroup _group;
        private RawImage _eyeEffect;
        private RawImage _upper;
        private RawImage _upperNext;
        private RawImage _lower;
        private RawImage _lowerNext;
        private IntroCutsceneArt _art;
        private float _scale;
        private Vector2 _canvasSize;
        private Vector2 _framePosition;
        private bool _pairB;
        private float _gap;

        /// <summary>그림이 없으면 null — 컷신은 눈 구간을 검은 화면으로 두고 시간만 흘린다.</summary>
        public static IntroEyeView Create(Transform parent, Vector2 canvasSize, IntroCutsceneArt art)
        {
            if (art == null || art.blackEye == null || art.eyeEffect == null || art.upperLid is not { Length: 3 } || art.lowerLid is not { Length: 3 })
            {
                Debug.LogWarning("[IntroEyeView] 눈 그림이 채워져 있지 않다 — 메뉴 BlueComplex/Cutscene/Rebuild Intro Art를 실행한다.");
                return null;
            }

            var go = new GameObject("Intro Eye", typeof(RectTransform), typeof(CanvasGroup)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var view = go.AddComponent<IntroEyeView>();
            view.Build(canvasSize, art);
            return view;
        }

        private void Build(Vector2 canvasSize, IntroCutsceneArt art)
        {
            _art = art;
            _canvasSize = canvasSize;
            _scale = Mathf.Max(canvasSize.x / FrameWidth, canvasSize.y / FrameHeight); // 16:9면 정확히 정수 배(1920x1080 → 6).

            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            // 프레임: 처음엔 화면 한가운데에 놓되, 확대의 중심(pivot)이 눈 중심이 되게 한다(확대하면서 눈 중심이 화면 한가운데로 온다 — PlayZoom).
            var pivot = new Vector2(EyeCenter.x / FrameWidth, 1f - EyeCenter.y / FrameHeight);
            var size = new Vector2(FrameWidth, FrameHeight) * _scale;
            _frame = (RectTransform)transform;
            _frame.anchorMin = _frame.anchorMax = new Vector2(0.5f, 0.5f);
            _frame.pivot = pivot;
            _frame.sizeDelta = size;
            _framePosition = new Vector2((0.5f - pivot.x) * size.x, (0.5f - pivot.y) * size.y);
            _frame.anchoredPosition = _framePosition;

            _eyeEffect = CreateLayer("EyeEffect", art.eyeEffect, FrameHeight);
            CreateLayer("BlackEye", art.blackEye, FrameHeight);
            _lower = CreateLayer("Lower Lid", art.lowerLid[0], art.lowerLid[0].height);
            _lowerNext = CreateLayer("Lower Lid Next", art.lowerLid[2], art.lowerLid[2].height);
            _upper = CreateLayer("Upper Lid", art.upperLid[0], art.upperLid[0].height);
            _upperNext = CreateLayer("Upper Lid Next", art.upperLid[2], art.upperLid[2].height);
            ResetPose();
        }

        /// <summary>(a) 감긴 눈: 눈꺼풀 프레임이 검은 화면에서 서서히 나타나 머문다. <paramref name="seconds"/>는 나타남 + 머묾의 합.</summary>
        public IEnumerator PlayClosed(float seconds)
        {
            ResetPose();

            var appear = Mathf.Min(seconds * 0.5f, 0.7f);
            yield return _group.DOFade(1f, appear).SetEase(Ease.OutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds - appear));
        }

        /// <summary>(b) 눈이 뜨인다: 깜빡임 A(눈꺼풀 1이 살짝) → 깜빡임 B(눈꺼풀 2가 절반) → 활짝 → EyeEffect 점등 → 눈꺼풀 3 걷어내기. 시간 배분은 <paramref name="seconds"/>에 비례한다.</summary>
        public IEnumerator PlayOpening(float seconds)
        {
            var k = Mathf.Max(0.01f, seconds) / BaseOpenTotal;
            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            var t = 0f;

            // 깜빡임 A — 눈꺼풀 1이 살짝 벌어졌다가 빠르게 닫힌다.
            t = Add(sequence, t, k * AOpen, Gap(BlinkASlit, k * AOpen, Ease.OutSine));
            t += k * AHoldOpen;
            t = Add(sequence, t, k * AClose, Gap(0f, k * AClose, Ease.InQuad));
            t += k * AHoldClosed;

            // 눈꺼풀 1 → S자 눈꺼풀로 바꿔 놓는다(둘 다 화면을 덮은 상태라 바뀌는 순간이 크게 보이지 않는다).
            sequence.InsertCallback(t, () => SwitchToPairB(Lids2Covered));

            // 깜빡임 B — 눈꺼풀 2가 절반쯤 벌어졌다가 빠르게 닫힌다.
            t = Add(sequence, t, k * BOpen, Gap(BlinkBHalf, k * BOpen, Ease.OutSine));
            t += k * BHoldOpen;
            t = Add(sequence, t, k * BClose, Gap(Lids2Covered, k * BClose, Ease.InQuad));
            t += k * BHoldClosed;

            // 활짝: 눈꺼풀 2가 원래 모양으로 벌어지고(느리게), 거의 다 벌어질 즈음 EyeEffect가 밝아진다.
            var open = k * FinalOpen;
            sequence.Insert(t, Gap(0f, open, Ease.OutCubic));
            sequence.Insert(t + open * 0.5f, _eyeEffect.DOFade(1f, open * 0.6f).SetEase(Ease.InOutSine));
            t += open;

            // 눈꺼풀을 위·아래로 걷어낸다.
            sequence.Insert(t, Gap(LidSlide, k * FinalSlide, Ease.OutCubic));

            yield return sequence.WaitForCompletion(true);
        }

        /// <summary>(c) 동공 확대: 동공 중심이 화면 한가운데로 옮겨 오면서 화면 전체를 처음엔 천천히, 갈수록 빨려 들어가듯 확대하고 끝에서 검게 사라진다. 다 끝나면 눈은 완전히 투명하다.</summary>
        public IEnumerator PlayZoom(float seconds)
        {
            // BlackEye의 검은 원이 화면 대각선의 절반을 덮을 때까지 — 여유를 둔다.
            var target = _canvasSize.magnitude * 0.5f / (PupilRadius * _scale) * 1.15f;

            // 확대 기준점(pivot)이 눈 중심이라 그 점은 원래 자리(화면 한가운데에서 조금 비껴 있다)에 머문다 — 프레임을 옮겨 동공을 화면 정중앙으로 가져온다.
            // 같은 곡선으로 옮기면(배율이 커지는 만큼만 움직이므로) 프레임 가장자리가 화면 안으로 들어오지 않는다.
            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(_frame.DOScale(target, seconds).SetEase(Ease.InCubic));
            sequence.Join(_frame.DOAnchorPos(Vector2.zero, seconds).SetEase(Ease.InCubic));
            sequence.Insert(seconds * 0.65f, _group.DOFade(0f, seconds * 0.35f).SetEase(Ease.InSine));
            yield return sequence.WaitForCompletion(true);
        }

        public void Hide()
        {
            DOTween.Kill(this);
            _group.alpha = 0f;
        }

        private void ResetPose()
        {
            _frame.localScale = Vector3.one;
            _frame.anchoredPosition = _framePosition;
            _group.alpha = 0f;
            _eyeEffect.color = new Color(1f, 1f, 1f, 0f);
            _pairB = false;
            SetLid(_upper, _art.upperLid[0], 1f);
            SetLid(_lower, _art.lowerLid[0], 1f);
            SetLid(_upperNext, _art.upperLid[2], 0f);
            SetLid(_lowerNext, _art.lowerLid[2], 0f);
            SetGap(0f);
        }

        /// <summary>S자 눈꺼풀 쌍(2와 같은 모양의 3번 그림)으로 넘어간다. 눈꺼풀 1은 감춘다.</summary>
        private void SwitchToPairB(float gap)
        {
            _pairB = true;
            _upper.color = _lower.color = new Color(1f, 1f, 1f, 0f);
            _upperNext.color = _lowerNext.color = Color.white;
            SetGap(gap);
        }

        /// <summary>지금 보이는 눈꺼풀 쌍의 간격을 정수 프레임 픽셀로 옮긴다(위 = +, 아래 = −). 도트 그림이라 한 픽셀씩 끊어서 움직인다.</summary>
        private void SetGap(float gap)
        {
            _gap = gap;
            var y = Mathf.Round(gap) * _scale;
            var up = _pairB ? _upperNext : _upper;
            var down = _pairB ? _lowerNext : _lower;
            up.rectTransform.anchoredPosition = new Vector2(0f, y);
            down.rectTransform.anchoredPosition = new Vector2(0f, -y);
        }

        private Tween Gap(float target, float seconds, Ease ease) =>
            DOTween.To(() => _gap, SetGap, target, Mathf.Max(0.01f, seconds)).SetEase(ease);

        /// <summary>시퀀스의 <paramref name="t"/> 시각에 트윈을 넣고, 그 트윈이 끝나는 시각을 돌려준다.</summary>
        private static float Add(Sequence sequence, float t, float duration, Tween tween)
        {
            sequence.Insert(t, tween);
            return t + duration;
        }

        private static void SetLid(RawImage image, Texture2D texture, float alpha)
        {
            image.texture = texture;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.rectTransform.anchoredPosition = Vector2.zero;
        }

        /// <summary>프레임 한가운데에 놓인 그림 한 층. 캔버스가 더 높은 그림(눈꺼풀 3)도 가운데 정렬이라 눈 위치가 어긋나지 않는다.</summary>
        private RawImage CreateLayer(string name, Texture2D texture, float pixelHeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(FrameWidth, pixelHeight) * _scale;
            rect.anchoredPosition = Vector2.zero;

            var raw = go.GetComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            return raw;
        }

        private void OnDestroy() => DOTween.Kill(this);
    }
}
