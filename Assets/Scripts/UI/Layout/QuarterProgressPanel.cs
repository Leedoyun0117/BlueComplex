using System;
using BlueComplex.UI.Motion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 쿼터 진행 스테이터스 창(UI 가이드 12번) — 목업의 노란 포스트잇(대화 포스트잇). "대화 / 현재 쿼터 / 전체 쿼터" 글자와, 현재 쿼터의 턴 수만큼 칸이 있고
    /// 현재 턴의 칸이 어둡게 칠해지며 하얀 점이 놓인다. 지난 턴의 칸은 회색, 마지막 칸은 노랑(키를 얻는 턴).
    /// 종이·압정·말림·떼어졌다 붙는 모션은 <see cref="Postit"/>이 맡고 이 패널은 내용만 채운다.
    /// 마우스를 올리면 테두리가 켜지고, 클릭하면 <see cref="Clicked"/>가 온다 —
    /// 전체 스테이지 오버레이를 여는 건 이 패널이 아니라 듣는 쪽이다. 표시만 한다 — 판정 없음.
    /// </summary>
    public sealed class QuarterProgressPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        /// <summary>포스트잇은 노란 종이라 호버 테두리는 흰색이 아니라 어두운 갈색으로 켠다(흰 테두리는 종이 위에서 안 보인다).</summary>
        private static readonly Color HoverBorder = new Color32(70, 56, 20, 255);

        private const float BorderThickness = 2.5f;

        /// <summary>붙어 있을 때의 기울기. 컴플렉스 포스트잇(+3°)과 반대쪽으로 기울여 두 장이 다르게 보이게 한다.</summary>
        private const float TiltDegrees = -2.5f;

        private Postit _postit;
        private CanvasGroup _hoverFrame;
        private TurnTrack _track;
        private TMP_Text _counter;

        public event Action Clicked;

        /// <summary>종이·압정·떼기/붙이기 모션. 턴이 넘어갈 때 PostitDirector가 이걸 움직인다.</summary>
        public Postit Postit => _postit;

        public static QuarterProgressPanel Create(Transform parent, int turnsPerQuarter, Vector2 anchorMin, Vector2 anchorMax,
            TMP_FontAsset font)
        {
            var rect = RuntimeUi.CreateRect(parent, "Quarter Progress Panel", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var panel = rect.gameObject.AddComponent<QuarterProgressPanel>();
            panel.Build(turnsPerQuarter, font);
            return panel;
        }

        /// <summary>현재 쿼터 안에서 지금이 몇 번째 턴인지(1부터, 0이면 점 없음).</summary>
        public void SetTurn(int turnInQuarter, bool animate) => _track.SetTurn(turnInQuarter, animate);

        /// <summary>"2 / 3" 글자. 쿼터가 시작되기 전(0)에는 1로 보여 준다.</summary>
        public void SetQuarter(int quarter, int quarterCount) =>
            _counter.text = $"{Mathf.Max(quarter, 1)} / {quarterCount}";

        public void OnPointerEnter(PointerEventData eventData) => _hoverFrame.alpha = 1f;

        public void OnPointerExit(PointerEventData eventData) => _hoverFrame.alpha = 0f;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            Clicked?.Invoke();
        }

        private void Build(int turnsPerQuarter, TMP_FontAsset font)
        {
            _postit = Postit.Attach(gameObject);
            _postit.SetRestTilt(TiltDegrees);

            var body = _postit.Content;
            RuntimeUi.CreateText(body, "Title", "대화", font, 26f, PostitStyle.Ink, TextAlignmentOptions.Center,
                new Vector2(0f, 0.6f), new Vector2(1f, 0.92f));
            _counter = RuntimeUi.CreateText(body, "Counter", "1 / 1", font, 26f, PostitStyle.Ink, TextAlignmentOptions.Center,
                new Vector2(0f, 0.32f), new Vector2(1f, 0.62f));

            _track = new TurnTrack(body, "Track", new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.31f), turnsPerQuarter, 12f,
                TurnTrackStyle.Sticky);

            _postit.ApplyHandFont();
            BuildHoverFrame();
        }

        /// <summary>호버하면 켜지는 테두리. 클릭을 받는 투명한 판 + 얇은 갈색 띠 네 개(종이 위에 얹혀 잘리지 않게 Overlay에 둔다).
        /// 투명해도 레이캐스트는 받는다(알파 히트 테스트를 안 쓴다) — 떼어져 있는 동안은 Postit이 판 전체의 레이캐스트를 끈다.</summary>
        private void BuildHoverFrame()
        {
            var overlay = _postit.Overlay;
            RuntimeUi.CreateImage(overlay, "Hit", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, raycastTarget: true);

            var frame = RuntimeUi.CreateStretched(overlay, "Hover Frame");
            _hoverFrame = frame.gameObject.AddComponent<CanvasGroup>();
            _hoverFrame.alpha = 0f;
            _hoverFrame.blocksRaycasts = false;

            var t = BorderThickness;
            RuntimeUi.CreateImage(frame, "Top", HoverBorder, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -t), Vector2.zero);
            RuntimeUi.CreateImage(frame, "Bottom", HoverBorder, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, t));
            RuntimeUi.CreateImage(frame, "Left", HoverBorder, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(t, 0f));
            RuntimeUi.CreateImage(frame, "Right", HoverBorder, new Vector2(1f, 0f), Vector2.one, new Vector2(-t, 0f), Vector2.zero);
        }

        private void OnDisable()
        {
            if (_hoverFrame != null) _hoverFrame.alpha = 0f;
            _track?.Kill();
        }
    }
}
