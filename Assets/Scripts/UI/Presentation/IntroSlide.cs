using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 오프닝 시퀀스가 PPT 목업 좌표를 그대로 옮겨 쓰는 16:9 틀. PPT 장표는 가로·세로를 0~1로 잰 비율(왼쪽 위가 원점)이라, 캔버스 안에 16:9로 꼭 맞게 앉힌 틀 위에서
    /// 같은 비율로 놓으면 목업과 같은 배치가 된다(캔버스가 16:9가 아니면 남는 자리는 막의 배경색으로 채워진다).
    /// </summary>
    internal sealed class IntroSlide
    {
        public const float Aspect = 16f / 9f;

        public RectTransform Root { get; }
        public Vector2 Size { get; }

        private IntroSlide(RectTransform root, Vector2 size)
        {
            Root = root;
            Size = size;
        }

        public static IntroSlide Create(Transform parent, Vector2 canvasSize)
        {
            var size = canvasSize.x / canvasSize.y > Aspect
                ? new Vector2(canvasSize.y * Aspect, canvasSize.y)
                : new Vector2(canvasSize.x, canvasSize.x / Aspect);

            var go = new GameObject("Slide", typeof(RectTransform), typeof(RectMask2D)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return new IntroSlide(rect, size);
        }

        /// <summary>
        /// 장표 비율 (x, y, w, h)로 자리를 잡은 빈 RectTransform을 만든다(y는 위에서부터). <paramref name="pptRotation"/>은 PPT의 회전 — 시계 방향이 양수다(유니티는 반시계가 양수라 부호를 뒤집는다).
        /// 부모는 <see cref="Root"/> 또는 그것과 같은 크기·중심으로 늘어난 자식이어야 한다.
        /// </summary>
        public RectTransform Place(Transform parent, string name, float x, float y, float w, float h, float pptRotation = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w * Size.x, h * Size.y);
            rect.anchoredPosition = new Vector2((x + w * 0.5f - 0.5f) * Size.x, (0.5f - (y + h * 0.5f)) * Size.y);
            rect.localRotation = Quaternion.Euler(0f, 0f, -pptRotation);
            return rect;
        }

        /// <summary><see cref="Place"/>가 잡는 자리를 만들지 않고 값만: 틀 중심 기준 중심 위치와 픽셀 크기. 이미 놓은 조각을 그 자리로 옮기거나 키울 때 쓴다.</summary>
        public (Vector2 position, Vector2 size) Slot(float x, float y, float w, float h) =>
            (new Vector2((x + w * 0.5f - 0.5f) * Size.x, (0.5f - (y + h * 0.5f)) * Size.y), new Vector2(w * Size.x, h * Size.y));

        /// <summary>장표 비율 좌표(원점 왼쪽 위)의 점을 틀 중심 기준 로컬 좌표로.</summary>
        public Vector2 ToLocal(float x, float y) => new Vector2((x - 0.5f) * Size.x, (0.5f - y) * Size.y);
    }
}
