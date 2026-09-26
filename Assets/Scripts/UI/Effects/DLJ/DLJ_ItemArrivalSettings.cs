using UnityEngine;

namespace BlueComplex.UI.Effects.DLJ
{
    /// <summary>DLJ 아이템 등장 연출 전용 설정. 기존 공용 UI 모션 설정과 독립적으로 사용한다.</summary>
    [CreateAssetMenu(fileName = "DLJItemArrivalSettings", menuName = "BlueComplex/DLJ/Item Arrival Settings")]
    public sealed class DLJ_ItemArrivalSettings : ScriptableObject
    {
        public const string ResourcePath = "DLJItemArrivalSettings";

        [Header("아이템 등장")]
        [Tooltip("첫 아이템의 확대 → 이동 → 삽입 → 안착까지의 시간(초).")]
        [Min(0.24f)] public float duration = 0.6f;

        [Tooltip("동시에 나타난 뒤 위에서 아래로 끼워지는 시간 간격(초).")]
        [Min(0f)] public float insertStagger = 0.2f;

        [Tooltip("삽입 전 아이템 밝기. 0은 검정, 1은 원래 색. 슬롯에 끼워지면서 원래 밝기로 돌아온다.")]
        [Range(0f, 1f)] public float preInsertBrightness = 0.45f;

        private static DLJ_ItemArrivalSettings _settings;

        public static DLJ_ItemArrivalSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;
                _settings = Resources.Load<DLJ_ItemArrivalSettings>(ResourcePath);
                if (_settings != null) return _settings;
                _settings = CreateInstance<DLJ_ItemArrivalSettings>();
                _settings.hideFlags = HideFlags.HideAndDontSave;
                return _settings;
            }
        }
    }
}
