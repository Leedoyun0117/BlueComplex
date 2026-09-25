using System;
using System.Collections;
using System.Collections.Generic;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 턴이 넘어갈 때 두 포스트잇(컴플렉스, 대화)을 떼었다 붙이는 연출의 진행자. 코어 이벤트를 구독하지 않는다 —
    /// <see cref="CinematicTurnResultPresenter"/>가 턴 결과 연출이 끝난 뒤 <see cref="PlayRefresh"/>를 불러야만 움직인다.
    ///
    /// 일반 턴: 두 포스트잇이 0.1초 시차로 떼어짐 → 글자 쓰는 소리(그 사이 내용 갱신) → 갱신된 포스트잇이 붙음.
    /// 키 턴: 포스트잇이 떼어지며 화면이 어두워짐 → 나츠 독백 → 화면이 다시 밝아지며 포스트잇이 붙음.
    /// 심박수 변화: 포스트잇이 떼어짐 → (<see cref="HeartbeatFocusDirector"/>: 표시기로 확대 → 심박수 소리 → 잦아들며 복귀) → 새 정보의 포스트잇이 붙음.
    ///
    /// 두 포스트잇의 위치는 이미 잡혀 있고(컴플렉스: 프리팹, 대화: QuarterHud) 이 클래스는 그 <see cref="Postit"/>만 찾아 움직인다.
    /// MainHud.prefab에 넣지 않고 처음 필요할 때 캔버스 아래에 짓는다(QuarterHud와 같은 방식).
    /// </summary>
    public sealed class PostitDirector : MonoBehaviour
    {
        private Transform _canvasRoot;
        private QuarterHud _hud;
        private ComplexStatusController _status;
        private KeyTurnOverlay _overlay;

        public static PostitDirector GetOrCreate(Transform canvasRoot)
        {
            var existing = canvasRoot.GetComponentInChildren<PostitDirector>(true);
            if (existing != null) return existing;

            var go = new GameObject("Postit Director", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var director = go.AddComponent<PostitDirector>();
            director._canvasRoot = canvasRoot;
            return director;
        }

        /// <summary>
        /// 포스트잇을 떼고, 그 사이에 <paramref name="refreshContent"/>로 내용을 갱신하고, 다시 붙인다. 이 코루틴이 끝나면 입력이 가능하다.
        /// <paramref name="monologue"/>가 비어 있지 않으면 키 턴 연출(암전 + 독백)이다.
        /// <paramref name="whilePeeled"/>가 있으면 심박수 변화 연출이다 — 포스트잇이 떼어진 사이에 그 코루틴(카메라 확대 등)이 끼고, 글자 쓰는 소리는 없다
        /// (내용은 떼어진 직후 고쳐 두므로, 확대된 화면에 이미 새 정보가 보인다). 키 턴과 겹치면 확대가 끝난 뒤에 암전이 시작된다 — 확대 화면이 어두워지지 않게.
        /// </summary>
        public IEnumerator PlayRefresh(string speaker, string monologue, Action refreshContent, IEnumerator whilePeeled = null)
        {
            var settings = UiMotion.Settings;
            var postits = FindPostits();
            var keyTurn = !string.IsNullOrEmpty(monologue);
            var focused = whilePeeled != null;

            var dim = keyTurn && !focused ? Overlay.FadeIn() : null;

            yield return Staggered(postits, postit => postit.Peel(), settings.postitStagger);
            if (dim != null && dim.IsActive()) yield return dim.WaitForCompletion(true);

            if (focused)
            {
                refreshContent();
                yield return whilePeeled;

                if (keyTurn)
                {
                    dim = Overlay.FadeIn();
                    if (dim.IsActive()) yield return dim.WaitForCompletion(true);
                }
            }

            if (keyTurn)
            {
                if (!focused) refreshContent();
                yield return Overlay.PlayMonologue(speaker, monologue);
            }
            else if (!focused)
            {
                UiSoundHooks.Play(UiSoundCue.Write);
                refreshContent();
                yield return new WaitForSecondsRealtime(settings.postitWrite);
            }

            var bright = keyTurn ? Overlay.FadeOut() : null;

            yield return Staggered(postits, postit => postit.Stick(), settings.postitStagger);
            if (bright != null && bright.IsActive()) yield return bright.WaitForCompletion(true);
        }

        /// <summary>진행 중인 연출을 멈추고 포스트잇을 붙은 상태로, 화면을 밝은 상태로 되돌린다(재시작).</summary>
        public void ResetAll()
        {
            foreach (var postit in FindPostits()) postit.SnapAttached();
            if (_overlay != null) _overlay.ResetNow();
        }

        /// <summary>시차를 두고 차례로 시작하고, 전부 끝날 때까지 기다린다. 컴플렉스가 먼저, 대화가 0.1초 뒤다.</summary>
        private static IEnumerator Staggered(IReadOnlyList<Postit> postits, Func<Postit, Sequence> start, float stagger)
        {
            var running = new List<Sequence>(postits.Count);
            for (var i = 0; i < postits.Count; i++)
            {
                if (i > 0) yield return new WaitForSecondsRealtime(stagger);
                running.Add(start(postits[i]));
            }

            foreach (var sequence in running)
                if (sequence.IsActive() && !sequence.IsComplete())
                    yield return sequence.WaitForCompletion(true);
        }

        private KeyTurnOverlay Overlay =>
            _overlay != null ? _overlay : _overlay = KeyTurnOverlay.Create(_canvasRoot, RuntimeFont());

        private TMPro.TMP_FontAsset RuntimeFont() => _canvasRoot.GetComponentInChildren<TMPro.TMP_Text>(true)?.font;

        /// <summary>떼고 붙일 포스트잇들(컴플렉스 → 대화 순). 아직 없는 것(그 HUD가 없는 씬)은 건너뛴다.</summary>
        private List<Postit> FindPostits()
        {
            if (_hud == null) _hud = QuarterHud.GetOrCreate(_canvasRoot);
            if (_status == null) _status = _canvasRoot.GetComponentInChildren<ComplexStatusController>(true);

            var result = new List<Postit>(2);
            if (_status != null && _status.Postit != null) result.Add(_status.Postit);
            if (_hud.ProgressPostit != null) result.Add(_hud.ProgressPostit);
            return result;
        }
    }
}
