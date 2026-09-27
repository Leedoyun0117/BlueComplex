using System;
using KTH;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace BlueComplex.Editor.DLJ
{
    /// <summary>Builds the Yuki turn cutscene without changing the shared cutscene scripts.</summary>
    public static class DLJ_YukiLineCutBuilder
    {
        private const string SourceFolder = "Assets/Art/Portraits/Yuki/LineCut";
        private const string OutputFolder = "Assets/TimeLine/DLJ";
        private const string ClipPath = OutputFolder + "/DLJ_YukiTurn.anim";
        private const string TimelinePath = OutputFolder + "/DLJ_YukiTurn.playable";
        private const string PrefabPath = OutputFolder + "/DLJ_YukiTurn.prefab";
        private const string PreviewPath = "Assets/Scenes/DLJ/DLJ_YukiTurnPreview.unity";
        private const int FrameCount = 10;
        private const float FramesPerSecond = 10f;
        private const float Duration = 1.8f;

        [MenuItem("BlueComplex/Cutscene/DLJ/Build Yuki Turn")]
        public static void Build()
        {
            EnsureFolder(OutputFolder);
            var sprites = ImportFrames();
            var clip = BuildAnimation(sprites);
            var timeline = BuildTimeline(clip, out var track);
            var prefab = BuildPrefab(sprites[0], timeline, track);
            BuildPreviewScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DLJ Yuki Turn] Built {PrefabPath}, {TimelinePath}, and {PreviewPath}");
        }

        private static Sprite[] ImportFrames()
        {
            var sprites = new Sprite[FrameCount];
            for (var i = 0; i < FrameCount; i++)
            {
                var path = $"{SourceFolder}/1_{i:0000}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException($"Missing cutscene frame: {path}");

                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    importer.maxTextureSize != 2048 || importer.mipmapEnabled)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.maxTextureSize = 2048; // 2048x1152 is enough for the 1920x1080 canvas.
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }

                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[i] == null) throw new InvalidOperationException($"Frame did not import as a sprite: {path}");
            }

            return sprites;
        }

        private static AnimationClip BuildAnimation(Sprite[] sprites)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "DLJ_YukiTurn" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.frameRate = FramesPerSecond;
            clip.wrapMode = WrapMode.Once;
            var keys = new ObjectReferenceKeyframe[FrameCount + 1];
            for (var i = 0; i < FrameCount; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / FramesPerSecond, value = sprites[i] };
            keys[FrameCount] = new ObjectReferenceKeyframe { time = Duration, value = sprites[FrameCount - 1] };
            AnimationUtility.SetObjectReferenceCurve(
                clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite"), keys);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static TimelineAsset BuildTimeline(AnimationClip animation, out AnimationTrack track)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timeline, TimelinePath);
            }

            track = null;
            foreach (var existing in timeline.GetOutputTracks())
            {
                if (existing is AnimationTrack animationTrack && existing.name == "Yuki Turn Frames")
                {
                    track = animationTrack;
                    break;
                }
            }
            if (track == null) track = timeline.CreateTrack<AnimationTrack>(null, "Yuki Turn Frames");

            TimelineClip cutsceneClip = null;
            foreach (var existing in track.GetClips())
            {
                cutsceneClip = existing;
                break;
            }
            if (cutsceneClip == null) cutsceneClip = track.CreateClip<AnimationPlayableAsset>();
            ((AnimationPlayableAsset)cutsceneClip.asset).clip = animation;
            cutsceneClip.displayName = "Turn and hold gaze";
            cutsceneClip.start = 0;
            cutsceneClip.duration = Duration;
            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(track);
            return timeline;
        }

        private static GameObject BuildPrefab(Sprite firstFrame, TimelineAsset timeline, AnimationTrack track)
        {
            var root = new GameObject("DLJ_YukiTurn", typeof(RectTransform), typeof(PlayableDirector), typeof(KTH_TimeLinePlay));
            var canvasRoot = new GameObject("Cutscene Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(root.transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var frame = new GameObject("Yuki Turn Frames", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Animator));
            frame.transform.SetParent(canvasRoot.transform, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            var image = frame.GetComponent<Image>();
            image.sprite = firstFrame;
            image.preserveAspect = true;
            image.raycastTarget = true; // Covers the game UI while the cutscene plays.

            var director = root.GetComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            director.SetGenericBinding(track, frame.GetComponent<Animator>());

            var player = new SerializedObject(root.GetComponent<KTH_TimeLinePlay>());
            var activateOnPlay = player.FindProperty("activateOnPlay");
            activateOnPlay.arraySize = 1;
            activateOnPlay.GetArrayElementAtIndex(0).objectReferenceValue = canvasRoot;
            player.FindProperty("playOnce").boolValue = true;
            player.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null) throw new InvalidOperationException($"Could not save {PrefabPath}");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildPreviewScene(GameObject prefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var director = instance.GetComponent<PlayableDirector>();
                director.playOnAwake = true; // Preview only; the reusable prefab waits for Request(timeline).
                PrefabUtility.RecordPrefabInstancePropertyModifications(director);
                instance.AddComponent<KTH_TimelineSpeed>().SetSpeed(0.2f); // Slow preview makes each frame inspectable.

                if (!EditorSceneManager.SaveScene(scene, PreviewPath))
                    throw new InvalidOperationException($"Could not save {PreviewPath}");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = folder.Substring(0, folder.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(folder.LastIndexOf('/') + 1));
        }
    }
}
