using System.Collections;
using BlueComplex.Core.Stage;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 엔딩 전체: 게임 화면이 검게 덮인다 → 컷신 9 "붙잡히는 유키"(그림 없이 타이핑되는 글) → 에필로그(노션 "엔딩" 문단을 클릭으로 한 문단씩) →
    /// 크레딧(위로 스크롤) → 검은 막이 걷혀 게임 화면으로 돌아온다. 전부 같은 검은 바탕 위라 끊김 없이 이어진다.
    ///
    /// 이 클래스는 순서와 시간만 정한다. 글 화면은 <see cref="EndingTextView"/>, 크레딧은 <see cref="EndingCreditsView"/>, 글은 <see cref="EndingContent"/>.
    /// 정식 진행 흐름(스테이지 3 클리어 뒤)에는 아직 연결하지 않았고 <see cref="Bootstrap.StageBootstrapper"/>의 디버그 키(F5)로만 재생한다.
    /// 분기 대사 장면(<see cref="BranchSceneDirector"/>)과 같은 "게임 UI 없이 배경+글, 클릭으로 진행"이지만, 검은 배경이고 문단이 길어
    /// (대사 오버레이의 세 줄짜리 자리에 안 들어간다) 글 화면을 따로 둔다.
    /// </summary>
    public sealed class EndingCutsceneDirector : MonoBehaviour
    {
        private const float FadeSeconds = 0.8f;
        private const float BeatSeconds = 0.5f;

        private Transform _canvasRoot;
        private GameObject _overlay;

        public static EndingCutsceneDirector GetOrCreate(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;

            var existing = canvasRoot.GetComponentInChildren<EndingCutsceneDirector>(true);
            if (existing != null) return existing;

            var go = new GameObject("Ending Cutscene Director", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var director = go.AddComponent<EndingCutsceneDirector>();
            director._canvasRoot = canvasRoot;
            return director;
        }

        public static EndingCutsceneDirector Find(Transform canvasRoot) =>
            canvasRoot != null ? canvasRoot.GetComponentInChildren<EndingCutsceneDirector>(true) : null;

        /// <summary>엔딩 전체를 재생하고, 끝나면 막을 걷어 게임 화면으로 돌아온다.</summary>
        public IEnumerator Play()
        {
            ResetNow();
            var font = _canvasRoot.GetComponentInChildren<TMP_Text>(true)?.font;
            var backdrop = BuildOverlay(font, out var text, out var credits);

            // 컷신 9: 게임 화면이 검게 덮이고, 그 위에서 글이 타이핑된다.
            yield return backdrop.DOFade(1f, FadeSeconds).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
            yield return new WaitForSecondsRealtime(BeatSeconds);
            yield return text.PlayParagraph(EndingContent.CaptureText);

            // 에필로그: 같은 검은 바탕에서 문단마다 클릭.
            yield return new WaitForSecondsRealtime(BeatSeconds);
            foreach (var paragraph in EndingContent.EpilogueParagraphs)
                yield return text.PlayParagraph(paragraph);

            // 크레딧.
            yield return new WaitForSecondsRealtime(BeatSeconds);
            yield return credits.Play();
            yield return new WaitForSecondsRealtime(BeatSeconds);

            yield return backdrop.DOFade(0f, FadeSeconds).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
            ResetNow();
        }

        /// <summary>진행 중인 엔딩을 멈추고 막을 치운다(재시작·다른 스테이지 선택·엔딩 재생 중 다시 재생).</summary>
        public void ResetNow()
        {
            DOTween.Kill(this);
            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }
        }

        private void OnDestroy() => DOTween.Kill(this);

        /// <summary>게임 화면 전체를 덮는 막(클릭도 막는다) 아래 검은 바탕과 글·크레딧 화면을 짓는다. 돌려주는 것은 검은 바탕 — 알파 0에서 시작한다.</summary>
        private Image BuildOverlay(TMP_FontAsset font, out EndingTextView text, out EndingCreditsView credits)
        {
            var root = new GameObject("Ending Cutscene", typeof(RectTransform), typeof(Image)) { layer = _canvasRoot.gameObject.layer };
            root.transform.SetParent(_canvasRoot, false);
            root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var blocker = root.GetComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true; // 엔딩 동안 뒤 화면의 클릭을 막는다.
            _overlay = root;

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)) { layer = root.layer };
            backdropGo.transform.SetParent(root.transform, false);
            Stretch((RectTransform)backdropGo.transform);
            var backdrop = backdropGo.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0f);
            backdrop.raycastTarget = false;

            text = EndingTextView.Create(root.transform, font);
            credits = EndingCreditsView.Create(root.transform, font);
            return backdrop;
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
