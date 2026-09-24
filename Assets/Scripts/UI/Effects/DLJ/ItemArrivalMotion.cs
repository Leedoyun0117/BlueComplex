using System;
using BlueComplex.UI.Layout;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BlueComplex.UI.Effects.DLJ
{
    /// <summary>UI 연출 기획: 빈칸 옆에서 확대 → 접근 → 빠른 삽입. 슬롯의 레이아웃은 유지한다.</summary>
    public static class ItemArrivalMotion
    {
        public static Sequence Play(RectTransform slot, Image background, TMP_Text label, Image icon,
            Color emptyColor, Action onFinished)
        {
            Canvas.ForceUpdateCanvases();
            var ghost = new GameObject("DLJ Item Arrival", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
            ghost.layer = slot.gameObject.layer;
            ghost.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)ghost.transform;
            rect.SetParent(slot.parent, false);
            rect.anchorMin = rect.anchorMax = slot.anchorMin;
            rect.pivot = slot.pivot;
            rect.sizeDelta = slot.rect.size;
            var ghostGroup = ghost.GetComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.interactable = false;

            var paper = ghost.AddComponent<Image>();
            paper.sprite = background.sprite;
            paper.type = background.type;
            paper.color = background.color;
            paper.material = background.material;
            paper.preserveAspect = background.preserveAspect;
            paper.raycastTarget = false;
            // 종이는 원본과 같은 윤곽 무늬여야 한다 — 테두리 복제(아래)보다 먼저 붙여서 복제본이 그 무늬를 물려받게 한다.
            var sourceSkin = background.GetComponent<PaperPanel>();
            if (sourceSkin != null) PaperPanel.Skin(paper, background.material, sourceSkin.Seed);
            foreach (var edge in background.GetComponents<Shadow>())
            {
                if (!edge.enabled) continue;
                var copy = (Shadow)ghost.AddComponent(edge.GetType());
                copy.effectColor = edge.effectColor;
                copy.effectDistance = edge.effectDistance;
                copy.useGraphicAlpha = edge.useGraphicAlpha;
            }
            // 슬롯 전체를 복제하지 않아 입력/게임 이벤트는 중복 생성되지 않는다.
            if (icon != null) CopyGraphic(icon, rect);
            if (label != null) CopyGraphic(label, rect);

            var filledColor = background.color;
            var labelEnabled = label != null && label.enabled;
            var iconEnabled = icon != null && icon.enabled;
            var group = slot.GetComponent<CanvasGroup>();
            var blocksRaycasts = group == null || group.blocksRaycasts;
            var interactable = group == null || group.interactable;
            background.color = emptyColor;
            if (label != null) label.enabled = false;
            if (icon != null) icon.enabled = false;
            if (group != null) { group.blocksRaycasts = false; group.interactable = false; }

            var total = Mathf.Max(0.24f, ItemArrivalSettings.Settings.duration);
            var width = Mathf.Max(1f, slot.rect.width);
            var offset = new Vector3(-width * 1.12f, slot.rect.height * 0.08f, 0f);
            var approach = new Vector3(-width * 0.16f, 0f, 0f);
            var scale = 0.06f;
            var tilt = -8f;
            void FollowSlot()
            {
                if (rect == null || slot == null) return;
                rect.sizeDelta = slot.rect.size;
                rect.localPosition = slot.localPosition + offset;
                rect.localRotation = slot.localRotation * Quaternion.Euler(0f, 0f, tilt);
                rect.localScale = slot.localScale * scale;
            }
            FollowSlot();

            var restored = false;
            void Restore()
            {
                if (restored) return;
                restored = true;
                if (background != null) background.color = filledColor;
                if (label != null) label.enabled = labelEnabled;
                if (icon != null) icon.enabled = iconEnabled;
                if (group != null) { group.blocksRaycasts = blocksRaycasts; group.interactable = interactable; }
                if (ghost != null) { ghost.SetActive(false); Object.Destroy(ghost); }
                onFinished?.Invoke();
            }

            // 세 단계를 겹치지 않는다. 패널이 접히거나 해상도가 바뀌어도 매 프레임 실제 슬롯을 따라간다.
            return DOTween.Sequence().SetUpdate(true).SetTarget(slot)
                .Append(DOTween.To(() => scale, value => scale = value, 1.08f, total * 0.3f).SetEase(Ease.OutBack, 1.2f))
                .Join(DOTween.To(() => tilt, value => tilt = value, -3f, total * 0.3f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => offset, value => offset = value, approach, total * 0.35f).SetEase(Ease.InOutSine))
                .Append(DOTween.To(() => offset, value => offset = value, Vector3.zero, total * 0.15f).SetEase(Ease.InCubic))
                .Join(DOTween.To(() => scale, value => scale = value, 0.97f, total * 0.15f).SetEase(Ease.InQuad))
                .Join(DOTween.To(() => tilt, value => tilt = value, 0f, total * 0.15f))
                .Append(DOTween.To(() => scale, value => scale = value, 1f, total * 0.2f).SetEase(Ease.OutQuad))
                .OnUpdate(FollowSlot).OnComplete(Restore).OnKill(Restore);
        }

        private static void CopyGraphic<T>(T source, RectTransform parent) where T : Graphic
        {
            var copy = Object.Instantiate(source, parent, false);
            copy.raycastTarget = false;
            // 아이콘/이름은 슬롯 직속 자식이다. 복제 시 앵커·폰트·색·여백도 그대로 보존한다.
            copy.gameObject.layer = parent.gameObject.layer;
        }

    }
}
