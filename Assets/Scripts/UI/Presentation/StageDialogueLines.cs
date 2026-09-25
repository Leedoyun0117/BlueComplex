using System;
using BlueComplex.Core.Stability;
using BlueComplex.Core.Tags;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>대사 한 줄의 화자. 플레이어는 작은따옴표(’…’), 유키는 큰따옴표나 따옴표 없이(원문 표기 그대로) 적힌다 — "스테이지 시작 대화" 원문 표기 기준.</summary>
    public enum DialogueSpeaker
    {
        Player,
        Yuki
    }

    [Serializable]
    public struct DialogueLine
    {
        public DialogueSpeaker speaker;
        [TextArea] public string text;
    }

    /// <summary>순서대로 재생할 대사 한 묶음(변형 하나).</summary>
    [Serializable]
    public struct DialogueVariant
    {
        public DialogueLine[] lines;
    }

    /// <summary>쿼터 분기·스테이지 클리어 대사를 고를 때 쓰는 크게 묶은 심박수 상태 — 흥분/안정/침체.</summary>
    public enum StageDialogueMood
    {
        Excited,
        Stable,
        Depressed
    }

    /// <summary>흥분/안정/침체 각각의 대사 후보 풀.</summary>
    [Serializable]
    public struct MoodDialoguePool
    {
        public DialogueVariant[] excited;
        public DialogueVariant[] stable;
        public DialogueVariant[] depressed;

        public DialogueVariant[] For(StageDialogueMood mood) => mood switch
        {
            StageDialogueMood.Excited => excited,
            StageDialogueMood.Depressed => depressed,
            _ => stable
        };
    }

    [Serializable]
    public struct StageDialogueEntry
    {
        public string stageId;

        /// <summary>인스펙터에서 어느 스테이지인지 알아보기 위한 이름표. 조회에는 쓰지 않는다.</summary>
        public string label;

        /// <summary>이 스테이지에 이 세션에서 "정말 처음" 들어올 때만 나오는 고정 시작 대사 — 무작위가 아니라 항상 이거다.
        /// 기획표의 "처음" 칸. lines가 비어 있으면 첫 진입에도 <see cref="stageStartReplay"/> 중 무작위(그것도 비었으면 대사 없이 넘어간다).</summary>
        public DialogueVariant stageStartFirst;

        /// <summary>두 번째 이후 진입(재시작·재방문)에 이 중 하나를 무작위로 고른다 — 기획표의 "그 후 (랜덤 변화)" 칸.</summary>
        public DialogueVariant[] stageStartReplay;

        /// <summary>각 쿼터가 끝나는 시점의 심박수 구간에 맞는 후보 중 하나를 무작위로 고른다.</summary>
        public MoodDialoguePool quarterEnd;

        /// <summary>스테이지 클리어 시점의 최종 심박수 구간에 맞는 대사(구간마다 보통 1개, 후보가 여럿이면 무작위로 고른다).</summary>
        public MoodDialoguePool stageClear;
    }

    /// <summary>
    /// "스테이지 시작 대화" 기획표를 그대로 옮기는 자리(Assets/Resources/StageDialogueLines) — 스테이지 id → 시작/쿼터 분기/클리어 대사.
    /// 대사는 코드가 아니라 이 에셋에 있다(ComplexReactionLines와 같은 방식). 스테이지 2·3은 표가 채워지는 대로 인스펙터에서 항목을 늘리면 된다.
    /// Resources에 두는 이유도 같다: 프리팹 배선 없이 어디서든 같은 표를 읽는다.
    /// </summary>
    public sealed class StageDialogueLines : ScriptableObject
    {
        public const string ResourcePath = "StageDialogueLines";

        [SerializeField] private StageDialogueEntry[] _entries = Array.Empty<StageDialogueEntry>();

        /// <param name="isFirstEntry">이 세션에서 이 스테이지에 처음 들어오는가. true면 <see cref="StageDialogueEntry.stageStartFirst"/>를
        /// 그대로(무작위 없이) 돌려주고, false면 stageStartReplay 중 하나를 무작위로 고른다.</param>
        /// <param name="range">[0, count) 범위의 인덱스를 고르는 함수(재진입일 때만 쓰인다). null이면 UnityEngine.Random(검증용으로 바꿔 끼울 수 있게 열어 둔다).</param>
        public DialogueVariant? PickStageStart(string stageId, bool isFirstEntry, Func<int, int> range = null)
        {
            var entry = FindEntry(stageId);
            if (entry == null) return null;

            if (isFirstEntry)
            {
                var first = entry.Value.stageStartFirst;
                if (first.lines != null && first.lines.Length > 0) return first;

                // "처음" 칸이 비어 있고 후보만 있는 스테이지(스테이지 2: 원문이 "(랜덤"으로만 나뉜다)는 처음에도 후보 중 무작위.
            }

            return Pick(entry.Value.stageStartReplay, range);
        }

        public DialogueVariant? PickQuarterEnd(string stageId, StageDialogueMood mood, Func<int, int> range = null) =>
            Pick(FindEntry(stageId)?.quarterEnd.For(mood), range);

        public DialogueVariant? PickStageClear(string stageId, StageDialogueMood mood, Func<int, int> range = null) =>
            Pick(FindEntry(stageId)?.stageClear.For(mood), range);

        private StageDialogueEntry? FindEntry(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return null;

            foreach (var entry in _entries)
                if (entry.stageId == stageId)
                    return entry;

            return null;
        }

        private static DialogueVariant? Pick(DialogueVariant[] variants, Func<int, int> range)
        {
            if (variants == null) return null;

            // 줄이 없는(인스펙터에서 늘리기만 한) 변형은 후보에서 뺀다.
            var count = 0;
            foreach (var variant in variants)
                if (variant.lines != null && variant.lines.Length > 0)
                    count++;
            if (count == 0) return null;

            var pick = count == 1 ? 0 : (range ?? DefaultRange)(count);
            foreach (var variant in variants)
            {
                if (variant.lines == null || variant.lines.Length == 0) continue;
                if (pick-- == 0) return variant;
            }

            return null;
        }

        private static int DefaultRange(int count) => UnityEngine.Random.Range(0, count);
    }

    /// <summary>스테이지 대사 조회. 에셋이 없거나 그 항목이 비어 있으면 null — 호출자는 대사 없이 넘어간다.</summary>
    public static class StageDialogues
    {
        private static StageDialogueLines _lines;
        private static StageDialogueLines Lines => _lines != null ? _lines : _lines = Resources.Load<StageDialogueLines>(StageDialogueLines.ResourcePath);

        public static DialogueVariant? PickStageStart(string stageId, bool isFirstEntry, Func<int, int> range = null) =>
            Lines != null ? Lines.PickStageStart(stageId, isFirstEntry, range) : null;

        public static DialogueVariant? PickQuarterEnd(string stageId, StageDialogueMood mood, Func<int, int> range = null) =>
            Lines != null ? Lines.PickQuarterEnd(stageId, mood, range) : null;

        public static DialogueVariant? PickStageClear(string stageId, StageDialogueMood mood, Func<int, int> range = null) =>
            Lines != null ? Lines.PickStageClear(stageId, mood, range) : null;
    }

    /// <summary>
    /// 심박수 값을 흥분/안정/침체 셋 중 하나로 묶는다("쿼터 분기"/"스테이지 클리어" 대사를 고를 때 쓰는 구분).
    /// HeartbeatZone.PolarityOf(침체 쪽/흥분 쪽/그 외 null)를 그대로 쓰고, null(안정+즉사)은 안정으로 묶는다 —
    /// 즉사 구간은 스테이지가 Failed로 끝나 이 대사들이 쓰일 일이 없다.
    /// </summary>
    public static class StageDialogueMoodClassifier
    {
        public static StageDialogueMood Classify(HeartbeatZone zone, int heartbeatValue)
        {
            var polarity = zone.PolarityOf(heartbeatValue);
            if (polarity == Polarity.Excited) return StageDialogueMood.Excited;
            if (polarity == Polarity.Depressed) return StageDialogueMood.Depressed;
            return StageDialogueMood.Stable;
        }
    }
}
