using System.Collections;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Layout;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 엔딩 전체: 게임 화면이 검게 덮인다 → 컷신 9 "붙잡히는 유키"(검은 바탕에 그림 없이 타이핑되는 글) → 에필로그(노션 "엔딩" 문단을 클릭으로 한 문단씩) →
    /// 크레딧(위로 스크롤) → 검은 막이 걷힌다(정식 흐름에서는 이어서 메인 화면으로 돌아간다 — StageEndController).
    /// 에필로그는 나츠의 1인칭 독백이라 메인 화면 배경(취조실 InterrogationRoom, 마우스 패럴랙스 포함, 버튼은 숨김) 위에서 진행한다 — 마지막 줄
    /// "나는 커피 한 잔과 함께 시간을 죽였다"가 커피가 놓인 이 배경과 맞는다. 컷신 9와 크레딧은 검은 바탕 그대로다.
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

        private const string MenuPrefabPath = "UI/MainMenu";

        private Transform _canvasRoot;
        private GameObject _overlay;

        /// <summary>엔딩이 화면을 덮고 있는 동안 참 — 태그 레이어(열쇠 아이콘 등, UiTagLayer)가 엔딩 위로 비치지 않게 가린다.</summary>
        public static bool AnyPlaying { get; private set; }

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
            AnyPlaying = true;
            var font = RuntimeUi.GameFont;
            var backdrop = BuildOverlay(font, out var text, out var credits, out var room);

            // 컷신 9: 게임 화면이 검게 덮이고, 그 위에서 글이 타이핑된다.
            yield return backdrop.DOFade(1f, FadeSeconds).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
            yield return new WaitForSecondsRealtime(BeatSeconds);
            yield return text.PlayParagraph(EndingContent.CaptureText);

            // 에필로그: 검은 바탕이 걷히며 메인 화면 배경(취조실)이 드러나고, 그 위에서 나츠의 독백을 문단마다 클릭.
            yield return new WaitForSecondsRealtime(BeatSeconds);
            if (room != null)
            {
                room.SetActive(true);
                text.SetMonologueLayout(true);
                yield return backdrop.DOFade(0f, FadeSeconds * 1.5f).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
                yield return new WaitForSecondsRealtime(BeatSeconds);
            }

            foreach (var paragraph in EndingContent.EpilogueParagraphs)
                yield return text.PlayParagraph(paragraph);

            // 크레딧: 다시 검은 바탕.
            yield return new WaitForSecondsRealtime(BeatSeconds);
            if (room != null)
            {
                yield return backdrop.DOFade(1f, FadeSeconds * 1.5f).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
                room.SetActive(false);
                text.SetMonologueLayout(false);
            }
            yield return credits.Play();
            yield return new WaitForSecondsRealtime(BeatSeconds);

            yield return backdrop.DOFade(0f, FadeSeconds).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
            ResetNow();
        }

        /// <summary>진행 중인 엔딩을 멈추고 막을 치운다(재시작·다른 스테이지 선택·엔딩 재생 중 다시 재생).</summary>
        public void ResetNow()
        {
            AnyPlaying = false;
            DOTween.Kill(this);
            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (_overlay != null) AnyPlaying = false;
        }

        /// <summary>게임 화면 전체를 덮는 막(클릭도 막는다) 아래 메인 화면 배경(꺼진 채)·검은 바탕·글·크레딧 화면을 짓는다. 돌려주는 것은 검은 바탕 — 알파 0에서 시작한다.</summary>
        private Image BuildOverlay(TMP_FontAsset font, out EndingTextView text, out EndingCreditsView credits, out GameObject room)
        {
            var root = new GameObject("Ending Cutscene", typeof(RectTransform), typeof(Image)) { layer = _canvasRoot.gameObject.layer };
            root.transform.SetParent(_canvasRoot, false);
            root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var blocker = root.GetComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true; // 엔딩 동안 뒤 화면의 클릭을 막는다.
            _overlay = root;

            room = BuildRoom(root.transform);

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

        /// <summary>메인 화면 프리팹(Resources/UI/MainMenu)을 배경으로만 띄운다 — 버튼 판과 제목 판은 끄고, 배경·패럴랙스는 그대로. 메인 화면과 같이 화면 가운데 16:9 틀에 앉힌다.
        /// 오프닝의 메인 화면(<see cref="MainMenuView"/>)과는 따로 만든 인스턴스라 버튼 콜백이 없다. 프리팹이 없으면 null — 에필로그는 검은 바탕에서 진행된다.</summary>
        private static GameObject BuildRoom(Transform parent)
        {
            var prefab = Resources.Load<GameObject>(MenuPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[EndingCutsceneDirector] 메인 화면 프리팹(Resources/{MenuPrefabPath})이 없다 — 에필로그를 검은 바탕에서 진행한다.");
                return null;
            }

            Canvas.ForceUpdateCanvases();
            var canvasSize = ((RectTransform)parent).rect.size;
            var slide = IntroSlide.Create(parent, canvasSize);
            slide.Root.name = "Epilogue Room";

            var instance = Instantiate(prefab, slide.Root, false);
            instance.name = prefab.name;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = parent.gameObject.layer;
            var buttons = instance.transform.Find("Buttons");
            if (buttons != null) buttons.gameObject.SetActive(false);
            var title = instance.transform.Find("Title"); // 제목 판("Blue Complex"·회사명·테두리)도 숨긴다 — 에필로그 동안은 배경 그림만.
            if (title != null) title.gameObject.SetActive(false);
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false; // 클릭은 글 화면이 받는다.

            slide.Root.gameObject.SetActive(false); // 에필로그가 시작될 때 켠다.
            return slide.Root.gameObject;
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
