using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신 3막: 시계. 검은 화면 한가운데에 PPT의 로마 숫자 아날로그 시계가 작게 나타나(전반부, 하드 컷) 머물다가,
    /// 후반부에 크기만 변하며 클로즈업된다 — 디졸브·모핑 없이 같은 그림을 그대로 키운다. 크기는 캔버스 높이에 대한 비율(PPT 4번 → 5번 장표).
    /// 떠 있는 내내(작게 + 확대) 시간이 빠르게 흐르는 느낌을 준다 — 소리(ClockTick 클립: 일정한 간격의 똑딱)에 바늘을 맞춘다: 똑딱마다 시침이 한 칸(30°) 딱 넘어가고,
    /// 분침은 똑딱 사이에 정확히 한 바퀴를 매끄럽게 돌아 매 똑딱마다 12시로 돌아온다.
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
        /// 바늘은 <paramref name="tickSeconds"/>(소리의 똑딱 간격)에 맞춘다 — 첫 똑딱은 나타나는 순간(시침이 한 칸 전 자리에서 딱 넘어온다).
        /// </summary>
        public IEnumerator Play(float appearSeconds, float zoomSeconds, float smallRatio, float largeRatio, float tickSeconds)
        {
            SetHeight(smallRatio);
            _group.alpha = 1f; // 나타남은 하드 컷.
            RunHands(appearSeconds + zoomSeconds, tickSeconds);
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

        private const float HourStep = -30f;      // 시계 방향으로 한 칸(1시간).
        private const float HourSnapSeconds = 0.12f; // 한 칸 넘어가는 데 걸리는 시간 — 똑딱 소리의 타격에 맞춰 딱 넘어간다.

        private void RunHands(float seconds, float tickSeconds)
        {
            seconds = Mathf.Max(0.01f, seconds);
            tickSeconds = Mathf.Max(0.05f, tickSeconds);
            var ticks = Mathf.Max(1, Mathf.FloorToInt(seconds / tickSeconds + 0.0001f));

            // 분침: 똑딱 사이에 정확히 한 바퀴(일정한 속도) — 전체 시간 동안 seconds/tickSeconds 바퀴.
            if (_minute != null)
            {
                _minute.localRotation = Quaternion.identity;
                _minute.DOLocalRotate(new Vector3(0f, 0f, -360f * seconds / tickSeconds), seconds, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this);
            }

            // 시침: 똑딱마다 한 칸. 첫 똑딱(t=0)이 나타나는 순간이라 한 칸 전(-HourStep) 자리에서 시작해 딱 넘어온다.
            if (_hour == null) return;

            _hour.localRotation = Quaternion.Euler(0f, 0f, -HourStep);
            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (var k = 0; k < ticks; k++)
            {
                var target = new Vector3(0f, 0f, HourStep * k);
                sequence.Insert(k * tickSeconds, _hour.DOLocalRotate(target, HourSnapSeconds).SetEase(Ease.OutBack, 2.2f));
            }
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
