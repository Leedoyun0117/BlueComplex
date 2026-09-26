using System.Collections;
using System.Linq;
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
    /// ⑤ 화면이 밝아지며 경찰서(게임 화면의 배경)가 드러나고 나츠가 나타난다. 그 뒤는 부른 쪽(튜토리얼 시작 대사)이 이어 간다.
    ///
    /// 이 클래스는 순서와 시간만 정한다. 화면 조각은 <see cref="IntroKnifeView"/>·<see cref="IntroEyeView"/>·<see cref="IntroNewsView"/>가 그리고,
    /// 시간 값은 <see cref="UiMotionSettings"/>의 "시작 컷신" 항목(인스펙터에서 조정), 뉴스 문구는 <see cref="IntroCutsceneContent"/>에 있다.
    /// <see cref="StageFlowHooks.PlayTutorialIntro"/>에 연결되어 튜토리얼 세션이 만들어진 뒤 첫 턴 전에 한 번 재생된다.
    /// </summary>
    public sealed class IntroCutsceneDirector : MonoBehaviour
    {
        /// <summary>경찰서 배경이 드러나기 시작한 뒤 나츠가 나타나기 시작하는 지점(밝아짐 시간에 대한 비율).</summary>
        private const float NatsuEntersAt = 0.45f;

        private Transform _canvasRoot;
        private GameObject _overlay;
        private CanvasGroup _natsu;
        private float _natsuOriginalAlpha = 1f;

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

        /// <summary>컷신 전체. 끝나면 막이 치워지고 화면은 밝은 상태(게임 화면)다.</summary>
        public IEnumerator Play()
        {
            var settings = UiMotion.Settings;
            var canvasSize = ((RectTransform)_canvasRoot).rect.size;

            ResetNow();
            var backdrop = BuildOverlay(canvasSize, out var knife, out var eye, out var news);
            HideNatsu();

            // ① 나이프: 검게 덮이는 것과 낙하가 겹쳐 흐른다.
            backdrop.DOFade(1f, settings.introFadeIn).SetUpdate(true).SetTarget(backdrop);
            yield return knife.Fall(settings.introKnifeFall, settings.introKnifeSpin).WaitForCompletion(true);

            // ② 눈: 감긴 눈 → 뜨임 → 동공 확대.
            yield return eye.PlayClosed(settings.introEyeClosed);
            yield return eye.PlayOpening(settings.introEyeOpen);
            yield return eye.PlayZoom(settings.introEyeZoom);
            eye.Hide();

            // ③ 암전.
            yield return new WaitForSecondsRealtime(settings.introBlackout);

            // ④ 뉴스.
            yield return news.Play(IntroCutsceneContent.NewsLines, settings.introNewsLine);
            news.Hide();

            // ⑤ 밝아지며 경찰서 배경 → 나츠 등장.
            var reveal = Mathf.Max(0.01f, settings.introReveal);
            backdrop.DOFade(0f, reveal).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(backdrop);
            yield return new WaitForSecondsRealtime(reveal * NatsuEntersAt);
            yield return ShowNatsu(reveal * (1f - NatsuEntersAt));

            ResetNow();
        }

        /// <summary>진행 중인 컷신을 멈추고 막을 치운다. 나츠도 원래대로 돌려놓는다(재시작·컷신 도중 세션 교체).</summary>
        public void ResetNow()
        {
            DOTween.Kill(this);

            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }

            RestoreNatsu();
        }

        private void OnDestroy() => DOTween.Kill(this);

        /// <summary>게임 화면 전체를 덮는 막(클릭도 막는다)과 그 위의 세 조각을 짓는다. 돌려주는 것은 검은 바탕 — 알파 0에서 시작한다.</summary>
        private Image BuildOverlay(Vector2 canvasSize, out IntroKnifeView knife, out IntroEyeView eye, out IntroNewsView news)
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

            var font = _canvasRoot.GetComponentInChildren<TMP_Text>(true)?.font;
            knife = IntroKnifeView.Create(root.transform, canvasSize, UiMotion.Settings.introKnifeSize);
            eye = IntroEyeView.Create(root.transform, canvasSize);
            news = IntroNewsView.Create(root.transform, canvasSize, font);
            return backdrop;
        }

        /// <summary>"Natsu Portrait"(MainHud)를 투명하게 해 두었다가 <see cref="ShowNatsu"/>로 나타나게 한다. 원래 알파는 기억해 뒀다 돌려준다.</summary>
        private void HideNatsu()
        {
            var natsu = _canvasRoot.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Natsu Portrait");
            if (natsu == null) return;

            _natsu = natsu.GetComponent<CanvasGroup>();
            if (_natsu == null) _natsu = natsu.gameObject.AddComponent<CanvasGroup>();

            _natsuOriginalAlpha = _natsu.alpha;
            _natsu.alpha = 0f;
        }

        private IEnumerator ShowNatsu(float seconds)
        {
            if (_natsu == null) yield break;

            yield return _natsu.DOFade(_natsuOriginalAlpha, Mathf.Max(0.01f, seconds)).SetEase(Ease.OutSine).SetUpdate(true).SetTarget(this)
                .WaitForCompletion(true);
        }

        private void RestoreNatsu()
        {
            if (_natsu == null) return;

            _natsu.alpha = _natsuOriginalAlpha;
            _natsu = null;
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
