using System.Collections;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// "심박수 변화 연출"의 가운데 세 단계 — 카메라가 심박수 표시기로 확대되고 → 심박수 소리가 2초간 앞으로 나오고 → 소리가 잦아들며 원래 화면으로 돌아온다.
    /// (앞뒤의 포스트잇 떼기/붙이기는 <see cref="PostitDirector"/>가 하고, 이 코루틴은 그 사이에 끼워진다.)
    /// 코어 이벤트를 구독하지 않는다 — <see cref="CinematicTurnResultPresenter"/>가 심박수 구간이 바뀐 턴에만 <see cref="Play"/>를 넘겨 준다.
    ///
    /// 확대는 <see cref="CrtZoom"/>(CRT 후처리가 합쳐진 화면을 확대), 소리는 <see cref="UiSoundHooks.FocusHeartbeat"/>(심박수 배경음을 키우고 상시 배경음을 물린다)로 한다.
    /// 표시기 위치는 확대할 때마다 다시 잰다 — 창 크기가 바뀌어도 어긋나지 않는다. MainHud.prefab에 넣지 않고 처음 필요할 때 캔버스 아래에 짓는다(PostitDirector와 같은 방식).
    /// </summary>
    public sealed class HeartbeatFocusDirector : MonoBehaviour
    {
        private Transform _canvasRoot;
        private RectTransform _monitor;
        private CrtZoom _zoom;

        public static HeartbeatFocusDirector GetOrCreate(Transform canvasRoot)
        {
            var existing = canvasRoot.GetComponentInChildren<HeartbeatFocusDirector>(true);
            if (existing != null) return existing;

            var go = new GameObject("Heartbeat Focus Director", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var director = go.AddComponent<HeartbeatFocusDirector>();
            director._canvasRoot = canvasRoot;
            return director;
        }

        /// <summary>확대 → 소리가 앞으로 나온 채 머묾 → 잦아들며 복귀. 이 코루틴이 끝나면 화면은 원래대로다.</summary>
        public IEnumerator Play()
        {
            var settings = UiMotion.Settings;
            var zoom = Zoom;

            var focus = new Vector2(0.5f, 0.5f);
            var factor = settings.heartFocusZoomRange.x;
            TryMeasureMonitor(settings, ref focus, ref factor);

            // 2) 카메라가 심박수 표시기로 확대된다 — 소리도 이때부터 앞으로 올라온다.
            UiSoundHooks.FocusHeartbeat(1f, settings.heartFocusZoomIn);
            yield return Wait(zoom.ZoomIn(focus, factor, settings.heartFocusZoomIn), settings.heartFocusZoomIn);

            // 3) 심박수 소리가 2초간 출력된다.
            yield return new WaitForSecondsRealtime(settings.heartFocusHold);

            // 4) 소리가 잦아들며 다시 원래 화면으로 돌아온다.
            UiSoundHooks.FocusHeartbeat(0f, settings.heartFocusZoomOut);
            yield return Wait(zoom.ZoomOut(settings.heartFocusZoomOut), settings.heartFocusZoomOut);
        }

        /// <summary>진행 중인 연출을 멈추고 화면을 원래 크기로, 소리를 평소로 되돌린다(재시작).</summary>
        public void ResetAll()
        {
            _zoom?.ResetNow();
            UiSoundHooks.FocusHeartbeat(0f, 0f);
        }

        private void OnDisable() => ResetAll();

        private CrtZoom Zoom => _zoom ??= new CrtZoom(FindFirstObjectByType<CrtEffectDriver>()?.CrtMaterial);

        /// <summary>CRT가 없어 확대할 수 없어도(빈 트윈) 소리와 시간은 그대로 흐르게 — 트윈이 없으면 그 시간만큼 기다린다.</summary>
        private IEnumerator Wait(Tween tween, float fallbackSeconds)
        {
            if (!Zoom.IsAvailable) yield return new WaitForSecondsRealtime(fallbackSeconds);
            else if (tween.IsActive() && !tween.IsComplete()) yield return tween.WaitForCompletion(true);
        }

        /// <summary>심박수 모니터 패널의 화면 위치(0~1)와, 그것이 화면의 <c>heartFocusFill</c>만큼 차도록 하는 배율. 못 재면 화면 중앙·하한 배율을 그대로 둔다.</summary>
        private void TryMeasureMonitor(UiMotionSettings settings, ref Vector2 focus, ref float factor)
        {
            if (_monitor == null) _monitor = _canvasRoot.GetComponentInChildren<HeartRateIndicatorPanel>(true)?.Root;
            if (_monitor == null) return;

            var canvas = _monitor.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (camera == null && (Screen.width <= 0 || Screen.height <= 0)) return;

            var corners = new Vector3[4];
            _monitor.GetWorldCorners(corners);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                var uv = ToScreenUv(camera, corner);
                min = Vector2.Min(min, uv);
                max = Vector2.Max(max, uv);
            }

            focus = (min + max) * 0.5f;
            var largest = Mathf.Max(max.x - min.x, max.y - min.y, 0.01f);
            factor = Mathf.Clamp(settings.heartFocusFill / largest, settings.heartFocusZoomRange.x, settings.heartFocusZoomRange.y);
        }

        /// <summary>월드 좌표의 화면 위치(0~1). UI 카메라가 있으면 그 뷰포트로 — UI가 그려지는 RT_UI가 곧 화면 전체라 뷰포트 비율이 그대로 CRT가 읽는 좌표다.</summary>
        private static Vector2 ToScreenUv(Camera camera, Vector3 world)
        {
            if (camera != null) return camera.WorldToViewportPoint(world);

            var screen = RectTransformUtility.WorldToScreenPoint(null, world);
            return new Vector2(screen.x / Screen.width, screen.y / Screen.height);
        }
    }
}
