using System.Collections;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// "스테이지 클리어 연출"의 화면 쪽 단계들: 어두운 막 위에 자물쇠가 나타나 하나씩 열린다 → (전부 열리면) 대사가 나오며 화면이 서서히 밝아진다 /
    /// (못 열면) 자물쇠도 함께 어두워지며 완전한 암전 → 컷신 전후·다음 스테이지 시작의 암전·밝아짐.
    /// 이 클래스는 각 단계를 코루틴으로 내주기만 한다 — 어떤 순서로 이어 붙일지(대사·컷신 훅·다음 스테이지 시작)는 <see cref="StageEndController"/>가 정한다.
    /// 코어 이벤트를 구독하지 않는다.
    ///
    /// 막(<see cref="ScreenCurtain"/>)과 자물쇠 줄(<see cref="StageLockRow"/>)을 캔버스 맨 위에 두고, MainHud.prefab에는 넣지 않는다 —
    /// 처음 필요할 때 캔버스 아래에 짓는다(<see cref="PostitDirector"/>와 같은 방식). 클리어 대사 오버레이는 이 위에 얹힌다(<c>SetAsLastSibling</c>).
    /// </summary>
    public sealed class StageClearDirector : MonoBehaviour
    {
        /// <summary>대사 한 줄이 끝날 때마다 막이 그만큼 걷히는 데 걸리는 시간.</summary>
        private const float BrightenStep = 0.6f;

        private Transform _canvasRoot;
        private ScreenCurtain _curtain;
        private StageLockRow _locks;

        public static StageClearDirector GetOrCreate(Transform canvasRoot)
        {
            var existing = canvasRoot.GetComponentInChildren<StageClearDirector>(true);
            if (existing != null) return existing;

            var rect = RuntimeUi.CreateStretched(canvasRoot, "Stage Clear Director");
            var director = rect.gameObject.AddComponent<StageClearDirector>();
            director._canvasRoot = canvasRoot;
            director._curtain = ScreenCurtain.Create(rect);
            director._locks = StageLockRow.Create(rect);
            return director;
        }

        /// <summary>자물쇠 화면: 화면이 어두워지고 → 자물쇠가 나타나고 → 획득한 키 수만큼 앞에서부터 하나씩 열린다(열쇠가 날아와 꽂힌다) → 잠시 머문다.</summary>
        public IEnumerator PlayLocks(StageEndPlan plan)
        {
            var settings = UiMotion.Settings;
            transform.SetAsLastSibling();
            _locks.Build(plan.LockCount);

            yield return _curtain.FadeTo(settings.keyTurnDim, settings.clearDimFade).WaitForCompletion(true);
            yield return _locks.FadeTo(1f, settings.lockAppear).WaitForCompletion(true);

            for (var i = 0; i < plan.OpenedCount; i++)
            {
                yield return _locks.PlayUnlock(i);
                yield return new WaitForSecondsRealtime(settings.lockInterval);
            }

            yield return new WaitForSecondsRealtime(settings.lockHold);
        }

        /// <summary>
        /// 모든 자물쇠가 열린 뒤: 클리어 대사가 나오며 화면이 서서히 밝아진다 — 대사 한 줄이 끝날 때마다 막이 조금씩 걷히고 자물쇠가 그만큼 옅어진다.
        /// 마지막 줄 뒤 남은 어둠이 걷히면 화면이 완전히 밝다. <paramref name="variant"/>가 없으면 대사 없이 바로 밝아진다.
        /// </summary>
        public IEnumerator PlayClearDialogue(DialogueVariant? variant)
        {
            var settings = UiMotion.Settings;
            var dim = settings.keyTurnDim;
            var floor = Mathf.Min(settings.clearBrightenFloor, dim);

            if (variant.HasValue)
            {
                yield return StageDialoguePlayer.GetOrCreate(_canvasRoot)
                    .PlayOver(variant.Value, progress => Brighten(Mathf.Lerp(dim, floor, progress), dim));
            }

            var finish = settings.clearBrightenFinish;
            _locks.FadeTo(0f, finish);
            yield return _curtain.FadeTo(0f, finish).WaitForCompletion(true);
            _locks.ResetNow();
        }

        /// <summary>막을 <paramref name="curtainAlpha"/>까지 걷고, 자물쇠도 막이 걷힌 비율만큼 옅어지게 한다(0.6초).</summary>
        private void Brighten(float curtainAlpha, float dim)
        {
            _curtain.FadeTo(curtainAlpha, BrightenStep);
            _locks.FadeTo(dim > 0f ? curtainAlpha / dim : 0f, BrightenStep);
        }

        /// <summary>실패: 자물쇠도 함께 어두워지며 화면이 완전한 암흑이 된다 → 잠시 머문다.</summary>
        public IEnumerator PlayBlackout()
        {
            var settings = UiMotion.Settings;
            var darken = _locks.DarkenAll(1f, settings.failBlackout);
            var fade = _curtain.FadeTo(1f, settings.failBlackout);

            yield return fade.WaitForCompletion(true);
            if (darken.IsActive() && !darken.IsComplete()) yield return darken.WaitForCompletion(true);

            _locks.ResetNow();
            yield return new WaitForSecondsRealtime(settings.failBlackHold);
        }

        /// <summary>화면이 완전히 어두워진다(컷신이 끝난 뒤, 다음 스테이지로 넘어가기 전).</summary>
        public IEnumerator FadeToBlack()
        {
            transform.SetAsLastSibling();
            yield return _curtain.FadeTo(1f, UiMotion.Settings.stageChangeFade).WaitForCompletion(true);
        }

        /// <summary>어둠이 걷히며 밝아진다(기다리지 않는다 — 새 스테이지가 이미 시작돼 있다).</summary>
        public Tween FadeFromBlack() => _curtain.FadeTo(0f, UiMotion.Settings.stageChangeFade);

        /// <summary>막이 완전히 어두운 채로 두고(기다림 없이) 바로 그 상태를 만든다 — 새 세션이 시작되며 막이 리셋된 직후, 같은 프레임에 어둠을 되살리는 데 쓴다.</summary>
        public void SnapBlack()
        {
            transform.SetAsLastSibling();
            _curtain.SetAlpha(1f);
        }

        /// <summary>진행 중인 연출을 멈추고 막·자물쇠를 치운다(재시작).</summary>
        public void ResetNow()
        {
            _curtain?.ResetNow();
            _locks?.ResetNow();
        }
    }
}
