using BlueComplex.UI.Layout;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 오프닝의 소녀. 주황 창문 앞에 정지 실루엣(<see cref="IntroCutsceneArt.girlSilhouette"/>)으로 나타났다가, 걷기 8프레임 시트(<see cref="IntroCutsceneArt.girlWalk"/>)로
    /// 화면을 걸어 가고, 사라진 뒤 화면 오른쪽 위에 거꾸로 선 모습으로 잠깐 나타난다. 위치는 PPT 장표 비율(가로·세로 0~1, 세로는 위에서부터)의 그림 왼쪽 위 모서리다 —
    /// 그림 한 칸(투명 여백 포함)의 왼쪽 위. 움직임은 <see cref="IntroSequencePlayer"/>가 정하고 이 조각은 자세만 받는다.
    /// </summary>
    internal sealed class IntroGirlView
    {
        // 정지 실루엣(PPT 7번 장표): 왼쪽 위 (0.435, 0.128), 크기 0.424 × 0.707. 창문 오른쪽(0.687)을 넘은 부분은 PPT에서 배경색 사각형이 가려 창문 안에서만 보인다.
        private const float StillLeft = 0.435f;
        private const float StillTop = 0.128f;
        private const float StillHeight = 0.707f;
        private const float StillClipRight = 0.687f;

        /// <summary>걷는 소녀 한 칸의 높이(화면 높이 비율, PPT 8~11번 장표).</summary>
        public const float WalkHeight = 0.607f;

        // 시트(2146×733)를 8칸으로 자르는 왼쪽 픽셀. 칸 너비는 2146/8이지만 둘째 프레임의 앞발·치맛자락이 6픽셀 다음 칸으로 넘어가 그 칸만 275로 민다.
        private const int FrameCount = 8;
        private const float SheetWidth = 2146f;
        private const float SheetHeight = 733f;
        private const float FrameWidth = SheetWidth / FrameCount;
        private static readonly float[] FrameLeft = { 0f, 275f, 544f, 806f, 1078f, 1341.5f, 1609.75f, 1877.75f };

        // 시트의 프레임마다 몸이 칸 안에서 제멋대로 흔들린다(머리·몸통 중심이 칸 안 122~170px로 오간다 — 걷는 동안 몸이 뒤로 밀리다 한 바퀴 돌 때 튄다).
        // 몸통이 늘 같은 자리에 있도록 프레임마다 그림을 옮길 픽셀(시트 기준, 오른쪽이 +). 머리와 몸통 중심의 평균으로 쟀다.
        private static readonly float[] FrameShift = { -29.5f, -16.5f, 7.4f, 13.2f, 8.2f, 15.1f, 9.4f, -7.2f };

        private readonly IntroSlide _slide;
        private readonly RectTransform _root;
        private readonly RawImage _stillImage;
        private readonly RectTransform _walkRect;
        private readonly RawImage _walkImage;
        private readonly Vector2 _walkSize; // 걷는 한 칸의 크기(장표 비율).

        private IntroGirlView(IntroSlide slide, RectTransform root, RawImage stillImage, RectTransform walkRect, RawImage walkImage, Vector2 walkSize)
        {
            _slide = slide;
            _root = root;
            _stillImage = stillImage;
            _walkRect = walkRect;
            _walkImage = walkImage;
            _walkSize = walkSize;
        }

        /// <summary>그림이 갖춰져 있지 않으면 null(그 경우 소녀 연출은 건너뛴다).</summary>
        public static IntroGirlView Create(IntroSlide slide, RectTransform parent, IntroCutsceneArt art)
        {
            if (art == null || art.girlSilhouette == null || art.girlWalk == null)
            {
                Debug.LogWarning("[IntroGirlView] 소녀 그림이 채워져 있지 않다 — 메뉴 BlueComplex/Cutscene/Rebuild Intro Art를 실행한다.");
                return null;
            }

            var root = RuntimeUi.CreateStretched(parent, "Girl");

            // 정지 실루엣은 창문 오른쪽 끝에서 잘린다.
            var clip = RuntimeUi.CreateStretched(root, "Still Clip");
            var mask = clip.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(0f, 0f, (1f - StillClipRight) * slide.Size.x, 0f);

            var stillWidth = StillHeight * art.girlSilhouette.width / art.girlSilhouette.height * slide.Size.y / slide.Size.x;
            var still = slide.Place(clip, "Still", StillLeft, StillTop, stillWidth, StillHeight);
            var stillImage = still.gameObject.AddComponent<RawImage>();
            stillImage.texture = art.girlSilhouette;
            stillImage.raycastTarget = false;
            stillImage.color = new Color(1f, 1f, 1f, 0f);

            var walkSize = new Vector2(WalkHeight * (FrameWidth / SheetHeight) * slide.Size.y / slide.Size.x, WalkHeight);
            var walk = slide.Place(root, "Walk", 0f, 0f, walkSize.x, walkSize.y);
            var walkImage = walk.gameObject.AddComponent<RawImage>();
            walkImage.texture = art.girlWalk;
            walkImage.raycastTarget = false;
            walk.gameObject.SetActive(false);

            return new IntroGirlView(slide, root, stillImage, walk, walkImage, walkSize);
        }

        public Tween FadeInStill(float seconds) => _stillImage.DOFade(1f, seconds).SetEase(Ease.InOutSine);

        public Tween FadeOutStill(float seconds) => _stillImage.DOFade(0f, seconds).SetEase(Ease.InOutSine);

        /// <summary>걷는 소녀를 켠다/끈다(정지 실루엣은 <see cref="FadeOutStill"/>로 따로 뺀다).</summary>
        public void ShowWalker(bool visible) => _walkRect.gameObject.SetActive(visible);

        /// <summary>
        /// 걷는 소녀의 자세. <paramref name="topLeft"/>는 그림 한 칸 왼쪽 위의 장표 비율 좌표, <paramref name="flipX"/>는 좌우 반전(원본은 오른쪽을 본다),
        /// <paramref name="flipY"/>는 위아래 반전(거꾸로 선 모습), <paramref name="frame"/>은 걷기 프레임 0~7.
        /// </summary>
        public void SetWalkPose(Vector2 topLeft, bool flipX, bool flipY, int frame)
        {
            var slot = _slide.Slot(topLeft.x, topLeft.y, _walkSize.x, _walkSize.y);
            var f = ((frame % FrameCount) + FrameCount) % FrameCount;

            // 몸통 위치를 프레임 사이에 고정한다(반전했으면 화면에서의 방향도 뒤집힌다).
            var shift = FrameShift[f] * slot.size.y / SheetHeight * (flipX ? -1f : 1f);
            _walkRect.anchoredPosition = slot.position + new Vector2(shift, 0f);
            _walkRect.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);
            _walkImage.uvRect = new Rect(FrameLeft[f] / SheetWidth, 0f, FrameWidth / SheetWidth, 1f);
        }

        public void Destroy() => Object.Destroy(_root.gameObject);
    }
}
