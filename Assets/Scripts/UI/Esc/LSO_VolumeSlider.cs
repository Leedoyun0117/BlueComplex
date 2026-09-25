using BlueComplex.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Esc
{
    /// <summary>
    /// 설정창의 볼륨 슬라이더 한 개. <see cref="channel"/>로 어느 채널(마스터/효과음/BGM/앰비언트)인지 정한다.
    /// 슬라이더마다 하나씩 붙인다.
    ///
    /// 믹서·저장은 전부 <see cref="AudioSettingsController"/>가 한다. 여기는 슬라이더와 그쪽을 잇기만 한다.
    ///
    /// ─── 컨트롤러의 슬라이더 칸은 비워 둘 것 ────────────────────────────────
    /// AudioSettingsController에도 슬라이더를 물리는 칸이 네 개 있다. 거기에도 물리고 이 컴포넌트도 쓰면
    /// 한 슬라이더에 리스너가 둘 붙어 같은 값을 두 번 쓴다. 둘 중 하나만 쓸 것 — 이 컴포넌트를 쓰면
    /// 설정창이 컨트롤러가 어느 씬에 있든 상관없이 스스로 찾아 붙으므로, 창을 프리팹으로 떼어 두기 쉽다.
    /// </summary>
    public sealed class LSO_VolumeSlider : MonoBehaviour
    {
        [SerializeField] private AudioChannel channel = AudioChannel.Master;
        [SerializeField] private Slider slider;

        [Tooltip("비워 두면 씬에서 찾는다. 씬에 없으면 볼륨이 조절되지 않는다.")]
        [SerializeField] private AudioSettingsController controller;

        [Tooltip("옆에 퍼센트를 띄울 때만. 비워 둬도 된다.")]
        [SerializeField] private TMP_Text valueLabel;

        private void Awake()
        {
            if (slider == null) slider = GetComponent<Slider>();

            if (slider == null)
            {
                Debug.LogWarning($"[LSO_VolumeSlider] slider가 비어 있다({channel}). 볼륨을 조절할 수 없다.", this);
                enabled = false;
                return;
            }

            // 컨트롤러가 0~1을 그대로 dB로 바꾸므로 범위를 여기서 못 박는다 — 인스펙터에서 다른 값으로
            // 잡혀 있으면 슬라이더 끝이 무음이 아니거나 최대가 안 나온다.
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            if (controller == null) controller = FindFirstObjectByType<AudioSettingsController>();
            if (controller == null)
            {
                Debug.LogWarning($"[LSO_VolumeSlider] 씬에서 AudioSettingsController를 찾지 못했다({channel}). " +
                    "볼륨이 조절되지도, 저장값이 복원되지도 않는다.", this);
            }
        }

        private void OnEnable()
        {
            if (controller == null) return;

            slider.onValueChanged.AddListener(OnSliderChanged);
            Pull();
        }

        private void OnDisable()
        {
            if (controller == null) return;
            slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        /// <summary>지금 채널 볼륨을 슬라이더에 채운다.</summary>
        private void Pull()
        {
            var value = controller.GetVolume(channel);
            // 값을 채우는 것뿐인데 onValueChanged가 울려 되돌아오면 안 된다.
            slider.SetValueWithoutNotify(value);
            UpdateLabel(value);
        }

        private void OnSliderChanged(float value)
        {
            // 믹서 적용과 PlayerPrefs 저장까지 컨트롤러가 한 번에 한다.
            controller.SetVolume(channel, value);
            UpdateLabel(value);
        }

        private void UpdateLabel(float value)
        {
            if (valueLabel == null) return;
            valueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
