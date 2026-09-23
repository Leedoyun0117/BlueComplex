using System.Text.RegularExpressions;
using BlueComplex.Core.Stage;
using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>스테이지 표기(목업 좌측 상단): "STAGE 01" 아래에 스테이지 이름("가라앉다"). 스테이지 설정(StageConfig)에서 읽는다 —
    /// 번호는 설정 id 끝의 숫자("stage_1" → 01)이고, 숫자가 없으면 번호 줄만 뺀다.</summary>
    public sealed class StageTitleView : SessionBoundView
    {
        [SerializeField] private TMP_Text _label;

        protected override void Subscribe(StageSession session) { }

        protected override void Unsubscribe(StageSession session) { }

        protected override void Render()
        {
            if (_label == null || Bootstrapper == null || Bootstrapper.Config == null) return;

            var config = Bootstrapper.Config;
            var digits = Regex.Match(config.Id ?? string.Empty, @"(\d+)$");
            var number = digits.Success ? $"STAGE {int.Parse(digits.Value):00}\n" : string.Empty;
            _label.text = number + config.DisplayName;
        }
    }
}
