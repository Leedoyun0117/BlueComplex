using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Turn;

namespace BlueComplex.Core.Stage
{
    /// <summary>가이드 단계가 나오는 순간 함께 터지는 연출. 원문의 "(효과음)" 표시 지점 — 소리 자체는 UI가 훅(UiSoundCue)으로 낸다.</summary>
    public enum GuideEffect
    {
        None,

        /// <summary>최면 접속이 시작되는 효과음(원문 "그럼, 최면 접속을 시작할테니 준비하게." 뒤의 "(효과음)").</summary>
        HypnosisConnect
    }
}
