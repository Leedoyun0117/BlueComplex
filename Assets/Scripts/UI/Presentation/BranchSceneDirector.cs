using System.Collections;
using System.Collections.Generic;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// "게임 장면" ↔ "분기 대사 장면" 전환. 게임 장면은 카드 트레이·인디케이터·아이템·HUD가 다 떠 있는 평소 화면이고,
    /// 분기 대사 장면은 매 쿼터(분기)가 끝날 때 게임 UI가 전부 빠지고 배경과 대사 글자만 남는 화면이다.
    ///
    /// 들어가는 연출(노션 "UI 연출 ▸ UI 빠져나가기 연출"): 초상화가 각자의 이름이 적힌 포스트잇으로 유키 → 나츠 순으로 덮이고,
    /// 덮인 채로 각자에게 가장 가까운 화면 가장자리(벽)로 빠져나간다. 그 뒤 나머지 게임 UI는 예전 그대로 캔버스 전체 알파 페이드로 빠진다.
    /// 돌아올 땐 반대로: 게임 UI가 페이드 인 되는 동안 초상화가 덮인 채 제자리로 돌아오고, 두 포스트잇이 함께 떼어진다(복귀 순서는 노션에 없어 동시).
    /// 포스트잇 붙임·뗌 모션은 게임 안 포스트잇과 같은 <see cref="Postit"/>(<see cref="Postit.Stick"/>·<see cref="Postit.Peel"/>)을 그대로 쓴다.
    ///
    /// 나머지 게임 UI는 최상위 캔버스의 <see cref="CanvasGroup"/> 하나로 한꺼번에 투명하게 만든다 — 나중에 생기는 패널도 저절로 따라오고,
    /// 안 보이는 동안 클릭도 받지 않는다. 대사 오버레이는 자기 중첩 캔버스를 가져 혼자 남는다.
    /// 대사 재생은 <see cref="StageDialoguePlayer"/>가 하고, 이 클래스는 언제 UI를 뺐다 되돌릴지만 정한다(대사 내용·시점은 <see cref="Bootstrap.StageBootstrapper"/>가 정한다).
    /// 처음 필요할 때 캔버스 아래에 짓는다(PostitDirector·StageDialoguePlayer와 같은 방식).
    /// </summary>
    public sealed class BranchSceneDirector : MonoBehaviour
    {
        /// <summary>덮는 순서 그대로다(유키가 먼저, 나츠가 나중). MainHud의 초상화 오브젝트 이름.</summary>
        private static readonly (string Object, string Label, float Tilt)[] Portraits =
        {
            ("Yuki Portrait", "유키", -2.5f),
            ("Natsu Portrait", "나츠", 2.5f),
        };

        /// <summary>포스트잇이 초상화 사각형보다 이만큼(비율) 더 크다 — 기울어도 모서리로 초상화가 삐져나오지 않게.</summary>
        private const float CoverPadding = 0.06f;

        /// <summary>벽 밖으로 나간 뒤 남기는 여유(캔버스 단위) — 그림자·외곽선·기울어진 종이 모서리까지 완전히 빠지게.</summary>
        private const float OffscreenMargin = 40f;

        private sealed class Cover
        {
            public RectTransform Portrait;
            public RectTransform Root;
            public Postit Postit;
            public string Label;
            public float Tilt;
            public Rect Bounds;
            public Vector2 Home;
            public Vector2 Away;
        }

        private Transform _canvasRoot;
        private CanvasGroup _gameUi;
        private readonly List<Cover> _covers = new();
        private Sequence _slide;
        private int _generation;

        public static BranchSceneDirector GetOrCreate(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;

            var existing = canvasRoot.GetComponentInChildren<BranchSceneDirector>(true);
            if (existing != null) return existing;

            var go = new GameObject("Branch Scene Director", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var director = go.AddComponent<BranchSceneDirector>();
            director._canvasRoot = canvasRoot;
            return director;
        }

        /// <summary>
        /// 초상화가 포스트잇에 덮여(유키 → 나츠) 벽으로 빠지고 → 나머지 게임 UI가 페이드로 빠지고 → 대사가 배경 위에서 재생되고 →
        /// 게임 UI가 돌아오며 초상화가 덮인 채 제자리로 오고 → 포스트잇이 떼어진다. 줄이 없으면 아무것도 안 한다.
        /// </summary>
        public IEnumerator Play(DialogueVariant variant)
        {
            if (variant.lines == null || variant.lines.Length == 0) yield break;

            ResetNow();
            var generation = _generation;
            var settings = UiMotion.Settings;
            BuildCovers();

            // 초상화가 덮인다: 유키 먼저, 그 직후 나츠.
            foreach (var cover in _covers)
            {
                AttachPostit(cover); // 붙이기 직전에 만든다 — 지어지는 순간 종이가 이미 붙은 모습이라, 미리 지으면 차례가 오기 전에 화면에 보인다.
                yield return cover.Postit.Stick().WaitForCompletion(true);
                if (generation != _generation) yield break;
            }

            // 덮인 채로 각자 가장 가까운 벽으로 빠진다. 나머지 UI의 페이드는 초상화가 반쯤 나갔을 때 시작한다.
            var exit = Mathf.Max(0.05f, settings.branchPortraitExit);
            var leaving = Slide(toAway: true, exit, settings.postitStagger);
            yield return new WaitForSecondsRealtime(exit * 0.5f);
            if (generation != _generation) yield break;

            var fade = settings.branchSceneFade;
            var hiding = FadeGameUi(0f, fade);
            if (leaving.IsActive() && !leaving.IsComplete()) yield return leaving.WaitForCompletion(true);
            if (hiding.IsActive() && !hiding.IsComplete()) yield return hiding.WaitForCompletion(true);
            if (generation != _generation) yield break;

            yield return StageDialoguePlayer.GetOrCreate(_canvasRoot).PlayOver(variant, null);
            if (generation != _generation) yield break;

            // 게임 UI가 돌아오는 동안 초상화도 덮인 채 제자리로 돌아오고, 도착하면 포스트잇이 떼어진다.
            var showing = FadeGameUi(1f, fade);
            var returning = Slide(toAway: false, exit, 0f);
            if (showing.IsActive() && !showing.IsComplete()) yield return showing.WaitForCompletion(true);
            if (returning.IsActive() && !returning.IsComplete()) yield return returning.WaitForCompletion(true);
            if (generation != _generation) yield break;

            var peeling = new List<Sequence>(_covers.Count);
            foreach (var cover in _covers) peeling.Add(cover.Postit.Peel());
            foreach (var peel in peeling)
                if (peel.IsActive() && !peel.IsComplete()) yield return peel.WaitForCompletion(true);
            if (generation != _generation) yield break;

            ClearCovers();
        }

        /// <summary>진행 중인 전환을 멈추고 초상화·게임 UI를 되돌린다(재시작). 게임 장면이 기본 상태다.</summary>
        public void ResetNow()
        {
            _generation++;
            _slide?.Kill();
            _slide = null;

            foreach (var cover in _covers)
                if (cover.Portrait != null) cover.Portrait.anchoredPosition = cover.Home;
            ClearCovers();

            if (_gameUi == null) return;

            _gameUi.DOKill();
            _gameUi.alpha = 1f;
            _gameUi.blocksRaycasts = true;
        }

        /// <summary>초상화마다 빠져나갈 벽 쪽 자리와 덮개 크기를 정한다(덮개 자체는 붙일 때 <see cref="AttachPostit"/>이 짓는다).</summary>
        private void BuildCovers()
        {
            ClearCovers();

            var canvas = (RectTransform)_canvasRoot;
            var screen = canvas.rect;
            var corners = new Vector3[4];

            foreach (var (objectName, label, tilt) in Portraits)
            {
                var portrait = FindPortrait(objectName);
                if (portrait == null || !portrait.gameObject.activeInHierarchy) continue;

                var bounds = ToCanvasRect(canvas, portrait, corners);
                _covers.Add(new Cover
                {
                    Portrait = portrait,
                    Label = label,
                    Tilt = tilt,
                    Bounds = bounds,
                    Home = portrait.anchoredPosition,
                    Away = portrait.anchoredPosition + AwayShift(screen, bounds),
                });
            }
        }

        /// <summary>초상화 위에 이름 포스트잇을 짓는다. 초상화의 자식이라 초상화와 함께 움직인다.</summary>
        private void AttachPostit(Cover cover)
        {
            var canvas = (RectTransform)_canvasRoot;
            var pad = new Vector2(cover.Bounds.width, cover.Bounds.height) * CoverPadding * (canvas.lossyScale.x / cover.Portrait.lossyScale.x);
            var root = RuntimeUi.CreateStretched(cover.Portrait, "Branch Cover");
            root.offsetMin = -pad;
            root.offsetMax = pad;

            var postit = Postit.Attach(root.gameObject);
            postit.SetRestTilt(cover.Tilt);
            var font = PostitStyle.HandFont != null ? PostitStyle.HandFont : RuntimeUi.FindFont(_canvasRoot);
            BuildLabel(postit, cover.Label, Mathf.Min(cover.Bounds.width, cover.Bounds.height) * 0.3f, font);

            cover.Root = root;
            cover.Postit = postit;
        }

        private RectTransform FindPortrait(string objectName)
        {
            foreach (var rect in _canvasRoot.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == objectName) return rect;

            return null;
        }

        private static void BuildLabel(Postit postit, string text, float fontSize, TMP_FontAsset font)
        {
            var rect = RuntimeUi.CreateStretched(postit.Content, "Name");
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = PostitStyle.Ink;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
        }

        private void ClearCovers()
        {
            foreach (var cover in _covers)
                if (cover.Root != null) Destroy(cover.Root.gameObject);

            _covers.Clear();
        }

        /// <summary>초상화 전부를 <paramref name="toAway"/>면 벽 밖 자리로, 아니면 제자리로 옮기는 트윈. 나갈 땐 가속하고 돌아올 땐 감속하며 자리 잡는다.</summary>
        private Sequence Slide(bool toAway, float duration, float stagger)
        {
            _slide?.Kill();
            _slide = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (var i = 0; i < _covers.Count; i++)
            {
                var cover = _covers[i];
                var target = toAway ? cover.Away : cover.Home;
                _slide.Insert(i * stagger, cover.Portrait.DOAnchorPos(target, duration).SetEase(toAway ? Ease.InCubic : Ease.OutCubic));
            }

            return _slide;
        }

        /// <summary>초상화가 벽 밖으로 완전히 빠지려면 얼마나 움직여야 하는지. 방향은 초상화 중심에서 화면 가장자리까지의 (화면 크기 대비) 거리가 가장 짧은 쪽이다.</summary>
        private static Vector2 AwayShift(Rect screen, Rect bounds)
        {
            var center = bounds.center;
            var left = (center.x - screen.xMin) / screen.width;
            var right = (screen.xMax - center.x) / screen.width;
            var bottom = (center.y - screen.yMin) / screen.height;
            var top = (screen.yMax - center.y) / screen.height;

            var nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));
            if (nearest == left) return new Vector2(-(bounds.xMax - screen.xMin) - OffscreenMargin, 0f);
            if (nearest == right) return new Vector2(screen.xMax - bounds.xMin + OffscreenMargin, 0f);
            if (nearest == bottom) return new Vector2(0f, -(bounds.yMax - screen.yMin) - OffscreenMargin);
            return new Vector2(0f, screen.yMax - bounds.yMin + OffscreenMargin);
        }

        private static Rect ToCanvasRect(RectTransform canvas, RectTransform rect, Vector3[] corners)
        {
            rect.GetWorldCorners(corners);
            var min = (Vector2)canvas.InverseTransformPoint(corners[0]);
            var max = min;
            for (var i = 1; i < 4; i++)
            {
                var p = (Vector2)canvas.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>게임 UI 전체의 알파를 <paramref name="alpha"/>로 옮긴다. 보이지 않는 동안(알파 0)엔 클릭을 받지 않는다.</summary>
        private Tween FadeGameUi(float alpha, float duration)
        {
            var group = GameUi;
            group.DOKill();
            if (alpha < 1f) group.blocksRaycasts = false;

            return group.DOFade(alpha, Mathf.Max(0.01f, duration)).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(group)
                .OnComplete(() => group.blocksRaycasts = alpha >= 1f);
        }

        private CanvasGroup GameUi
        {
            get
            {
                if (_gameUi != null) return _gameUi;

                var root = _canvasRoot.GetComponentInParent<Canvas>().rootCanvas.gameObject;
                _gameUi = root.GetComponent<CanvasGroup>();
                if (_gameUi == null) _gameUi = root.AddComponent<CanvasGroup>();
                return _gameUi;
            }
        }
    }
}
