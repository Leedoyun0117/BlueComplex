using System;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Turn;

namespace BlueComplex.UI.Presentation
{
    /// <summary>턴 결과를 대사창에 낼 순수 감정 반응 대사로 바꾼다(심박수·키 획득 등은 심박수 모니터·자물쇠 연출 등 별도 시각 요소가
    /// 전달하므로 여기서는 다루지 않는다). 컴플렉스 발현만은 예외 — 노션 "UI 연출" 표: "컴플렉스 발현 이펙트 — 나츠의 독백으로 전달된다."(<see cref="BuildComplexSpawnLine"/>).
    /// Immediate/Cinematic 두 Presenter가 같이 쓴다.</summary>
    public static class TurnSummaryFormatter
    {
        /// <summary>컴플렉스가 발동하는 순간 대사창에 뜨는 짧은 이벤트 대사 — 컴플렉스 id로 <see cref="ComplexReactionLines"/> 표에서 뽑는다
        /// (대사가 여럿이면 무작위). 표에 대사가 없는 컴플렉스는 이름만 넣은 공통 문구로 대신한다. 표시 이름에 이미 "컴플렉스"가 들어 있다("죄책감 컴플렉스").</summary>
        public static string BuildComplexEventLine(ComplexInstance complex) =>
            ComplexReactions.Pick(complex.Definition.Id) ?? $"\"{complex.Definition.DisplayName}\"이(가) 반응했다!";

        /// <summary>컴플렉스가 새로 발현되는 순간 대사창에 뜨는 나츠의 독백 — 턴 번호·괄호 숫자 등 내부 정보는 넣지 않는다.
        /// 한 턴에 새로 발현되는 컴플렉스는 최대 하나뿐이다(TurnRunner.CompleteTurn — 스폰 시도가 턴마다 한 번).</summary>
        public static string BuildComplexSpawnLine(ComplexInstance spawned) =>
            $"—\"{spawned.Definition.DisplayName}\"이(가) 나타났어.";

        /// <summary>손에 낼 단서가 없어 턴이 그냥 넘어갈 때 대사창에 나오는 짧은 대사. 컴플렉스 발현·특성 발현·키 판정·스테이지 결과는
        /// 뇌 UI·포스트잇·심박수 모니터·자물쇠 연출 등 별도 시각 요소로 이미 전달되므로 여기서는 넣지 않는다(노션 "대화와 반응" 표에도 이 경우의 대사는 없다).</summary>
        private static readonly string[] PassLines =
        {
            "딱히… 떠오르는 게 없어.",
            "음… 아무 생각도 안 나네.",
        };

        public static string BuildPassLine(Func<int, int> range = null) =>
            PassLines[(range ?? DefaultRange)(PassLines.Length)];

        private static int DefaultRange(int count) => UnityEngine.Random.Range(0, count);

        /// <summary>"대화와 반응" 표 "2. 결과에 따른 대사" — 그 턴의 최종 감정 태그 조합(종류만, 개수 무시)에 붙는
        /// 유키의 결과 대사. 표에 없는 조합(태그 없음, 침체/흥분 감정이 섞인 경우 등)은 null — 호출자가 그 턴엔 건너뛴다.</summary>
        public static string BuildResultTagLine(TurnReport report) =>
            report.IsPass ? null : ResultTagReactions.Pick(report.FinalTags);

        /// <summary>MemorySpaceBubble의 고정 표시용 — "혐오 ×2, 슬픔  ▲ 흥분" 형태.
        /// 침체/흥분 구분은 이번 턴 심박수 변화 방향(HeartbeatDelta)으로 색을 입힌다.</summary>
        public static string BuildFinalEmotionSummary(TurnReport report)
        {
            var parts = report.FinalTags.Emotions
                .Select(pair => pair.Value > 1
                    ? $"{KoreanLabels.Emotion(pair.Key)} ×{pair.Value}"
                    : KoreanLabels.Emotion(pair.Key));

            var body = string.Join(", ", parts);
            if (string.IsNullOrEmpty(body)) body = "(없음)";

            var direction = report.HeartbeatDelta switch
            {
                > 0 => "<color=#D97B4A>▲ 흥분</color>",
                < 0 => "<color=#5B8FB0>▼ 침체</color>",
                _ => "<color=#AAAAAA>- 변화 없음</color>"
            };

            return $"{body}  {direction}";
        }
    }
}
