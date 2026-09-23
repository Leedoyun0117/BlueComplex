using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 엑스레이 판넬을 매단 관절 팔 — 아트가 없어서 굵은 선분 두 개(위팔·아래팔)와 관절 원 세 개(어깨·팔꿈치·손목)로 그린다.
    /// 어깨(고정점)와 손목(판넬이 매달린 점)만 정하면 2관절 역기구학으로 팔꿈치가 정해진다 — 접힘/펼침은 손목이 움직이는 것이고,
    /// 접힌 상태(손목이 어깨 바로 옆)에서는 두 선분이 서로 포개져 접히고 펼치면 뻗는다. 팔꿈치는 항상 화면 안쪽(오른쪽)으로 꺾인다.
    /// 좌표는 전부 컨테이너 기준 픽셀(왼쪽 위 원점, y 아래로 +) — 호출자가 컨테이너 크기 비율(<c>unit</c>)을 넘긴다.
    /// </summary>
    public sealed class XrayArm : MonoBehaviour
    {
        [SerializeField] private RectTransform _upper;
        [SerializeField] private RectTransform _lower;
        [SerializeField] private RectTransform _shoulder;
        [SerializeField] private RectTransform _elbow;
        [SerializeField] private RectTransform _wrist;

        /// <summary>선분 굵기와 관절 원 지름(참조 픽셀).</summary>
        private const float BarThickness = 18f;
        private const float JointSize = 30f;

        private void Awake()
        {
            // 관절 원 스프라이트는 프리팹에 굽지 않고 런타임에 입힌다(RuntimeUi.Circle과 같은 방식).
            foreach (var joint in new[] { _shoulder, _elbow, _wrist })
            {
                var image = joint != null ? joint.GetComponent<UnityEngine.UI.Image>() : null;
                if (image != null) image.sprite = RuntimeUi.Circle;
            }
        }

        /// <summary>
        /// 어깨 <paramref name="shoulder"/>에서 손목 <paramref name="wristTarget"/>까지 팔을 뻗는다. 목표가 닿는 거리(두 선분 길이의 합)보다 멀면 그 방향으로 최대한 뻗은 손목 위치를 돌려준다.
        /// </summary>
        /// <param name="length">선분 하나의 길이(참조 픽셀).</param>
        /// <param name="unit">참조 픽셀 → 실제 컨테이너 좌표 배율.</param>
        public Vector2 Solve(Vector2 shoulder, Vector2 wristTarget, float length, float unit)
        {
            var delta = wristTarget - shoulder;
            var distance = Mathf.Clamp(delta.magnitude, 0.001f, length * 1.98f);
            var direction = delta.sqrMagnitude > 1e-6f ? delta.normalized : Vector2.down;
            var wrist = shoulder + direction * distance;

            // 두 선분 길이가 같으므로 팔꿈치는 어깨-손목 선의 수직이등분선 위에 있다. 안쪽(+x) 쪽으로 꺾는다.
            var half = distance * 0.5f;
            var height = Mathf.Sqrt(Mathf.Max(0f, length * length - half * half));
            var normal = new Vector2(-direction.y, direction.x); // y 아래 좌표계에서 진행 방향의 오른쪽 아래
            if (normal.x < 0f) normal = -normal;
            var elbow = shoulder + direction * half + normal * height;

            PlaceBar(_upper, shoulder, elbow, unit);
            PlaceBar(_lower, elbow, wrist, unit);
            PlaceJoint(_shoulder, shoulder, unit);
            PlaceJoint(_elbow, elbow, unit);
            PlaceJoint(_wrist, wrist, unit);
            return wrist;
        }

        private static void PlaceBar(RectTransform bar, Vector2 from, Vector2 to, float unit)
        {
            if (bar == null) return;

            var delta = to - from;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            bar.anchoredPosition = new Vector2(from.x, -from.y) * unit;
            bar.sizeDelta = new Vector2(delta.magnitude * unit, BarThickness * unit);
            bar.localRotation = Quaternion.Euler(0f, 0f, -angle); // y 아래 좌표의 각도는 화면(y 위)에서 부호가 뒤집힌다.
        }

        private static void PlaceJoint(RectTransform joint, Vector2 at, float unit)
        {
            if (joint == null) return;

            joint.anchoredPosition = new Vector2(at.x, -at.y) * unit;
            joint.sizeDelta = Vector2.one * JointSize * unit;
        }
    }
}
