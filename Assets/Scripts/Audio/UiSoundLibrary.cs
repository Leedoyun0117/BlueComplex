using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using UnityEngine;

namespace BlueComplex.Audio
{
    /// <summary>라이브러리 조회. 에셋이 없으면(아직 Ensure를 안 돌렸거나 Resources 밖에 있으면) 빈 라이브러리로 동작해
    /// 아무 소리도 나지 않을 뿐 예외는 나지 않는다.</summary>
    public static class UiSoundLibrary
    {
        private static SoundLibrary _current;

        public static SoundLibrary Current
        {
            get
            {
                if (_current != null) return _current;

                _current = Resources.Load<SoundLibrary>(SoundLibrary.ResourcePath);
                if (_current != null) return _current;

                _current = ScriptableObject.CreateInstance<SoundLibrary>();
                _current.hideFlags = HideFlags.HideAndDontSave;
                return _current;
            }
        }
    }
}
