using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Stage;
using BlueComplex.Core.Turn;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>
    /// 키 구역 표시: 스테이지 시작 시 확정된 4개 구역을 전부 나열하고, 현재 턴 구역을 강조한다.
    /// 심박수 0~200 스케일 바 위에 구역과 현재 심박 위치를 겹쳐 그린다.
    /// </summary>
    internal sealed class DebugKeyZoneView : DebugSessionView
    {
        private RectTransform _barTrack;
        private RectTransform _indicator;
        private Text _listText;
        private readonly List<GameObject> _markers = new();

        protected override void BuildUI()
        {
            var go = gameObject;
            DebugUIFactory.AddVerticalLayout(go, spacing: 4);

            var barContainer = DebugUIFactory.CreatePanel(transform, "Bar", new Color(0.1f, 0.1f, 0.13f));
            DebugUIFactory.AddLayoutElement(barContainer.gameObject, minHeight: 24, flexibleWidth: 1);

            _barTrack = DebugUIFactory.StretchFull(barContainer);

            var indicatorGo = new GameObject("HeartbeatIndicator", typeof(RectTransform), typeof(Image));
            indicatorGo.transform.SetParent(_barTrack, false);
            indicatorGo.GetComponent<Image>().color = Color.white;
            _indicator = (RectTransform)indicatorGo.transform;
            _indicator.anchorMin = new Vector2(0.5f, 0f);
            _indicator.anchorMax = new Vector2(0.5f, 1f);
            _indicator.sizeDelta = new Vector2(3f, 0f);

            _listText = DebugUIFactory.CreateText(transform, "ZoneList", 14);
        }

        protected override void SubscribeSession(StageSession session)
        {
            session.Runner.TurnBegan += OnTurnBegan;
            session.Runner.TurnResolved += OnTurnResolved;
        }

        protected override void UnsubscribeSession(StageSession session)
        {
            session.Runner.TurnBegan -= OnTurnBegan;
            session.Runner.TurnResolved -= OnTurnResolved;
        }

        private void OnTurnBegan(int turn) => Render();
        private void OnTurnResolved(TurnReport report) => Render();

        protected override void Render()
        {
            if (Session == null) return;

            RebuildMarkers();

            var fraction = Mathf.InverseLerp(Heartbeat.MinValue, Heartbeat.MaxValue, Session.Heartbeat.Value);
            _indicator.anchorMin = new Vector2(fraction, 0f);
            _indicator.anchorMax = new Vector2(fraction, 1f);

            var currentTurn = Session.Runner.CurrentTurn;
            var lines = Session.Keys.KeyTurns.OrderBy(t => t)
                .Select(turn =>
                {
                    var text = Session.Keys.Zones.TryGetValue(turn, out var zone)
                        ? $"{turn}턴: {zone.StartSlot}~{zone.StartSlot + zone.Width - 1}"
                        : $"{turn}턴: (미확정)";
                    return turn == currentTurn ? $"▶ {text}" : $"   {text}";
                });
            _listText.text = string.Join("   ", lines);
        }

        private void RebuildMarkers()
        {
            foreach (var marker in _markers) Destroy(marker);
            _markers.Clear();

            var currentTurn = Session.Runner.CurrentTurn;
            foreach (var pair in Session.Keys.Zones)
            {
                var turn = pair.Key;
                var zone = pair.Value;

                var markerGo = new GameObject($"Zone_{turn}", typeof(RectTransform), typeof(Image));
                markerGo.transform.SetParent(_barTrack, false);
                markerGo.GetComponent<Image>().color = turn == currentTurn
                    ? new Color(1f, 0.85f, 0.2f, 0.9f)
                    : new Color(0.3f, 0.6f, 0.9f, 0.6f);

                var rt = (RectTransform)markerGo.transform;
                var min = Mathf.InverseLerp(Heartbeat.MinValue, Heartbeat.MaxValue, zone.StartSlot);
                var max = Mathf.InverseLerp(Heartbeat.MinValue, Heartbeat.MaxValue, zone.StartSlot + zone.Width);
                rt.anchorMin = new Vector2(min, 0f);
                rt.anchorMax = new Vector2(max, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                markerGo.transform.SetSiblingIndex(0);
                _markers.Add(markerGo);
            }

            _indicator.SetAsLastSibling();
        }
    }
}
