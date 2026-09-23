using BlueComplex.Audio;
using BlueComplex.UI.Motion;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 사운드 라이브러리 에셋(Assets/Resources/SoundLibrary.asset)과 씬의 SoundManager를 없으면 만든다.
    /// UiMotionSettingsTool/CrtSetupTool과 같은 규칙 — 이미 있으면 손대지 않는다(인스펙터에서 채운 클립이 남아야 한다).
    /// </summary>
    public static class SoundSetupTool
    {
        private const string LibraryAssetPath = "Assets/Resources/SoundLibrary.asset";
        private const string SoundFolder = "Assets/Sound/";

        private static readonly Vector2 DefaultPitchRange = new(0.97f, 1.03f);

        /// <summary>Assets/Sound에 있는 지금 있는 파일 이름 → 큐. 나중에 소리가 추가되면 여기 줄을 더하거나,
        /// 그냥 에셋 인스펙터에서 SoundLibrary의 항목에 클립을 끌어넣으면 된다(둘 다 된다).
        ///
        /// 관절 팔은 펼침/접힘 소리가 원래 달라야 하는데 아직 파일이 하나뿐이라, 구분될 때까지 임시로
        /// 같은 클립을 피치만 반대 방향으로 살짝 틀어서 쓴다 — 접히는 클립이 따로 생기면 이 줄만 바꾸면 된다.</summary>
        private static readonly (string fileName, UiSoundCue cue, Vector2 pitchRange)[] KnownClips =
        {
            ("기계관절움직이는사운드.mp3", UiSoundCue.MechanicalJointOpen, new Vector2(1f, 1.05f)),
            ("기계관절움직이는사운드.mp3", UiSoundCue.MechanicalJointClose, new Vector2(0.85f, 0.92f)),
            ("글씨쓰는사운드.mp3", UiSoundCue.Write, DefaultPitchRange),
            ("아이템사용사운드.mp3", UiSoundCue.ItemUse, DefaultPitchRange),
            ("시계움직임사운드.mp3", UiSoundCue.ClockTick, DefaultPitchRange),
            ("버튼클릭음.mp3", UiSoundCue.ButtonClick, DefaultPitchRange),
            ("단서집는소리.mp3", UiSoundCue.ClueTake, DefaultPitchRange),
            ("포스트 잇 사운드_[cut_1sec] (mp3cut.net).mp3", UiSoundCue.PostitPeel, DefaultPitchRange),
        };

        [MenuItem("BlueComplex/Audio/Ensure Sound Library")]
        public static void EnsureLibrary()
        {
            if (AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryAssetPath) != null)
            {
                Debug.Log($"[SoundSetupTool] 이미 있다({LibraryAssetPath}) — 손대지 않는다. 큐가 어긋난 것 같으면 " +
                          "BlueComplex/Audio/Rebuild Sound Library (덮어쓰기)를 써라.");
                return;
            }

            BuildLibrary();
        }

        /// <summary>있어도 무조건 다시 만든다 — enum(UiSoundCue)에 값을 끼워 넣어서 이미 저장된 에셋의
        /// 클립-큐 연결이 어긋났을 때 쓴다(예: 버튼 클릭음을 눌렀는데 시계 소리가 나는 경우). 인스펙터에서
        /// 직접 손으로 채운 값(파일 목록에 없는 클립 등)이 있으면 덮어써 없어지니 주의.</summary>
        [MenuItem("BlueComplex/Audio/Rebuild Sound Library (덮어쓰기)")]
        public static void RebuildLibrary()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryAssetPath);
            if (existing != null) AssetDatabase.DeleteAsset(LibraryAssetPath);

            BuildLibrary();
        }

        private static void BuildLibrary()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

            var library = ScriptableObject.CreateInstance<SoundLibrary>();
            var serialized = new SerializedObject(library);
            var entries = serialized.FindProperty("_entries");

            var found = 0;
            entries.arraySize = KnownClips.Length;
            for (var i = 0; i < KnownClips.Length; i++)
            {
                var (fileName, cue, pitchRange) = KnownClips[i];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + fileName);
                if (clip == null)
                    Debug.LogWarning($"[SoundSetupTool] 클립을 못 찾았다: {SoundFolder}{fileName}");
                else
                    found++;

                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("cue").enumValueIndex = (int)cue;
                var clips = entry.FindPropertyRelative("clips");
                clips.arraySize = clip != null ? 1 : 0;
                if (clip != null) clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
                entry.FindPropertyRelative("volume").floatValue = 1f;
                entry.FindPropertyRelative("pitchRange").vector2Value = pitchRange;
            }

            serialized.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(library, LibraryAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SoundSetupTool] 사운드 라이브러리 에셋을 만들었다: {LibraryAssetPath} (클립 {found}/{KnownClips.Length}개 연결)");
        }

        [MenuItem("BlueComplex/Audio/Ensure Sound Manager In Scene")]
        public static void EnsureSoundManager()
        {
            var manager = Object.FindFirstObjectByType<SoundManager>();
            if (manager != null)
            {
                Debug.Log("[SoundSetupTool] 씬에 SoundManager가 이미 있다 — 손대지 않는다.");
                return;
            }

            var go = new GameObject("SoundManager");
            go.AddComponent<SoundManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create SoundManager");
            Debug.Log("[SoundSetupTool] 씬에 SoundManager를 만들었다.");
        }
    }
}
