using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>아이템 슬롯 4개(기획서: 시작 시 4개가 주어진다). 표시만 한다 — 판정 없음, 코어 이벤트도 직접 구독하지 않는다.
    /// 보유하지 않은 슬롯은 ItemSlotView가 스스로 감추므로, 코어의 보유 한도(ItemInventory.Capacity)가 몇이든 슬롯 수는 그대로 둔다.
    ///
    /// 왼쪽에 붙은 "접기" 탭을 누르면 패널이 화면 오른쪽 밖으로 밀려 들어가고(탭만 남는다), 탭이 "펼치기"로 바뀐다. 다시 누르면 돌아온다.
    /// 펼친 자리는 Awake 시점의 앵커를 그대로 기억한다 — MainHud의 배치가 바뀌어도 여기 하드코딩할 필요가 없다.</summary>
    public sealed class ItemDisplayPanel : MonoBehaviour
    {
        private const float FoldDuration = 0.3f;

        /// <summary>접을 때 패널 폭에 더해 더 밀어내는 양(화면 폭 대비). 패널이 캔버스 오른쪽 끝(1.0)을 완전히 넘어가야 한다 —
        /// 안 그러면 CRT 배럴 왜곡이 가장자리를 안쪽으로 당겨서 접힌 패널의 왼쪽 띠가 화면에 삐져나온다.</summary>
        private const float FoldOvershoot = 0.03f;

        [SerializeField] private ItemSlotView[] _slots;
        [SerializeField] private Button _foldButton;
        [SerializeField] private TMP_Text _foldLabel;

        private RectTransform _rect;
        private Vector2 _openAnchorMin;
        private Vector2 _openAnchorMax;
        private Tween _foldTween;

        public RectTransform Root => (RectTransform)transform;
        public int SlotCount => _slots?.Length ?? 0;
        public bool IsFolded { get; private set; }

        public ItemSlotView GetSlot(int index) => _slots[index];

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _openAnchorMin = _rect.anchorMin;
            _openAnchorMax = _rect.anchorMax;

            if (_foldButton != null) _foldButton.onClick.AddListener(ToggleFold);
        }

        private void OnDestroy()
        {
            _foldTween?.Kill();
            if (_foldButton != null) _foldButton.onClick.RemoveListener(ToggleFold);
        }

        public void ToggleFold() => SetFolded(!IsFolded);

        public void SetFolded(bool folded)
        {
            if (IsFolded == folded) return;
            IsFolded = folded;
            if (_foldLabel != null) _foldLabel.text = folded ? "펼치기" : "접기";

            // 패널 폭(+여유)만큼 오른쪽으로 밀면 왼쪽에 붙은 탭만 화면 오른쪽 끝에 남는다.
            var shift = new Vector2(_openAnchorMax.x - _openAnchorMin.x + FoldOvershoot, 0f);
            var targetMin = folded ? _openAnchorMin + shift : _openAnchorMin;
            var targetMax = folded ? _openAnchorMax + shift : _openAnchorMax;

            _foldTween?.Kill();
            _foldTween = DOTween.Sequence()
                .Append(_rect.DOAnchorMin(targetMin, FoldDuration).SetEase(Ease.OutQuad))
                .Join(_rect.DOAnchorMax(targetMax, FoldDuration).SetEase(Ease.OutQuad));
        }
    }
}
