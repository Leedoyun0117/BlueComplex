using System.Collections;
using BlueComplex.Core.Stability;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 나츠 초상화("Natsu Portrait", MainHud)의 표정. "표정과 반응" 기획표 그대로:
    /// 평소엔 3초마다 눈을 깜박이고, 매우 침체·매우 흥분이면 당황, 각 쿼터의 키 대화 동안은 집중,
    /// 안정 상태로 막 들어서면 안도(일회성)로 잠깐 바뀐다. 판정 자체는 <see cref="PortraitReactionRules"/>가 한다 —
    /// 이 컴포넌트는 그 결과로 스프라이트만 고른다.
    ///
    /// 프리팹에 미리 안 붙어 있다 — 유키와 달리 정지 사진 카드였던 자리라 컴포넌트가 없다. HeartRateController가
    /// "Natsu Portrait" 오브젝트를 이름으로 찾아 없으면 붙인다(런타임 자동 부착, 프리팹 굽기 불필요). 무표정 스프라이트는
    /// 이미 그 오브젝트의 Image에 구워져 있는 것(natsu_neutral)을 그대로 기준으로 삼고, 나머지 표정은
    /// Assets/Resources/UI/Portraits/Natsu/(natsu_blink·natsu_flustered·natsu_focus·natsu_relief)에서 스스로 찾는다 —
    /// 없으면 그 표정 대신 조용히 무표정을 유지한다.
    /// </summary>
    public sealed class NatsuPortraitView : MonoBehaviour
    {
        [SerializeField] private Image _image;

        private Sprite _neutral;
        private Sprite _blink;
        private Sprite _flustered;
        private Sprite _focus;
        private Sprite _relief;

        /// <summary>지금 바탕 표정(무표정 또는 당황). 집중은 별도 오버레이(_focused)라 여기 안 들어가고, 안도는 일회성 펄스라 역시 안 들어간다.</summary>
        private Sprite _baseSprite;

        private bool _focused;
        private Tween _reliefTween;
        private HeartbeatState? _lastState;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            _neutral = _image != null ? _image.sprite : null;
            _baseSprite = _neutral;

            _blink = Resources.Load<Sprite>("UI/Portraits/Natsu/natsu_blink");
            _flustered = Resources.Load<Sprite>("UI/Portraits/Natsu/natsu_flustered");
            _focus = Resources.Load<Sprite>("UI/Portraits/Natsu/natsu_focus");
            _relief = Resources.Load<Sprite>("UI/Portraits/Natsu/natsu_relief");

            StartCoroutine(BlinkLoop());
        }

        /// <summary>쿼터의 키 대화 동안 켠다 — HeartRateController.SyncTurnState가 HeartRateBarView.SetKeyTurn과 같은 타이밍에 부른다.</summary>
        public void SetFocused(bool focused)
        {
            _focused = focused;
            Refresh();
        }

        /// <summary>턴 결과의 심박수 상태로 당황/안도를 갱신한다 — HeartRateController가 심박수 표시를 갱신하는 시점(PlayTurnResult·아이템 사용·세션 시작)에 같이 부른다.</summary>
        public void ReactToHeartbeat(HeartbeatState state)
        {
            var enteringStable = state == HeartbeatState.Stable && _lastState.HasValue && _lastState.Value != HeartbeatState.Stable;
            _lastState = state;

            // 안도 펄스가 끝나고 돌아갈 자리도 지금 상태(안정 → 당황 아님)로 먼저 갱신해 둔다 — 안 그러면 펄스가 끝난 뒤
            // 막 벗어난 당황 표정으로 되돌아가 버린다.
            _baseSprite = PortraitReactionRules.IsNatsuFlustered(state) ? _flustered : _neutral;

            if (enteringStable) PulseRelief();
            Refresh();
        }

        /// <summary>안정 상태로 막 들어선 순간 잠깐 안도 표정으로 바뀌었다가 평소로 돌아온다. 깜박임보다 조금 더 오래 머문다(스펙: "눈을 길게 감았다가 뜬다").</summary>
        private void PulseRelief()
        {
            if (_image == null || _relief == null) return;

            _reliefTween?.Kill();
            var hold = Mathf.Max(0.6f, UiMotion.Settings.portraitFlash * 2f);
            _image.sprite = _relief;
            _reliefTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .AppendInterval(hold)
                .AppendCallback(Refresh);
        }

        private void Refresh()
        {
            if (_reliefTween != null && _reliefTween.IsActive()) return; // 안도 펄스가 도는 중엔 덮지 않는다.
            if (_image == null) return;

            if (_focused && _focus != null) { _image.sprite = _focus; return; }
            _image.sprite = _baseSprite != null ? _baseSprite : _neutral;
        }

        /// <summary>3초마다 짧게 눈을 감는다 — 집중·당황·안도 등 특수 표정이 떠 있는 동안엔 끼어들지 않는다("아래에 해당하지 않는 일반 상황"의 근사).</summary>
        private IEnumerator BlinkLoop()
        {
            var wait = new WaitForSeconds(3f);
            while (true)
            {
                yield return wait;

                if (_image == null || _blink == null) continue;
                if (_focused || _baseSprite != _neutral) continue;
                if (_reliefTween != null && _reliefTween.IsActive()) continue;

                var prior = _image.sprite;
                _image.sprite = _blink;
                yield return new WaitForSeconds(0.15f);
                if (_image.sprite == _blink) _image.sprite = prior;
            }
        }

        private void OnDisable() => DOTween.Kill(this);
    }
}
