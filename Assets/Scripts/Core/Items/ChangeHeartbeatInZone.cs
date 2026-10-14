using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>
    /// 착한 사마리아인·심호흡 — 심박수가 <see cref="Side"/> 구간에 있을 때만 <see cref="Amount"/>만큼 바꾼다(사마리아인: 침체면 +10, 심호흡: 흥분이면 -10).
    /// 아니면 심박수는 그대로다. 구간 판정은 사용하는 시점의 심박수 기준이다.
    /// </summary>
    public sealed class ChangeHeartbeatInZone : IItemBehaviour
    {
        public Polarity Side { get; }
        public int Amount { get; }

        public ChangeHeartbeatInZone(Polarity side, int amount)
        {
            Side = side;
            Amount = amount;
        }

        public void OnActivate(ItemActivationContext context)
        {
            if (context.Zone.PolarityOf(context.Heartbeat.Value) == Side) context.Heartbeat.Change(Amount);
        }
    }
}
