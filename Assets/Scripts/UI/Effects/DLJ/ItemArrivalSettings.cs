using UnityEngine;

namespace BlueComplex.UI.Effects.DLJ
{
    /// <summary>DLJ 아이템 등장 연출 전용 설정. 기존 공용 UI 모션 설정과 독립적으로 사용한다.</summary>
    [CreateAssetMenu(fileName = "DLJItemArrivalSettings", menuName = "BlueComplex/DLJ/Item Arrival Settings")]
    public sealed class ItemArrivalSettings : ScriptableObject
    {
        public const string ResourcePath = "DLJItemArrivalSettings";

        [Header("아이템 등장")]
        [Tooltip("확대 → 이동 → 삽입 → 안착까지의 전체 시간(초).")]
        [Min(0.24f)] public float duration = 0.42f;

        private static ItemArrivalSettings _settings;

        public static ItemArrivalSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;
                _settings = Resources.Load<ItemArrivalSettings>(ResourcePath);
                if (_settings != null) return _settings;
                _settings = CreateInstance<ItemArrivalSettings>();
                _settings.hideFlags = HideFlags.HideAndDontSave;
                return _settings;
            }
        }
    }
}
