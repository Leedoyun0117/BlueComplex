using System;
using System.Collections;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 게임을 켰을 때의 오프닝 전체(PPT 목업 BlueComplex_UI v6)를 한 번에 재생하는 컴포넌트. 노션 "게임 시작 연출"의 고래의 눈 버전은 폐기되었다.
    /// 튜토리얼 시작 컷신(<see cref="IntroCutsceneDirector"/>)과는 별개다.
    ///
    ///  1 유키 방(창문·벽시계·화분·노을빛) → 2 그 위에 "Yuki?" 포스트잇 → 3 암전 → 4 흰 포스트잇 4장(Car. Natsu. B. ?.)이 흩어진 자리에 하나씩 붙고 곧바로 2×2 창문 모양으로 미끄러져 정렬 →
    ///  5 4장이 주황으로 바뀌며 글씨가 Death. By. Complex. ?.로(배경은 어두운 채) → 6 창문 안에 소녀 실루엣 → 7 화면이 주황빛으로 → 8 소녀가 왼쪽에서 걸어 들어와 오른쪽으로 걷다 C에서 사라짐 →
    ///  9 종이색으로 바뀌며 포스트잇 바탕이 사라지고 글씨만 남음 → 10 그 종이색 화면 오른쪽 위에 거꾸로 선 소녀가 정지 이미지로 잠깐 → 11 글씨가 한 줄 "Death. By. Complex. ?."로 모여 타이핑되듯 나타남 →
    ///  12 암전 → 13 뉴스 → 14 타이틀 → 15 메인 화면. (시계 장면은 뺐다.)
    ///
    /// 메인 화면(<see cref="IntroMainMenuView"/>)의 "취조시작"이 <see cref="Begin"/>에 넘긴 콜백을 부르고 — 그것이 게임 시작(StageBootstrapper)이다 — 막을 걷는다.
    /// 메인 화면 전까지는 Space로 통째로 건너뛰어 곧장 메인 화면으로 갈 수 있다(긴 연출을 매번 다 볼 필요는 없다). 건너뛴 뒤에는 메인 화면의 버튼만 눌린다.
    /// 장면 조각은 단계마다 새로 지어 지우고, 시간 값은 <see cref="UiMotionSettings"/>의 "오프닝 시퀀스"에, 그림은 <see cref="IntroCutsceneArt"/>에 있다.
    /// </summary>
    public sealed class IntroSequencePlayer : MonoBehaviour
    {
        private static readonly Color NearBlack = new Color32(0x0D, 0x0D, 0x0D, 255);
        private static readonly Color DeepBrown = new Color32(0x18, 0x0B, 0x02, 255);
        private static readonly Color Orange = new Color32(0xCA, 0x66, 0x02, 255);
        private static readonly Color NoteGray = new Color32(0xD9, 0xD9, 0xD9, 255);
        private static readonly Color NoteWhite = new Color32(0xF5, 0xF5, 0xF2, 255);
        private static readonly Color Ink = Color.black;

        /// <summary>10단계에서 화면이 바뀌는 "종이색" — 게임 안의 종이 색(<see cref="MockupStyle.Paper"/>).</summary>
        private static Color PaperTint => MockupStyle.Paper;

        private const string FinalLine = "Death. By. Complex. ?.";
        private const float NoteWidth = 0.212f;  // 흩어져 붙는 단어 포스트잇의 크기(PPT 4번 장표, 화면 비율).
        private const float NoteHeight = 0.355f;

        // PPT 4~6번 장표: 흰 포스트잇 4장이 흩어진 자리에 붙고(4번), 2×2로 정렬되었다가(5번), 주황이 되며 글씨가 Death. By. Complex. ?.로 바뀐다(6번).
        // 붙는 순서대로: (처음 글씨, 붙는 자리 x·y, 정렬된 자리 x·y·w·h, 마지막 자리 = WindowNotes의 몇 번째).
        private static readonly (string text, float x, float y, float sx, float sy, float sw, float sh, int slot)[] ScatterNotes =
        {
            ("Car.", 0.105f, 0.219f, 0.548f, 0.122f, 0.172f, 0.355f, 1),
            ("Natsu.", 0.474f, 0.147f, 0.356f, 0.119f, 0.168f, 0.355f, 0),
            ("B.", 0.334f, 0.445f, 0.351f, 0.516f, 0.174f, 0.355f, 2),
            ("?.", 0.657f, 0.515f, 0.548f, 0.518f, 0.172f, 0.355f, 3)
        };

        // PPT 6번 장표의 주황 포스트잇 네 장(왼쪽 위·오른쪽 위·왼쪽 아래·오른쪽 아래): (글씨, x, y, w, h).
        private static readonly (string text, float x, float y, float w, float h)[] WindowNotes =
        {
            ("Death.", 0.334f, 0.089f, 0.166f, 0.355f), ("By.", 0.514f, 0.089f, 0.159f, 0.355f),
            ("Complex.", 0.336f, 0.465f, 0.164f, 0.355f), ("?.", 0.517f, 0.465f, 0.158f, 0.355f)
        };

        private Transform _canvasRoot;
        private RectTransform _overlay;
        private Image _backdrop;
        private RectTransform _layer;
        private IntroSlide _slide;
        private Image _veil;
        private TMP_Text _hint;
        private Tween _hintTween;
        private Vector2 _canvasSize;
        private IntroCutsceneArt _art;
        private TMP_FontAsset _font;
        private Coroutine _routine;
        private Action _startGame;
        private IntroMainMenuView _menu;
        private bool _skippable;

        public static IntroSequencePlayer GetOrCreate(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;

            var existing = canvasRoot.GetComponentInChildren<IntroSequencePlayer>(true);
            if (existing != null) return existing;

            var go = new GameObject("Intro Sequence Player", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var player = go.AddComponent<IntroSequencePlayer>();
            player._canvasRoot = canvasRoot;
            return player;
        }

        /// <summary>
        /// 어느 캔버스에도 기대지 않는 전용 화면 캔버스(Screen Space Overlay, 맨 위)에 플레이어를 짓는다. 씬에 쓸 만한(켜져 있는) 캔버스가 없을 때의 대비책이다 —
        /// 코루틴은 켜져 있는 오브젝트에서만 돈다.
        /// </summary>
        public static IntroSequencePlayer CreateStandalone()
        {
            var go = new GameObject("Intro Sequence Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return GetOrCreate(go.transform);
        }

        /// <summary>지금 화면을 덮고 있는(또는 덮으려는) 플레이어. 디버그 스테이지 시작이 오프닝을 끊을 때 쓴다 — 플레이어가 어느 캔버스 밑에 있든 찾을 수 있다.</summary>
        public static IntroSequencePlayer Current { get; private set; }

        public static IntroSequencePlayer Find(Transform canvasRoot) =>
            canvasRoot != null ? canvasRoot.GetComponentInChildren<IntroSequencePlayer>(true) : null;

        /// <summary>오프닝이 화면을 덮고 있는가(재생 중이거나 메인 화면이 떠 있는 동안).</summary>
        public bool IsActive => _overlay != null;

        /// <summary>지금 메인 화면(14단계)이 떠 있는가.</summary>
        public bool IsAtMainMenu => _menu != null;

        /// <summary>처음(1단계)부터 재생한다. <paramref name="startGame"/>은 메인 화면에서 "취조시작"을 눌렀을 때, 화면이 어두워진 뒤 한 번 불린다(게임 세션을 여는 일).</summary>
        public void Begin(Action startGame)
        {
            ResetNow();

            Current = this;
            _startGame = startGame;
            DOTween.SetTweensCapacity(800, 100); // 단어 포스트잇이 수십 장 동시에 붙는다 — 트윈이 기본 용량(200)을 넘으면 자동 확장 경고가 뜬다.
            _art = IntroCutsceneArt.Load();
            _font = RuntimeUi.FindFont(_canvasRoot) ?? FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include)?.font; // 전용 캔버스엔 글자가 없어 씬의 것을 빌린다.
            BuildOverlay();

            // 오프닝은 자기 배경곡(FinalDetection)만 깐다 — 상시 배경음(Fragile Notes)과 심박수 배경음이 켜져 있었다면 끈다.
            UiSoundHooks.StopAmbient();
            UiSoundHooks.SetBed(null);
            _routine = StartCoroutine(Run());
        }

        /// <summary>진행 중인 오프닝을 멈추고 막을 치운다(디버그로 스테이지를 바로 시작할 때 등).</summary>
        public void ResetNow()
        {
            StopAllCoroutines();
            DOTween.Kill(this);
            _routine = null;
            _skippable = false;
            _menu = null;
            if (Current == this) Current = null;
            UiSoundHooks.Stop(UiSoundCue.LightGlow);
            UiSoundHooks.StopTheme(UiMotion.Settings.seqThemeFadeOut); // 디버그로 스테이지를 바로 시작하면 곡이 페이드아웃되며 새 게임 배경음과 겹친다. 이미 꺼졌으면(메인 화면 이후) 아무 일도 없다.

            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
                _overlay = null;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (Current == this) Current = null;
            UiSoundHooks.StopTheme(0f); // 정적 상태(CurrentTheme)가 다음 재생으로 새지 않게.
        }

        private void Update()
        {
            if (!_skippable) return;

            // 건너뛰기는 Space만 — 클릭·Enter는 무시한다(Game 뷰 포커스용 클릭이 오프닝을 끊지 않게).
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) Skip();
        }

        // ------------------------------------------------------------------
        // 흐름
        // ------------------------------------------------------------------

        private IEnumerator Run()
        {
            // 캔버스 크기는 Start 시점엔 아직 CanvasScaler가 맞추기 전이라 틀리다 — 막(검은 화면)만 먼저 덮어 두고 두 프레임 뒤에 잰다.
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            _canvasSize = ((RectTransform)_canvasRoot).rect.size;
            _slide = IntroSlide.Create(_layer, _canvasSize);

            _skippable = true;
            UiSoundHooks.StartTheme(UiSoundCue.IntroTheme, UiMotion.Settings.seqThemeFadeIn);
            yield return Opening();
            yield return ShowMenu(fadeOutFirst: true);
        }

        /// <summary>메인 화면 전까지를 건너뛰고 메인 화면으로: 재빨리 어두워진 뒤 조각을 치우고 메인 화면을 연다.</summary>
        private void Skip()
        {
            _skippable = false;
            if (_routine != null) StopCoroutine(_routine);
            DOTween.Kill(this);
            UiSoundHooks.Stop(UiSoundCue.LightGlow); // 빛 번지는 소리가 건너뛴 메인 화면까지 이어지지 않게(뉴스 잡음은 뉴스 조각이 지워지며 스스로 끈다).
            _routine = StartCoroutine(SkipRoutine());
        }

        private IEnumerator SkipRoutine()
        {
            HideHint();
            yield return FadeVeil(1f, 0.25f);
            yield return ShowMenu(fadeOutFirst: false);
        }

        private IEnumerator Opening()
        {
            var s = UiMotion.Settings;

            // 1~2 유키 방, 그 위에 포스트잇.
            yield return YukiRoom(s);

            // 3 암전.
            yield return Blackout(s, DeepBrown);

            // 4~12 단어 포스트잇 4장 → 창문 → 소녀 → 종이색 → Death. By. Complex. ?.
            yield return PaperWords(s);

            // 13 암전.
            yield return Blackout(s, Color.black);

            // 14~16 뉴스.
            yield return News(s);

            // 17 타이틀.
            yield return Title(s);
        }

        /// <summary>지금 화면을 검게 덮고, 조각을 치우고, 다음 장면의 바탕색을 깔아 둔 채 잠시 머문다(덮개는 그대로 — 다음 장면이 걷는다).</summary>
        private IEnumerator Blackout(UiMotionSettings s, Color nextBackdrop)
        {
            yield return FadeVeil(1f, s.seqFade);
            ClearLayer();
            _backdrop.color = nextBackdrop;
            yield return Wait(s.seqBlackout);
        }

        // ------------------------------------------------------------------
        // 1~2 유키 방
        // ------------------------------------------------------------------

        private IEnumerator YukiRoom(UiMotionSettings s)
        {
            _backdrop.color = NearBlack;
            var step = NewStep("Yuki Room");

            // PPT: 그림 왼쪽 17.174%를 잘라 (0.007, 0.067) 크기 (0.986 × 0.845)로 놓는다.
            var frame = _slide.Place(step, "Picture", 0.007f, 0.067f, 0.986f, 0.845f);
            if (_art != null && _art.yukiRoom != null)
            {
                var raw = frame.gameObject.AddComponent<RawImage>();
                raw.texture = _art.yukiRoom;
                raw.uvRect = new Rect(0.17174f, 0f, 1f - 0.17174f, 1f);
                raw.raycastTarget = false;
            }
            else
            {
                Debug.LogWarning("[IntroSequencePlayer] 유키 방 그림이 채워져 있지 않다 — 메뉴 BlueComplex/Cutscene/Rebuild Intro Art를 실행한다.");
                frame.gameObject.AddComponent<Image>().color = new Color32(0x6E, 0x74, 0x24, 255);
            }

            yield return FadeVeil(0f, s.seqFade * 1.6f);
            ShowHintLater();
            yield return Wait(s.seqRoomHold);

            // 2: 유키 위에 포스트잇(PPT 2번: 회색 D9D9D9). 붙는 모션·소리(닿는 소리, 압정)는 게임 안 포스트잇(Postit.Stick) 그대로다.
            var note = IntroNoteView.Create(_slide.Place(step, "Yuki Note", 0.380f, 0.374f, 0.212f, 0.355f), "Yuki?", NoteGray, Ink, _font, _slide.Size.y * 0.085f);
            yield return note.Stick().WaitForCompletion(true);
            yield return Wait(s.seqYukiNoteHold);
        }

        // ------------------------------------------------------------------
        // 4~12 단어 포스트잇 4장 → 창문 → 소녀 → Death. By. Complex. ?.
        // ------------------------------------------------------------------

        private IEnumerator PaperWords(UiMotionSettings s)
        {
            _backdrop.color = DeepBrown;
            var step = NewStep("Window");

            var wordSize = _slide.Size.y * 0.085f;
            var notes = new IntroNoteView[WindowNotes.Length]; // 마지막 자리(WindowNotes) 순서.
            var placed = new IntroNoteView[ScatterNotes.Length]; // 붙은 순서.

            yield return FadeVeil(0f, s.seqFade);

            // 4: 흰 포스트잇 4장이 흩어진 자리에 하나씩 붙는다(붙는 순간에 만든다 — 만들어 두면 붙기 전에 보인다).
            for (var i = 0; i < ScatterNotes.Length; i++)
            {
                var n = ScatterNotes[i];
                placed[i] = IntroNoteView.Create(_slide.Place(step, "Note " + n.text, n.x, n.y, NoteWidth, NoteHeight), n.text, NoteWhite, Ink, _font, wordSize);
                notes[n.slot] = placed[i];
                yield return placed[i].Stick().WaitForCompletion(true);
                if (i < ScatterNotes.Length - 1) yield return Wait(s.seqWindowInterval);
            }

            // 4→5: 다 붙은 직후 각자 자리에서 2×2 창문 모양(6번 장표의 자리)으로 0.3초 동안 미끄러져 정렬된다. 이 뒤로 색이 바뀌는 동안은 자리가 움직이지 않는다.
            var snapTime = Mathf.Max(0.05f, s.seqNotesSnap);
            var snap = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (var i = 0; i < ScatterNotes.Length; i++)
            {
                var final = WindowNotes[ScatterNotes[i].slot];
                var slot = _slide.Slot(final.x, final.y, final.w, final.h);
                snap.Join(placed[i].Rect.DOAnchorPos(slot.position, snapTime).SetEase(Ease.OutQuad));
                snap.Join(placed[i].Rect.DOSizeDelta(slot.size, snapTime).SetEase(Ease.OutQuad));
            }

            yield return snap.WaitForCompletion(true);
            yield return Wait(0.4f);

            // 5→6: 4장이 제자리에 선 채 색만 흰색에서 주황으로 바뀌고 글씨가 바뀐다(배경은 그대로 어둡다). 왼쪽 위·오른쪽 위·왼쪽 아래·오른쪽 아래 순서로 하나씩 시간차를 두고 바뀐다.
            // 시작 시각은 Join이 아니라 Insert로 못 박는다 — InsertCallback이 Join의 기준 시각을 옮겨 버려서, Join으로 이어 붙이면 첫 장만 먼저 시작하고 나머지 셋이 같이 시작한다.
            var recolorTime = Mathf.Max(0.1f, s.seqNotesRecolor);
            var recolorStagger = Mathf.Max(0f, s.seqNotesRecolorStagger);
            var recolor = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (var slot = 0; slot < WindowNotes.Length; slot++)
            {
                var view = notes[slot];
                var final = WindowNotes[slot];
                var start = slot * recolorStagger;
                recolor.Insert(start, DOTween.To(() => 0f, t => view.SetPaper(Color.Lerp(NoteWhite, Orange, t)), 1f, recolorTime).SetEase(Ease.InOutSine));
                recolor.InsertCallback(start + recolorTime * 0.5f, () => view.Label.text = final.text);
            }

            yield return recolor.WaitForCompletion(true);
            yield return Wait(0.5f);

            // 7: 창문 안에 소녀가 정지 자세의 실루엣으로 나타나 잠깐 머문다.
            var girl = IntroGirlView.Create(_slide, step, _art);
            if (girl != null)
            {
                yield return girl.FadeInStill(Mathf.Max(0.1f, s.seqGirlAppear)).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
                yield return Wait(s.seqWindowHold);
            }

            // 8: 화면 전체가 즉시(컷 전환) 주황빛으로 바뀌고(실루엣은 사라진다) 스위치 소리(LightGlow 큐)가 난다.
            UiSoundHooks.Play(UiSoundCue.LightGlow);
            girl?.HideStill();
            _backdrop.color = Orange;

            // 9: 소녀가 왼쪽에서 걸어 들어와 오른쪽으로 걷다가 C에서 사라진다.
            if (girl != null) yield return GirlWalk(s, girl);

            // 10: 화면 색이 종이색으로 바뀌고, 포스트잇 바탕(주황 사각형)은 사라져 글씨만 남는다.
            var tint = Mathf.Max(0.1f, s.seqPaperTint);
            foreach (var note in notes) note.ReleaseLabel(step); // 글씨는 종이에서 빼 두고 종이만 지운다.

            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Append(_backdrop.DOColor(PaperTint, tint).SetEase(Ease.InOutSine));
            foreach (var note in notes) sequence.Join(note.FadeOutPaper(tint));
            yield return sequence.WaitForCompletion(true);

            // 11: 종이색이 된 화면 위, 오른쪽 위에 거꾸로 선 소녀가 나타나 남는다(글자 재배열 장면까지 그대로 — 암전 때 막이 치워지며 같이 사라진다).
            if (girl != null) yield return GirlUpsideDown(s, girl);

            yield return Wait(0.5f);

            // 12: 글씨가 재배열되며 한 줄로 합쳐지고, 타이핑되듯 나타난다.
            yield return GatherAndType(s, step, notes, wordSize);
            yield return Wait(s.seqTextHold);
        }

        // PPT 8~11번 장표의 소녀 키프레임(그림 한 칸 왼쪽 위, 화면 비율): A 왼쪽 가장자리 → B 화면 아래를 따라 → C 오른쪽. D는 C에서 사라진 뒤 거꾸로(상하·좌우 반전) 화면 오른쪽 바깥에서 걸어 들어와 서는 자리.
        private static readonly Vector2 GirlA = new Vector2(0.031f, 0.477f);
        private static readonly Vector2 GirlB = new Vector2(0.182f, 0.465f);
        private static readonly Vector2 GirlC = new Vector2(0.805f, 0.478f);
        private static readonly Vector2 GirlD = new Vector2(0.785f, -0.077f);
        private static readonly Vector2 GirlDStart = new Vector2(1.02f, -0.077f); // D와 같은 높이, 그림 한 칸이 통째로 화면 오른쪽 바깥에 있는 자리.

        /// <summary>
        /// 소녀가 A→B→C로 걷고(반전 없이 오른쪽을 본다), C에서 즉시 사라진다. 걷기 프레임은 시간이 아니라 움직인 거리로 넘겨 — 빠르게 가면 발도 빨리 구른다.
        /// </summary>
        private IEnumerator GirlWalk(UiMotionSettings s, IntroGirlView girl)
        {
            var aspect = _slide.Size.y / _slide.Size.x; // 세로 비율 거리를 가로 비율로 바꾸는 계수.
            var stride = Mathf.Max(0.01f, s.seqGirlStride);
            var cycles = 0f;
            var last = GirlA;

            girl.ShowWalker(true);
            girl.SetWalkPose(GirlA, false, false, 0);

            // A→B→C
            var walkTime = Mathf.Max(0.5f, s.seqGirlWalk);
            var t = 0f;
            while (t < walkTime)
            {
                var pos = OnPath(t / walkTime, GirlA, GirlB, GirlC, aspect);
                cycles += Dist(last, pos, aspect) / stride;
                last = pos;
                girl.SetWalkPose(pos, false, false, (int)(cycles * 8f));
                yield return null;
                t += Time.unscaledDeltaTime;
            }

            // C에서 즉시 사라진다.
            girl.SetWalkPose(GirlC, false, false, (int)(cycles * 8f));
            girl.ShowWalker(false);
        }

        /// <summary>
        /// 종이색이 된 화면에서, 잠깐 빈 뒤 위아래·좌우로 뒤집힌 소녀가 화면 오른쪽 바깥에서 걷기 사이클을 그대로 재생하며 걸어 들어와 D에 선다(왼쪽을 본다). 사라지게 하지 않는다 —
        /// 소녀는 막(step)에 남아 있다가 다음 막으로 넘어가는 암전 뒤 <see cref="ClearLayer"/>에서 함께 치워진다. 걷기 프레임은 첫 걷기와 같이 움직인 거리로 넘긴다.
        /// </summary>
        private IEnumerator GirlUpsideDown(UiMotionSettings s, IntroGirlView girl)
        {
            yield return Wait(s.seqGirlGap);

            var aspect = _slide.Size.y / _slide.Size.x;
            var stride = Mathf.Max(0.01f, s.seqGirlStride);
            var walkIn = Mathf.Max(0.3f, s.seqGirlWalkIn);
            var cycles = 0f;
            var last = GirlDStart;

            girl.SetWalkPose(GirlDStart, true, true, 0);
            girl.ShowWalker(true);

            var t = 0f;
            while (t < walkIn)
            {
                var pos = Vector2.Lerp(GirlDStart, GirlD, t / walkIn);
                cycles += Dist(last, pos, aspect) / stride;
                last = pos;
                girl.SetWalkPose(pos, true, true, (int)(cycles * 8f));
                yield return null;
                t += Time.unscaledDeltaTime;
            }

            cycles += Dist(last, GirlD, aspect) / stride;
            girl.SetWalkPose(GirlD, true, true, (int)(cycles * 8f));
            yield return Wait(Mathf.Max(0.5f, s.seqGirlFlash - walkIn)); // 걸어 들어온 뒤 머무는 시간(들어오는 시간까지 합쳐 seqGirlFlash).
        }

        private static float Dist(Vector2 a, Vector2 b, float aspect) => new Vector2(b.x - a.x, (b.y - a.y) * aspect).magnitude;

        /// <summary>세 점을 잇는 꺾은선 위에서, 이동 거리에 비례해(등속) u(0~1)만큼 간 자리.</summary>
        private static Vector2 OnPath(float u, Vector2 a, Vector2 b, Vector2 c, float aspect)
        {
            var first = Dist(a, b, aspect);
            var total = first + Dist(b, c, aspect);
            var d = Mathf.Clamp01(u) * total;
            return d <= first ? Vector2.Lerp(a, b, first > 0f ? d / first : 1f) : Vector2.Lerp(b, c, (d - first) / (total - first));
        }

        private IEnumerator GatherAndType(UiMotionSettings s, RectTransform step, IntroNoteView[] notes, float wordSize)
        {
            var font = notes[0].Label.font;

            // 합쳐진 한 줄(아직 안 보인다) — 각 단어가 갈 자리를 이 글자 배치에서 잰다.
            var mergedRect = RuntimeUi.CreateRect(step, "Merged Line", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            mergedRect.sizeDelta = new Vector2(_slide.Size.x, wordSize * 2f);
            mergedRect.anchoredPosition = Vector2.zero;
            var merged = mergedRect.gameObject.AddComponent<TextMeshProUGUI>();
            merged.font = font;
            merged.fontSize = wordSize;
            merged.color = Ink;
            merged.alignment = TextAlignmentOptions.Center;
            merged.textWrappingMode = TextWrappingModes.NoWrap;
            merged.raycastTarget = false;
            merged.text = FinalLine;
            merged.ForceMeshUpdate();

            var info = merged.textInfo;
            var targets = new float[notes.Length];
            var cursor = 0;
            for (var i = 0; i < notes.Length; i++)
            {
                var length = WindowNotes[i].text.Length;
                var first = info.characterInfo[cursor];
                var last = info.characterInfo[cursor + length - 1];
                targets[i] = (first.bottomLeft.x + last.topRight.x) * 0.5f;
                cursor += length + 1; // 단어 사이 공백 한 칸
            }

            merged.maxVisibleCharacters = 0;

            // 글씨만 종이 판에서 빼내 제 자리에서 한 줄 위의 자기 자리로 미끄러진다.
            var gather = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            var labels = new RectTransform[notes.Length];
            for (var i = 0; i < notes.Length; i++)
            {
                var label = (RectTransform)notes[i].Label.transform;
                label.SetParent(step, true);
                var keep = label.position; // 앵커를 바꿔도 화면에서 보이는 자리는 그대로여야 한다.
                label.anchorMin = label.anchorMax = label.pivot = new Vector2(0.5f, 0.5f);
                label.sizeDelta = new Vector2(_slide.Size.x * 0.25f, wordSize * 2f);
                label.position = keep;
                labels[i] = label;
                gather.Join(label.DOAnchorPos(new Vector2(targets[i], 0f), Mathf.Max(0.2f, s.seqTextGather)).SetEase(Ease.InOutCubic));
            }

            yield return gather.WaitForCompletion(true);
            yield return Wait(0.35f);

            // 모인 글씨는 사라지고 한 줄이 한 글자씩 쳐진다.
            foreach (var label in labels)
            {
                var text = label.GetComponent<TMP_Text>();
                DOTween.To(() => text.alpha, v => text.alpha = v, 0f, 0.25f).SetUpdate(true).SetTarget(this);
            }

            for (var i = 1; i <= FinalLine.Length; i++)
            {
                merged.maxVisibleCharacters = i;
                if (FinalLine[i - 1] != ' ') UiSoundHooks.Play(UiSoundCue.Type);
                yield return Wait(s.seqTypeInterval);
            }
        }

        // ------------------------------------------------------------------
        // 뉴스, 타이틀
        // ------------------------------------------------------------------

        private IEnumerator News(UiMotionSettings s)
        {
            yield return FadeVeil(1f, s.seqFade);
            ClearLayer();
            _backdrop.color = Color.black;

            // 화면 전체(막 전체)를 덮는 뉴스 조각 — 검은 화면에서 흘러들어오고, 줄이 바뀔 때마다 지직거리는 소리(UiSoundCue.TvStatic)가 난다.
            var news = IntroNewsView.Create(_layer, _canvasSize, _font);
            yield return FadeVeil(0f, 0.05f);
            yield return news.Play(IntroCutsceneContent.NewsLines, s.introNewsFadeIn, s.introNewsLine, s.introNewsStatic);
            news.Hide();
        }

        private IEnumerator Title(UiMotionSettings s)
        {
            yield return FadeVeil(1f, 0.3f);
            ClearLayer();
            _backdrop.color = DeepBrown;

            var step = NewStep("Title");
            var group = step.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            // 자간을 넓게: 글자 사이에 <space> 태그로 간격을 준다(characterSpacing은 이 폰트에서 글자마다 들쭉날쭉했다).
            var title = RuntimeUi.CreateText(step, "Title", SpacedTitle("BLUE COMPLEX."), _font, _slide.Size.y * 0.075f, Color.white,
                TextAlignmentOptions.Center, new Vector2(0f, 0.42f), new Vector2(1f, 0.58f));
            title.richText = true;

            yield return FadeVeil(0f, 0.4f);
            yield return group.DOFade(1f, Mathf.Max(0.1f, s.seqTitleFade)).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
            yield return Wait(s.seqTitleHold);
        }

        private static string SpacedTitle(string text)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in text)
            {
                if (c == ' ') sb.Append("<space=1.1em>");
                else sb.Append(c).Append("<space=0.32em>");
            }

            return sb.ToString();
        }

        // ------------------------------------------------------------------
        // 메인 화면 → 게임 시작
        // ------------------------------------------------------------------

        /// <summary>메인 화면을 연다. <paramref name="fadeOutFirst"/>가 참이면 지금 화면을 먼저 검게 덮고, 아니면 이미 덮여 있다고 본다.</summary>
        private IEnumerator ShowMenu(bool fadeOutFirst)
        {
            var s = UiMotion.Settings;
            _skippable = false;
            HideHint();

            // 타이틀이 끝났다 — 오프닝 배경곡이 메인 화면으로 넘어가는 동안 서서히 사라진다(건너뛰기로 온 경우도 같다). 게임 배경음은 "취조시작"으로 스테이지가 열릴 때 올라온다.
            UiSoundHooks.StopTheme(s.seqThemeFadeOut);

            if (fadeOutFirst) yield return FadeVeil(1f, s.seqFade * 0.5f);
            ClearLayer();
            _backdrop.color = Color.black;
            _menu = IntroMainMenuView.Create(_slide, _art, _font, OnStartPressed, OnNotesPressed, OnQuitPressed);
            yield return FadeVeil(0f, s.seqMenuFade);
        }

        private void OnStartPressed()
        {
            if (_menu == null) return;

            _menu.SetInteractable(false);
            _routine = StartCoroutine(StartGameRoutine());
        }

        /// <summary>메인 화면이 어두워진다 → 게임 세션을 연다 → 막이 걷히며 게임 화면이 드러난다.</summary>
        private IEnumerator StartGameRoutine()
        {
            var s = UiMotion.Settings;
            yield return FadeVeil(1f, s.seqStartFade);

            _startGame?.Invoke();
            yield return null;

            ClearLayer();
            _menu = null;
            _backdrop.color = Color.clear;
            yield return FadeVeil(0f, s.seqStartFade);

            ResetNow();
        }

        private void OnNotesPressed() => _menu?.ShowToast("단서 노트는 준비 중입니다.");

        private static void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------
        // 막·조각
        // ------------------------------------------------------------------

        /// <summary>게임 화면 전체를 덮는 막(클릭도 막는다). 처음엔 완전히 검다 — 어둠 속에서 시작해 1단계가 밝아진다.</summary>
        private void BuildOverlay()
        {
            var root = new GameObject("Intro Sequence", typeof(RectTransform), typeof(Image)) { layer = _canvasRoot.gameObject.layer };
            root.transform.SetParent(_canvasRoot, false);
            root.transform.SetAsLastSibling();
            _overlay = (RectTransform)root.transform;
            Stretch(_overlay);

            var blocker = root.GetComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true; // 오프닝 동안 뒤 화면의 클릭을 막는다(메인 화면 버튼은 이 위에 있다).

            _backdrop = NewFullImage("Backdrop", NearBlack);
            _layer = RuntimeUi.CreateStretched(_overlay, "Layer");
            _veil = NewFullImage("Veil", Color.black);

            var hint = new GameObject("Skip Hint", typeof(RectTransform)) { layer = root.layer };
            hint.transform.SetParent(_overlay, false);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = new Vector2(0.55f, 0.02f);
            hintRect.anchorMax = new Vector2(0.98f, 0.07f);
            hintRect.offsetMin = hintRect.offsetMax = Vector2.zero;
            _hint = hint.AddComponent<TextMeshProUGUI>();
            if (_font != null) _hint.font = _font;
            _hint.text = "Space  건너뛰기";
            _hint.fontSize = 22f;
            _hint.color = new Color(1f, 1f, 1f, 0.45f);
            _hint.alignment = TextAlignmentOptions.MidlineRight;
            _hint.raycastTarget = false;
            _hint.alpha = 0f;
        }

        private Image NewFullImage(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { layer = _overlay.gameObject.layer };
            go.transform.SetParent(_overlay, false);
            Stretch((RectTransform)go.transform);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>16:9 틀 위에 이번 단계의 조각을 올릴 빈 자리(이전 단계 조각은 <see cref="ClearLayer"/>가 이미 치웠다).</summary>
        private RectTransform NewStep(string name)
        {
            ClearLayer();
            return RuntimeUi.CreateStretched(_slide.Root, name);
        }

        /// <summary>단계 조각을 전부 치운다. 16:9 틀(<see cref="IntroSlide.Root"/>)은 남기고 그 안의 것과, 틀 밖에 직접 붙은 조각(뉴스)을 지운다.</summary>
        private void ClearLayer()
        {
            for (var i = _slide.Root.childCount - 1; i >= 0; i--) Destroy(_slide.Root.GetChild(i).gameObject);
            for (var i = _layer.childCount - 1; i >= 0; i--)
            {
                var child = _layer.GetChild(i);
                if (child != _slide.Root) Destroy(child.gameObject);
            }
        }

        private void ShowHintLater() =>
            _hintTween = DOTween.To(() => _hint.alpha, v => _hint.alpha = v, 1f, 0.8f).SetDelay(1.2f).SetUpdate(true).SetTarget(this);

        private void HideHint()
        {
            if (_hint == null) return;
            _hintTween?.Kill();
            _hint.alpha = 0f;
        }

        private IEnumerator FadeVeil(float alpha, float seconds) =>
            _veil.DOFade(alpha, Mathf.Max(0.01f, seconds)).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);

        private static WaitForSecondsRealtime Wait(float seconds) => new WaitForSecondsRealtime(Mathf.Max(0f, seconds));

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
