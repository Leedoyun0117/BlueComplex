using System;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Complexes;
using BlueComplex.Core.Clues;
using BlueComplex.Core.Items;
using BlueComplex.UI.Layout;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>대상 선택 모드에서 강조되는 뷰(컴플렉스 행·뇌 영역·단서 카드). 강조를 켜고 끄기만 한다 — 클릭은 뷰가 <see cref="ItemTargetSelector.TryPick(ComplexInstance)"/>로 넘긴다.</summary>
    public interface ITargetHighlight
    {
        void SetTargetable(bool on);
    }
}
