using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>어느 종이 머티리얼을 쓸지. 패널은 큰 종이, 카드는 패널 위에 얹힌 작은 종이(지금은 같은 값의 별도 머티리얼 — 나중에 따로 조정할 수 있게).</summary>
    public enum PaperKind
    {
        Panel,
        Card,
    }
}
