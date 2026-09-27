using System.Collections;
using System.Reflection;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Rendering;
using KTH;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 종료 컷신(김태호의 Timeline 컷신, Assets/TimeLine/Prefabs)을 게임 화면 위에 재생한다 — <see cref="StageFlowHooks.PlayCutscene"/>의 실제 구현이다.
    ///
    /// 컷신 프리팹은 월드 스페이스 캔버스 + 원근 카메라(fov 63.3, 원점에서 +z를 봄)로 만들어져 있다. 게임 화면의 HUD·CRT 합성과 섞이지 않게 전용 카메라를 하나 세워 화면 전체를 덮는다:
    ///  · 프리팹은 재생할 때마다 새로 만든다(<see cref="StageOrigin"/> — 게임 배경 카메라 시야 밖) → 페이드·대사 순번 같은 재생 상태가 매번 처음부터다. 끝나면 지운다.
    ///  · 카메라는 UI 카메라와 같은 렌더러(CRT·기분 효과가 없는 Base_Renderer)를 쓴다 — 메인 카메라의 CRT 패스는 RT_UI(HUD)를 위에 덧그리기 때문이다.
    ///  · 컷신이 화면을 덮는 동안 게임 소리(배경음·효과음)는 꺼진다(<see cref="UiSoundHooks.SuppressGameSounds"/>) — 컷신 자체의 소리만 들린다.
    ///  · 컷신이 하나 끝나도 카메라(검은 배경)는 남는다 — 번호가 이어지는 통합 컷신(6+7, 10+11) 사이에 게임 화면이 비치지 않게. 스테이지 종료 연출이 <see cref="Release"/>로 걷는다.
    ///
    /// 컷신이 끝났음은 <see cref="KTH_TimeLinePlay.Finished"/>로 안다.
    /// </summary>
    public sealed class StageCutsceneHost : MonoBehaviour
    {
        /// <summary>컷신 오브젝트가 놓이는 레이어(이름 없는 예비 레이어) — 전용 카메라만 이 레이어를 그린다.</summary>
        private const int CutsceneLayer = 30;

        /// <summary>컷신 프리팹의 오브젝트인가. 컷신 캔버스는 최상위 캔버스라 게임의 "캔버스 루트 찾기"에 잡히면 안 된다 — 끝난 컷신은 Destroy가 프레임 끝에야 실행돼서,
        /// 같은 프레임에 새 스테이지가 대화 오버레이를 그 밑에 지으면 오버레이가 함께 사라진다.</summary>
        public static bool IsCutsceneObject(GameObject go) => go.layer == CutsceneLayer;

        /// <summary>컷신을 세우는 월드 위치. 게임 배경 카메라(far 1000)의 시야 밖이다.</summary>
        private static readonly Vector3 StageOrigin = new(10000f, 0f, 0f);

        /// <summary>컷신 프리팹의 기준 카메라 시야각(KTH Test Scene의 Main Camera). 월드 캔버스가 이 시야를 채우도록 크기가 잡혀 있다.</summary>
        private const float FieldOfView = 63.3f;

        /// <summary>게임 화면이 컷신 카메라로 넘어갈 때 검게 덮이는 시간.</summary>
        private const float CoverFade = 0.5f;

        private static StageCutsceneHost _current;

        private Camera _camera;
        private GameObject _instance;

        /// <summary>컷신 카메라가 화면을 덮고 있는가(컷신 재생 중이거나 재생 사이).</summary>
        public bool Covering => _camera != null;

        public static StageCutsceneHost GetOrCreate()
        {
            if (_current != null) return _current;

            var go = new GameObject("Stage Cutscene Host");
            _current = go.AddComponent<StageCutsceneHost>();
            return _current;
        }

        /// <summary>지금 떠 있는 호스트(없으면 null) — 없는 걸 굳이 만들지 않고 치울 때 쓴다.</summary>
        public static StageCutsceneHost Find() => _current;

        /// <summary>번호의 컷신을 재생하는 코루틴(끝날 때까지 yield). 프리팹이 없는 번호는 null — 호출한 쪽이 건너뛴다.</summary>
        public IEnumerator Play(int number)
        {
            var catalog = StageCutsceneCatalog.Load();
            if (catalog == null)
            {
                Debug.LogWarning($"[StageCutsceneHost] Resources/{StageCutsceneCatalog.ResourcePath}가 없다 — BlueComplex/Cutscene/Rebuild Cutscene Catalog를 돌려라. 컷신 #{number}은 건너뛴다.");
                return null;
            }

            var prefab = catalog.Find(number);
            return prefab != null ? Run(number, prefab) : null;
        }

        /// <summary>컷신 카메라와 남은 컷신을 걷는다 — 화면이 게임 화면으로 돌아온다. 호출 전에 게임 화면 위에 이어질 막(검정)을 미리 깔아 두면 밝은 프레임이 비치지 않는다.</summary>
        public void Release()
        {
            if (_instance != null) Destroy(_instance);
            _instance = null;

            if (_camera != null) Destroy(_camera.gameObject);
            _camera = null;

            UiSoundHooks.SuppressGameSounds(false); // 게임 소리가 컷신과 함께 돌아온다.
        }

        private void OnDestroy()
        {
            if (_current == this) _current = null;
            Release();
        }

        private IEnumerator Run(int number, GameObject prefab)
        {
            if (_camera == null) yield return CoverIn();

            var instance = Instantiate(prefab, StageOrigin, Quaternion.identity);
            instance.name = prefab.name;
            instance.SetActive(true); // 09_B_Training처럼 프리팹 루트가 꺼진 채 저장된 것이 있다 — 꺼져 있으면 재생기(KTH_TimeLinePlay)가 요청을 못 받는다.
            _instance = instance;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CutsceneLayer;

            var director = instance.GetComponentInChildren<PlayableDirector>(true);
            if (director == null || director.playableAsset == null)
            {
                Debug.LogWarning($"[StageCutsceneHost] 컷신 #{number}({prefab.name})에 재생할 타임라인이 없다 — 건너뛴다.");
                Destroy(instance);
                yield break;
            }

            BindCinemachine(director);

            var asset = director.playableAsset;
            var finished = false;
            void OnFinished(PlayableAsset done) { if (done == asset) finished = true; }

            KTH_TimeLinePlay.Finished += OnFinished;
            if (!KTH_TimeLinePlay.Request(asset))
            {
                KTH_TimeLinePlay.Finished -= OnFinished;
                Debug.LogWarning($"[StageCutsceneHost] 컷신 #{number}({prefab.name})을 재생하지 못했다(재생기가 꺼져 있거나 이미 재생됨) — 건너뛴다.");
                Destroy(instance);
                yield break;
            }

            while (!finished && instance != null) yield return null;

            KTH_TimeLinePlay.Finished -= OnFinished;
            if (instance != null) Destroy(instance);
            if (_instance == instance) _instance = null;
        }

        /// <summary>시네머신 트랙이 있는 컷신(08)은 트랙이 움직일 카메라(브레인)가 프리팹 밖 씬 오브젝트라 프리팹에 저장돼 있지 않다 — 전용 카메라의 브레인을 물려 준다.</summary>
        private void BindCinemachine(PlayableDirector director)
        {
            if (_camera == null) return; // 바인딩 도중 컷신이 걷혔다(스테이지를 바로 시작한 경우).

            CinemachineBrain brain = null;
            foreach (var output in director.playableAsset.outputs)
            {
                if (output.outputTargetType != typeof(CinemachineBrain)) continue;

                if (brain == null)
                {
                    brain = _camera.GetComponent<CinemachineBrain>();
                    if (brain == null) brain = _camera.gameObject.AddComponent<CinemachineBrain>();
                }

                director.SetGenericBinding(output.sourceObject, brain);
            }
        }

        /// <summary>게임 화면이 검게 덮인 뒤 컷신 카메라로 넘어간다 — 컷신이 게임 UI 위에서 갑자기 튀어나오지 않게.</summary>
        private IEnumerator CoverIn()
        {
            var cover = new GameObject("Cutscene Cover", typeof(Canvas), typeof(CanvasGroup), typeof(Image));
            var canvas = cover.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            cover.GetComponent<Image>().color = Color.black;
            var group = cover.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            for (var t = 0f; t < CoverFade; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / CoverFade;
                yield return null;
            }

            // 카메라가 서는 프레임에 막을 걷는다 — 카메라는 검은 배경이라 화면은 계속 검다.
            BuildCamera();
            Destroy(cover);
        }

        private void BuildCamera()
        {
            var go = new GameObject("Cutscene Camera") { layer = CutsceneLayer };
            go.transform.position = StageOrigin;

            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 1 << CutsceneLayer;
            cam.orthographic = false;
            cam.fieldOfView = FieldOfView;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.depth = 100f; // 메인 카메라(-1)·UI 카메라(-2)보다 위
            cam.allowHDR = false;
            cam.allowMSAA = false;

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            UseUiCameraRenderer(data);

            _camera = cam;

            // 컷신이 화면을 덮는 동안 게임 소리는 끈다 — 컷신 자체의 소리(타임라인 오디오)만 들린다. Release가 되돌린다.
            UiSoundHooks.SuppressGameSounds(true);
        }

        /// <summary>UI 카메라가 쓰는 렌더러(CRT 패스가 없는 것)를 그대로 쓴다. 인덱스는 UI 카메라가 들고 있는 값을 읽는다 — 렌더러 목록 순서를 여기서 가정하지 않는다.</summary>
        private static void UseUiCameraRenderer(UniversalAdditionalCameraData data)
        {
            var rig = FindFirstObjectByType<UiCompositorRig>();
            var uiData = rig != null ? rig.GetComponent<Camera>()?.GetUniversalAdditionalCameraData() : null;
            var field = typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex", BindingFlags.NonPublic | BindingFlags.Instance);

            if (uiData != null && field != null && field.GetValue(uiData) is int index && index >= 0)
            {
                data.SetRenderer(index);
                return;
            }

            Debug.LogWarning("[StageCutsceneHost] UI 카메라의 렌더러를 읽지 못했다 — 컷신이 기본 렌더러(CRT)로 그려져 HUD가 위에 비칠 수 있다.");
        }
    }
}
