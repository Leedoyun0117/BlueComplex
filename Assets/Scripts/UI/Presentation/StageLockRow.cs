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
        /// <summary>자물쇠 그림에서 실제로 그려진 폭(64칸 중 44칸, 좌우 10칸은 투명) — lock.png 전용 값. 간격은 이 보이는 폭 기준으로 재야 균등하게 보인다.</summary>
        private const float VisibleWidth = 44f / 64f;

        /// <summary>보이는 자물쇠 폭 대비 자물쇠 사이 간격(눈에 보이는 빈틈)의 비율.</summary>
        private const float GapRatio = 0.35f;

        /// <summary>자물쇠 한 칸의 목표 높이 — 화면 세로 길이 대비 비율. 자리가 넉넉하면 항상 이 크기로 그린다.</summary>
        private const float TargetHeightFraction = 0.25f;

        /// <summary>화면 좌우에 남기는 최소 여백(화면 가로 길이 대비, 한쪽 기준) — 자물쇠 줄 전체가 이 여백 안쪽에서만 넓어진다.</summary>
        private const float ScreenMarginFraction = 0.06f;

        private readonly List<StageLockView> _locks = new();
        private CanvasGroup _group;

        public int Count => _locks.Count;

        public static StageLockRow Create(Transform parent)
        {
            var rect = RuntimeUi.CreateRect(parent, "Stage Lock Row", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var row = rect.gameObject.AddComponent<StageLockRow>();
            row._group = rect.gameObject.AddComponent<CanvasGroup>();
            row._group.alpha = 0f;
            row._group.blocksRaycasts = false;
            row._group.interactable = false;
            return row;
        }

        /// <summary>
        /// 자물쇠를 <paramref name="count"/>개로 새로 늘어놓는다(전부 닫힌 채, 안 보이는 상태에서). 자물쇠 한 칸의 크기는 화면 세로 길이의
        /// <see cref="TargetHeightFraction"/>을 목표로 하되, <paramref name="area"/>(부모 로컬 좌표)의 가로 폭이 그보다 좁으면 화면 여백(<see cref="ScreenMarginFraction"/>)까지
        /// 넓혀서 자리를 만든다(그래도 부족하면 그만큼 줄어든다) — 자물쇠는 항상 <see cref="GapRatio"/>만큼의 같은 빈틈으로 area의 중앙에 늘어선다(개수가 몇 개든).
        /// 간격은 그림 칸(투명 여백 포함)이 아니라 그려진 폭으로 잰다. 어디가 "빈 자리"인지는 부른 쪽(<see cref="StageClearDirector"/>)이 잰다 — 이 클래스는 배치만 한다.
        /// </summary>
        public void Build(int count, Rect area)
        {
            Clear();

            var visibleUnits = count * VisibleWidth + (count - 1) * VisibleWidth * GapRatio; // 크기 1 기준으로 그려진 줄 전체의 폭
            var screenRect = ((RectTransform)transform.parent).rect; // 부모(Stage Clear Director)는 캔버스 전체에 꽉 차 있다 — 곧 화면 크기.
            var targetSize = screenRect.height * TargetHeightFraction;
            var widthBudget = Mathf.Max(area.width, screenRect.width * (1f - 2f * ScreenMarginFraction));
            var size = Mathf.Min(targetSize, widthBudget / visibleUnits);
            var pitch = size * VisibleWidth * (1f + GapRatio); // 이웃 자물쇠 중심 사이 거리

            var rect = (RectTransform)transform;
            rect.anchoredPosition = area.center;
            rect.sizeDelta = new Vector2(size * visibleUnits, size);

            for (var i = 0; i < count; i++)
            {
                var view = StageLockView.Create(rect, $"Lock {i + 1}", size / 64f);
                ((RectTransform)view.transform).anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * pitch, 0f);
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
