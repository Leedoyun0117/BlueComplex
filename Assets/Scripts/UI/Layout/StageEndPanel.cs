using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>스테이지 종료 오버레이. 표시만 한다 — 판정 없음. 재시작 버튼 클릭은 그대로 중계된다.</summary>
    public sealed class StageEndPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _overlay;
        [SerializeField] private TMP_Text _outcomeText;
        [SerializeField] private Button _restartSameSeedButton;
        [SerializeField] private Button _restartNewSeedButton;

        public RectTransform Root => (RectTransform)transform;
        public Button RestartSameSeedButton => _restartSameSeedButton;
        public Button RestartNewSeedButton => _restartNewSeedButton;

        public void Show(string outcomeText)
        {
            _overlay.SetActive(true);
            _outcomeText.text = outcomeText;
        }

        public void Hide() => _overlay.SetActive(false);
    }
}
