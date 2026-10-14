using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 키 아이콘. 카탈로그에 "key" 스프라이트(Assets/Art/UI/Icons/key.png)가 있으면 그걸 쓰고, 없으면 임시 도형(원 머리 + 축 + 톱니)으로 그린다.
    /// 정사각형 안에 그려지므로 AspectRatioFitter로 부모 칸 안에 정사각형으로 맞춘다. 색은 틴트로 준다(획득 파랑 / 미획득 회색).
    /// 움직임(찍히기 등)은 AspectRatioFitter가 위치·크기를 쥐고 있는 Root가 아니라 그 안의 <see cref="Mover"/>에 준다.
    /// </summary>
    internal sealed class KeyGlyph
    {
        private readonly Image[] _parts;
        private readonly CanvasGroup _group;
        private Tween _stamp;
        private Tween _flash;

        public RectTransform Root { get; }

        /// <summary>열쇠 그림만 담은 안쪽 판. 위치·기울기·크기·투명도를 트윈으로 움직이는 대상.</summary>
        public RectTransform Mover { get; }

        public KeyGlyph(Transform slot, string name)
        {
            Root = RuntimeUi.CreateStretched(slot, name);
            var fitter = Root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            Mover = RuntimeUi.CreateStretched(Root, "Mover");
            _group = Mover.gameObject.AddComponent<CanvasGroup>();

            var sprite = UiIcons.GetExact(UiIconCatalog.KeyId);
            if (sprite != null)
            {
                var icon = RuntimeUi.CreateImage(Mover, "Icon", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, sprite);
                icon.preserveAspect = true;
                _parts = new[] { icon };
                return;
            }

            _parts = new[]
            {
                RuntimeUi.CreateImage(Mover, "Head", Color.white, new Vector2(0.08f, 0.30f), new Vector2(0.42f, 0.70f),
                    Vector2.zero, Vector2.zero, RuntimeUi.Circle),
                RuntimeUi.CreateImage(Mover, "Shaft", Color.white, new Vector2(0.38f, 0.45f), new Vector2(0.94f, 0.55f),
                    Vector2.zero, Vector2.zero),
                RuntimeUi.CreateImage(Mover, "Tooth A", Color.white, new Vector2(0.68f, 0.28f), new Vector2(0.76f, 0.47f),
                    Vector2.zero, Vector2.zero),
                RuntimeUi.CreateImage(Mover, "Tooth B", Color.white, new Vector2(0.84f, 0.33f), new Vector2(0.92f, 0.47f),
                    Vector2.zero, Vector2.zero),
            };
        }

        public void SetColor(Color color)
        {
            foreach (var part in _parts) part.color = color;
        }

        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        /// <summary>빈 칸에 열쇠가 "탁" 찍힌다: 위에서 크게 떨어져 칸에 닿으며 살짝 눌리고, 닿는 순간 밝아졌다가 작게 튀며 가라앉는다.
        /// <paramref name="onImpact"/>는 닿는 순간 한 번 불린다(소리 훅·칸 배경 번쩍임용).</summary>
        public void PlayStamp(Color finalColor, Color flashColor, Action onImpact)
        {
            var motion = UiMotion.Settings;
            Kill();

            SetColor(finalColor);
            _group.alpha = 0f;
            Mover.anchoredPosition = new Vector2(0f, motion.keyDropHeight);
            Mover.localScale = Vector3.one * 1.55f;
            Mover.localRotation = Quaternion.Euler(0f, 0f, 14f);

            var bounceHeight = motion.keyDropHeight * 0.14f;
            var up = motion.keyBounce * 0.4f;
            var down = motion.keyBounce * 0.6f;

            _stamp = DOTween.Sequence().SetUpdate(true).SetTarget(Mover)
                .Append(_group.DOFade(1f, motion.keyFall * 0.4f))
                .Join(Mover.DOAnchorPosY(0f, motion.keyFall).SetEase(Ease.InQuad))
                .Join(Mover.DOScale(0.9f, motion.keyFall).SetEase(Ease.InQuad))
                .Join(Mover.DOLocalRotate(Vector3.zero, motion.keyFall).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    PlayFlash(flashColor, finalColor, motion.keyFlash);
                    onImpact?.Invoke();
                })
                .Append(Mover.DOAnchorPosY(bounceHeight, up).SetEase(Ease.OutQuad))
                .Join(Mover.DOScale(1.1f, up).SetEase(Ease.OutQuad))
                .Append(Mover.DOAnchorPosY(0f, down).SetEase(Ease.InQuad))
                .Join(Mover.DOScale(1f, down).SetEase(Ease.InQuad));
        }

        private void PlayFlash(Color from, Color to, float seconds)
        {
            _flash?.Kill();
            var current = from;
            SetColor(from);
            _flash = DOTween.To(() => current, c =>
                {
                    current = c;
                    SetColor(c);
                }, to, seconds)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(Mover);
        }

        /// <summary>진행 중인 움직임을 멈추고 원래 자세로 되돌린다.</summary>
        public void Kill()
        {
            _stamp?.Kill();
            _flash?.Kill();
            Mover.DOKill();
            _group.DOKill();
            _group.alpha = 1f;
            Mover.anchoredPosition = Vector2.zero;
            Mover.localScale = Vector3.one;
            Mover.localRotation = Quaternion.identity;
        }
    }
}
