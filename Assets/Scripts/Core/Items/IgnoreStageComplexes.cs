using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Items
{
    /// <summary>
    /// 감정적 설득 — 스테이지가 지정한 컴플렉스(예: '침체' 감정에 영향을 주는 것들)를 지속 시간 동안 무시한다.
    /// 어떤 컴플렉스인지는 스테이지마다 다르므로 <see cref="StageParameterKey"/>로 StageConfig.ItemParameters에서 읽는다.
    /// </summary>
    public sealed class IgnoreStageComplexes : IItemBehaviour
    {
        public string StageParameterKey { get; }

        public IgnoreStageComplexes(string stageParameterKey) => StageParameterKey = stageParameterKey;

        public void OnActivate(ItemActivationContext context) =>
            context.AddFilter(new IdSetFilter(context.Parameter(StageParameterKey)));

        private sealed class IdSetFilter : IComplexFilter
        {
            private readonly string[] _ids;
            public IdSetFilter(System.Collections.Generic.IReadOnlyList<string> ids) => _ids = ids.ToArray();
            public bool ShouldIgnore(ComplexInstance complex) => _ids.Contains(complex.Definition.Id);
        }
    }
}
