using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신 2막: 차 주행. 노을 진 주황 하늘과 도시 스카이라인 실루엣(PPT 3번 장표 그대로 — 어두운 직사각형 다섯 개와 땅) 앞에서
    /// 포드 모델 T 도트 그림은 화면 가로 중앙에 멈춰 있고, 건물 실루엣이 오른쪽에서 왼쪽으로 일정한 속도로 흘러가 차가 달리는 것처럼 보인다.
    /// 건물은 화면 너비 한 장(PPT 배치)을 좌우로 이어 붙인 두 장이 돌아가며 이어지므로 얼마나 흘러도 끊기지 않는다. 차는 바퀴·서스펜션이 출렁이듯 도트 한 칸씩 위아래로 살짝 뛴다.
    /// 화면은 <see cref="Drive"/>가 한 번에(하드 컷) 켜고 <see cref="Hide"/>로 끈다.
    /// </summary>
    internal sealed class IntroCarView : MonoBehaviour
    {
        private static readonly Color Sky = new(202f / 255f, 102f / 255f, 2f / 255f);
        private static readonly Color Silhouette = new(24f / 255f, 11f / 255f, 2f / 255f);

        /// <summary>PPT 3번 장표의 건물(직사각형): 왼쪽 x, 윗변 y(둘 다 화면 비율, y는 위에서부터). 너비는 모두 같고 아래는 땅에 묻힌다.</summary>
        private static readonly Vector2[] Buildings =
        {
            new(0.1221f, 0.3651f), new(0.2980f, 0.2825f), new(0.5080f, 0.3434f), new(0.6069f, 0.4646f), new(0.7574f, 0.2705f)
        };
        private const float BuildingWidth = 0.0886f;
        private const float GroundTop = 0.6349f;

        /// <summary>차 그림 한 변(64px)이 화면에서 정수 배가 되게 하는 기준 — 캔버스 높이 270당 1배(1080p에서 4배). 도트가 깨지지 않는다.</summary>
        private const float PixelsPerCarPixel = 270f;
        private const float CarSourceSize = 64f;

        /// <summary>서스펜션 출렁임: 도트 한 칸(정수 배 확대의 1칸)씩 <see cref="BobRate"/>번/초 바뀌는 0·1 패턴.</summary>
        private static readonly int[] BobPattern = { 0, 1, 0, 0, 1, 0 };
        private const float BobRate = 9f;

        /// <summary>차 그림 세로 중심(화면 위에서부터의 비율). 바퀴 바닥이 PPT의 차 위치(화면의 약 88%)에 온다.</summary>
        private const float CarCenterY = 0.79f;

        private CanvasGroup _group;
        private RectTransform _car;
        private readonly RectTransform[] _tiles = new RectTransform[2];
        private float _tileWidth;
        private float _carY;
        private float _bobStep;
        private bool _driving;

        /// <summary>차 그림이 없으면 null — 컷신은 이 구간을 검은 화면으로 두고 시간만 흘린다.</summary>
        public static IntroCarView Create(Transform parent, Vector2 canvasSize, IntroCutsceneArt art)
        {
            if (art == null || art.car == null)
            {
                Debug.LogWarning("[IntroCarView] 차 그림이 채워져 있지 않다 — 메뉴 BlueComplex/Cutscene/Rebuild Intro Art를 실행한다.");
                return null;
            }

            var go = new GameObject("Intro Car", typeof(RectTransform), typeof(CanvasGroup)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);

            var view = go.AddComponent<IntroCarView>();
            view.Build(canvasSize, art.car);
            return view;
        }

        private void Build(Vector2 canvasSize, Texture2D carTexture)
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _tileWidth = canvasSize.x;
            CreateBlock("Sky", transform, Vector2.zero, Vector2.one, Sky);
            for (var i = 0; i < _tiles.Length; i++) _tiles[i] = CreateBuildingTile(canvasSize);
            SetScroll(0f);
            CreateBlock("Ground", transform, Vector2.zero, new Vector2(1f, 1f - GroundTop), Silhouette);

            var scale = Mathf.Max(1, Mathf.RoundToInt(canvasSize.y / PixelsPerCarPixel));
            var size = CarSourceSize * scale;

            var go = new GameObject("Car", typeof(RectTransform), typeof(RawImage)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            _car = (RectTransform)go.transform;
            _car.anchorMin = _car.anchorMax = _car.pivot = new Vector2(0.5f, 0.5f);
            _car.sizeDelta = new Vector2(size, size);

            var raw = go.GetComponent<RawImage>();
            raw.texture = carTexture;
            raw.raycastTarget = false;

            _carY = Mathf.Round((0.5f - CarCenterY) * canvasSize.y);
            _bobStep = scale;
            _car.anchoredPosition = new Vector2(0f, _carY);
        }

        /// <summary>화면을 켜고(하드 컷) 건물을 오른쪽에서 왼쪽으로 <paramref name="screens"/>장 분량 일정한 속도로 흘려 보낸다. 차는 가운데에서 위아래로만 살짝 뛴다.</summary>
        public Tween Drive(float seconds, float screens)
        {
            _group.alpha = 1f;
            _driving = true;
            SetScroll(0f);
            return DOTween.To(() => 0f, v => SetScroll(v * screens), 1f, Mathf.Max(0.01f, seconds))
                .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this);
        }

        private void Update()
        {
            if (!_driving) return;

            var step = Mathf.FloorToInt(Time.unscaledTime * BobRate);
            _car.anchoredPosition = new Vector2(0f, _carY + BobPattern[step % BobPattern.Length] * _bobStep);
        }

        /// <summary>건물 두 장을 왼쪽으로 <paramref name="screens"/>장 분량 밀어 놓는다. 한 장 너비를 넘으면 그만큼 되돌려 이어 붙는다(경계가 보이지 않는다).</summary>
        private void SetScroll(float screens)
        {
            var offset = -(screens % 1f) * _tileWidth;
            _tiles[0].anchoredPosition = new Vector2(offset, 0f);
            _tiles[1].anchoredPosition = new Vector2(offset + _tileWidth, 0f);
        }

        public void Hide()
        {
            DOTween.Kill(this);
            _driving = false;
            _group.alpha = 0f;
        }

        /// <summary>PPT 배치의 건물 다섯 개가 든 화면 크기 한 장. 왼쪽 아래를 기준으로 놓는다.</summary>
        private RectTransform CreateBuildingTile(Vector2 canvasSize)
        {
            var go = new GameObject("Buildings", typeof(RectTransform)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.sizeDelta = canvasSize;

            foreach (var building in Buildings)
                CreateBlock("Building", rect, new Vector2(building.x, 0f), new Vector2(building.x + BuildingWidth, 1f - building.y), Silhouette);
            return rect;
        }

        private void CreateBlock(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { layer = gameObject.layer };
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy() => DOTween.Kill(this);
    }
}
