using BlueComplex.Core.Stage;
using BlueComplex.UI.Bootstrap;
using UnityEngine;

namespace BlueComplex.UI.DebugPlay
{
    /// <summary>
    /// StageSession 이벤트 구독/해제를 공통 처리하는 디버그 뷰 베이스.
    /// 재시작으로 세션이 교체될 때마다 이전 세션 구독을 풀고 새 세션에 다시 건다.
    /// 코어 이벤트를 구독만 할 뿐 게임 규칙은 판정하지 않는다.
    /// </summary>
    internal abstract class DebugSessionView : MonoBehaviour
    {
        protected StageBootstrapper Bootstrapper { get; private set; }
        protected StageSession Session { get; private set; }

        public void Bind(StageBootstrapper bootstrapper)
        {
            Bootstrapper = bootstrapper;
            BuildUI();

            bootstrapper.SessionStarted += HandleSessionStarted;
            if (bootstrapper.Session != null) HandleSessionStarted(bootstrapper.Session);
        }

        private void OnDisable()
        {
            if (Bootstrapper != null) Bootstrapper.SessionStarted -= HandleSessionStarted;
            if (Session != null) UnsubscribeSession(Session);
        }

        private void HandleSessionStarted(StageSession session)
        {
            if (Session != null) UnsubscribeSession(Session);
            Session = session;
            SubscribeSession(session);
            Render();
        }

        /// <summary>UI 계층을 한 번만 만든다. Bind 시점에 호출된다.</summary>
        protected abstract void BuildUI();

        /// <summary>새 세션의 코어 이벤트를 구독한다.</summary>
        protected abstract void SubscribeSession(StageSession session);

        /// <summary>이전 세션의 코어 이벤트 구독을 해제한다.</summary>
        protected abstract void UnsubscribeSession(StageSession session);

        /// <summary>현재 세션 상태를 화면에 다시 그린다.</summary>
        protected abstract void Render();
    }
}
