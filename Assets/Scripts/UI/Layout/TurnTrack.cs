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
    /// <summary>
    /// 인디케이터 점 + 칸 줄. 턴 수만큼의 칸을 늘어놓고(마지막 칸은 노랑 — 키를 얻는 턴) 하얀 점이 현재 턴의 칸에 놓인다.
    /// 쿼터 진행 창과 전체 스테이지 오버레이의 쿼터 블록이 같은 모양이라 함께 쓴다.
    /// </summary>
    internal sealed class TurnTrack
    {
        private const float DotMoveDuration = 0.25f;

        private readonly RectTransform _dot;
        private readonly Image _dotImage;
        private readonly Image[] _cells;
        private readonly TurnTrackStyle _style;
        private readonly int _turns;

        private float _dotFraction;
        private bool _dotShown;
        private Tween _moveTween;

        public TurnTrack(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int turns, float dotSize,
            TurnTrackStyle style = null)
        {
            _turns = turns;
            _style = style ?? TurnTrackStyle.Dark;

            var root = RuntimeUi.CreateRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            _cells = new Image[turns];
            for (var i = 0; i < turns; i++)
            {
                var isLast = i == turns - 1;
                var cell = RuntimeUi.CreateImage(root, $"Turn {i + 1}", isLast ? _style.LastCell : _style.Cell,
                    new Vector2(i / (float)turns, 0.12f), new Vector2((i + 1) / (float)turns, 0.88f),
                    new Vector2(3f, 0f), new Vector2(-3f, 0f));

                if (_style.CellOutline.a > 0f)
                {
                    var outline = cell.gameObject.AddComponent<Outline>();
                    outline.effectColor = _style.CellOutline;
                    outline.effectDistance = new Vector2(1.2f, -1.2f);
                    outline.useGraphicAlpha = false;
                }

                _cells[i] = cell;
            }

            _dotImage = RuntimeUi.CreateImage(root, "Dot", _style.Dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, RuntimeUi.Circle);
            _dot = _dotImage.rectTransform;
            _dot.sizeDelta = new Vector2(dotSize, dotSize);
            _dot.gameObject.SetActive(false);
        }

        /// <summary>현재 턴(1부터)의 칸으로 점을 옮긴다. 범위 밖(0 등)이면 점을 감춘다.
        /// animate가 아니거나 점이 처음 나타나는 경우(쿼터가 바뀌어 리셋된 경우 포함)에는 그 자리로 바로 놓는다.</summary>
        public void SetTurn(int turn, bool animate)
        {
            _moveTween?.Kill();
            PaintCells(turn);

            if (turn < 1 || turn > _turns)
            {
                _dotShown = false;
                _dot.gameObject.SetActive(false);
                return;
            }

            var target = (turn - 0.5f) / _turns;
            _dot.gameObject.SetActive(true);

            if (animate && _dotShown && !Mathf.Approximately(_dotFraction, target))
                _moveTween = DOVirtual.Float(_dotFraction, target, DotMoveDuration, MoveDot).SetEase(Ease.OutQuad);
            else
                MoveDot(target);

            _dotShown = true;
        }

        public void Kill() => _moveTween?.Kill();

        /// <summary>칸 색을 되돌리고, 스타일이 원하면 현재 턴의 칸만 어둡게 칠한다. 점은 마지막 칸이면 노랑(키를 얻는 턴이라는 표시를 유지한다).</summary>
        private void PaintCells(int turn)
        {
            var paintCurrent = _style.CurrentCell.a > 0f;
            var paintPast = _style.PastCell.a > 0f;
            for (var i = 0; i < _cells.Length; i++)
            {
                var isLast = i == _cells.Length - 1;
                var isCurrent = i + 1 == turn;
                var isPast = i + 1 < turn;
                _cells[i].color = paintCurrent && isCurrent ? _style.CurrentCell
                    : isLast ? _style.LastCell
                    : paintPast && isPast ? _style.PastCell
                    : _style.Cell;
            }

            var currentIsLast = turn == _cells.Length;
            _dotImage.color = paintCurrent && currentIsLast ? _style.LastCell : _style.Dot;
        }

        private void MoveDot(float fraction)
        {
            _dotFraction = fraction;
            _dot.anchorMin = new Vector2(fraction, 0.5f);
            _dot.anchorMax = new Vector2(fraction, 0.5f);
            _dot.anchoredPosition = Vector2.zero;
        }
    }
}
