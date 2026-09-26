using System.Collections;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 게임 시작 컷신(PPT 애니마틱): 전부 검은 화면 위에서 ① 나이프 낙하 → ② 눈(감김 → 뜨임 → 동공으로 확대) → ③ 짧은 암전 → ④ TV 뉴스 자막 3줄 →
    /// ⑤ 화면이 밝아지며 경찰서(InterrogationRoom.png — 그림 속에 나츠가 앉아 있다)가 드러난다.
    /// <see cref="Play"/>는 여기까지이고 경찰서 그림은 <b>화면에 남는다</b> — 튜토리얼 오프닝 대화가 그 그림 위에서 이어지고(<see cref="HoldsRoom"/>),
    /// 대화가 끝나면 부른 쪽이 <see cref="Dismiss"/>로 게임 화면으로 걷는다.
    ///
    /// 이 클래스는 순서와 시간만 정한다. 화면 조각은 <see cref="IntroKnifeView"/>·<see cref="IntroEyeView"/>·<see cref="IntroNewsView"/>가 그리고(그림은 <see cref="IntroCutsceneArt"/>),
    /// 시간 값은 <see cref="UiMotionSettings"/>의 "시작 컷신" 항목(인스펙터에서 조정), 뉴스 문구는 <see cref="IntroCutsceneContent"/>에 있다.
    /// <see cref="StageFlowHooks.PlayTutorialIntro"/>에 연결되어 튜토리얼 세션이 만들어진 뒤 첫 턴 전에 한 번 재생된다.
    /// </summary>
    public sealed class IntroCutsceneDirector : MonoBehaviour
    {
        private Transform _canvasRoot;
        private GameObject _overlay;
        private CanvasGroup _room;
        private Image _backdrop;

        public static IntroCutsceneDirector GetOrCreate(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;

            var existing = canvasRoot.GetComponentInChildren<IntroCutsceneDirector>(true);
            if (existing != null) return existing;

            var go = new GameObject("Intro Cutscene Director", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var director = go.AddComponent<IntroCutsceneDirector>();
            director._canvasRoot = canvasRoot;
            return director;
        }

        public static IntroCutsceneDirector Find(Transform canvasRoot) =>
            canvasRoot != null ? canvasRoot.GetComponentInChildren<IntroCutsceneDirector>(true) : null;

        /// <summary>경찰서 그림이 화면에 떠 있고 게임 화면으로 걷히기를(<see cref="Dismiss"/>) 기다리는 중인가. 이 동안 이어지는 대화는 그림 위에서 한다.</summary>
        public bool HoldsRoom { get; private set; }

        /// <summary>컷신 전체(경찰서가 밝아지고 잠시 머무는 데까지). 끝나도 막은 치워지지 않는다 — 경찰서 그림이 남고 <see cref="HoldsRoom"/>이 켜진다(그림이 없으면 막은 바로 걷힌다).</summary>
        public IEnumerator Play()
        {
            var settings = UiMotion.Settings;
            var canvasSize = ((RectTransform)_canvasRoot).rect.size;

            ResetNow();
            var backdrop = BuildOverlay(canvasSize, out var knife, out var eye, out var news, out var room);

            // ① 나이프: 검게 덮이는 것과 낙하가 겹쳐 흐른다.
            backdrop.DOFade(1f, settings.introFadeIn).SetUpdate(true).SetTarget(backdrop);
            yield return knife.Fall(settings.introKnifeFall, settings.introKnifeSpin).WaitForCompletion(true);

            // ② 눈: 감긴 눈 → 뜨임 → 동공 확대. 눈 그림이 없으면 같은 시간만 검은 화면으로 흘린다.
            if (eye != null)
            {
                yield return eye.PlayClosed(settings.introEyeClosed);
                yield return eye.PlayOpening(settings.introEyeOpen);
                yield return eye.PlayZoom(settings.introEyeZoom);
                eye.Hide();
            }
            else
            {
                yield return new WaitForSecondsRealtime(settings.introEyeClosed + settings.introEyeOpen + settings.introEyeZoom);
            }

            // ③ 암전.
            yield return new WaitForSecondsRealtime(settings.introBlackout);

            // ④ 뉴스.
            yield return news.Play(IntroCutsceneContent.NewsLines, settings.introNewsLine);
            news.Hide();

            // ⑤ 밝아지며 경찰서 → 잠시 머묾. 게임 화면으로 걷는 것은 Dismiss(부른 쪽이 대화가 끝난 뒤 부른다).
            if (room != null)
            {
                yield return room.DOFade(1f, Mathf.Max(0.01f, settings.introReveal)).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
                yield return new WaitForSecondsRealtime(settings.introRoomHold);
            }

            _room = room;
            _backdrop = backdrop;
            if (room == null) yield return Dismiss(); // 그림이 없으면 남길 것이 없다 — 바로 게임 화면으로.
            else HoldsRoom = true;
        }

        /// <summary>경찰서 그림을 걷어 게임 화면으로 넘어간다. 막이 없으면(이미 걷혔거나 재시작) 아무 일도 안 한다.</summary>
        public IEnumerator Dismiss()
        {
            if (_overlay == null) yield break;

            var roomOut = Mathf.Max(0.01f, UiMotion.Settings.introRoomOut);
            if (_room != null) _room.DOFade(0f, roomOut).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
            if (_backdrop != null) yield return _backdrop.DOFade(0f, roomOut).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);

            ResetNow();
        }

        /// <summary>진행 중인 컷신을 멈추고 막을 치운다(재시작·컷신 도중 세션 교체).</summary>
        public void ResetNow()
        {
            DOTween.Kill(this);
            HoldsRoom = false;
            _room = null;
            _backdrop = null;

            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }
        }

        private void OnDestroy() => DOTween.Kill(this);

        /// <summary>게임 화면 전체를 덮는 막(클릭도 막는다)과 그 위의 세 조각을 짓는다. 돌려주는 것은 검은 바탕 — 알파 0에서 시작한다.</summary>
        private Image BuildOverlay(Vector2 canvasSize, out IntroKnifeView knife, out IntroEyeView eye, out IntroNewsView news, out CanvasGroup room)
        {
            var root = new GameObject("Intro Cutscene", typeof(RectTransform), typeof(Image)) { layer = _canvasRoot.gameObject.layer };
            root.transform.SetParent(_canvasRoot, false);
            root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var blocker = root.GetComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true; // 컷신 동안 뒤 화면의 클릭을 막는다.
            _overlay = root;

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)) { layer = root.layer };
            backdropGo.transform.SetParent(root.transform, false);
            Stretch((RectTransform)backdropGo.transform);
            var backdrop = backdropGo.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0f);
            backdrop.raycastTarget = false;

            var art = IntroCutsceneArt.Load();
            var font = _canvasRoot.GetComponentInChildren<TMP_Text>(true)?.font;
            room = BuildRoom(root.transform, art);
            knife = IntroKnifeView.Create(root.transform, canvasSize, UiMotion.Settings.introKnifeSize);
            eye = IntroEyeView.Create(root.transform, canvasSize, art);
            news = IntroNewsView.Create(root.transform, canvasSize, font);
            return backdrop;
        }

        /// <summary>경찰서(취조실) 그림 — 검은 바탕 위, 나이프·눈·뉴스 아래 층. 알파 0에서 시작한다. 그림이 없으면 null(밝아지며 게임 화면이 바로 드러난다).</summary>
        private static CanvasGroup BuildRoom(Transform parent, IntroCutsceneArt art)
        {
            if (art == null || art.room == null) return null;

            var go = new GameObject("Room", typeof(RectTransform), typeof(CanvasGroup), typeof(RawImage)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);

            var raw = go.GetComponent<RawImage>();
            raw.texture = art.room;
            raw.raycastTarget = false;

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
