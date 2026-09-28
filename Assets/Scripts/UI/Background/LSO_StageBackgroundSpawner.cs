using System;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Bootstrap;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 스테이지가 열릴 때 그 방의 배경 프리팹을 깐다. 씬에 빈 오브젝트를 만들어 붙이고, 원점에 둔다.
    ///
    /// ─── 왜 프리팹인가 ────────────────────────────────────────────────────
    /// 방 하나의 배경은 quad 한 장이 아니라 열몇 장이다(room2 기준 14장). 레이어마다 z·스케일·머티리얼·
    /// sortingOrder·프레임 속도가 붙고 조명과 파티클까지 따라온다. 그걸 코드로 지으면 코드가 그 값을 전부
    /// 알아야 한다 — 그건 씬이지 코드가 아니다. 눈으로 맞춰야 하는 값이라 프리팹에서 authoring 한다.
    /// 방마다 파일이 갈리므로 네 명이 각자 브랜치에서 일할 때 씬 충돌도 줄어든다.
    ///
    /// ─── StageIdleBackground와의 관계 ─────────────────────────────────────
    /// 그쪽은 "방 하나 = 시트 한 장"을 전제로 스스로 생겨나 quad 한 장을 깐다.
    /// 프리팹이 있는 스테이지에서는 두 배경이 겹치므로 없애거나 꺼 둔다(<see cref="destroyIdleBackground"/>).
    /// <b>프리팹이 없는 스테이지에서는 건드리지 않는다</b> — 아직 옮기지 않은 방은 그대로 돌아간다.
    ///
    /// 되켤 때는 Bind를 한 번 불러 준다. SessionBoundView는 OnDisable에서 세션 구독을 놓는데 다시 잡는
    /// 길이 없어서(OnEnable이 없다), 그냥 켜기만 하면 스테이지가 바뀌어도 알아채지 못한다.
    ///
    /// ─── 세션이 열리기 전에는 아무것도 깔지 않는다 ────────────────────────
    /// 오프닝과 메인 화면 뒤에 방이 비치면 안 된다. SessionStarted는 "취조시작"이 부르는
    /// BeginNewSession에서만 쏘이므로, 그때까지 이 컴포넌트는 아무것도 만들지 않고
    /// StageIdleBackground도 꺼 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_StageBackgroundSpawner : MonoBehaviour
    {
        /// <summary>스테이지 번호와 그 방의 배경 프리팹 한 쌍.</summary>
        [Serializable]
        private struct StageEntry
        {
            [Tooltip("StageBootstrapper.StageNumber와 같은 값(1부터).")]
            public int stage;

            [Tooltip("그 방의 배경 프리팹. 씬에 있던 Background Rig를 그대로 프리팹으로 뺀 것.")]
            public GameObject prefab;
        }

        [Header("Stage는 배열 순서가 아니라 진짜 스테이지 번호(1부터)")]
        [Tooltip("스테이지별 배경 프리팹. 순서는 상관없고 비어 있는 번호는 StageIdleBackground가 맡는다.")]
        [SerializeField] private StageEntry[] backgrounds = new StageEntry[0];

        [Tooltip("비워 두면 씬에서 찾는다.")]
        [SerializeField] private StageBootstrapper bootstrapper;

        [Tooltip("프리팹으로 배경을 깔면 StageIdleBackground를 아예 없앤다. " +
                 "끄면 숨기기만 한다 — 프리팹이 없는 스테이지로 돌아갈 수 있을 때 그렇게 둘 것.")]
        [SerializeField] private bool destroyIdleBackground = true;

        /// <summary>지금 깔려 있는 배경. 아직 세션이 안 열렸거나 프리팹이 없는 스테이지면 null.</summary>
        public GameObject Background => _instance;

        private GameObject _instance;
        private int _instanceStage;
        private StageIdleBackground _idle;
        private bool _idleHidden;
        private bool _sessionStarted;

        /// <summary>구독은 Awake에 걸어야 한다 — StageBootstrapper.Start()가 첫 SessionStarted를 쏘기 전이어야 한다.</summary>
        private void Awake()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<StageBootstrapper>();
            if (bootstrapper == null)
            {
                Debug.LogWarning($"[{nameof(LSO_StageBackgroundSpawner)}] StageBootstrapper가 없다 — 배경을 깔 수 없다.", this);
                enabled = false;
                return;
            }

            bootstrapper.SessionStarted += HandleSessionStarted;
            if (bootstrapper.Session != null) _sessionStarted = true;

            // 프리팹을 자식으로 붙이면서 authoring 자세를 그대로 쓴다 — 이 오브젝트가 원점에 있어야 맞는다.
            if (transform.position != Vector3.zero || transform.rotation != Quaternion.identity)
                Debug.LogWarning($"[{nameof(LSO_StageBackgroundSpawner)}] 원점에 두지 않으면 배경이 그만큼 밀린다. " +
                    $"지금 위치 {transform.position}, 회전 {transform.rotation.eulerAngles}.", this);
        }

        /// <summary>
        /// StageIdleBackground는 우리와 같은 타이밍(AfterSceneLoad)에 스스로 생기는데 둘의 순서가 정해져 있지 않다 —
        /// 모든 Awake가 끝난 Start에서 찾는다.
        /// </summary>
        private void Start()
        {
            _idle = FindFirstObjectByType<StageIdleBackground>(FindObjectsInactive.Include);

            // 세션이 이미 열렸다면(오프닝을 건너뛰는 설정) HandleSessionStarted가 이미 정리했다.
            if (_sessionStarted) return;

            // 아직 어느 스테이지인지 모른다 — 없애지 않고 숨기기만 한다.
            HideIdle(false);
        }

        private void OnDestroy()
        {
            if (bootstrapper != null) bootstrapper.SessionStarted -= HandleSessionStarted;

            // 오프닝 중에 씬이 바뀌면 배경이 꺼진 채로 남는다.
            RestoreIdle();
        }

        private void HandleSessionStarted(StageSession session)
        {
            _sessionStarted = true;
            Apply(bootstrapper.StageNumber);
        }

        private void Apply(int stage)
        {
            var prefab = Find(stage);

            if (prefab == null)
            {
                // 아직 프리팹으로 옮기지 않은 방 — StageIdleBackground에게 돌려준다.
                Despawn();
                RestoreIdle();
                return;
            }

            // 이 방은 프리팹이 맡는다 — 겹치는 쪽은 없애도 된다.
            HideIdle(true);

            // 같은 스테이지 재시작이면 다시 짓지 않는다 — 애니메이션 재생 위치가 튀지 않게.
            if (_instance != null && _instanceStage == stage) return;

            Despawn();

            // instantiateInWorldSpace: false — 프리팹에 저장된 위치를 로컬 값으로 그대로 쓴다.
            _instance = Instantiate(prefab, transform, false);

            // 씬에 남아 있는 옛 리그와 이름이 겹치면 Hierarchy에서 구분이 안 된다.
            _instance.name = $"{prefab.name} (Stage {stage})";
            _instanceStage = stage;

            // 프리팹이 꺼진 채로 저장됐거나 누가 껐어도 깔리도록 한 번 확인한다.
            if (!_instance.activeSelf) _instance.SetActive(true);

            WarnAboutStrayRigs();
        }

        /// <summary>
        /// 씬에 옛 Background Rig가 남아 있으면 알려 준다.
        ///
        /// 남아 있으면 배경이 두 벌이 되고, 더 나쁜 건 StageIdleBackground가 깨어나며 그걸 꺼 버린다는 점이다
        /// (BackgroundLayerRig를 찾아 SetActive(false) 한다). 이름까지 같으면 "스폰된 배경이 꺼져 있다"로 보인다.
        /// </summary>
        private void WarnAboutStrayRigs()
        {
            foreach (var rig in FindObjectsByType<BackgroundLayerRig>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rig.transform.IsChildOf(transform)) continue; // 우리가 깐 것.

                Debug.LogWarning($"[{nameof(LSO_StageBackgroundSpawner)}] 씬에 배경 리그 '{rig.name}'가 남아 있다 — " +
                    "프리팹으로 뺐으면 씬에서는 지울 것. 두 벌이 겹치고 StageIdleBackground가 그걸 꺼 버린다.", rig);
            }
        }

        private GameObject Find(int stage)
        {
            foreach (var entry in backgrounds)
                if (entry.stage == stage) return entry.prefab;

            return null;
        }

        private void Despawn()
        {
            if (_instance == null) return;

            Destroy(_instance);
            _instance = null;
            _instanceStage = 0;
        }

        /// <summary>
        /// <see cref="destroyIdleBackground"/>면 아예 없앤다. 스스로 다시 생기지는 않는다 —
        /// EnsureExists는 씬이 로드될 때 한 번만 돈다.
        /// 아니면 숨기기만 한다. 이미 꺼져 있던 건 건드리지 않는다(되돌릴 때 켜 버리면 안 된다).
        /// </summary>
        /// <param name="allowDestroy">
        /// 세션이 열리기 전에는 false — 아직 어느 스테이지인지 몰라서, 프리팹이 없는 방이면 되돌려 줘야 한다.
        /// </param>
        private void HideIdle(bool allowDestroy)
        {
            if (_idle == null) return;

            if (destroyIdleBackground && allowDestroy)
            {
                Destroy(_idle.gameObject);
                _idle = null;
                _idleHidden = false;
                return;
            }

            if (_idleHidden || !_idle.gameObject.activeSelf) return;

            _idle.gameObject.SetActive(false);
            _idleHidden = true;
        }

        private void RestoreIdle()
        {
            if (_idle == null)
            {
                // 없앤 뒤에 프리팹 없는 스테이지로 왔다 — 되살릴 길이 없다.
                if (destroyIdleBackground && _sessionStarted)
                    Debug.LogWarning($"[{nameof(LSO_StageBackgroundSpawner)}] 프리팹이 없는 스테이지인데 " +
                        "StageIdleBackground를 이미 없앴다 — 배경이 비어 있다. " +
                        "스테이지를 오가야 하면 Destroy Idle Background를 끌 것.", this);
                return;
            }

            if (!_idleHidden) return;

            _idleHidden = false;
            _idle.gameObject.SetActive(true);

            // 꺼져 있는 동안 놓친 세션 구독을 되붙인다. Bind는 현재 세션이 있으면 즉시 Render까지 한다.
            if (bootstrapper != null) _idle.Bind(bootstrapper);
        }
    }
}
