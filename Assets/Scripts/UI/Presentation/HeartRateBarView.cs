using BlueComplex.Core.Stability;
using BlueComplex.UI.Layout;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 심박수 모니터의 왼쪽 화면: 심전도 파형(<see cref="EcgWaveGraphic"/>, BPM에 맞춰 흐른다) + 화면 아래 얇은 0~200 눈금 띠.
    /// 띠 위에 현재 값 마커를 그리고, 지금 진행 중인 쿼터의 목표 구역 하나만 강조한다(모니터 안 "목표 N~M" 문구 포함). 표시만 한다 — 판정 없음.
    /// 예전의 색 구간 막대는 없앴다 — 띠는 무채색이고 즉사 구간(양 끝)만 붉게 표시한다.
    ///
    /// UI 기획서는 쿼터별 목표 심박수를 숫자로만 보여주지만(쿼터 HUD의 전체 스테이지 오버레이), 플레이어가
    /// "지금 심박수가 목표에 가까운가"를 판단하려면 현재 값 옆에 목표 구간이 있어야 해서 현재 쿼터 것만 바에 남겼다.
    /// 나머지 쿼터의 구역은 바에 그리지 않는다 — 여러 개를 그리면 구간이 겹쳐 탭이 포개지고 미래 쿼터 정보가 바를 어지럽힌다.
    ///
    /// 구역 슬롯 배열은 프리팹에 이미 구워진 필드를 그대로 쓴다(구 프리팹은 슬롯 4개) — 첫 슬롯 하나만 쓰고
    /// 나머지는 숨긴다. 재생성한 프리팹은 슬롯이 하나다.
    /// </summary>
    public sealed class HeartRateBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _barArea;
        [SerializeField] private RectTransform _marker;
        [SerializeField] private RectTransform[] _keyZoneOverlays;
        [SerializeField] private Image[] _keyZoneImages;
        [SerializeField] private TMP_Text[] _keyZoneLabels;
        [SerializeField] private EcgWaveGraphic _ecg;

        private static readonly Color CurrentColor = new Color32(255, 210, 70, 255);
        private static readonly Color SuccessColor = new Color32(90, 230, 130, 255);
        private static readonly Color FailColor = new Color32(150, 60, 60, 160);

        private int _shownQuarter;
        private KeyZone? _shownZone;
        private bool? _result;
        private Tween _pulseTween;

        private bool HasBand => _keyZoneOverlays != null && _keyZoneOverlays.Length > 0 && _keyZoneImages != null &&
                                _keyZoneImages.Length > 0;

        /// <summary>바에 그릴 목표 구역을 정한다(null이면 감춘다). 같은 쿼터·같은 구역이면 이미 기록된 성공/실패 표시를 그대로 둔다 —
        /// 매 턴 다시 불러도 결과 색이 지워지지 않는다.</summary>
        public void SetTargetZone(int quarter, KeyZone? zone)
        {
            if (quarter == _shownQuarter && Equals(zone, _shownZone)) return;

            _shownQuarter = quarter;
            _shownZone = zone;
            _result = null;
            Refresh();
        }

        /// <summary>세션이 새로 시작될 때 표시를 처음으로 되돌린다.</summary>
        public void ResetTargetZone()
        {
            _shownQuarter = 0;
            _shownZone = null;
            _result = null;
            Refresh();
        }

        /// <summary>쿼터 마지막 턴 종료 시점의 키 판정 결과를 기록한다. 지금 바에 그려진 쿼터의 결과일 때만 반영한다.
        /// 호출 시점(연출 타이밍)은 Presenter가 쥔다 — 이 메서드는 상태만 반영한다.</summary>
        public void RecordKeyResult(int quarter, bool success)
        {
            if (quarter != _shownQuarter || _shownZone == null) return;

            _result = success;
            Refresh();
            PlayResultPop();
        }

        /// <summary>심전도 파형의 속도(BPM)와 선 색을 정한다. snap이면 속도도 바로 그 값으로.</summary>
        public void SetPulse(int bpm, Color color, bool snap)
        {
            if (_ecg != null) _ecg.SetPulse(bpm, color, snap);
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

        private void Refresh()
        {
            if (!HasBand) return;

            _pulseTween?.Kill();

            // 첫 슬롯만 쓴다. 구 프리팹에 남은 나머지 슬롯은 숨긴다.
            for (var slot = 1; slot < _keyZoneOverlays.Length; slot++)
                _keyZoneOverlays[slot].gameObject.SetActive(false);

            var overlay = _keyZoneOverlays[0];
            overlay.localScale = Vector3.one;

            var label = _keyZoneLabels != null && _keyZoneLabels.Length > 0 ? _keyZoneLabels[0] : null;

            if (_shownZone is not { } zone)
            {
                overlay.gameObject.SetActive(false);
                // 라벨은 구역 탭의 자식이 아니라 모니터 화면에 따로 붙어 있어서 탭이 꺼져도 남는다.
                if (label != null) label.text = string.Empty;
                return;
            }

            // 행(KeyZoneRow) 밖으로 나가는 구역이 없도록 0~1로 가둔다.
            var min = Mathf.Clamp01(zone.StartSlot / (float)Heartbeat.MaxValue);
            var max = Mathf.Max(Mathf.Clamp01((zone.StartSlot + zone.Width) / (float)Heartbeat.MaxValue), min);

            overlay.gameObject.SetActive(true);
            overlay.anchorMin = new Vector2(min, 0f);
            overlay.anchorMax = new Vector2(max, 1f);
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            var image = _keyZoneImages[0];

            // 목표 심박수는 구역의 양 끝 값 그대로 — 오버레이에 쓰는 문구와 같다. 라벨은 어두운 화면 위 글자라 구역 색을 그대로 따른다.
            if (label != null) label.text = $"목표 {zone.StartSlot}~{zone.StartSlot + zone.Width - 1}";

            if (_result == true)
            {
                image.color = SuccessColor;
                if (label != null) label.color = SuccessColor;
            }
            else if (_result == false)
            {
                image.color = FailColor;
                if (label != null) label.color = new Color(1f, 1f, 1f, 0.45f);
            }
            else
            {
                image.color = CurrentColor;
                if (label != null) label.color = CurrentColor;
                _pulseTween = overlay.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }

        private void PlayResultPop()
        {
            if (!HasBand || !_keyZoneOverlays[0].gameObject.activeSelf) return;

            var overlay = _keyZoneOverlays[0];
            _pulseTween?.Kill();
            overlay.localScale = Vector3.one;
            _pulseTween = overlay.DOScale(1.15f, 0.18f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad)
                .OnComplete(() => overlay.localScale = Vector3.one);
        }

        private void OnDisable() => _pulseTween?.Kill();
    }
}
