using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BlueComplex.Core.Turn;
using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlueComplex.Editor
{
    /// <summary>
    /// 재시작 상태 잔류 회귀 체크(Play 모드). 판이 진행되는 도중에 재시작하고, 옛 판이 남긴 화면·장부 상태가 전부 사라졌는지 본다.
    /// UI 스크립트는 어셈블리 정의가 없어 EditMode 테스트 어셈블리가 참조할 수 없으므로, 기존 DLJ 검사처럼 에디터 검사로 둔다.
    ///
    /// 실행: 메뉴 Tools/BlueComplex/Checks/Restart Residue (Play Mode), 또는 배치 <c>-executeMethod BlueComplex.Editor.RestartResidueCheck.RunBatch</c>
    /// (종료 코드 0 = 통과, 3 = 실패). 결과는 프로젝트 <c>Logs/RestartResidueCheck.txt</c>에 남는다.
    ///
    /// 시나리오
    ///  A. 결과 태그 칩이 떠 있는 연출 도중 재시작(같은 시드) — 칩·엑스레이·풍선 선·요약 글자·대사창·장부 pending이 전부 초기화되는가.
    ///  B. 칩이 위로 떠오르는 도중(RiseResultTags 진행 중) 재시작 후 새 판 첫 턴 — 옛 시퀀스의 OnComplete가 새 판 칩을 지우지 않는가.
    ///  C. 실패 확정 턴 — 결과 패널(재시작 버튼)이 연출이 끝난 뒤에야 뜨는가, 판정(해금)은 즉시 확정되는가, 그 뒤 재시작해도 잔류가 없는가.
    /// </summary>
    [InitializeOnLoad]
    public static class RestartResidueCheck
    {
        private const string StateKey = "RestartResidueCheck";
        private const string LogPath = "Logs/RestartResidueCheck.txt";

        private static IEnumerator _script;
        private static object _wait;
        private static StageBootstrapper _boot;
        private static ITurnResultPresenter _presenter;
        private static readonly StringBuilder Log = new();
        private static int _failures;
        private static double _t0;

        static RestartResidueCheck()
        {
            if (SessionState.GetInt(StateKey, 0) == 0) return;
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/BlueComplex/Checks/Restart Residue (Play Mode)")]
        public static void RunFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Start(batch: false);
        }

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            Log.Clear();
            _failures = 0;
            _script = null;
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
            SessionState.SetInt(StateKey, batch ? 2 : 1);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        // -----------------------------------------------------------------
        // 러너(에디터 update로 코루틴 흉내 — double = 초 대기, Func<bool> = 조건 대기)
        // -----------------------------------------------------------------

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var now = EditorApplication.timeSinceStartup;
            if (_script == null) { _script = Script(); _t0 = now; _wait = now + 3.0; }
            if (_wait is double until && now < until) return;
            if (_wait is Func<bool> pred && !pred()) return;
            try
            {
                if (!_script.MoveNext()) Finish();
                else _wait = _script.Current is double d ? now + d : _script.Current;
            }
            catch (Exception e)
            {
                Fail("스크립트 예외: " + e);
                Finish();
            }
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            var batch = SessionState.GetInt(StateKey, 0) == 2;
            SessionState.SetInt(StateKey, 0);

            Log.AppendLine(_failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({_failures})");
            try { File.WriteAllText(LogPath, Log.ToString()); } catch { /* 로그는 부가 */ }

            if (_failures == 0) Debug.Log("[RestartResidueCheck] PASS\n" + Log);
            else Debug.LogError("[RestartResidueCheck] FAIL\n" + Log);

            if (batch) EditorApplication.Exit(_failures == 0 ? 0 : 3);
            else EditorApplication.isPlaying = false;
        }

        private static void W(string s) => Log.AppendLine($"[{EditorApplication.timeSinceStartup - _t0:0.00}s] {s}");

        private static void Fail(string s)
        {
            _failures++;
            W("FAIL " + s);
        }

        private static void Check(bool ok, string what)
        {
            if (ok) W("ok   " + what);
            else Fail(what);
        }

        private static T Find<T>() where T : Component => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        private static IEnumerable<object> Iter(IEnumerator e)
        {
            while (e.MoveNext()) yield return e.Current;
        }

        // -----------------------------------------------------------------
        // 화면·장부 상태 읽기
        // -----------------------------------------------------------------

        private static string LabelText(object view, string field) =>
            (view.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(view) as TMP_Text)?.text ?? "<null>";

        private static bool EndPanelShown()
        {
            var panel = Find<StageEndPanel>();
            var overlay = (GameObject)typeof(StageEndPanel).GetField("_overlay", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
            return overlay != null && overlay.activeSelf;
        }

        /// <summary>재시작 직후 화면·장부에 옛 판의 흔적이 하나도 없어야 한다. (다음 프레임에 지워지는 칩 오브젝트는 세지 않는다 — HasResultTags는 목록 기준.)</summary>
        private static void CheckClean(string when)
        {
            var bubble = Find<MemorySpaceBubble>();
            var xray = Find<ComplexXrayPanel>();
            var dialogue = Find<DialogueText>();
            var idle = (string)typeof(DialogueText).GetField("_idleLine", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dialogue);

            Check(!bubble.HasResultTags, $"{when}: 결과 태그 칩이 없다");
            Check(!bubble.IsEngaged, $"{when}: 풍선(IsEngaged)이 꺼져 있다");
            Check(LabelText(bubble, "_summaryText") == string.Empty, $"{when}: 최종 감정 요약 글자가 비어 있다 (지금 '{LabelText(bubble, "_summaryText")}')");
            Check(!xray.IsOpen && xray.Fold == 0f, $"{when}: 엑스레이가 접혀 있다 (IsOpen={xray.IsOpen}, fold={xray.Fold:0.00})");
            Check(LabelText(dialogue, "_label") == idle, $"{when}: 대사창이 처음 화면(대기 글)이다 (지금 '{LabelText(dialogue, "_label")}')");
            Check(!_presenter.IsPresenting, $"{when}: Presenter가 연출 중이 아니다");
            Check(_boot.Session.Ledger.PendingCount == 0, $"{when}: 장부 pending이 0이다 (지금 {_boot.Session.Ledger.PendingCount})");
        }

        // -----------------------------------------------------------------
        // 진행 도우미
        // -----------------------------------------------------------------

        private static IEnumerator ClickThroughIntro()
        {
            var start = EditorApplication.timeSinceStartup;
            while (_boot.Session == null || _boot.Session.Hand.Cards.Count == 0)
            {
                var overlay = Find<StageDialogueOverlay>();
                if (overlay != null && overlay.gameObject.activeInHierarchy)
                    overlay.OnPointerClick(new PointerEventData(EventSystem.current));
                if (EditorApplication.timeSinceStartup - start > 90) { Fail("인트로 대화 뒤에도 손패가 안 생겼다"); yield break; }
                yield return 0.5;
            }

            yield return 0.5;
        }

        private static IEnumerator WaitPresenterIdle(double max = 90)
        {
            var g = EditorApplication.timeSinceStartup;
            while (_presenter.IsPresenting && EditorApplication.timeSinceStartup - g < max) yield return 0.1;
            if (_presenter.IsPresenting) Fail("연출이 제한 시간 안에 끝나지 않았다");
        }

        private static IEnumerator WaitUntilTrue(Func<bool> condition, double max, string what)
        {
            var g = EditorApplication.timeSinceStartup;
            while (!condition() && EditorApplication.timeSinceStartup - g < max) yield return 0.03;
            if (!condition()) Fail($"기다리던 상태가 오지 않았다: {what}");
        }

        /// <summary>첫 카드를 내고 결과 태그 칩이 뜰 때까지 기다린다(컴플렉스 발동 대사 등을 지나 연출 중반). 칩이 안 뜨는 턴이면 false.</summary>
        private static IEnumerator PlayUntilChips(Action<bool> done)
        {
            var bubble = Find<MemorySpaceBubble>();
            _boot.PlayCardAtIndex(0);
            var g = EditorApplication.timeSinceStartup;
            while (!bubble.HasResultTags && _presenter.IsPresenting && EditorApplication.timeSinceStartup - g < 60) yield return 0.03;
            done(bubble.HasResultTags);
        }

        private static IEnumerator Script()
        {
            yield return 1.0;
            _boot = Find<StageBootstrapper>();
            _presenter = Find<Canvas>().transform.GetComponentInChildren<ITurnResultPresenter>(true);
            if (_boot == null || _presenter == null) { Fail("StageBootstrapper/Presenter를 못 찾았다(GameScene 확인)"); yield break; }
            W($"presenter={_presenter.GetType().Name}, seed={_boot.CurrentSeed}");

            var bubble = Find<MemorySpaceBubble>();

            // ---- A. 연출 도중(칩이 떠 있는 순간) 재시작 -----------------------------------------------
            W("== A. 결과 태그 칩이 떠 있는 연출 도중 재시작");
            foreach (var w in Iter(ClickThroughIntro())) yield return w;

            var gotChips = false;
            foreach (var w in Iter(PlayUntilChips(v => gotChips = v))) yield return w;
            Check(gotChips, "A 준비: 첫 턴에 결과 태그 칩이 떴다");
            yield return 0.3;
            Check(_boot.Session.Ledger.PendingCount > 0, "A 준비: 재시작 전 장부에 미확정 관찰이 있다(누수 확인 대상)");
            Check(_presenter.IsPresenting && Find<ComplexXrayPanel>().IsOpen, "A 준비: 연출이 진행 중이고 엑스레이가 열려 있다");

            _boot.RestartWithSameSeed();
            yield return 0.1;
            CheckClean("A 재시작 +0.1s");
            yield return 2.5; // 옛 대사 타이핑/칩 시퀀스가 되살아나는지
            CheckClean("A 재시작 +2.6s");

            // ---- B. 칩이 떠오르는(Rise) 도중 재시작 → 새 판 첫 턴에서 옛 시퀀스가 새 칩을 지우지 않는가 ----------
            W("== B. 칩 상승 도중 재시작 → 새 판 첫 턴");
            foreach (var w in Iter(ClickThroughIntro())) yield return w;
            gotChips = false;
            foreach (var w in Iter(PlayUntilChips(v => gotChips = v))) yield return w;
            Check(gotChips, "B 준비: 첫 턴에 결과 태그 칩이 떴다");

            // 칩이 다 나타난 뒤(alpha 1) → Presenter가 올려 보내기 시작해 alpha가 내려가는 순간까지 기다린다.
            Func<CanvasGroup> firstChip = () => bubble.transform.Cast<Transform>()
                .Where(t => t.name.StartsWith("Result Tag")).Select(t => t.GetComponent<CanvasGroup>()).FirstOrDefault(g => g != null);
            yield return (Func<bool>)(() => firstChip() is { } g && g.alpha >= 0.99f || !_presenter.IsPresenting);
            yield return (Func<bool>)(() => firstChip() is { } g && g.alpha < 0.9f || !_presenter.IsPresenting);
            var inRise = _presenter.IsPresenting && firstChip() is { } c && c.alpha < 0.9f;
            Check(inRise, "B 준비: 칩이 떠오르는(사라지는) 도중이다");

            _boot.RestartWithSameSeed();
            yield return 0.1;
            CheckClean("B 재시작 +0.1s");
            foreach (var w in Iter(ClickThroughIntro())) yield return w;

            gotChips = false;
            foreach (var w in Iter(PlayUntilChips(v => gotChips = v))) yield return w;
            Check(gotChips, "B: 새 판 첫 턴에도 결과 태그 칩이 떴다");
            // 옛 Rise 시퀀스가 살아 있었다면 남은 시간(<= tagRise) 안에 OnComplete → ClearResultTags가 새 칩을 지운다. 새 판 칩은 최소 tagHold 동안은 떠 있어야 한다.
            var hold = Math.Max(0.3, Math.Min(0.6, BlueComplex.UI.Motion.UiMotion.Settings.tagHold * 0.75));
            var until = EditorApplication.timeSinceStartup + hold;
            var vanishedEarly = false;
            while (EditorApplication.timeSinceStartup < until)
            {
                if (!bubble.HasResultTags) { vanishedEarly = true; break; }
                yield return 0.03;
            }

            Check(!vanishedEarly, "B: 새 판 칩이 옛 시퀀스에 지워지지 않고 유지된다");
            foreach (var w in Iter(WaitPresenterIdle())) yield return w;

            // ---- C. 실패 확정 턴: 결과 패널은 연출이 끝난 뒤에 ---------------------------------------
            W("== C. 실패 확정 턴 — 결과 패널 타이밍");
            var session = _boot.Session;
            var failed = false;
            foreach (var target in new[] { 200, 0, 200, 0, 200, 0 })
            {
                if (session.Runner.Outcome != StageOutcome.InProgress) break;
                foreach (var w in Iter(WaitPresenterIdle())) yield return w;
                session.Heartbeat.Change(target - session.Heartbeat.Value);
                _boot.PlayCardAtIndex(0);
                failed = session.Runner.Outcome == StageOutcome.Failed;
                if (failed) break;
            }

            Check(failed, "C 준비: 즉사 구간으로 판이 실패로 끝났다");
            if (failed)
            {
                Check(_presenter.IsPresenting, "C: 실패를 확정한 턴의 연출이 진행 중이다");
                Check(!EndPanelShown(), "C: 그 연출이 도는 동안 결과 패널(재시작 버튼)이 아직 안 떴다");
                Check(session.Ledger.PendingCount == 0, "C: 판정(해금)은 즉시 확정됐다 — pending 0");

                // 연출이 도는 내내 패널이 떠 있지 않아야 한다.
                var shownDuring = false;
                var g = EditorApplication.timeSinceStartup;
                while (_presenter.IsPresenting && EditorApplication.timeSinceStartup - g < 90)
                {
                    if (EndPanelShown()) shownDuring = true;
                    yield return 0.05;
                }

                Check(!shownDuring, "C: 연출이 끝나기 전에는 한 번도 패널이 뜨지 않았다");
                yield return (Func<bool>)EndPanelShown;
                Check(EndPanelShown(), "C: 연출이 끝난 뒤 결과 패널이 뜬다");

                Find<StageEndPanel>().RestartNewSeedButton.onClick.Invoke();
                yield return 0.1;
                CheckClean("C 패널 재시작 +0.1s");
                Check(!EndPanelShown(), "C: 재시작하면 패널이 닫힌다");
            }

            // ---- D. F1/F2 경로: 판 도중 StartStage로 바꿔도 pending이 넘어가지 않는다 -----------------
            W("== D. 판 도중 F1/F2(StartStage) 재시작 — pending 누적 없음");
            foreach (var w in Iter(ClickThroughIntro())) yield return w;
            _boot.PlayCardAtIndex(0);
            foreach (var w in Iter(WaitPresenterIdle())) yield return w;
            _boot.StartStage(2);
            yield return 0.1;
            Check(_boot.Session.Ledger.PendingCount == 0, $"D: StartStage(2) 뒤 pending이 0이다 (지금 {_boot.Session.Ledger.PendingCount})");
            _boot.StartStage(1);
            yield return 0.1;
            Check(_boot.Session.Ledger.PendingCount == 0, $"D: StartStage(1) 뒤 pending이 0이다 (지금 {_boot.Session.Ledger.PendingCount})");
        }
    }
}
