using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신 3막: 시계. 검은 화면 한가운데에 PPT의 로마 숫자 아날로그 시계가 작게 나타나(전반부, 하드 컷) 머물다가,
    /// 후반부에 크기만 변하며 클로즈업된다 — 디졸브·모핑 없이 같은 그림을 그대로 키운다. 크기는 캔버스 높이에 대한 비율(PPT 4번 → 5번 장표).
    /// 떠 있는 내내(작게 + 확대) 분침이 여러 바퀴 돌고 시침이 그 1/12만큼 함께 돌아, 시간이 빠르게 흐르는 느낌을 준다.
    /// 바늘은 문자판 그림에서 떼어 낸 별도 그림이고(<see cref="IntroCutsceneArt.clockHour"/>·<see cref="IntroCutsceneArt.clockMinute"/>), 문자판과 같은 크기라 회전 중심만 한가운데 허브에 맞춘다.
    /// </summary>
    internal sealed class IntroClockView : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _group;
        private RectTransform _hour;
        private RectTransform _minute;
        private float _aspect;
        private float _canvasHeight;

        /// <summary>바늘의 회전 중심(허브) — 문자판 그림 안의 위치(왼쪽 아래 기준 0~1). 원본 그림에서 허브 구멍의 중심을 잰 값이다.</summary>
        private static readonly Vector2 HubPivot = new(0.5009f, 0.5007f);

        /// <summary>그림이 없으면 null — 컷신은 이 구간을 검은 화면으로 두고 시간만 흘린다.</summary>
        public static IntroClockView Create(Transform parent, Vector2 canvasSize, IntroCutsceneArt art)
        {
            if (art == null || art.clock == null)
            {
                Debug.LogWarning("[IntroClockView] 시계 그림이 채워져 있지 않다 — 메뉴 BlueComplex/Cutscene/Rebuild Intro Art를 실행한다.");
                return null;
            }

            var go = new GameObject("Intro Clock", typeof(RectTransform), typeof(RawImage), typeof(CanvasGroup)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var view = go.AddComponent<IntroClockView>();
            view._rect = (RectTransform)go.transform;
            view._rect.anchorMin = view._rect.anchorMax = view._rect.pivot = new Vector2(0.5f, 0.5f);
            view._rect.anchoredPosition = Vector2.zero;
            view._aspect = (float)art.clock.width / art.clock.height;
            view._canvasHeight = canvasSize.y;

            var face = go.GetComponent<RawImage>();
            face.texture = art.clock;
            face.raycastTarget = false;

            view._group = go.GetComponent<CanvasGroup>(); // 문자판과 바늘을 한꺼번에 켜고 끈다.
            view._group.alpha = 0f;
            view._group.blocksRaycasts = false;

            view._hour = CreateHand(go.transform, "Hour Hand", art.clockHour);
            view._minute = CreateHand(go.transform, "Minute Hand", art.clockMinute);
            return view;
        }

        /// <summary>
        /// 작은 시계가 나타나 <paramref name="appearSeconds"/> 머문 뒤, <paramref name="zoomSeconds"/> 동안 크기만 변해 클로즈업된다.
        /// 분침은 그 전체 시간 동안 <paramref name="minuteTurns"/>바퀴를 일정한 속도로 시계 방향으로 돌고, 시침은 그 1/12만큼 돈다.
        /// </summary>
        public IEnumerator Play(float appearSeconds, float zoomSeconds, float smallRatio, float largeRatio, float minuteTurns)
        {
            SetHeight(smallRatio);
            _group.alpha = 1f; // 나타남은 하드 컷.
            SpinHands(appearSeconds + zoomSeconds, minuteTurns);
            yield return new WaitForSecondsRealtime(appearSeconds);

            yield return DOTween.To(() => smallRatio, SetHeight, largeRatio, Mathf.Max(0.01f, zoomSeconds))
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
        }

        public void Hide()
        {
            DOTween.Kill(this);
            _group.alpha = 0f;
            foreach (var hand in new[] { _hour, _minute })
                if (hand != null) hand.localRotation = Quaternion.identity;
        }

        private void SpinHands(float seconds, float minuteTurns)
        {
            seconds = Mathf.Max(0.01f, seconds);
            Spin(_minute, -360f * minuteTurns, seconds);
            Spin(_hour, -360f * minuteTurns / 12f, seconds);
        }

        private void Spin(RectTransform hand, float degrees, float seconds)
        {
            if (hand == null) return;

            hand.localRotation = Quaternion.identity;
            hand.DOLocalRotate(new Vector3(0f, 0f, degrees), seconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this);
        }

        /// <summary>문자판 위에 겹치는 바늘 한 장(문자판과 같은 크기). 그림이 없으면 null — 그 바늘은 그냥 안 돈다(문자판에 바늘이 없어 보이지만 컷신은 흐른다).</summary>
        private static RectTransform CreateHand(Transform parent, string name, Texture2D texture)
        {
            if (texture == null) return null;

            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = HubPivot;

            var raw = go.GetComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            return rect;
        }

        private void SetHeight(float ratio)
        {
            var height = _canvasHeight * ratio;
            _rect.sizeDelta = new Vector2(height * _aspect, height);
        }

        private void OnDestroy() => DOTween.Kill(this);
    }
}
