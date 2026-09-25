using UnityEngine;

namespace UI.Esc
{
    /// <summary>
    /// 설정의 밝기 값을 화면 밝기 패스(<c>BlueComplex/LSO/ScreenBrightness</c>)의 머티리얼에 꽂는다.
    /// 설정창이 없는 씬에서도 저장된 밝기가 적용되어야 하므로, 설정 UI가 아니라 씬에 상시 올라가 있는다.
    ///
    /// 렌더러 피처가 물고 있는 머티리얼은 <b>에셋</b>이다 — 여기서 값을 쓰면 플레이를 멈춘 뒤에도 그 값이
    /// 에셋에 남는다. 에디터에서 이리저리 만지다 보면 프로젝트에 이상한 밝기가 커밋될 수 있어서,
    /// 에디터에서는 플레이를 멈출 때 원래 값으로 되돌린다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_BrightnessApplier : MonoBehaviour
    {
        private static readonly int UserBrightnessId = Shader.PropertyToID("_UserBrightness");

        [Tooltip("렌더러 피처(Full Screen Pass)에 물려 둔 것과 같은 머티리얼. Assets/Settings/LSO/LSO_ScreenBrightness.mat")]
        [SerializeField] private Material brightnessMaterial;

        private float _originalValue = 1f;
        private bool _bound;

        private void OnEnable()
        {
            if (brightnessMaterial == null)
            {
                Debug.LogWarning("[LSO_BrightnessApplier] brightnessMaterial이 비어 있다. 밝기 설정이 화면에 반영되지 않는다. " +
                    "Assets/Settings/LSO/LSO_ScreenBrightness.mat 을 물릴 것.", this);
                return;
            }

            if (!brightnessMaterial.HasProperty(UserBrightnessId))
            {
                Debug.LogWarning($"[LSO_BrightnessApplier] '{brightnessMaterial.name}'에 _UserBrightness가 없다. " +
                    "BlueComplex/LSO/ScreenBrightness 셰이더를 쓰는 머티리얼인지 확인할 것.", this);
                return;
            }

            _originalValue = brightnessMaterial.GetFloat(UserBrightnessId);
            _bound = true;

            LSO_GameSettings.BrightnessChanged += Apply;
            Apply(LSO_GameSettings.Brightness); // 저장돼 있던 값을 지금 화면에 반영한다.
        }

        private void OnDisable()
        {
            if (!_bound) return;
            _bound = false;

            LSO_GameSettings.BrightnessChanged -= Apply;

#if UNITY_EDITOR
            // 플레이를 멈추면 에셋을 원래대로 — 실험하던 값이 프로젝트에 남지 않게.
            brightnessMaterial.SetFloat(UserBrightnessId, _originalValue);
#endif
        }

        private void Apply(float brightness) => brightnessMaterial.SetFloat(UserBrightnessId, brightness);
    }
}
