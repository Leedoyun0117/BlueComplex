using System.Collections;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// "게임 장면" ↔ "분기 대사 장면" 전환. 게임 장면은 카드 트레이·인디케이터·아이템·HUD가 다 떠 있는 평소 화면이고,
    /// 분기 대사 장면은 매 쿼터(분기)가 끝날 때 게임 UI가 전부 빠지고 배경과 대사 글자만 남는 화면이다.
    ///
    /// 게임 UI를 하나씩 숨기지 않고 최상위 캔버스의 <see cref="CanvasGroup"/> 하나로 한꺼번에 투명하게 만든다 — 나중에 생기는 패널도 저절로 따라오고,
    /// 안 보이는 동안 클릭도 받지 않는다. 대사 오버레이는 그 그룹을 무시(<see cref="CanvasGroup.ignoreParentGroups"/>)해 혼자 남는다.
    /// 대사 재생은 <see cref="StageDialoguePlayer"/>가 하고, 이 클래스는 언제 UI를 뺐다 되돌릴지만 정한다(대사 내용·시점은 <see cref="Bootstrap.StageBootstrapper"/>가 정한다).
    /// 처음 필요할 때 캔버스 아래에 짓는다(PostitDirector·StageDialoguePlayer와 같은 방식).
    /// </summary>
    public sealed class BranchSceneDirector : MonoBehaviour
    {
        private Transform _canvasRoot;
        private CanvasGroup _gameUi;

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

        /// <summary>게임 UI가 빠지고(페이드) → 대사가 배경 위에서 재생되고 → 게임 UI가 돌아온다(페이드). 줄이 없으면 아무것도 안 한다.</summary>
        public IEnumerator Play(DialogueVariant variant)
        {
            if (variant.lines == null || variant.lines.Length == 0) yield break;

            var fade = UiMotion.Settings.branchSceneFade;
            yield return FadeGameUi(0f, fade).WaitForCompletion(true);
            yield return StageDialoguePlayer.GetOrCreate(_canvasRoot).PlayOver(variant, null);
            yield return FadeGameUi(1f, fade).WaitForCompletion(true);
        }

        /// <summary>진행 중인 전환을 멈추고 게임 UI를 되돌린다(재시작). 게임 장면이 기본 상태다.</summary>
        public void ResetNow()
        {
            if (_gameUi == null) return;

            _gameUi.DOKill();
            _gameUi.alpha = 1f;
            _gameUi.blocksRaycasts = true;
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
