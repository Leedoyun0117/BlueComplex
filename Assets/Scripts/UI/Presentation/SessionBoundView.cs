using BlueComplex.Core.Stage;
using BlueComplex.UI.Bootstrap;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// StageSession 이벤트 구독/해제를 공통 처리하는 정식 UI 뷰 베이스.
    /// Assets/Scripts/UI/Debug/DebugSessionView.cs와 동일한 패턴 — 재시작으로 세션이
    /// 교체될 때마다 이전 세션 구독을 풀고 새 세션에 다시 건다. 중앙 코디네이터 없이
    /// 각 Controller가 스스로 StageBootstrapper를 구독한다.
    /// </summary>
    public abstract class SessionBoundView : MonoBehaviour
    {
        // 프리팹 자체는 씬 오브젝트(StageBootstrapper)를 참조할 수 없으므로, 이 필드는 프리팹
        // 생성 시점엔 비워 두고 MainHud를 씬에 배치한 뒤(UICompositorSetupTool) 채워 넣는다.
        [SerializeField] private StageBootstrapper _bootstrapper;

        protected StageBootstrapper Bootstrapper { get; private set; }
        protected StageSession Session { get; private set; }

        /// <summary>추가 Awake 로직이 필요하면 override하고 base.Awake()를 호출할 것.</summary>
        protected virtual void Awake()
        {
            // 프리팹에 나중에 추가된 뷰는 씬 인스턴스에 이 필드의 오버라이드가 없다(UICompositorSetupTool을 다시 돌려야 채워진다) —
            // 비어 있으면 씬의 StageBootstrapper를 직접 찾아 그 단계를 건너뛰어도 붙게 한다.
            if (_bootstrapper == null) _bootstrapper = FindFirstObjectByType<StageBootstrapper>();
            if (_bootstrapper != null) Bind(_bootstrapper);
        }

        /// <summary>StageBootstrapper.Start()가 첫 SessionStarted를 쏘기 전에 구독을 걸어야 한다 — Awake에서만 부를 것.</summary>
        public void Bind(StageBootstrapper bootstrapper)
        {
            Bootstrapper = bootstrapper;
            bootstrapper.SessionStarted += HandleSessionStarted;
            if (bootstrapper.Session != null) HandleSessionStarted(bootstrapper.Session);
        }

        private void OnDisable()
        {
            if (Bootstrapper != null) Bootstrapper.SessionStarted -= HandleSessionStarted;
            if (Session != null) Unsubscribe(Session);
        }

        private void HandleSessionStarted(StageSession session)
        {
            if (Session != null) Unsubscribe(Session);
            Session = session;
            Subscribe(session);
            Render();
        }

        /// <summary>새 세션의 코어 이벤트를 구독한다.</summary>
        protected abstract void Subscribe(StageSession session);

        /// <summary>이전 세션의 코어 이벤트 구독을 해제한다.</summary>
        protected abstract void Unsubscribe(StageSession session);

        /// <summary>현재 세션 상태를 화면에 다시 그린다.</summary>
        protected abstract void Render();
    }
}
