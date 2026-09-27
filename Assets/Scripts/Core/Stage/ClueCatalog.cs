using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Tags;

namespace BlueComplex.Core.Stage
{
    /// <summary>
    /// 본편(스테이지 1→2→3)의 모든 단서, 스테이지 순·저작 순. 시작 화면의 단서 노트가 이 순서로 목록을 만든다.
    /// 튜토리얼 단서는 넣지 않는다 — 튜토리얼은 장부도 따로라(<see cref="TutorialContent"/>) 본편 노트에 나올 일이 없다.
    /// 스테이지가 늘면 여기에 더한다.
    /// </summary>
    public static class ClueCatalog
    {
        public static IReadOnlyList<ClueDefinition> MainStory(IEmotionPolarityTable polarityTable)
        {
            var configs = new[]
            {
                PrototypeContent.PrototypeStage(polarityTable),
                Stage2Content.Stage2(polarityTable),
                Stage3Content.Stage3(polarityTable)
            };

            // 같은 단서를 두 스테이지가 공유하게 되더라도 노트에는 한 번만.
            var seen = new HashSet<string>();
            return configs.SelectMany(c => c.Clues).Where(def => seen.Add(def.Id)).ToList();
        }
    }
}
