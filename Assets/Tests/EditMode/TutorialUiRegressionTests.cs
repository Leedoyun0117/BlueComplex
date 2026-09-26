using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Stage;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 튜토리얼 UI 회귀 테스트. UI는 Assembly-CSharp에 있어 이 어셈블리가 직접 참조하지 못하므로 리플렉션으로 실제 컴포넌트를 만들어 확인한다.
    ///
    /// 배경(2026-09-26): 오프닝 대화의 선택지 버튼이 화면엔 보이는데 눌러도 반응이 없었다. 원인은 가이드의 클릭 막이 아니라 대사 묶음(CanvasGroup)이
    /// blocksRaycasts=false라서 그 안의 선택지 버튼까지 클릭을 못 받고 배경(오버레이)이 대신 받던 것이다. 이벤트를 직접 실행하는 검증은 이걸 못 잡는다 —
    /// 그래서 여기서는 실제 히트 테스트(<see cref="Graphic.Raycast(Vector2, Camera)"/>, 캔버스 그룹의 레이캐스트 필터까지 거친다)로 확인한다.
    /// </summary>
    public class TutorialUiRegressionTests
    {
        private const BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private GameObject _canvasGo;
        private GameObject _cameraGo;
        private Camera _camera;

        private static Type Ui(string name)
        {
            var type = Type.GetType($"BlueComplex.UI.Presentation.{name}, Assembly-CSharp");
            Assert.IsNotNull(type, $"UI 타입 {name}을(를) 찾을 수 없다");
            return type;
        }

        [SetUp]
        public void SetUp()
        {
            _cameraGo = new GameObject("Test Camera", typeof(Camera));
            _camera = _cameraGo.GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;

            _canvasGo = new GameObject("Test Canvas", typeof(Canvas));
            var canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasGo != null) UnityEngine.Object.DestroyImmediate(_canvasGo);
            if (_cameraGo != null) UnityEngine.Object.DestroyImmediate(_cameraGo);
        }

        private MonoBehaviour CreateOverlay()
        {
            var overlay = (MonoBehaviour)Ui("StageDialogueOverlay").GetMethod("Create")
                .Invoke(null, new object[] { _canvasGo.transform, null });
            overlay.gameObject.SetActive(true);

            // 대사가 재생되는 동안의 상태를 맞춘다: PlaySequence/PlayLinesOver가 루트 그룹을 켜서 뒤 UI 클릭을 막는다(_group.blocksRaycasts = true).
            // 대사 묶음(_textGroup)은 켜지 않는다 — 그건 재생 코드가 건드리지 않는 값이라 선택지가 스스로 켜야 한다(이게 이번에 고친 부분).
            var group = (CanvasGroup)overlay.GetType().GetField("_group", NonPublic).GetValue(overlay);
            group.blocksRaycasts = true;
            return overlay;
        }

        private static object ChoiceLine(string text)
        {
            var lineType = Ui("DialogueLine");
            var line = Activator.CreateInstance(lineType);
            lineType.GetField("text").SetValue(line, text);
            lineType.GetField("isChoice").SetValue(line, true);
            return line;
        }

        private static IEnumerator StartChoice(MonoBehaviour overlay, string text)
        {
            var routine = (IEnumerator)overlay.GetType().GetMethod("PlayChoice", NonPublic).Invoke(overlay, new[] { ChoiceLine(text) });
            Assert.IsTrue(routine.MoveNext(), "선택지 줄은 선택을 기다린다");
            return routine;
        }

        private Vector2 ScreenCenterOf(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(_camera, (corners[0] + corners[2]) * 0.5f);
        }

        // ── 오프닝 선택지 ────────────────────────────────────────────────────────

        [Test]
        public void OpeningChoice_ReceivesRealClicks_WhileShowing()
        {
            var overlay = CreateOverlay();
            var routine = StartChoice(overlay, "B는 누군데 갑자기 유키를 입양한거죠?");

            var choice = overlay.transform.Find("Dialogue/Choice");
            Assert.IsNotNull(choice, "선택지 버튼");
            Assert.IsTrue(choice.gameObject.activeInHierarchy);

            var image = choice.GetComponent<Image>();
            Assert.IsTrue(image.raycastTarget, "버튼 그래픽이 레이캐스트 대상이어야 한다");
            Assert.IsTrue(choice.GetComponent<Button>().interactable);

            // 실제 히트 테스트: 부모 캔버스 그룹(blocksRaycasts)까지 거친다. 예전엔 여기서 false였다.
            Assert.IsTrue(image.Raycast(ScreenCenterOf((RectTransform)choice), _camera),
                "선택지 버튼이 클릭을 받아야 한다 — 대사 묶음의 CanvasGroup이 레이캐스트를 막고 있으면 안 된다");

            // 눌러 보면 다음 줄로 넘어간다.
            overlay.GetType().GetMethod("OnChoiceClicked", NonPublic).Invoke(overlay, null);
            Assert.IsFalse(routine.MoveNext(), "선택지를 누르면 그 줄이 끝난다");
            Assert.IsFalse(choice.gameObject.activeSelf, "누른 뒤 선택지 버튼은 사라진다");
        }

        [Test]
        public void OpeningChoice_AllThreeChoices_AreClickableInTurn()
        {
            var overlay = CreateOverlay();

            foreach (var text in new[] { "B는 누군데 갑자기 유키를 입양한거죠?", "B에 대한 더 자세한 정보가 필요합니다.", "네." })
            {
                var routine = StartChoice(overlay, text);
                var choice = overlay.transform.Find("Dialogue/Choice");

                Assert.IsTrue(choice.GetComponent<Image>().Raycast(ScreenCenterOf((RectTransform)choice), _camera), $"'{text}' 선택지가 클릭을 받아야 한다");
                overlay.GetType().GetMethod("OnChoiceClicked", NonPublic).Invoke(overlay, null);
                Assert.IsFalse(routine.MoveNext(), text);
            }
        }

        [Test]
        public void OpeningChoice_TextGroupStopsBlockingAgain_AfterTheChoice()
        {
            // 선택지가 떠 있는 동안만 대사 묶음이 레이캐스트를 받는다 — 일반 줄에서는 원래대로 막지 않는다.
            var overlay = CreateOverlay();
            var textGroup = (CanvasGroup)overlay.GetType().GetField("_textGroup", NonPublic).GetValue(overlay);
            Assert.IsFalse(textGroup.blocksRaycasts, "평소엔 안 받는다");

            var routine = StartChoice(overlay, "네.");
            Assert.IsTrue(textGroup.blocksRaycasts, "선택지가 떠 있는 동안");

            overlay.GetType().GetMethod("OnChoiceClicked", NonPublic).Invoke(overlay, null);
            routine.MoveNext();
            Assert.IsFalse(textGroup.blocksRaycasts, "선택지가 끝나면 다시 안 받는다");
        }

        [Test]
        public void OpeningChoice_BackgroundClickDoesNotAdvance()
        {
            var overlay = CreateOverlay();
            var routine = StartChoice(overlay, "네.");

            overlay.GetType().GetMethod("OnPointerClick").Invoke(overlay, new object[] { new PointerEventData(null) });

            Assert.IsTrue(routine.MoveNext(), "배경 클릭으로는 선택지 줄이 안 넘어간다");
            Assert.IsTrue(overlay.transform.Find("Dialogue/Choice").gameObject.activeSelf);
        }

        [Test]
        public void OverlayReset_ClearsTheChoiceState()
        {
            var overlay = CreateOverlay();
            StartChoice(overlay, "네.");

            overlay.GetType().GetMethod("ResetNow").Invoke(overlay, null);

            var textGroup = (CanvasGroup)overlay.GetType().GetField("_textGroup", NonPublic).GetValue(overlay);
            Assert.IsFalse(textGroup.blocksRaycasts);
            Assert.IsFalse(overlay.transform.Find("Dialogue/Choice").gameObject.activeSelf);
        }

        // ── 가이드 클릭 막은 오프닝 동안 꺼져 있다 ────────────────────────────────

        [Test]
        public void GuideMask_IsOffUntilTheFirstStepStarts()
        {
            var guideType = Ui("TutorialGuide");
            var guide = (MonoBehaviour)guideType.GetMethod("GetOrCreate").Invoke(null, new object[] { _canvasGo.transform });
            Assert.IsNotNull(guide);

            var session = TutorialContent.CreateSession(new BlueComplex.Core.Tags.DefaultEmotionPolarityTable(), new SystemRandomSource(1), new ClueKnowledgeLedger());
            guideType.GetMethod("Begin").Invoke(guide, new object[] { session });

            var mask = guide.transform.Find("Click Mask");
            var bubble = guide.transform.Find("Bubble");
            Assert.IsNotNull(mask);
            Assert.IsFalse(mask.gameObject.activeSelf, "세션에 붙은 직후(= 시작 대화가 도는 동안)에는 클릭 막이 꺼져 있어야 한다");
            Assert.IsFalse(bubble.gameObject.activeSelf, "말풍선도 아직 없다");
            Assert.IsFalse((bool)guideType.GetProperty("IsActive").GetValue(guide), "첫 턴이 시작되기 전에는 안내가 시작되지 않았다");

            guideType.GetMethod("ResetNow").Invoke(guide, null);
            Assert.IsFalse(mask.gameObject.activeSelf);
        }
    }
}
