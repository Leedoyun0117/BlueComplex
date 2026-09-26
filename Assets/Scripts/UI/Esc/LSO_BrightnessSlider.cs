using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Esc
{
    /// <summary>
    /// 설정창의 밝기 슬라이더. 끄는 동안 화면이 바로 따라오고, 창이 닫힐 때 저장한다.
    ///
    /// <see cref="LSO_EscPanel"/>의 Opened/Closed 이벤트만 구독한다 — 항목이 늘어나도 그쪽을 고칠 필요가 없다.
    /// 화면에 실제로 반영하는 건 <see cref="LSO_BrightnessApplier"/>다. 여기는 값을 만지기만 한다.
    /// </summary>
    public sealed class LSO_BrightnessSlider : MonoBehaviour
    {
        [SerializeField] private LSO_EscPanel panel;
        [SerializeField] private Slider slider;
        [Tooltip("옆에 퍼센트를 띄울 때만. 비워 둬도 된다.")]
        [SerializeField] private TMP_Text valueLabel;

        private void Awake()
        {
            if (slider == null)
            {
                Debug.LogWarning("[LSO_BrightnessSlider] slider가 비어 있다. 밝기를 조절할 수 없다.", this);
                enabled = false;
                return;
            }

            // 범위는 설정 쪽이 정한다 — 인스펙터 값과 어긋나 슬라이더 끝에서 잘리는 일이 없게 여기서 맞춘다.
            slider.minValue = LSO_GameSettings.MinBrightness;
            slider.maxValue = LSO_GameSettings.MaxBrightness;
            slider.wholeNumbers = false;

            if (panel == null)
            {
                panel = GetComponentInParent<LSO_EscPanel>();
                if (panel == null)
                    Debug.LogWarning("[LSO_BrightnessSlider] panel을 찾지 못했다. 창이 닫힐 때 저장되지 않는다.", this);
            }
        }

        private void OnEnable()
        {
            slider.onValueChanged.AddListener(OnSliderChanged);
            if (panel != null)
            {
                panel.Opened += Pull;
                panel.Closed += LSO_GameSettings.Save;
            }

            Pull();
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (panel != null)
            {
                panel.Opened -= Pull;
                panel.Closed -= LSO_GameSettings.Save;
            }
        }

        /// <summary>지금 설정 값을 슬라이더에 채운다. 창을 열 때마다 부른다.</summary>
        private void Pull()
        {
            // 값을 채우는 것뿐인데 onValueChanged가 울려 되돌아오면 안 된다.
            slider.SetValueWithoutNotify(LSO_GameSettings.Brightness);
            UpdateLabel(LSO_GameSettings.Brightness);
        }

        private void OnSliderChanged(float value)
        {
            // 설정 쪽이 BrightnessChanged를 쏘고, Applier가 그걸 받아 화면에 반영한다(저장은 창이 닫힐 때).
            LSO_GameSettings.Brightness = value;
            UpdateLabel(value);
        }

        private void UpdateLabel(float value)
        {
            if (valueLabel == null) return;
            valueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
