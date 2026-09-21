using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 단서 카드 한 장의 종이 같은 움직임. 시간·크기 값은 전부 UiMotionSettings(인스펙터)에서 온다.
    ///
    /// <list type="bullet">
    /// <item>호버: 살짝 떠오르며 그림자가 길어진다.</item>
    /// <item>집기(드래그 시작): 카드의 겉모습을 복제한 "손에 든 카드"(고스트)가 커서를 따라다니며 더 크게 들리고 약간 기울고 그림자가 멀어진다. 원래 슬롯은 흐려진 채 남는다.</item>
    /// <item>놓기 실패: 고스트가 원래 자리로 날아가 종이처럼 흔들리며 안착한다.</item>
    /// <item>놓기 성공: 고스트가 기억 풍선 쪽으로 빨려 들어가며 사라진다.</item>
    /// </list>
    /// 슬롯 자체를 끌고 다니지 않는 이유: 드롭이 성공하면 슬롯은 그 즉시 다음 카드로 갱신되므로(손패가 줄어든다) 날아가는 카드는 슬롯과 분리돼 있어야 한다.
    /// 호버 위치 오프셋은 레이아웃 그룹이 슬롯 위치를 덮어써도 오차가 쌓이지 않게 <see cref="LayoutSafeOffset"/>으로 준다.
    /// </summary>
    [RequireComponent(typeof(ClueCardView))]
    public sealed class ClueCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Vector2 RestShadowDistance = new(2f, -2f);
        private static readonly Vector2 HoverShadowDistance = new(6f, -10f);
        private static readonly Vector2 HeldShadowDistance = new(16f, -26f);
        private const float RestShadowAlpha = 0.22f;
        private const float HoverShadowAlpha = 0.36f;
        private const float HeldShadowAlpha = 0.4f;

        private ClueCardView _view;
        private RectTransform _rect;
        private Shadow _shadow;
        private LayoutSafeOffset _offset;
        private float _hover;
        private Tween _hoverTween;

        private RectTransform _canvasRect;
        private RectTransform _ghost;
        private Shadow _ghostShadow;
        private CanvasGroup _ghostGroup;
        private float _lift;
        private float _wobble;
        private float _tilt;
        private Tween _liftTween;
        private Tween _flightTween;

        /// <summary>손에 든 카드가 있는 동안(집은 뒤 날아가 사라지거나 안착할 때까지) true — 그동안 새로 집을 수 없다.</summary>
        public bool IsBusy => _ghost != null;

        private void Awake()
        {
            _view = GetComponent<ClueCardView>();
            _rect = (RectTransform)transform;
            _offset = new LayoutSafeOffset(_rect);
            _shadow = MockupStyle.AddShadow(gameObject);
            ApplyRestPose();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsBusy || _view.IsEmpty) return;
            TweenHover(1f);
        }

        public void OnPointerExit(PointerEventData eventData) => TweenHover(0f);

        private void TweenHover(float target)
        {
            _hoverTween?.Kill();
            _hoverTween = DOTween.To(() => _hover, v =>
                {
                    _hover = v;
                    ApplyRestPose();
                }, target, UiMotion.Settings.clueHover)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);
        }

        private void ApplyRestPose()
        {
            var motion = UiMotion.Settings;
            _rect.localScale = Vector3.one * Mathf.Lerp(1f, motion.clueHoverScale, _hover);
            _rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -1.2f, _hover));
            _offset.SetY(_hover * motion.clueHoverLift);
            SetShadow(_shadow, Vector2.Lerp(RestShadowDistance, HoverShadowDistance, _hover),
                Mathf.Lerp(RestShadowAlpha, HoverShadowAlpha, _hover));
        }

        private static void SetShadow(Shadow shadow, Vector2 distance, float alpha)
        {
            if (shadow == null) return;
            shadow.effectDistance = distance;
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        }

        /// <summary>손에 든 카드를 만들어 커서 자리에 놓고 들어 올린다. 만들 수 없으면(이미 들고 있음) false.</summary>
        public bool BeginHold(Vector2 canvasLocalPoint, RectTransform canvasRect)
        {
            if (IsBusy) return false;

            // 호버로 떠 있던 슬롯은 제자리로 내려놓는다 — 고스트가 자기 자세를 이어받는다.
            _hoverTween?.Kill();
            _hover = 0f;
            ApplyRestPose();

            _canvasRect = canvasRect;
            _ghost = _view.BuildGhost(canvasRect);
            _ghost.SetAsLastSibling();
            _ghost.localPosition = canvasLocalPoint;
            _ghostGroup = _ghost.GetComponent<CanvasGroup>();
            _ghostShadow = MockupStyle.AddShadow(_ghost.gameObject);

            _view.SetLifted(true);

            var motion = UiMotion.Settings;
            _tilt = (Random.value < 0.5f ? -1f : 1f) * motion.cluePickupTilt;
            _lift = 0f;
            _wobble = 0f;
            ApplyGhostPose();

            _liftTween?.Kill();
            _liftTween = DOTween.To(() => _lift, v =>
                {
                    _lift = v;
                    ApplyGhostPose();
                }, 1f, motion.cluePickup)
                .SetEase(Ease.OutBack).SetUpdate(true).SetTarget(this);

            UiSoundHooks.Play(UiSoundCue.Paper);
            return true;
        }

        public void Follow(Vector2 canvasLocalPoint)
        {
            if (_ghost != null && _flightTween == null) _ghost.localPosition = canvasLocalPoint;
        }

        /// <summary>놓았다. accepted면 <paramref name="suckTargetWorld"/>(기억 풍선 중심)로 빨려 들어가고, 아니면 원래 자리로 돌아간다.</summary>
        public void EndHold(bool accepted, Vector3? suckTargetWorld)
        {
            if (_ghost == null) return;

            _liftTween?.Kill();
            _flightTween?.Kill();

            if (accepted && suckTargetWorld is { } target) PlaySuck(target);
            else PlayReturn();
        }

        private void PlaySuck(Vector3 targetWorld)
        {
            var motion = UiMotion.Settings;
            var target = _canvasRect.InverseTransformPoint(targetWorld);
            var seconds = motion.clueSuck;
            var startLift = _lift;
            var startScale = _ghost.localScale.x;

            // 빨려 들어가는 동안 들린 자세를 풀고(그림자 가까워짐) 돌면서 작아진다.
            _flightTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(_ghost.DOLocalMove(target, seconds).SetEase(Ease.InCubic))
                .Join(DOTween.To(() => 0f, p =>
                    {
                        _ghost.localScale = Vector3.one * Mathf.Lerp(startScale, 0.1f, p);
                        _ghost.localRotation = Quaternion.Euler(0f, 0f, _tilt + Mathf.Sign(_tilt) * 50f * p);
                        SetShadow(_ghostShadow, Vector2.Lerp(HeldShadowDistance, RestShadowDistance, p * startLift),
                            Mathf.Lerp(HeldShadowAlpha, 0f, p));
                    }, 1f, seconds).SetEase(Ease.InCubic))
                .Join(_ghostGroup.DOFade(0f, seconds).SetEase(Ease.InQuad))
                .OnComplete(FinishHold);
        }

        private void PlayReturn()
        {
            var motion = UiMotion.Settings;
            var home = _canvasRect.InverseTransformPoint(_rect.TransformPoint(_rect.rect.center));

            var fly = motion.clueReturnFly;
            var settle = motion.clueReturnSettle;

            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(_ghost.DOLocalMove(home, fly).SetEase(Ease.OutCubic))
                .Join(DOTween.To(() => _lift, v =>
                    {
                        _lift = v;
                        ApplyGhostPose();
                    }, 0f, fly).SetEase(Ease.OutQuad));

            // 도착하면 종이처럼 좌우로 흔들리며 가라앉는다(감쇠하는 기울기).
            var swings = new[] { 7f, -5f, 3f, -1.5f, 0f };
            var weights = new[] { 0.25f, 0.25f, 0.2f, 0.15f, 0.15f };
            sequence.AppendCallback(() => UiSoundHooks.Play(UiSoundCue.Paper));
            for (var i = 0; i < swings.Length; i++)
            {
                sequence.Append(DOTween.To(() => _wobble, v =>
                    {
                        _wobble = v;
                        ApplyGhostPose();
                    }, swings[i], settle * weights[i]).SetEase(Ease.InOutSine));
            }

            _flightTween = sequence.OnComplete(FinishHold);
        }

        private void ApplyGhostPose()
        {
            if (_ghost == null) return;

            var motion = UiMotion.Settings;
            _ghost.localScale = Vector3.one * Mathf.Lerp(1f, motion.cluePickupScale, _lift);
            _ghost.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, _tilt, _lift) + _wobble);
            SetShadow(_ghostShadow, Vector2.Lerp(RestShadowDistance, HeldShadowDistance, _lift),
                Mathf.Lerp(RestShadowAlpha, HeldShadowAlpha, _lift));
        }

        private void FinishHold()
        {
            _flightTween = null;
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
            _ghostShadow = null;
            _ghostGroup = null;
            _view.RestoreVisibility();
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            _flightTween = null;
            if (_ghost == null) return;

            Destroy(_ghost.gameObject);
            _ghost = null;
            _view.RestoreVisibility();
        }
    }
}
