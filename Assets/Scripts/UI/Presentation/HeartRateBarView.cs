using BlueComplex.Core.Stability;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 심박수 모니터의 왼쪽 화면(<see cref="EcgWaveGraphic"/>)을 다룬다: 흐르는 심전도 파형 + 현재 쿼터의 목표 심박수 띠.
    /// 세로축이 심박수 눈금이라 목표 띠와 파형 봉우리가 같은 높이 함수(<see cref="HeartbeatMonitorScale"/>)를 쓴다 —
    /// 이 뷰는 목표 구역의 하한·상한 BPM과 현재 BPM을 그래픽에 넘기기만 하고, 높이 계산은 그래픽 한 곳에서만 일어난다.
    /// 예전의 색 구간 막대·0~200 눈금 띠·현재 값 마커는 없어졌다. 표시만 한다 — 판정 없음.
    ///
    /// 목표 띠는 지금 진행 중인 쿼터의 것 하나만 그린다(미래 쿼터의 구역까지 그리면 띠가 겹쳐 파형과 비교하기 어렵다 — 전체 목표는 쿼터 HUD의 오버레이에 있다).
    /// 키 턴(쿼터 마지막 턴)에는 띠가 강조되며 맥동하고, 키 판정 결과가 오면 성공(초록)/실패(붉음)로 번쩍인다.
    /// </summary>
    public sealed class HeartRateBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] _keyZoneLabels;
        [SerializeField] private EcgWaveGraphic _ecg;

        private static readonly Color CurrentColor = new Color32(255, 210, 70, 255);
        private static readonly Color SuccessColor = new Color32(90, 230, 130, 255);
        private static readonly Color FailColor = new Color32(214, 84, 84, 255);

        private int _shownQuarter;
        private KeyZone? _shownZone;
        private bool _keyTurn;
        private bool? _result;

        private TMP_Text Label => _keyZoneLabels != null && _keyZoneLabels.Length > 0 ? _keyZoneLabels[0] : null;

        public EcgWaveGraphic Ecg => _ecg;

        /// <summary>세션이 시작될 때 코어의 구간표에서 모니터 눈금(생존 구간의 위쪽 끝)을 가져온다.</summary>
        public void BindScale(HeartbeatZone zone)
        {
            if (_ecg != null) _ecg.Scale = HeartbeatMonitorScale.FromZone(zone);
        }

        /// <summary>바에 그릴 목표 구역을 정한다(null이면 감춘다). 같은 쿼터·같은 구역이면 이미 기록된 성공/실패 표시를 그대로 둔다 —
        /// 매 턴 다시 불러도 결과 색이 지워지지 않는다. 쿼터가 바뀌면 띠가 새 자리로 부드럽게 옮겨 간다.</summary>
        public void SetTargetZone(int quarter, KeyZone? zone)
        {
            if (quarter == _shownQuarter && Equals(zone, _shownZone)) return;

            _shownQuarter = quarter;
            _shownZone = zone;
            _result = null;
            _keyTurn = false;
            Refresh(animate: true);
        }

        /// <summary>지금 턴이 쿼터의 마지막 턴(키 판정 턴)인지. true면 띠를 강조한다.</summary>
        public void SetKeyTurn(bool keyTurn)
        {
            if (_keyTurn == keyTurn) return;

            _keyTurn = keyTurn;
            ApplyEmphasis();
        }

        /// <summary>세션이 새로 시작될 때 표시를 처음으로 되돌린다.</summary>
        public void ResetTargetZone()
        {
            _shownQuarter = 0;
            _shownZone = null;
            _result = null;
            _keyTurn = false;
            Refresh(animate: false);
        }

        /// <summary>쿼터 마지막 턴 종료 시점의 키 판정 결과를 기록한다. 지금 바에 그려진 쿼터의 결과일 때만 반영한다.
        /// 호출 시점(연출 타이밍)은 Presenter가 쥔다 — 이 메서드는 상태만 반영한다.</summary>
        public void RecordKeyResult(int quarter, bool success)
        {
            if (quarter != _shownQuarter || _shownZone == null) return;

            _result = success;
            Refresh(animate: false);
            if (_ecg != null) _ecg.FlashBand(UiMotion.Settings.bandResultFlash);
        }

        /// <summary>파형의 높이·속도(BPM)와 선 색을 정한다. snap이면 바로, 아니면 <c>heartTransition</c> 동안 부드럽게 — 턴 결과 연출의 심박수 이동과 같은 타이밍이다.</summary>
        public void SetPulse(int bpm, Color color, bool irregular, bool snap)
        {
            if (_ecg != null) _ecg.SetPulse(bpm, color, irregular, snap, UiMotion.Settings.heartTransition);
        }

        private void Refresh(bool animate)
        {
            var label = Label;

            if (_shownZone is not { } zone)
            {
                if (_ecg != null) _ecg.HideBand();
                // 라벨은 모니터 화면에 따로 붙어 있어서 띠가 꺼져도 남는다.
                if (label != null) label.text = string.Empty;
                return;
            }

            // 목표 심박수는 구역의 양 끝 값 그대로 — 쿼터 오버레이의 문구와 같다. 띠 경계도 이 두 값을 그대로 쓴다.
            var lo = zone.StartSlot;
            var hi = zone.StartSlot + zone.Width - 1;
            var tint = _result switch { true => SuccessColor, false => FailColor, _ => CurrentColor };

            if (_ecg != null) _ecg.ShowBand(lo, hi, tint, animate, UiMotion.Settings.bandMove);

            if (label != null)
            {
                label.text = $"목표 {lo}~{hi}";
                label.color = _result == false ? new Color(1f, 1f, 1f, 0.5f) : tint;
            }

            ApplyEmphasis();
        }

        private void ApplyEmphasis()
        {
            if (_ecg == null) return;
            _ecg.SetBandEmphasis(_keyTurn && _result == null && _shownZone != null, UiMotion.Settings.keyBandPulse);
        }
    }
}
