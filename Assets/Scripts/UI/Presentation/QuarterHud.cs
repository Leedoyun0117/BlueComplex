using System;
using System.Collections.Generic;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Layout;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 쿼터 HUD 묶음: 키 표시 창(UI 가이드 11번) + 쿼터 진행 스테이터스 창(12번) + 전체 스테이지 오버레이.
    /// 표시 상태만 쥐고 있다 — 코어 이벤트를 직접 구독하지 않고, 갱신 시점은 전부 <see cref="HeartRateController"/>가
    /// 넘겨준다(그쪽 호출 시점은 ITurnResultPresenter가 쥔다). 그래서 하얀 점 이동과 키 아이콘 색 변화가
    /// 컴플렉스/태그 연출보다 앞서 튀지 않는다.
    ///
    /// MainHud.prefab을 다시 굽지 않아도 되도록(굽으면 손으로 다듬은 배치가 날아간다) 패널은 프리팹에 넣지 않고
    /// 처음 필요할 때 루트 캔버스 아래에 코드로 짓는다 — <see cref="ClueBookPanel"/>과 같은 방식(GetOrCreate).
    /// 쿼터 수·쿼터당 턴 수는 세션이 쥔 스테이지 설정(StageConfig.Quarters)에서 읽으므로 하드코딩이 없다.
    /// </summary>
    public sealed class QuarterHud : MonoBehaviour
    {
        // 디자인 목업 배치(화면 비율, 아래쪽 원점). 위치는 여기서만 바꾼다.
        // 키 카드는 목업 표(L 0.61, W 0.24, H 0.15)에서 T만 0.63 → 0.562로 올렸다 — 기울어진 카드의 아래 모서리가 단서 패널(T 0.74)을 파고들지 않게.
        private static readonly (Vector2 Min, Vector2 Max) KeyWindowAnchors =
            (new Vector2(0.61f, 0.288f), new Vector2(0.85f, 0.438f));

        // 쿼터 진행(대화) 포스트잇: 목업 표(L 0.57, T 0.13, W 0.08, H 0.12)에서 T만 0.115로 — 심박수 모니터(아래 끝 0.135)의 하단 모서리에 걸쳐 붙는다
        // (압정이 모니터 케이스 위, 종이 윗부분이 케이스 하단 여백을 덮는다. 모니터 화면·BPM 글자는 안 가린다).
        private static readonly (Vector2 Min, Vector2 Max) ProgressWindowAnchors =
            (new Vector2(0.57f, 0.765f), new Vector2(0.65f, 0.885f));

        private Transform _canvasRoot;
        private StageSession _session;
        private StageBootstrapper _bootstrapper;

        private KeyStatusPanel _keyPanel;
        private QuarterProgressPanel _progressPanel;
        private StageOverviewOverlay _overview;
        private int _builtQuarterCount;
        private int _builtTurnsPerQuarter;

        /// <summary>표시용 쿼터별 키 결과(인덱스 0 = 1쿼터). 코어의 Keys.Results가 아니라 Presenter가 알려준 만큼만 반영한다.</summary>
        private bool?[] _results = Array.Empty<bool?>();

        private int _quarter;
        private int _turnInQuarter;

        public bool IsOverviewOpen => _overview != null && _overview.IsOpen;

        /// <summary>대화 포스트잇(쿼터 진행 창)의 포스트잇 컴포넌트. 턴이 넘어갈 때 떼었다 붙이는 건 <see cref="PostitDirector"/>가 한다.</summary>
        public Postit ProgressPostit => _progressPanel != null ? _progressPanel.Postit : null;

        public static QuarterHud GetOrCreate(Transform canvasRoot)
        {
            var existing = canvasRoot.GetComponentInChildren<QuarterHud>(true);
            if (existing != null) return existing;

            var go = new GameObject("Quarter Hud", typeof(RectTransform)) { layer = canvasRoot.gameObject.layer };
            go.transform.SetParent(canvasRoot, false);

            var hud = go.AddComponent<QuarterHud>();
            hud._canvasRoot = canvasRoot;
            return hud;
        }

        /// <summary>세션이 시작(재시작 포함)될 때 부른다 — 표시 상태를 전부 처음으로 되돌린다.</summary>
        public void Bind(StageSession session, StageBootstrapper bootstrapper)
        {
            _session = session;
            _bootstrapper = bootstrapper;

            var schedule = session.Keys.Schedule;
            EnsureBuilt(schedule);
            _overview.Hide();

            _results = new bool?[schedule.QuarterCount];
            _quarter = 0;
            _turnInQuarter = 0;
            Refresh(animate: false);
        }

        /// <summary>현재 쿼터와 그 안에서의 턴 위치를 반영한다. 같은 쿼터 안에서 턴이 넘어가면 점이 한 칸 움직이고,
        /// 쿼터가 바뀌면 진행 창이 처음 칸으로 리셋된다(움직이지 않고 바로 놓인다).</summary>
        public void Sync(int quarter, int turnInQuarter)
        {
            var sameQuarter = quarter == _quarter;
            _quarter = quarter;
            _turnInQuarter = turnInQuarter;
            Refresh(animate: sameQuarter);
        }

        /// <summary>쿼터의 키 판정 결과를 반영한다. 획득이면 그 칸의 빈 자리에 열쇠가 찍힌다(KeyStatusPanel).</summary>
        public void RecordKeyResult(int quarter, bool success)
        {
            if (quarter < 1 || quarter > _results.Length) return;

            _results[quarter - 1] = success;
            Refresh(animate: true);
        }

        /// <summary>스테이지가 끝나면 오버레이를 닫는다 — 결과 패널이 그 밑에 깔리지 않게.</summary>
        public void CloseOverview()
        {
            if (_overview != null) _overview.Hide();
        }

        private void Refresh(bool animate)
        {
            if (_session == null || _keyPanel == null) return;

            _keyPanel.SetResults(_results, animate);
            _progressPanel.SetQuarter(_quarter, _session.Keys.Schedule.QuarterCount);
            _progressPanel.SetTurn(_turnInQuarter, animate);
            _overview.Refresh(_quarter, _turnInQuarter, BuildTargetTexts(), _results, animate);
        }

        /// <summary>쿼터별 목표 심박수 문구. 키 구역은 시작 위치부터 끝 위치까지(양 끝 포함) 닿아야 얻으므로 범위 그대로 보여준다 —
        /// 한 숫자만 보여주면 그 값에 정확히 맞춰야 하는 것으로 오해한다. 구역은 StartStage에서 확정되므로 그 전에는 "-".</summary>
        private string[] BuildTargetTexts()
        {
            var schedule = _session.Keys.Schedule;
            var texts = new string[schedule.QuarterCount];
            for (var quarter = 1; quarter <= schedule.QuarterCount; quarter++)
            {
                texts[quarter - 1] = _session.Keys.Zones.TryGetValue(schedule.LastTurnOf(quarter), out var zone)
                    ? $"{zone.StartSlot}~{zone.StartSlot + zone.Width - 1}"
                    : "-";
            }

            return texts;
        }

        private void EnsureBuilt(QuarterSchedule schedule)
        {
            if (_keyPanel != null && _builtQuarterCount == schedule.QuarterCount &&
                _builtTurnsPerQuarter == schedule.TurnsPerQuarter)
                return;

            DestroyPanels();

            var font = RuntimeUi.FindFont(_canvasRoot);
            _keyPanel = KeyStatusPanel.Create(_canvasRoot, schedule.QuarterCount, KeyWindowAnchors.Min, KeyWindowAnchors.Max, font);
            _progressPanel = QuarterProgressPanel.Create(_canvasRoot, schedule.TurnsPerQuarter,
                ProgressWindowAnchors.Min, ProgressWindowAnchors.Max, font);
            _progressPanel.Clicked += OpenOverview;

            var tray = _canvasRoot.GetComponentInChildren<ClueCardTray>(true);
            var items = _canvasRoot.GetComponentInChildren<ItemDisplayPanel>(true);
            PlaceAfter(tray != null ? tray.transform : null, _keyPanel.transform, _progressPanel.transform);

            _overview = StageOverviewOverlay.Create(_canvasRoot, font,
                schedule.QuarterCount, schedule.TurnsPerQuarter,
                new[] { items != null ? items.transform : null, tray != null ? tray.transform : null, _keyPanel.transform });
            _overview.Opened += OnOverviewOpened;
            _overview.Closed += OnOverviewClosed;

            _builtQuarterCount = schedule.QuarterCount;
            _builtTurnsPerQuarter = schedule.TurnsPerQuarter;
        }

        private void DestroyPanels()
        {
            if (_overview != null)
            {
                _overview.Hide();
                Destroy(_overview.gameObject);
            }

            if (_keyPanel != null) Destroy(_keyPanel.gameObject);
            if (_progressPanel != null) Destroy(_progressPanel.gameObject);
        }

        /// <summary>새로 만든 상시 창을 단서 트레이 바로 뒤에 둔다 — 결과 패널·툴팁 같은 위에 얹혀야 하는 것들 밑에 머문다.</summary>
        private static void PlaceAfter(Transform anchor, params Transform[] panels)
        {
            if (anchor == null) return;

            var index = anchor.GetSiblingIndex() + 1;
            foreach (var panel in panels)
                panel.SetSiblingIndex(index++);
        }

        private void OpenOverview()
        {
            if (_session == null) return;

            Refresh(animate: false);
            _overview.Show();
        }

        private void OnOverviewOpened()
        {
            if (_bootstrapper != null) _bootstrapper.InputBlocked = true;
        }

        private void OnOverviewClosed()
        {
            if (_bootstrapper != null) _bootstrapper.InputBlocked = false;
        }
    }
}
