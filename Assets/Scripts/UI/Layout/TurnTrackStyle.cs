using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>턴 칸 줄의 색 구성. 어두운 오버레이용(기본)과 노란 포스트잇용이 있다.</summary>
    internal sealed class TurnTrackStyle
    {
        public Color Cell;
        public Color LastCell;
        public Color Dot;

        /// <summary>알파가 0보다 크면 현재 턴의 칸을 이 색으로 칠한다(점은 그 위에 놓인다). 0이면 점만 움직인다.</summary>
        public Color CurrentCell;

        /// <summary>칸 테두리(알파 0이면 없음).</summary>
        public Color CellOutline;

        /// <summary>알파가 0보다 크면 이미 지난 턴의 칸을 이 색으로 칠한다(마지막 칸은 항상 LastCell). 0이면 안 지난 칸과 같다.</summary>
        public Color PastCell;

        /// <summary>어두운 배경 위: 회색 칸, 노란 마지막 칸, 하얀 점.</summary>
        public static readonly TurnTrackStyle Dark = new TurnTrackStyle
        {
            Cell = new Color32(140, 140, 150, 200),
            LastCell = new Color32(255, 210, 70, 255),
            Dot = Color.white,
            CurrentCell = Color.clear,
            CellOutline = Color.clear,
        };

        /// <summary>노란 포스트잇 위(목업): 연한 칸에 얇은 테두리, 노란 마지막 칸, 현재 턴은 어두운 칸에 하얀 점.</summary>
        public static readonly TurnTrackStyle Sticky = new TurnTrackStyle
        {
            Cell = new Color32(236, 236, 232, 255),
            LastCell = new Color32(255, 236, 0, 255),
            Dot = Color.white,
            CurrentCell = new Color32(44, 44, 46, 255),
            CellOutline = new Color32(78, 72, 64, 255),
            PastCell = new Color32(196, 192, 176, 255),
        };
    }
}
