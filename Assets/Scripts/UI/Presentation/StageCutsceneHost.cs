using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Rendering;
using DG.Tweening;
using KTH;
using TMPro;
using UI.Esc;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
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
    ///  · 재생 중 Space로 건너뛴다(오프닝과 같은 "Space  건너뛰기" 안내가 우측 하단에 뜬다). 건너뛰면 재생 중이던 타임라인을 멈추고 프리팹·컷신 효과음을 즉시 치운 뒤
    ///    코루틴이 정상적으로 끝난다 — 이어지던 번호(통합 컷신 6+7 등, 클리어 때 몰아 보는 나머지)도 이번 컷신 묶음이 끝날 때까지(<see cref="Release"/>) 함께 건너뛴다.
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

        /// <summary>지금 컷신 코루틴이 도는 중인가(막이 덮이는 동안 포함) — 이때만 Space가 먹는다.</summary>
        private bool _running;

        /// <summary>Space로 건너뛰었다. 이번 컷신 묶음이 끝나 <see cref="Release"/>될 때까지 남은 번호도 재생하지 않는다.</summary>
        private bool _skipped;

        private TMP_Text _hint;
        private Tween _hintTween;
        private LSO_EscPanel _escPanel;

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

            if (_skipped)
            {
                Debug.Log($"[StageCutsceneHost] 컷신 #{number} — 건너뛰기 중이라 재생하지 않는다.");
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
            _running = false;
            _skipped = false;
            HideHint();
            if (_pausedForEsc) ResumeFromEsc();
            ReturnEsc(); // 설정창을 게임 캔버스로 되돌린다(열려 있으면 열린 채로).

            if (_camera != null) Destroy(_camera.gameObject);
            _camera = null;

            UiSoundHooks.SuppressGameSounds(false); // 게임 소리가 컷신과 함께 돌아온다.
        }

        private void OnDestroy()
        {
            if (_current == this) _current = null;
            Release();
        }

        private void Update()
        {
            if (_escPanel == null) _escPanel = FindFirstObjectByType<LSO_EscPanel>(FindObjectsInactive.Include);
            var escOpen = _escPanel != null && _escPanel.IsOpen;

            // 컷신 위에서 설정창이 열려 있는 동안 컷신(타임라인·소리)도 멈춘다. 건너뛰기 안내도 창 위에 겹치지 않게 감춘다.
            var pause = escOpen && Covering;
            if (pause != _pausedForEsc)
            {
                if (pause) PauseForEsc();
                else ResumeFromEsc();
            }

            if (_hint != null) _hint.enabled = !escOpen;

            if (!_running || _skipped) return;
            if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame) return;

            // 설정창(ESC)이 열려 있으면 그 창의 입력이다 — 컷신을 건너뛰지 않는다.
            if (escOpen) return;

            Skip();
        }

        // ── 컷신 위의 설정창 ─────────────────────────────────────────────────
        //
        // 설정창은 게임 캔버스(MainHud, UI 카메라 → RT_UI → CRT 합성) 안에 있어 정렬 순서를 아무리 올려도 컷신 카메라(depth 100, 화면 전체) 밑에 깔린다.
        // 그래서 컷신 카메라가 화면을 덮는 동안만 설정창을 이 호스트의 오버레이 캔버스(카메라와 무관하게 맨 마지막에 그려짐)로 옮겼다가, 걷힐 때 제자리로 돌린다.
        // 컷신 카메라는 CRT 없이 그리므로 그 위의 설정창도 CRT 없이 평평하게 그려지는 게 맞고, 클릭 보정(CRT 곡률)도 그동안 끈다.
        // 창 크기는 게임 캔버스와 같은 기준 해상도(1920×1080, 너비·높이 0.5)로 맞춰 둔 오버레이라 그대로다.

        private Transform _escHome;
        private int _escHomeIndex;
        private Material _escCrtMaterial;
        private bool _pausedForEsc;
        private PlayableDirector _pausedDirector;
        private readonly List<AudioSource> _pausedAudio = new();
        private readonly List<LSO_EscSilhouette> _silhouettes = new();

        private void LiftEscOverCutscene()
        {
            if (_escPanel == null) _escPanel = FindFirstObjectByType<LSO_EscPanel>(FindObjectsInactive.Include);
            if (_escPanel == null || _escHome != null) return;

            var panel = _escPanel.transform;
            _escHome = panel.parent;
            _escHomeIndex = panel.GetSiblingIndex();
            panel.SetParent(Overlay, false);
            panel.SetAsLastSibling();

            var raycaster = _escPanel.GetComponent<DistortionCorrectedGraphicRaycaster>();
            if (raycaster != null)
            {
                _escCrtMaterial = raycaster.CrtMaterial;
                raycaster.SetCrtMaterial(null); // 곡률 0 → 보정 없는 일반 레이캐스트
            }

            // 창 뒤 실루엣(LSO_EscSilhouette)은 게임 UI(RT_UI)를 떠서 비추는데, 컷신 동안 창 뒤에 있는 건 게임 UI가 아니라 컷신이다 —
            // 게임 HUD 실루엣이 컷신 위에 뜨지 않게 그동안 끈다(창 자체의 어두운 배경만 남는다).
            _silhouettes.Clear();
            foreach (var silhouette in _escPanel.GetComponentsInChildren<LSO_EscSilhouette>(true))
            {
                if (!silhouette.enabled) continue;
                silhouette.enabled = false;
                var image = silhouette.GetComponent<RawImage>();
                if (image != null) image.enabled = false;
                _silhouettes.Add(silhouette);
            }
        }

        private void ReturnEsc()
        {
            if (_escHome == null) return;

            if (_escPanel != null)
            {
                var panel = _escPanel.transform;
                panel.SetParent(_escHome, false);
                panel.SetSiblingIndex(Mathf.Min(_escHomeIndex, _escHome.childCount - 1));
                _escPanel.GetComponent<DistortionCorrectedGraphicRaycaster>()?.SetCrtMaterial(_escCrtMaterial);
            }

            foreach (var silhouette in _silhouettes)
                if (silhouette != null) silhouette.enabled = true; // 다음에 열릴 때부터 다시 게임 UI 실루엣을 뜬다.
            _silhouettes.Clear();

            _escHome = null;
            _escCrtMaterial = null;
        }

        /// <summary>설정창이 열리면 컷신을 멈춘다. 타임라인은 게임 시간으로 돌아 설정창의 시간 정지(timeScale 0)로도 멈추지만, 컷신 소리(KTH_TimelineAudioSignal의 AudioSource,
        /// KTH_Sfx의 공용 소스)는 timeScale과 무관하게 계속 나서 따로 일시정지한다. 설정창 정지 옵션을 꺼도 멈추도록 타임라인도 직접 Pause한다.</summary>
        private void PauseForEsc()
        {
            _pausedForEsc = true;
            _pausedAudio.Clear();

            if (_instance != null)
            {
                var director = _instance.GetComponentInChildren<PlayableDirector>(true);
                if (director != null && director.state == PlayState.Playing)
                {
                    director.Pause();
                    _pausedDirector = director;
                }

                foreach (var source in _instance.GetComponentsInChildren<AudioSource>(true)) PauseAudio(source);
            }

            var sfx = GameObject.Find("[KTH_Sfx]");
            if (sfx != null)
                foreach (var source in sfx.GetComponents<AudioSource>()) PauseAudio(source);
        }

        private void PauseAudio(AudioSource source)
        {
            if (source == null || !source.isPlaying) return;
            source.Pause();
            _pausedAudio.Add(source);
        }

        private void ResumeFromEsc()
        {
            _pausedForEsc = false;

            if (_pausedDirector != null && _pausedDirector.state == PlayState.Paused) _pausedDirector.Resume();
            _pausedDirector = null;

            foreach (var source in _pausedAudio)
                if (source != null) source.UnPause();
            _pausedAudio.Clear();
        }

        /// <summary>Space: 재생 중인 타임라인을 멈추고 프리팹과 컷신 효과음을 바로 치운다. 기다리던 코루틴(<see cref="Run"/>)은 다음 프레임에 정상적으로 끝난다.
        /// 카메라(검은 배경)는 남겨 둔다 — 이어지는 게임 흐름(스테이지 종료 연출)이 평소처럼 막을 깔고 <see cref="Release"/>한다.</summary>
        private void Skip()
        {
            _skipped = true;
            HideHint();
            Debug.Log("[StageCutsceneHost] Space — 컷신을 건너뛴다.");

            if (_instance != null)
            {
                var director = _instance.GetComponentInChildren<PlayableDirector>(true);
                if (director != null && director.state == PlayState.Playing) director.Stop(); // KTH_TimeLinePlay.Finished가 온다.
                Destroy(_instance);
                _instance = null;
            }

            KTH_Sfx.StopAll(); // 컷신 효과음은 프리팹 밖의 공용 소스에서 나서 프리팹을 지워도 남는다.
        }

        private IEnumerator Run(int number, GameObject prefab)
        {
            _running = true;
            ShowHint();
            try
            {
                if (_camera == null) yield return CoverIn();
                if (_skipped) yield break; // 막이 덮이는 사이에 건너뛰었다 — 카메라(검은 화면)만 선 채로 끝난다.

                yield return PlayInstance(number, prefab);
            }
            finally
            {
                _running = false;
            }
        }

        private IEnumerator PlayInstance(int number, GameObject prefab)
        {
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

        /// <summary>오프닝의 건너뛰기 안내와 같은 모양(우측 하단, 흐린 흰 글자, 잠시 뒤 서서히)으로 컷신 위에 띄운다. 컷신 카메라보다 위에 그려지도록 오버레이 캔버스에 둔다.</summary>
        private void ShowHint()
        {
            if (_skipped) return;
            if (_hint == null) BuildHint();
            if (_hintTween != null && _hintTween.IsActive()) return; // 통합 컷신의 다음 번호 — 이미 떠 있거나 뜨는 중이다.
            if (_hint.alpha > 0f) return;

            _hintTween = DOTween.To(() => _hint.alpha, v => _hint.alpha = v, 1f, 0.8f).SetDelay(1.2f).SetUpdate(true).SetTarget(this);
        }

        private void HideHint()
        {
            _hintTween?.Kill();
            _hintTween = null;
            if (_hint != null) _hint.alpha = 0f;
        }

        private RectTransform _overlay;

        /// <summary>컷신 위에 그리는 오버레이 캔버스(건너뛰기 안내, 컷신 동안의 설정창). 게임 캔버스(MainHud)와 같은 기준 해상도로 스케일한다.</summary>
        private RectTransform Overlay
        {
            get
            {
                if (_overlay != null) return _overlay;

                // 컷신 레이어에 둔다 — 최상위 캔버스라 게임의 "캔버스 루트 찾기"(IsCutsceneObject로 거른다)에 잡히면 안 된다. 오버레이 캔버스는 레이어와 무관하게 그려진다.
                var go = new GameObject("Cutscene Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)) { layer = CutsceneLayer };
                go.transform.SetParent(transform, false);
                var canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue - 1; // 막(CoverIn)보다는 아래, 컷신·게임 화면보다는 위.

                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                return _overlay = (RectTransform)go.transform;
            }
        }

        private void BuildHint()
        {
            var text = new GameObject("Skip Hint", typeof(RectTransform)) { layer = CutsceneLayer };
            text.transform.SetParent(Overlay, false);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(0.55f, 0.02f);
            rect.anchorMax = new Vector2(0.98f, 0.07f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            _hint = text.AddComponent<TextMeshProUGUI>();
            var font = FindGameFont();
            if (font != null) _hint.font = font;
            _hint.text = "Space  건너뛰기";
            _hint.fontSize = 22f;
            _hint.color = new Color(1f, 1f, 1f, 0.45f);
            _hint.alignment = TextAlignmentOptions.MidlineRight;
            _hint.raycastTarget = false;
            _hint.alpha = 0f;
        }

        /// <summary>한글이 들어 있는 게임 글꼴을 씬의 글자에서 빌린다(컷신 프리팹의 글자는 곧 지워지니 건너뛴다).</summary>
        private static TMP_FontAsset FindGameFont()
        {
            foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.font != null && !IsCutsceneObject(text.gameObject)) return text.font;

            return null;
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
            LiftEscOverCutscene(); // 이제부터 화면은 컷신 카메라 차지 — 설정창은 그 위 오버레이로.

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
