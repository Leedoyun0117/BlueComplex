using System.Collections;
using System.Collections.Generic;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 클리어 연출의 자물쇠 줄 — 클리어에 필요한 키 개수만큼의 <see cref="StageLockView"/>를 가운데 정렬로 늘어놓고,
    /// 앞에서부터 하나씩 열고, 통째로 나타나거나 사라지게 한다. 표시만 한다 — 몇 개를 열지는 <see cref="StageClearDirector"/>(= 코어의 <c>StageEndPlan</c>)가 정한다.
    /// </summary>
    public sealed class StageLockRow : MonoBehaviour
    {
        /// <summary>도트 한 칸을 화면 픽셀 4개로 — 자물쇠 한 칸이 256px.</summary>
        private const int PixelScale = 4;
        private const float Gap = 96f;

        /// <summary>줄 중심의 세로 위치(화면 비율). 클리어 대사(0.4~0.6)가 열쇠 손잡이 아래로 지나가도록 위쪽에 둔다.</summary>
        private const float LockRowHeight = 0.74f;

        private readonly List<StageLockView> _locks = new();
        private CanvasGroup _group;

        public int Count => _locks.Count;

        public static StageLockRow Create(Transform parent)
        {
            var rect = RuntimeUi.CreateRect(parent, "Stage Lock Row", new Vector2(0.5f, LockRowHeight), new Vector2(0.5f, LockRowHeight), Vector2.zero, Vector2.zero);
            var row = rect.gameObject.AddComponent<StageLockRow>();
            row._group = rect.gameObject.AddComponent<CanvasGroup>();
            row._group.alpha = 0f;
            row._group.blocksRaycasts = false;
            row._group.interactable = false;
            return row;
        }

        /// <summary>자물쇠를 <paramref name="count"/>개로 새로 늘어놓는다(전부 닫힌 채, 안 보이는 상태에서).</summary>
        public void Build(int count)
        {
            Clear();

            var rect = (RectTransform)transform;
            var size = 64f * PixelScale;
            var total = count * size + (count - 1) * Gap;
            rect.sizeDelta = new Vector2(total, size);

            for (var i = 0; i < count; i++)
            {
                var view = StageLockView.Create(rect, $"Lock {i + 1}", PixelScale);
                var x = -total * 0.5f + size * 0.5f + i * (size + Gap);
                ((RectTransform)view.transform).anchoredPosition = new Vector2(x, 0f);
                _locks.Add(view);
            }

            _group.alpha = 0f;
        }

        /// <summary>줄 전체를 <paramref name="alpha"/>까지 페이드한다. 진행 중이던 페이드는 끊는다(대사가 진행되는 동안 여러 번 이어 불린다).</summary>
        public Tween FadeTo(float alpha, float duration)
        {
            _group.DOKill();
            return _group.DOFade(alpha, Mathf.Max(0.01f, duration)).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        /// <summary><paramref name="index"/>번 자물쇠를 연다(열쇠가 날아와 꽂히고 돌아가고 고리가 들린다).</summary>
        public IEnumerator PlayUnlock(int index)
        {
            if (index < 0 || index >= _locks.Count) yield break;
            yield return _locks[index].PlayUnlock();
        }

        /// <summary>모든 자물쇠(와 꽂힌 열쇠)를 함께 어둡게 한다.</summary>
        public Tween DarkenAll(float amount, float duration)
        {
            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            foreach (var view in _locks) sequence.Join(view.Darken(amount, duration));
            return sequence;
        }

        /// <summary>진행 중인 연출을 멈추고 자물쇠를 치운다(재시작).</summary>
        public void ResetNow()
        {
            DOTween.Kill(this);
            if (_group != null)
            {
                _group.DOKill();
                _group.alpha = 0f;
            }

            Clear();
        }

        private void Clear()
        {
            foreach (var view in _locks)
            {
                if (view == null) continue;
                DOTween.Kill(view);
                Destroy(view.gameObject);
            }

            _locks.Clear();
        }
    }
}
