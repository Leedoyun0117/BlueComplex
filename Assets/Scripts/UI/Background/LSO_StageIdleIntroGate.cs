using BlueComplex.UI.Bootstrap;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 오프닝·인트로 전환 동안 <see cref="StageIdleBackground"/>를 꺼 두고, 실제 메인 게임이 준비되면 되켠다.
    ///
    /// ─── 왜 필요한가 ──────────────────────────────────────────────────────
    /// 배경은 씬에 없다 — <see cref="StageIdleBackground"/>의 EnsureExists가 Play 시작마다 스스로 만들고,
    /// Awake에서 곧바로 ShowStage(1)까지 한다. 그래서 오프닝과 메인 화면 뒤에 이미 스테이지 1 배경이 깔려 있다.
    ///
    /// ─── 실제 게임 준비 완료가 기준이다 ────────────────────────────────────
    /// SessionStarted는 세션만 만든 직후라 시작 컷신·대사·지직거림 전환이 아직 남아 있다.
    /// <see cref="StageBootstrapper.MainGameReady"/>는 그 모든 연출 뒤 <c>TurnRunner.StartStage</c>가 끝난 직후에 온다.
    ///
    /// ─── 되켤 때 구독을 다시 붙여야 한다 ──────────────────────────────────
    /// <c>SessionBoundView.OnDisable</c>은 SessionStarted 구독을 놓지만 다시 잡는 길이 없다(OnEnable이 없다).
    /// 그래서 꺼 둔 동안 세션이 시작되면 배경이 그 이벤트를 놓쳐 스테이지 1에 머문다.
    /// 되켠 직후 <c>Bind</c>를 한 번 불러 구독을 되붙인다 — Bind는 현재 세션이 있으면 즉시 Render까지 한다.
    /// (Bind는 "Awake에서만 부를 것"이라 주석돼 있지만, 껐다 켜는 경로에서는 이게 유일한 복구 수단이다.
    ///  껐다는 사실을 아는 경우에만 부르므로 이중 구독은 생기지 않는다.)
    ///
    /// 배경과 같은 방식으로 스스로 생긴다 — 씬이나 프리팹에 배치할 필요가 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_StageIdleIntroGate : MonoBehaviour
    {
        private const string ObjectName = "LSO Stage Idle Intro Gate";

        private StageBootstrapper _bootstrapper;
        private StageIdleBackground _background;
        private bool _hid;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindFirstObjectByType<LSO_StageIdleIntroGate>(FindObjectsInactive.Include) != null) return;
            if (FindFirstObjectByType<StageBootstrapper>() == null) return; // 게임 씬이 아니다.

            new GameObject(ObjectName).AddComponent<LSO_StageIdleIntroGate>();
        }

        /// <summary>구독은 Awake에 걸어야 한다 — StageBootstrapper.Start()가 첫 SessionStarted를 쏘기 전이어야 한다.</summary>
        private void Awake()
        {
            _bootstrapper = FindFirstObjectByType<StageBootstrapper>();
            if (_bootstrapper == null)
            {
                Debug.LogWarning($"[{nameof(LSO_StageIdleIntroGate)}] StageBootstrapper가 없다 — 배경을 껐다 켤 수 없다.", this);
                enabled = false;
                return;
            }

            _bootstrapper.MainGameReady += HandleMainGameReady;
        }

        /// <summary>
        /// 배경은 우리와 같은 AfterSceneLoad에 생기는데 둘의 순서가 정해져 있지 않다 —
        /// 모든 Awake가 끝난 Start에서 찾는다.
        /// </summary>
        private void Start()
        {
            _background = FindFirstObjectByType<StageIdleBackground>(FindObjectsInactive.Include);
            if (_background == null)
            {
                // 시트가 아직 임포트되지 않으면 배경이 아예 만들어지지 않는다 — 그건 정상이다.
                return;
            }

            if (!_background.gameObject.activeSelf) return; // 누가 이미 꺼 뒀다 — 되켤 책임을 넘겨받지 않는다.

            _background.gameObject.SetActive(false);
            _hid = true;
        }

        private void OnDestroy()
        {
            if (_bootstrapper != null) _bootstrapper.MainGameReady -= HandleMainGameReady;
            // 오프닝 중에 씬이 바뀌면 배경이 꺼진 채로 남는다.
            Restore();
        }

        private void HandleMainGameReady()
        {
            Restore();

            // 한 번 켜면 할 일이 끝난다 — 재시작으로 다시 쏘여도 배경은 이미 켜져 있다.
            _bootstrapper.MainGameReady -= HandleMainGameReady;
        }

        private void Restore()
        {
            if (!_hid || _background == null) return;

            _hid = false;
            _background.gameObject.SetActive(true);

            // 꺼져 있는 동안 놓친 세션 구독을 되붙인다. 현재 세션이 있으면 Bind가 바로 Render까지 한다.
            _background.Bind(_bootstrapper);
        }
    }
}
