using System;
using System.IO;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Save;
using UnityEngine;

namespace BlueComplex.UI.Bootstrap
{
    /// <summary>
    /// 게임 세이브(Application.persistentDataPath/save.json). 지금은 단서 지식(<see cref="ClueKnowledgeLedger"/>)만 —
    /// 볼륨 같은 설정은 PlayerPrefs에 따로 있고, 스테이지 진행도는 아직 저장하지 않는다. 늘릴 땐 <see cref="SaveData"/>에 필드를 더한다.
    ///
    /// 저장 시점은 <see cref="StageBootstrapper"/>가 정한다(런 종료 CommitRun 직후, 게임 종료). 실패해도 게임은 계속 간다 — 경고만 남긴다.
    /// </summary>
    public static class GameSave
    {
        public const string FileName = "save.json";

        private static SaveFileStore _store;

        public static SaveFileStore Store => _store ??= new SaveFileStore(
            Path.Combine(Application.persistentDataPath, FileName),
            data => JsonUtility.ToJson(data, prettyPrint: true),
            JsonUtility.FromJson<SaveData>);

        /// <summary>세이브가 있으면 장부에 복원한다. 없거나 깨졌으면 빈 장부 그대로(깨진 파일은 .corrupt로 치워진다).</summary>
        public static void LoadInto(ClueKnowledgeLedger ledger)
        {
            try
            {
                if (Store.TryLoad(out var data, out var error))
                {
                    ledger.Restore(data.Clues);
                    Debug.Log($"[GameSave] 불러옴: 단서 {data.Clues.Count}개 ({Store.Path})");
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
                Store.Save(new SaveData { Clues = ledger.ToSaveEntries() });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameSave] 저장 실패: {e.Message}");
            }
        }

        /// <summary>세이브 파일을 지운다(디버그 — 처음 하는 사람의 상태로 시험할 때). 지금 메모리의 장부는 그대로다.</summary>
        public static void Delete()
        {
            try { Store.Delete(); }
            catch (Exception e) { Debug.LogWarning($"[GameSave] 삭제 실패: {e.Message}"); }
        }
    }
}
