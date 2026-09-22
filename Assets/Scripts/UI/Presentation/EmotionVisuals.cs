using BlueComplex.Core.Tags;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 감정 태그의 표시 색. 기억 공간에 뜨는 결과 태그 칩이 쓴다 — 침체 쪽 감정(슬픔·혐오·공포)은 차가운 청록, 흥분 쪽(행복·사랑·분노)은 따뜻한 주황
    /// (기획서 생각 공간 이미지의 "슬픔" 청록 칩 / "분노" 주황 칩). 침체/흥분 구분은 코어 기본 극성표(인디케이터 이동량 계산과 같은 표)를 따른다.
    /// </summary>
    public static class EmotionVisuals
    {
        private static readonly IEmotionPolarityTable Table = new DefaultEmotionPolarityTable();

        private static readonly Color Depressed = new Color32(22, 96, 132, 255);
        private static readonly Color Excited = new Color32(226, 132, 86, 255);

        public static bool IsExcited(EmotionTag emotion) => Table.GetPolarity(emotion) == Polarity.Excited;

        public static Color ChipColor(EmotionTag emotion) => IsExcited(emotion) ? Excited : Depressed;
    }
}
