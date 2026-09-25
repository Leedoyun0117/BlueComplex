using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 시작/쿼터 분기/스테이지 클리어 대화의 진행자. <see cref="StageDialogueOverlay"/>를 처음 필요할 때 캔버스 아래에 짓고(QuarterHud·PostitDirector와 같은 방식),
    /// <see cref="Play"/> 호출로 변형 하나를 재생한다. 어느 시점에 무슨 대사를 재생할지는 <see cref="Bootstrap.StageBootstrapper"/>(시작·쿼터 분기)와
    /// <see cref="StageEndController"/>(클리어)가 정한다 — 이 클래스는 재생만 한다.
    /// </summary>
    public sealed class StageDialoguePlayer : MonoBehaviour
    {
        private Transform _canvasRoot;
        private StageDialogueOverlay _overlay;

        public static StageDialoguePlayer GetOrCreate(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;

            var existing = canvasRoot.GetComponentInChildren<StageDialoguePlayer>(true);
            if (existing != null) return existing;

            var go = new GameObject("Stage Dialogue Player", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var player = go.AddComponent<StageDialoguePlayer>();
            player._canvasRoot = canvasRoot;
            return player;
        }

        public IEnumerator Play(DialogueVariant variant) => Overlay.PlaySequence(variant);

        /// <summary>자기 암전 막 없이 줄만 재생한다(다른 막이 이미 화면을 덮고 있을 때). 한 줄이 끝날 때마다 진행도(0~1)를 알린다.</summary>
        public IEnumerator PlayOver(DialogueVariant variant, Action<float> onLineDone) => Overlay.PlayLinesOver(variant, onLineDone);

        /// <summary>진행 중인 연출을 멈추고 막을 치운다(재시작). 아직 한 번도 재생한 적 없으면(오버레이가 없으면) 아무 일도 안 한다.</summary>
        public void ResetNow() => _overlay?.ResetNow();

        private StageDialogueOverlay Overlay =>
            _overlay != null ? _overlay : _overlay = StageDialogueOverlay.Create(_canvasRoot, RuntimeFont());

        private TMP_FontAsset RuntimeFont() => _canvasRoot.GetComponentInChildren<TMP_Text>(true)?.font;
    }
}
