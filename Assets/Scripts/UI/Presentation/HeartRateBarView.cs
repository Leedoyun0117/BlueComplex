using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Stability;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 0~200 심박수 바 위에 현재 값 마커를 그리고, 바와는 분리된 레이어(위쪽 KeyZoneRow, 셋업 도구가
    /// 그렇게 배치한다)에 키 구역 4개를 턴 라벨과 함께 그린다. 표시만 한다 — 판정 없음.
    /// 세그먼트 배경(구간 색칠)은 정적이라 셋업 도구가 프리팹에 미리 색칠해 둔다.
    /// </summary>
    public sealed class HeartRateBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _barArea;
        [SerializeField] private RectTransform _marker;
        [SerializeField] private RectTransform[] _keyZoneOverlays;
        [SerializeField] private Image[] _keyZoneImages;
        [SerializeField] private TMP_Text[] _keyZoneLabels;

        private const float LaneGapPixels = 1f;

        private static readonly Color UpcomingColor = new Color32(140, 140, 150, 140);
        private static readonly Color CurrentColor = new Color32(255, 210, 70, 255);
        private static readonly Color SuccessColor = new Color32(90, 230, 130, 255);
        private static readonly Color FailColor = new Color32(150, 60, 60, 110);

        /// <summary>슬롯 i가 담당하는 턴. 안 쓰는 슬롯은 -1.</summary>
        private int[] _overlayTurns;

        /// <summary>슬롯별 판정 결과. null=아직 판정 전, true=성공, false=실패.</summary>
        private bool?[] _results;

        /// <summary>재호출로 레이아웃이 실제로 바뀌었는지 판단하는 서명 — 매 턴 OnTurnBegan이
        /// SetKeyZones를 다시 불러도(스테이지 중엔 구역 배정이 안 바뀐다) _results를 지우지 않기 위함.</summary>
        private List<int> _lastTurnSignature;

        private Tween[] _pulseTweens;
        private int _activeTurn = -1;

        /// <summary>스테이지 시작 시(또는 매 턴 재확인 시) 확정된 키 구역 전부를 턴 1부터 보여준다.
        /// 구역 배정 자체가 이전과 같으면(같은 스테이지 진행 중) 이미 기록된 성공/실패 상태를 보존한다.</summary>
        public void SetKeyZones(IReadOnlyDictionary<int, KeyZone> zonesByTurn)
        {
            var turns = new List<int>(zonesByTurn.Keys);
            turns.Sort();

            var isFreshLayout = _lastTurnSignature == null || !turns.SequenceEqual(_lastTurnSignature);
            _lastTurnSignature = turns;

            _overlayTurns = new int[_keyZoneOverlays.Length];
            if (isFreshLayout) _results = new bool?[_keyZoneOverlays.Length];
            _pulseTweens ??= new Tween[_keyZoneOverlays.Length];

            var used = Mathf.Min(turns.Count, _keyZoneOverlays.Length);
            var ranges = new (float min, float max)[used];
            for (var i = 0; i < used; i++)
            {
                var zone = zonesByTurn[turns[i]];
                // 행(KeyZoneRow) 밖으로 나가는 탭이 없도록 0~1로 가둔다.
                var min = Mathf.Clamp01(zone.StartSlot / (float)Heartbeat.MaxValue);
                var max = Mathf.Clamp01((zone.StartSlot + zone.Width) / (float)Heartbeat.MaxValue);
                ranges[i] = (min, Mathf.Max(max, min));
            }

            var lanes = AssignLanes(ranges, out var laneCount);

            for (var i = 0; i < _keyZoneOverlays.Length; i++)
            {
                if (i < used)
                {
                    var turn = turns[i];
                    _overlayTurns[i] = turn;

                    var laneHeight = 1f / laneCount;
                    var overlay = _keyZoneOverlays[i];
                    overlay.gameObject.SetActive(true);
                    overlay.anchorMin = new Vector2(ranges[i].min, lanes[i] * laneHeight);
                    overlay.anchorMax = new Vector2(ranges[i].max, (lanes[i] + 1) * laneHeight);
                    // 위아래 레인 사이에 1px씩 틈을 둔다.
                    overlay.offsetMin = new Vector2(0f, LaneGapPixels);
                    overlay.offsetMax = new Vector2(0f, -LaneGapPixels);

                    if (_keyZoneLabels != null && i < _keyZoneLabels.Length && _keyZoneLabels[i] != null)
                        _keyZoneLabels[i].text = $"{turn}턴";
                }
                else
                {
                    _overlayTurns[i] = -1;
                    _keyZoneOverlays[i].gameObject.SetActive(false);
                }
            }

            if (isFreshLayout) ApplyAllStates();
        }

        /// <summary>현재 턴에 해당하는 키 구역만 강조(펄스)한다. 나머지는 이미 기록된 성공/실패/대기 상태로 표시한다.</summary>
        public void SetActiveTurn(int turn)
        {
            if (_overlayTurns == null) return;
            _activeTurn = turn;
            ApplyAllStates();
        }

        /// <summary>턴 결과 판정 후 해당 턴의 키 구역이 성공/실패했는지 기록한다.
        /// 호출 시점(연출 타이밍)은 Presenter가 쥔다 — 이 메서드는 상태만 반영한다.</summary>
        public void RecordKeyZoneResult(int turn, bool success)
        {
            if (_overlayTurns == null) return;

            for (var i = 0; i < _overlayTurns.Length; i++)
            {
                if (_overlayTurns[i] != turn) continue;
                _results[i] = success;
                ApplyState(i);
                break;
            }
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

        /// <summary>구간이 겹치는 탭끼리는 서로 다른 레인(세로 단)에 놓는다. 턴 순서대로 겹치지 않는
        /// 가장 낮은 레인(0=바에 가장 가까운 아래쪽)에 배정하므로 안 겹치면 전부 레인 0 한 줄이다.
        /// 같은 레인의 탭은 구간이 겹치지 않아 라벨도 포개지지 않는다.</summary>
        private static int[] AssignLanes((float min, float max)[] ranges, out int laneCount)
        {
            var lanes = new int[ranges.Length];
            laneCount = 1;

            for (var i = 0; i < ranges.Length; i++)
            {
                var lane = 0;
                while (OverlapsLane(ranges, lanes, i, lane)) lane++;
                lanes[i] = lane;
                laneCount = Mathf.Max(laneCount, lane + 1);
            }

            return lanes;
        }

        private static bool OverlapsLane((float min, float max)[] ranges, int[] lanes, int index, int lane)
        {
            for (var j = 0; j < index; j++)
            {
                if (lanes[j] != lane) continue;
                if (ranges[index].min < ranges[j].max && ranges[j].min < ranges[index].max) return true;
            }

            return false;
        }

        private void ApplyAllStates()
        {
            for (var i = 0; i < _overlayTurns.Length; i++) ApplyState(i);
        }

        private void ApplyState(int i)
        {
            if (_overlayTurns[i] < 0) return;

            var image = _keyZoneImages[i];
            var label = _keyZoneLabels != null && i < _keyZoneLabels.Length ? _keyZoneLabels[i] : null;
            var overlay = _keyZoneOverlays[i];

            _pulseTweens[i]?.Kill();
            overlay.localScale = Vector3.one;

            if (_overlayTurns[i] == _activeTurn)
            {
                image.color = CurrentColor;
                if (label != null) label.color = Color.black;
                _pulseTweens[i] = overlay.DOScale(1.12f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                return;
            }

            var result = _results != null && i < _results.Length ? _results[i] : null;
            if (result == true)
            {
                image.color = SuccessColor;
                if (label != null) label.color = Color.black;
            }
            else if (result == false)
            {
                image.color = FailColor;
                if (label != null) label.color = new Color(1f, 1f, 1f, 0.5f);
            }
            else
            {
                image.color = UpcomingColor;
                if (label != null) label.color = Color.white;
            }
        }

        private void OnDisable()
        {
            if (_pulseTweens == null) return;
            foreach (var tween in _pulseTweens) tween?.Kill();
        }
    }
}
