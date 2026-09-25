using System;
using UnityEngine;

namespace UI.Esc
{
    /// <summary>
    /// 플레이어가 설정창에서 바꾼 값을 담고 저장하는 곳. 지금은 밝기뿐이지만 볼륨 등이 같은 방식으로 들어온다.
    ///
    /// 저장은 PlayerPrefs를 쓴다 — 이 프로젝트엔 설정 저장 선례가 없었고, 값이 몇 개 되지 않아
    /// 파일 포맷을 따로 정할 이유가 없다. 항목이 크게 늘거나 구조가 생기면 그때 JSON으로 옮기면 되고,
    /// 읽고 쓰는 자리가 여기 하나라 옮기는 비용도 이 파일 안에서 끝난다.
    ///
    /// 값이 바뀌면 <see cref="BrightnessChanged"/>가 즉시 울린다. 슬라이더를 끄는 동안 화면이 같이
    /// 변해야 하므로, "적용"은 저장과 분리되어 있다 — 저장은 설정창이 닫힐 때 한 번만 한다.
    /// </summary>
    public static class LSO_GameSettings
    {
        private const string BrightnessKey = "LSO.Settings.Brightness";

        public const float MinBrightness = 0.4f;
        public const float MaxBrightness = 1.6f;
        public const float DefaultBrightness = 1f;

        /// <summary>밝기가 바뀔 때마다 울린다(슬라이더를 끄는 도중 포함). 화면에 즉시 반영하는 쪽이 구독한다.</summary>
        public static event Action<float> BrightnessChanged;

        private static float _brightness = float.NaN;

        /// <summary>1이 기본. 낮추면 어두워지고 올리면 밝아진다.</summary>
        public static float Brightness
        {
            get
            {
                // 처음 읽는 시점에 저장된 값을 끌어온다 — 어느 씬에서 먼저 물어보든 같은 값이 나온다.
                if (float.IsNaN(_brightness))
                    _brightness = Mathf.Clamp(
                        PlayerPrefs.GetFloat(BrightnessKey, DefaultBrightness), MinBrightness, MaxBrightness);

                return _brightness;
            }
            set
            {
                var clamped = Mathf.Clamp(value, MinBrightness, MaxBrightness);
                if (!float.IsNaN(_brightness) && Mathf.Approximately(_brightness, clamped)) return;

                _brightness = clamped;
                BrightnessChanged?.Invoke(clamped);
            }
        }

        /// <summary>디스크에 쓴다. 설정창이 닫힐 때처럼 한 번만 부르면 된다 — 슬라이더를 끄는 내내 부를 필요는 없다.</summary>
        public static void Save()
        {
            PlayerPrefs.SetFloat(BrightnessKey, Brightness);
            PlayerPrefs.Save();
        }

        /// <summary>기본값으로 되돌린다. 저장까지 하지는 않는다(<see cref="Save"/>를 따로 부를 것).</summary>
        public static void ResetToDefaults() => Brightness = DefaultBrightness;
    }
}
