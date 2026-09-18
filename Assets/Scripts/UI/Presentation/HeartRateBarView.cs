using System.Collections.Generic;
using BlueComplex.Core.Stability;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 0~200 심박수 바 위에 현재 값 마커와 키 구역 4개를 그린다. 표시만 한다 — 판정 없음.
    /// 세그먼트 배경(구간 색칠)은 정적이라 셋업 도구가 프리팹에 미리 색칠해 둔다.
    /// </summary>
    public sealed class HeartRateBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _barArea;
        [SerializeField] private RectTransform _marker;
        [SerializeField] private RectTransform[] _keyZoneOverlays;
        [SerializeField] private Image[] _keyZoneImages;

        private static readonly Color InactiveZoneColor = new Color32(255, 255, 255, 70);
        private static readonly Color ActiveZoneColor = new Color32(255, 210, 70, 200);

        private int[] _overlayTurns;

        /// <summary>스테이지 시작 시 한 번 호출 — 확정된 키 구역 전부를 턴 1부터 보여준다.</summary>
        public void SetKeyZones(IReadOnlyDictionary<int, KeyZone> zonesByTurn)
        {
            var turns = new List<int>(zonesByTurn.Keys);
            turns.Sort();

            _overlayTurns = new int[_keyZoneOverlays.Length];

            for (var i = 0; i < _keyZoneOverlays.Length; i++)
            {
                if (i < turns.Count)
                {
                    var turn = turns[i];
                    var zone = zonesByTurn[turn];
                    _overlayTurns[i] = turn;

                    var minX = zone.StartSlot / (float)Heartbeat.MaxValue;
                    var maxX = (zone.StartSlot + zone.Width) / (float)Heartbeat.MaxValue;

                    var overlay = _keyZoneOverlays[i];
                    overlay.gameObject.SetActive(true);
                    overlay.anchorMin = new Vector2(minX, 0f);
                    overlay.anchorMax = new Vector2(maxX, 1f);
                    overlay.offsetMin = Vector2.zero;
                    overlay.offsetMax = Vector2.zero;

                    _keyZoneImages[i].color = InactiveZoneColor;
                }
                else
                {
                    _overlayTurns[i] = -1;
                    _keyZoneOverlays[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>현재 턴에 해당하는 키 구역만 강조한다. 키 턴이 아니면 전부 비강조.</summary>
        public void SetActiveTurn(int turn)
        {
            if (_overlayTurns == null) return;

            for (var i = 0; i < _overlayTurns.Length; i++)
                _keyZoneImages[i].color = _overlayTurns[i] == turn ? ActiveZoneColor : InactiveZoneColor;
        }

        public void MoveMarker(int value, bool animate)
        {
            var t = Mathf.Clamp01(value / (float)Heartbeat.MaxValue);
            var targetX = t * _barArea.rect.width;

            if (animate)
                _marker.DOAnchorPosX(targetX, 0.25f).SetEase(Ease.OutQuad);
            else
                _marker.anchoredPosition = new Vector2(targetX, _marker.anchoredPosition.y);
        }
    }
}
