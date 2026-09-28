using System;
using System.IO;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Save;
using UnityEngine;

namespace BlueComplex.UI.Bootstrap
{
    /// <summary>
    /// 게임 세이브(Application.persistentDataPath/save.json). 단서 지식(<see cref="ClueKnowledgeLedger"/>)과
    /// 스테이지 진행도(<see cref="TutorialCleared"/>/<see cref="HighestStageCleared"/>)를 담는다 —
    /// 볼륨 같은 설정은 PlayerPrefs에 따로 있다. 늘릴 땐 <see cref="SaveData"/>에 필드를 더한다.
    ///
    /// 진행도는 파일에 쓸 때마다(<see cref="Save"/>) 매번 같이 실리므로, 불러온 값을 메모리에 캐시해 둔다 —
    /// 그러지 않으면 단서 지식만으로 만든 <see cref="SaveData"/>가 진행도를 0/false로 덮어써 버린다.
    ///
    /// 저장 시점은 <see cref="StageBootstrapper"/>/<see cref="BlueComplex.UI.Presentation.StageEndController"/>가 정한다
    /// (런 종료 CommitRun 직후, 튜토리얼/스테이지 클리어, 게임 종료). 실패해도 게임은 계속 간다 — 경고만 남긴다.
    /// </summary>
    public static class GameSave
    {
        public const string FileName = "save.json";

        private static SaveFileStore _store;

        /// <summary>테스트/스크래치 사본 하니스가 세이브 파일 경로를 실제 persistentDataPath 밖으로 돌릴 때 쓴다 —
        /// 여러 스크래치 사본이 ProjectSettings(회사명·제품명)를 그대로 복사해 오면 전부 같은 실제 persistentDataPath를
        /// 가리켜 서로의 세이브를 덮어쓴다(겪은 사고: 튜토리얼 클리어 여부·본 단서 수가 다른 세션 사이에서 바뀌어 있었다).
        /// 씬을 열기 전, <see cref="Store"/>에 처음 접근하기 전에 설정해야 한다. 참고: [[reference-unity-scratch-save-path-collision]].</summary>
        public static string TestPathOverride { get; set; }

        public static SaveFileStore Store => _store ??= new SaveFileStore(
            TestPathOverride ?? Path.Combine(Application.persistentDataPath, FileName),
            data => JsonUtility.ToJson(data, prettyPrint: true),
            JsonUtility.FromJson<SaveData>);

        /// <summary>다음 접근 때 <see cref="Store"/>를 <see cref="TestPathOverride"/>로 다시 만들게 한다 —
        /// 이미 한 번 접근해 캐시된 뒤에 경로를 바꾸는 테스트 하니스가 쓴다.</summary>
        public static void ResetStoreForTests() => _store = null;

        /// <summary>튜토리얼을 클리어했는가(불러온 값 캐시, <see cref="MarkTutorialCleared"/>가 갱신한다).</summary>
        public static bool TutorialCleared { get; private set; }

        /// <summary>클리어한 가장 높은 스테이지 번호(0 = 아직 없음, <see cref="MarkStageCleared"/>가 갱신한다).</summary>
        public static int HighestStageCleared { get; private set; }

        /// <summary>세이브가 있으면 장부와 진행도를 복원한다. 없거나 깨졌으면 처음 하는 사람의 상태 그대로(깨진 파일은 .corrupt로 치워진다).</summary>
        public static void LoadInto(ClueKnowledgeLedger ledger)
        {
            TutorialCleared = false;
            HighestStageCleared = 0;

            try
            {
                if (Store.TryLoad(out var data, out var error))
                {
                    ledger.Restore(data.Clues);
                    TutorialCleared = data.TutorialCleared;
                    HighestStageCleared = data.HighestStageCleared;
                }
                else if (error != null)
                {
                    Debug.LogWarning($"[GameSave] 세이브가 깨져 빈 상태로 시작한다({error}). 원본은 {Store.Path}.corrupt");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameSave] 불러오기 실패 — 빈 상태로 시작한다: {e.Message}");
            }
        }

        public static void Save(ClueKnowledgeLedger ledger)
        {
            try
            {
                Store.Save(new SaveData
                {
                    Clues = ledger.ToSaveEntries(),
                    TutorialCleared = TutorialCleared,
                    HighestStageCleared = HighestStageCleared,
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameSave] 저장 실패: {e.Message}");
            }
        }

        /// <summary>튜토리얼 클리어를 기록하고 곧바로 저장한다. 실패나 중도 이탈에서는 부르지 않는다.</summary>
        public static void MarkTutorialCleared(ClueKnowledgeLedger ledger)
        {
            TutorialCleared = true;
            Save(ledger);
        }

        /// <summary>스테이지 클리어를 기록하고(이미 더 높은 스테이지를 클리어했으면 그대로 둔다) 곧바로 저장한다. 실패나 중도 이탈에서는 부르지 않는다.</summary>
        public static void MarkStageCleared(ClueKnowledgeLedger ledger, int stageNumber)
        {
            if (stageNumber > HighestStageCleared) HighestStageCleared = stageNumber;
            Save(ledger);
        }

        /// <summary>세이브 파일을 지운다(디버그 — 처음 하는 사람의 상태로 시험할 때). 지금 메모리의 장부·진행도는 그대로다.</summary>
        public static void Delete()
        {
            try { Store.Delete(); }
            catch (Exception e) { Debug.LogWarning($"[GameSave] 삭제 실패: {e.Message}"); }
        }

        /// <summary>플레이어용 "세이브 데이터 초기화": 파일을 지우고 <paramref name="ledger"/>와 캐시된 진행도까지
        /// 처음 하는 사람의 상태로 되돌린다. <see cref="Delete"/>만으로는 메모리에 이미 불러온 진행도가 다음 저장에서
        /// 파일에 그대로 되살아나 버린다 — 그래서 메모리도 함께 비운다.</summary>
        public static void ResetAll(ClueKnowledgeLedger ledger)
        {
            Delete();
            TutorialCleared = false;
            HighestStageCleared = 0;
            ledger?.Clear();
        }
    }
}
