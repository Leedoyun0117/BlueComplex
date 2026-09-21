using System;
using UnityEngine;

namespace BlueComplex.UI.Background
{
    /// <summary>
    /// 2D 도트 레이어를 카메라 앞 Z축에 분리 배치한다.
    ///
    /// 모든 레이어 PNG는 같은 캔버스(예: 3200×1800)를 채우고 각자의 위치가 이미 맞춰져 있다 — 좌표를 옮기지 않고
    /// 깊이만 벌리면 원근 때문에 먼 레이어는 작아지고 가까운 레이어는 커져서 정렬이 깨진다. 그래서 레이어를
    /// "카메라 정지 자세에서 정확히 같은 크기로 보이도록" 거리에 비례해 스케일한다(scale = 거리 / 기준 거리).
    /// 이렇게 하면 카메라가 가만히 있을 땐 완성본과 픽셀 단위로 일치하고, 카메라가 움직일 때만 시차가 생긴다.
    ///
    /// 이 리그의 Transform이 곧 "카메라 정지 자세"다 — 레이어는 리그의 로컬 +Z 방향으로 놓인다.
    /// 카메라 자체는 이 리그의 자식이 아니다(카메라가 움직여도 레이어가 따라오면 시차가 사라진다).
    /// 배치는 <see cref="Apply"/>가 한 번 계산해 Transform에 써 넣는 것뿐이고, 런타임 Update는 없다.
    /// </summary>
    public sealed class BackgroundLayerRig : MonoBehaviour
    {
        [Serializable]
        public struct Layer
        {
            [Tooltip("이 레이어의 최상위 Transform. 자식으로 캔버스 크기의 쿼드가 있어야 한다.")]
            public Transform Root;

            [Tooltip("카메라 정지 위치에서 이 레이어까지의 거리(월드 유닛). 작을수록 카메라에 가깝다.")]
            public float Distance;
        }

        [Tooltip("시야각(FOV)을 읽어올 카메라. 값은 Apply 시점에 한 번만 읽는다.")]
        [SerializeField] private Camera _camera;

        [Tooltip("레이어 PNG의 픽셀 크기. 모든 레이어가 같은 캔버스를 쓴다.")]
        [SerializeField] private Vector2 _canvasPixels = new Vector2(3200f, 1800f);

        [Tooltip("스프라이트 Pixels Per Unit. 모든 레이어 PNG에 같은 값을 쓴다.")]
        [SerializeField] private float _pixelsPerUnit = 100f;

        [SerializeField] private Layer[] _layers = Array.Empty<Layer>();

        public Vector2 CanvasPixels => _canvasPixels;
        public float PixelsPerUnit => _pixelsPerUnit;

        /// <summary>캔버스 전체(세로 기준)가 카메라 시야에 정확히 들어차는 거리.</summary>
        public float FullFrameDistance
        {
            get
            {
                var fov = _camera != null ? _camera.fieldOfView : 60f;
                var halfHeight = _canvasPixels.y / _pixelsPerUnit * 0.5f;
                return halfHeight / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            }
        }

        /// <summary>거리 distance에 놓인 레이어의 균일 스케일. 1픽셀 = 1/PPU 유닛인 쿼드가 정지 자세에서 화면에 같은 크기로 보이게 한다.</summary>
        public float ScaleAt(float distance) => distance / FullFrameDistance;

        /// <summary>캔버스 좌표(픽셀, 좌상단 원점)를 distance 거리 레이어 평면 위의 월드 위치로 바꾼다.</summary>
        public Vector3 CanvasPixelToWorld(Vector2 pixel, float distance)
        {
            var offsetUnits = new Vector2(pixel.x - _canvasPixels.x * 0.5f, _canvasPixels.y * 0.5f - pixel.y) / _pixelsPerUnit;
            var scale = ScaleAt(distance);
            return transform.TransformPoint(new Vector3(offsetUnits.x * scale, offsetUnits.y * scale, distance));
        }

        public void SetLayers(Camera camera, Layer[] layers)
        {
            _camera = camera;
            _layers = layers;
        }

        /// <summary>각 레이어를 리그 로컬 (0, 0, Distance)에 놓고 거리에 비례해 스케일한다.</summary>
        [ContextMenu("Apply Layout")]
        public void Apply()
        {
            foreach (var layer in _layers)
            {
                if (layer.Root == null) continue;

                layer.Root.SetParent(transform, worldPositionStays: false);
                layer.Root.localPosition = new Vector3(0f, 0f, layer.Distance);
                layer.Root.localRotation = Quaternion.identity;
                layer.Root.localScale = Vector3.one * ScaleAt(layer.Distance);
            }
        }
    }
}
