using BlueComplex.UI.Layout;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 오프닝의 소녀. 주황 창문 앞에 고개를 숙이는 8프레임(<see cref="IntroCutsceneArt.girlNod"/>)이 한 번 재생되며 나타났다가, 걷기 8프레임 시트(<see cref="IntroCutsceneArt.girlWalk"/>)로
    /// 화면을 걸어 가고, 사라진 뒤 거꾸로(상하·좌우 반전) 화면 오른쪽 바깥에서 걸어 들어와 선다. 위치는 PPT 장표 비율(가로·세로 0~1, 세로는 위에서부터)의 그림 왼쪽 위 모서리다 —
    /// 그림 한 칸(투명 여백 포함)의 왼쪽 위. 움직임은 <see cref="IntroSequencePlayer"/>가 정하고 이 조각은 자세만 받는다.
    /// </summary>
    internal sealed class IntroGirlView
    {
        // 고개 숙임 자리(PPT 7번 장표): 왼쪽 위 (0.435, 0.128), 높이 0.707. 창문 오른쪽(0.687)을 넘은 부분은 PPT에서 배경색 사각형이 가려 창문 안에서만 보인다.
        private const float StillLeft = 0.435f;
        private const float StillTop = 0.128f;
        private const float StillHeight = 0.707f;
        private const float StillClipRight = 0.687f;

        /// <summary>걷는 소녀 한 칸의 높이(화면 높이 비율, PPT 8~11번 장표).</summary>
        public const float WalkHeight = 0.607f;

        // 고개 숙임 시트(girl_nod_8frames.png, 4000×450, 알파 채널)의 8프레임. 알파>10 기준으로 자동 검출한 왼쪽·오른쪽 끝 픽셀(포함)과, 프레임 안에서 잰 어깨 중심의 x(왼쪽 끝에서부터),
        // 그리고 시트 전체(위에서부터) 기준 몸통 아랫단의 y. 걷기 시트와 달리 프레임마다 아랫단 y가 다르다(고개를 숙이며 몸이 앞으로 쏠려 옷단이 살짝 든다) — 그래서 세로도
        // 가로(어깨 중심)와 같은 방식으로 앵커를 잡는다: 각 프레임의 아랫단 행을 화면의 같은 자리에 고정해(피벗) 바닥이 흔들리지 않게 한다.
        private const int NodFrameCount = 8;
        private const float NodSheetWidth = 4000f;
        private const float NodSheetHeight = 450f;
        private static readonly float[] NodFrameLeft = { 37f, 375f, 740f, 1104f, 1509f, 1907f, 2316f, 2720f };
        private static readonly float[] NodFrameRight = { 311f, 651f, 1019f, 1387f, 1796f, 2203f, 2619f, 3029f };
        private static readonly float[] NodShoulderX = { 167.0f, 168.5f, 171.5f, 175.5f, 180.0f, 187.5f, 194.0f, 199.5f };
        private static readonly float[] NodBottomY = { 429f, 426f, 423f, 423f, 419f, 417f, 415f, 406f };

        // 이전 정지 실루엣(girl_silhouette.png, 1295×1214)의 원본 가로/세로와, 알파>10 기준 목이 가장 좁은 행의 가로 중심(이미지 폭 대비) — 그 실루엣이 화면에 있던 자리를
        // 그대로 계산해 새 시트의 어깨 중심 앵커를 거기 맞춘다.
        private const float StillImageAspect = 1295f / 1214f;
        private const float StillShoulderXFraction = 0.5486f;

        // 시트(girl_walk_cycle_v2.png, 5000×733, 알파 채널)의 8프레임. 알파>10 기준으로 자동 검출한 왼쪽·오른쪽 끝 픽셀(포함)과, 프레임 안에서 잰 상체 중심의 x(왼쪽 끝에서부터).
        // 세로는 8프레임 모두 발끝이 y=621(위에서부터)로 같아 시트 전체 높이를 그대로 쓰면 바닥선이 맞는다.
        // 가로는 프레임 전체 상자의 중심이 아니라 상체 중심을 앵커로 삼는다 — 다리가 벌어지며 상자 폭이 달라져도 몸통은 한 자리에 있고, 상체 중심 자체의 자연스러운 좌우 진동만 남는다.
        private const int FrameCount = 8;
        private const float SheetWidth = 5000f;
        private const float SheetHeight = 733f;
        private static readonly float[] FrameLeft = { 189f, 502f, 786f, 1060f, 1391f, 1704f, 1988f, 2262f };
        private static readonly float[] FrameRight = { 423f, 706f, 980f, 1310f, 1625f, 1908f, 2182f, 2512f };
        private static readonly float[] FrameBodyX = { 142.5f, 110.5f, 115f, 115f, 142.5f, 110.5f, 115f, 115f };

        // 한 칸(자리)의 가로 폭 기준: 장표 좌표 topLeft가 가리키는 칸의 폭. 앵커(상체 중심)는 이 칸의 가로 중심에 놓인다 — 지난 시트(균등 8칸 2146/8)와 같은 크기다.
        private const float SlotWidthPx = 2146f / FrameCount;

        private readonly IntroSlide _slide;
        private readonly RectTransform _root;
        private readonly RawImage _nodImage;
        private readonly RectTransform _nodRect;
        private readonly Vector2 _nodAnchor; // 고개 숙임 프레임의 피벗이 고정되는 자리(장표 중심 기준 로컬 좌표) — 프레임이 바뀌어도 이 점은 그대로다.
        private readonly float _nodPixelScale; // 시트 한 픽셀이 화면에서 차지하는 크기.
        private readonly RectTransform _walkRect;
        private readonly RawImage _walkImage;
        private readonly Vector2 _walkSize; // 걷는 한 칸의 크기(장표 비율).

        private IntroGirlView(IntroSlide slide, RectTransform root, RawImage nodImage, RectTransform nodRect, Vector2 nodAnchor, float nodPixelScale,
            RectTransform walkRect, RawImage walkImage, Vector2 walkSize)
        {
            _slide = slide;
            _root = root;
            _nodImage = nodImage;
            _nodRect = nodRect;
            _nodAnchor = nodAnchor;
            _nodPixelScale = nodPixelScale;
            _walkRect = walkRect;
            _walkImage = walkImage;
            _walkSize = walkSize;
        }

        /// <summary>그림이 갖춰져 있지 않으면 null(그 경우 소녀 연출은 건너뛴다).</summary>
        public static IntroGirlView Create(IntroSlide slide, RectTransform parent, IntroCutsceneArt art)
        {
            if (art == null || art.girlNod == null || art.girlWalk == null)
            {
                Debug.LogWarning("[IntroGirlView] 소녀 그림이 채워져 있지 않다 — 메뉴 BlueComplex/Cutscene/Rebuild Intro Art를 실행한다.");
                return null;
            }

            var root = RuntimeUi.CreateStretched(parent, "Girl");

            // 고개 숙임·정지 자세는 창문 오른쪽 끝에서 잘린다.
            var clip = RuntimeUi.CreateStretched(root, "Still Clip");
            var mask = clip.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(0f, 0f, (1f - StillClipRight) * slide.Size.x, 0f);

            // 고개 숙임 프레임: 피벗(어깨 중심 x, 아랫단 y)이 놓일 고정 자리는 이전 정지 실루엣이 있던 자리(왼쪽 위 StillLeft·StillTop, 높이 StillHeight로 놓였을 때
            // 그 실루엣의 목 중심 x와 바닥 y)로 한 번만 잡는다 — 이후 프레임은 피벗만 바뀌고 이 자리는 그대로다.
            var nodPixelScale = StillHeight * slide.Size.y / NodSheetHeight;
            var stillWidth = StillHeight * StillImageAspect * slide.Size.y / slide.Size.x;
            var nodAnchor = slide.ToLocal(StillLeft + StillShoulderXFraction * stillWidth, StillTop + StillHeight);

            var nodRect = RuntimeUi.CreateRect(clip, "Nod", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var nodImage = nodRect.gameObject.AddComponent<RawImage>();
            nodImage.texture = art.girlNod;
            nodImage.raycastTarget = false;
            nodImage.color = new Color(1f, 1f, 1f, 0f);

            var walkSize = new Vector2(WalkHeight * (SlotWidthPx / SheetHeight) * slide.Size.y / slide.Size.x, WalkHeight);
            var walk = slide.Place(root, "Walk", 0f, 0f, walkSize.x, walkSize.y);
            var walkImage = walk.gameObject.AddComponent<RawImage>();
            walkImage.texture = art.girlWalk;
            walkImage.raycastTarget = false;
            walk.gameObject.SetActive(false);

            var view = new IntroGirlView(slide, root, nodImage, nodRect, nodAnchor, nodPixelScale, walk, walkImage, walkSize);
            view.SetNodFrame(0);
            return view;
        }

        public Tween FadeInNod(float seconds) => _nodImage.DOFade(1f, seconds).SetEase(Ease.InOutSine);

        /// <summary>고개 숙임 그림을 즉시 지운다(컷 전환).</summary>
        public void HideNod() => _nodImage.color = new Color(1f, 1f, 1f, 0f);

        /// <summary>고개 숙임 프레임(0~7)을 놓는다. 어깨 중심(가로)·아랫단(세로)을 피벗으로 삼아 <see cref="_nodAnchor"/>에 고정하므로,
        /// 프레임마다 폭·아랫단 위치가 달라져도 몸이 앞으로 자연스럽게 쏠리는 이동만 남고 떨리지 않는다.</summary>
        public void SetNodFrame(int frame)
        {
            var f = ((frame % NodFrameCount) + NodFrameCount) % NodFrameCount;
            var framePx = NodFrameRight[f] - NodFrameLeft[f] + 1f;

            _nodRect.sizeDelta = new Vector2(framePx * _nodPixelScale, NodSheetHeight * _nodPixelScale);
            _nodRect.pivot = new Vector2(NodShoulderX[f] / framePx, 1f - NodBottomY[f] / NodSheetHeight);
            _nodRect.anchoredPosition = _nodAnchor;
            _nodImage.uvRect = new Rect(NodFrameLeft[f] / NodSheetWidth, 0f, framePx / NodSheetWidth, 1f);
        }

        /// <summary>걷는 소녀를 켠다/끈다(고개 숙임 그림은 <see cref="HideNod"/>로 따로 뺀다).</summary>
        public void ShowWalker(bool visible) => _walkRect.gameObject.SetActive(visible);

        /// <summary>
        /// 걷는 소녀의 자세. <paramref name="topLeft"/>는 그림 한 칸 왼쪽 위의 장표 비율 좌표, <paramref name="flipX"/>는 좌우 반전(원본은 오른쪽을 본다),
        /// <paramref name="flipY"/>는 위아래 반전(거꾸로 선 모습), <paramref name="frame"/>은 걷기 프레임 0~7.
        /// </summary>
        public void SetWalkPose(Vector2 topLeft, bool flipX, bool flipY, int frame)
        {
            var slot = _slide.Slot(topLeft.x, topLeft.y, _walkSize.x, _walkSize.y);
            var f = ((frame % FrameCount) + FrameCount) % FrameCount;

            // 프레임 한 장을 알파 경계 그대로 잘라(폭이 프레임마다 다르다) 상체 중심을 피벗으로 삼아 칸의 가로 중심에 놓는다.
            // 피벗이 상체 중심이라 반전(localScale -1)해도 몸통은 제자리에 남고 그림만 뒤집힌다.
            var framePx = FrameRight[f] - FrameLeft[f] + 1f;
            var k = slot.size.y / SheetHeight;
            _walkRect.sizeDelta = new Vector2(framePx * k, slot.size.y);
            _walkRect.pivot = new Vector2(FrameBodyX[f] / framePx, 0.5f);
            _walkRect.anchoredPosition = slot.position;
            _walkRect.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);
            _walkImage.uvRect = new Rect(FrameLeft[f] / SheetWidth, 0f, framePx / SheetWidth, 1f);
        }

        public void Destroy() => Object.Destroy(_root.gameObject);
    }
}
