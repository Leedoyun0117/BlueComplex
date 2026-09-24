using System.Linq;
using System.Reflection;
using NUnit.Framework;
using BlueComplex.Core.Stability;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 단서 검열은 없다 — 극단 심박수 구간에서 정보를 가리면 키를 노려 극단으로 밀수록 추리가 막히는 자기모순이라 삭제했다.
    /// 해금(ClueKnowledgeLedger)은 별개 시스템이라 그대로 살아 있다 — 그쪽은 기존 해금 테스트가 지킨다.
    /// </summary>
    public class NoCensorshipTests
    {
        [Test]
        public void CoreAssembly_HasNoCensorshipTypesOrMembers()
        {
            var coreTypes = typeof(HeartbeatZone).Assembly.GetTypes();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            Assert.IsEmpty(coreTypes.Where(t => t.Name.Contains("Censor")).Select(t => t.FullName), "검열 타입이 남아 있다");
            Assert.IsEmpty(coreTypes.SelectMany(t => t.GetMembers(flags).Select(m => $"{t.FullName}.{m.Name}"))
                                    .Where(name => name.Contains("Censor")), "검열 멤버가 남아 있다");
        }

        [Test]
        public void ZoneDefinitions_CarryOnlyStateAndSpawnChance()
        {
            var zone = new HeartbeatZone();

            foreach (var value in new[] { 0, 25, 55, 85, 125, 170, 195 })
            {
                var definition = zone.Resolve(value);
                Assert.AreEqual(zone.StateOf(value), definition.State, $"심박수 {value}");
                Assert.AreEqual(zone.ComplexSpawnChanceOf(value), definition.ComplexSpawnChance, $"심박수 {value}");
            }
        }
    }
}
