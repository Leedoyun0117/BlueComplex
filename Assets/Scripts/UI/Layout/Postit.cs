using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 재사용 가능한 포스트잇: 연노랑 종이 + 상단 중앙 압정 + 우하단 드롭 섀도, 평상시 우하단 모서리가 살짝 말려 있다.
    /// 어떤 RectTransform 위에든 붙일 수 있다 — 붙이면 자기 아래에 종이 겹(<see cref="PostitGraphic"/>)을 짓고
    /// <see cref="Content"/>가 글씨·칸을 얹는 자리다. 이미 자식이 있으면(프리팹으로 굽힌 목록) 그 자식을 <see cref="Content"/>로 옮겨 온다(레이아웃 그룹 포함).
    ///
    /// <see cref="Peel"/>은 우하단 모서리부터 말려 올라가 압정이 빠지고 종이가 튀었다가 떨어지는 연출이고, <see cref="Stick"/>은 그 반대로
    /// 위에서 내려와 눌리고 압정이 꽂힌다. 이 컴포넌트는 코어 이벤트를 구독하지 않는다 — 언제 떼고 붙일지는 Presenter가 쥔다.
    /// 시간값은 UiMotionSettings에서 온다. 소리는 <see cref="UiSoundHooks"/>로 알리기만 한다.
    ///
    /// 구조(위가 뒤): Sheet(움직이는 판) ▸ 드롭 섀도 · 종이(Mask) ▸ [Content · 말림 그림자] · 말린 뒷면 · Overlay · 압정.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Postit : MonoBehaviour
    {
        [Tooltip("붙어 있을 때의 기울기(도). 루트에 이미 회전이 있으면(프리팹) 그 값을 이어받는다.")]
        [SerializeField] private float _restTilt = 3f;

        [Tooltip("압정 머리 중심이 종이 윗변에서 내려온 거리(픽셀).")]
        [SerializeField] private float _pinInset = 15f;

        private static readonly Vector2 PinPopOffset = new Vector2(24f, 38f);

        private readonly PostitCurl _curl = new PostitCurl();

        private RectTransform _sheet;
        private RectTransform _content;
        private RectTransform _overlay;
        private RectTransform _pin;
        private CanvasGroup _sheetGroup;
        private CanvasGroup _pinGroup;
        private CanvasGroup _pinShadowGroup;
        private PostitGraphic[] _graphics;

        private Vector2 _pinRest;
        private bool _built;
        private Sequence _motion;
        private Sequence _pinMotion;

        /// <summary>글씨·칸을 얹는 자리. 종이 모양(접힘선 앞)으로 잘려서 말려 올라가는 만큼 함께 사라진다.</summary>
        public RectTransform Content
        {
            get
            {
                EnsureBuilt();
                return _content;
            }
        }

        /// <summary>종이와 말린 뒷면 위, 압정 아래의 자리 — 테두리 같은 잘리면 안 되는 것을 얹는다.</summary>
        public RectTransform Overlay
        {
            get
            {
                EnsureBuilt();
                return _overlay;
            }
        }

        /// <summary>움직이는 판(기울기·위치·크기가 여기에 걸린다). 루트는 고정된 앵커 틀로 남는다.</summary>
        public RectTransform Sheet
        {
            get
            {
                EnsureBuilt();
                return _sheet;
            }
        }

        /// <summary>붙어 있는가. 떼어지기 시작하면 false, 다시 붙는 모션이 끝나면 true — 붙어 있을 때만 클릭을 받는다.</summary>
        public bool IsAttached { get; private set; } = true;

        public float RestTilt => _restTilt;

        /// <summary>루트에 포스트잇을 붙인다(이미 있으면 그대로). 이미 있는 자식은 <see cref="Content"/>로 옮겨진다.</summary>
        public static Postit Attach(GameObject target)
        {
            var existing = target.GetComponent<Postit>();
            return existing != null ? existing : target.AddComponent<Postit>();
        }

        private void Awake() => EnsureBuilt();

        private void OnDisable()
        {
            // 연출 도중 꺼지면 반쯤 떼어진 채 남지 않게 붙은 상태로 되돌린다(다시 켜졌을 때 화면 밖이면 안 된다).
            if (_built && _sheet != null) SnapAttached();
        }

        /// <summary>기울기를 바꾼다. 붙어 있으면 바로 반영된다.</summary>
        public void SetRestTilt(float degrees)
        {
            _restTilt = degrees;
            if (_built && IsAttached && _motion == null) _sheet.localRotation = Quaternion.Euler(0f, 0f, _restTilt);
        }

        /// <summary><see cref="Content"/> 아래의 모든 글자를 손글씨로 바꾼다. 내용을 다 넣은 뒤 한 번 부른다(이미 손글씨인 글자는 건너뛴다).</summary>
        public void ApplyHandFont()
        {
            EnsureBuilt();
            var font = PostitStyle.HandFont;
            if (font == null) return;

            foreach (var text in _content.GetComponentsInChildren<TMP_Text>(true))
                if (text.font != font) PostitStyle.ApplyHand(text);
        }

        // ------------------------------------------------------------------
        // 모션
        // ------------------------------------------------------------------

        /// <summary>
        /// 떼어낸다: 우하단 모서리가 먼저 말려 올라가며 압정 쪽으로 접혀 들어가고 → 일정 이상 들리면 압정이 빠지며 종이가 살짝 튀었다가 →
        /// 화면 밖으로 떨어진다. 끝나면 판은 숨겨진 채로 남는다(<see cref="Stick"/>이 다시 보이게 한다).
        /// </summary>
        public Sequence Peel()
        {
            EnsureBuilt();
            KillMotion();

            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            if (!IsAttached) return sequence;

            IsAttached = false;
            _sheetGroup.blocksRaycasts = false;
            UiSoundHooks.Play(UiSoundCue.PostitPeel);

            var settings = UiMotion.Settings;
            var total = Mathf.Max(0.12f, settings.postitPeel);
            var lift = total * settings.postitPinPopAt;
            var fall = total - lift;
            var hop = fall * 0.28f;
            var drop = fall - hop;
            var distance = FallDistance();

            // 1) 모서리가 들린다 — 처음엔 붙어 있어 더디고 점점 빨라진다.
            sequence.Append(TweenCurl(0.5f, lift, Ease.InQuad));
            sequence.Join(_sheet.DOLocalRotate(new Vector3(0f, 0f, _restTilt + 2.5f), lift).SetEase(Ease.InQuad));
            sequence.Join(_sheet.DOAnchorPosY(4f, lift).SetEase(Ease.InQuad));

            // 2) 압정이 빠진다.
            sequence.AppendCallback(PopPin);

            // 3) 종이가 살짝 튀었다가
            sequence.Append(_sheet.DOAnchorPosY(20f, hop).SetEase(Ease.OutQuad));
            sequence.Join(TweenCurl(0.62f, hop, Ease.OutQuad));
            sequence.Join(TweenShadowLift(0.6f, hop));

            // 4) 화면 밖으로 떨어진다 — 말림은 끝까지 가서 두루마리처럼 말려 올라간다.
            sequence.Append(_sheet.DOAnchorPos(new Vector2(-10f, -distance), drop).SetEase(Ease.InQuad));
            sequence.Join(_sheet.DOLocalRotate(new Vector3(0f, 0f, _restTilt - 14f), drop).SetEase(Ease.InQuad));
            sequence.Join(TweenCurl(1f, drop, Ease.Linear));
            sequence.Join(TweenShadowLift(1f, drop));
            sequence.Insert(lift + hop + drop * 0.7f, _sheetGroup.DOFade(0f, drop * 0.3f));

            sequence.OnComplete(() => _sheetGroup.alpha = 0f);
            _motion = sequence;
            return sequence;
        }

        /// <summary>
        /// 새 포스트잇이 붙는다: 약간 위에서 내려와 자리에 눌리고(살짝 눌렸다 펴지는 스케일 바운스) → 압정이 마지막에 작게 튀며 꽂히고 →
        /// 우하단 모서리는 기본 말림 상태로 돌아온다. 끝나면 <see cref="IsAttached"/>가 true다.
        /// </summary>
        public Sequence Stick()
        {
            EnsureBuilt();
            KillMotion();

            var settings = UiMotion.Settings;
            var total = Mathf.Max(0.2f, settings.postitStick);
            var fall = total * 0.5f;
            var press = total * 0.08f;
            var release = total * 0.22f;
            var pinStart = fall + press * 0.5f;
            var pinDrop = total * 0.16f;
            var pinBounce = total * 0.2f;

            // 출발 자세: 위에 뜬 채 모서리가 조금 들려 있고 그림자는 멀다. 압정은 아직 없다.
            _sheetGroup.blocksRaycasts = false;
            _sheetGroup.alpha = 0f;
            _sheet.anchoredPosition = new Vector2(0f, 72f);
            _sheet.localRotation = Quaternion.Euler(0f, 0f, _restTilt + 5f);
            _sheet.localScale = Vector3.one;
            _curl.Set(0.14f, 1f);
            HidePin();

            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);

            // 위에서 내려온다(살짝 자리 아래로 눌려 들어갔다가 돌아온다).
            sequence.Append(_sheetGroup.DOFade(1f, fall * 0.35f));
            sequence.Join(_sheet.DOAnchorPosY(-3f, fall).SetEase(Ease.InQuad));
            sequence.Join(_sheet.DOLocalRotate(new Vector3(0f, 0f, _restTilt), fall).SetEase(Ease.InQuad));
            sequence.Join(TweenShadowLift(0.15f, fall, Ease.InQuad));

            // 닿는 순간 눌린다.
            sequence.AppendCallback(() => UiSoundHooks.Play(UiSoundCue.Paper));
            sequence.Append(_sheet.DOScale(new Vector3(1.03f, 0.95f, 1f), press).SetEase(Ease.OutQuad));
            sequence.Join(TweenShadowLift(0f, press, Ease.OutQuad));

            // 펴지며 자리를 잡고, 들려 있던 모서리는 기본 말림으로 돌아온다.
            sequence.Append(_sheet.DOScale(Vector3.one, release).SetEase(Ease.OutBack, 2.4f));
            sequence.Join(_sheet.DOAnchorPosY(0f, release).SetEase(Ease.OutQuad));
            sequence.Join(TweenCurl(0f, release, Ease.OutQuad));

            // 압정이 마지막에 꽂힌다.
            sequence.Insert(pinStart, TweenPinDrop(pinDrop, pinBounce));

            sequence.OnComplete(() =>
            {
                _motion = null;
                SnapAttached();
            });
            _motion = sequence;
            return sequence;
        }

        /// <summary>모든 연출을 멈추고 붙어 있는 자세로 되돌린다(평상시 말림, 압정 꽂힘, 클릭 가능).</summary>
        public void SnapAttached()
        {
            if (!_built) return;

            KillMotion();
            _curl.Set(0f, 0f);
            _sheet.anchoredPosition = Vector2.zero;
            _sheet.localRotation = Quaternion.Euler(0f, 0f, _restTilt);
            _sheet.localScale = Vector3.one;
            _sheetGroup.alpha = 1f;
            _sheetGroup.blocksRaycasts = true;
            ShowPin();
            IsAttached = true;
        }

        private void KillMotion()
        {
            _motion?.Kill();
            _motion = null;
            _pinMotion?.Kill();
            _pinMotion = null;
        }

        private Tween TweenCurl(float target, float duration, Ease ease) =>
            DOTween.To(() => _curl.Amount, value => _curl.Amount = value, target, duration).SetEase(ease).SetUpdate(true);

        private Tween TweenShadowLift(float target, float duration, Ease ease = Ease.Linear) =>
            DOTween.To(() => _curl.ShadowLift, value => _curl.ShadowLift = value, target, duration).SetEase(ease).SetUpdate(true);

        // ------------------------------------------------------------------
        // 압정
        // ------------------------------------------------------------------

        /// <summary>압정이 빠져 앞으로 튀어 나가며 사라진다. 종이 위의 압정 그림자는 바로 걷힌다.</summary>
        private void PopPin()
        {
            _pinMotion?.Kill();
            _pinMotion = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(_pin.DOAnchorPos(_pinRest + PinPopOffset, 0.2f).SetEase(Ease.OutQuad))
                .Join(_pin.DOScale(1.9f, 0.2f).SetEase(Ease.OutQuad))
                .Join(_pin.DOLocalRotate(new Vector3(0f, 0f, -35f), 0.2f))
                .Join(_pinGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad))
                .Join(_pinShadowGroup.DOFade(0f, 0.08f));
        }

        /// <summary>압정이 위에서 내려꽂힌다: 크게 떠 있다가 종이에 닿고(소리) 작게 튀었다 가라앉는다.</summary>
        private Sequence TweenPinDrop(float drop, float bounce)
        {
            var sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Append(_pinGroup.DOFade(1f, drop * 0.3f));
            sequence.Join(_pin.DOScale(1f, drop).SetEase(Ease.InQuad));
            sequence.Join(_pin.DOAnchorPos(_pinRest, drop).SetEase(Ease.InQuad));
            sequence.Join(_pin.DOLocalRotate(Vector3.zero, drop).SetEase(Ease.InQuad));
            sequence.Join(_pinShadowGroup.DOFade(1f, drop).SetEase(Ease.InQuad));

            sequence.AppendCallback(() => UiSoundHooks.Play(UiSoundCue.Pin));
            sequence.Append(_pin.DOScale(1.3f, bounce * 0.4f).SetEase(Ease.OutQuad));
            sequence.Append(_pin.DOScale(1f, bounce * 0.6f).SetEase(Ease.InQuad));
            return sequence;
        }

        private void HidePin()
        {
            _pin.anchoredPosition = _pinRest + new Vector2(0f, 26f);
            _pin.localScale = Vector3.one * 1.9f;
            _pin.localRotation = Quaternion.identity;
            _pinGroup.alpha = 0f;
            _pinShadowGroup.alpha = 0f;
        }

        private void ShowPin()
        {
            _pin.anchoredPosition = _pinRest;
            _pin.localScale = Vector3.one;
            _pin.localRotation = Quaternion.identity;
            _pinGroup.alpha = 1f;
            _pinShadowGroup.alpha = 1f;
        }

        /// <summary>판이 캔버스 아래 끝을 완전히 벗어나는 데 필요한 낙하 거리(판 부모의 국소 단위).</summary>
        private float FallDistance()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return 900f;

            var parent = (RectTransform)_sheet.parent;
            var canvasCorners = new Vector3[4];
            ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(canvasCorners);
            var sheetCorners = new Vector3[4];
            _sheet.GetWorldCorners(sheetCorners);

            var sheetBottom = float.MaxValue;
            foreach (var corner in sheetCorners) sheetBottom = Mathf.Min(sheetBottom, corner.y);

            var canvasBottom = canvasCorners[0].y;
            var localDrop = parent.InverseTransformPoint(new Vector3(0f, sheetBottom, 0f)).y
                            - parent.InverseTransformPoint(new Vector3(0f, canvasBottom, 0f)).y;
            return Mathf.Max(200f, localDrop + 120f);
        }

        // ------------------------------------------------------------------
        // 구조
        // ------------------------------------------------------------------

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var root = (RectTransform)transform;

            // 루트가 이미 종이 모양(배경 Image·그림자)을 갖고 있었다면 끈다 — 이제 겹이 종이를 그린다. 기울기는 판으로 옮긴다.
            var background = GetComponent<Image>();
            if (background != null) background.enabled = false;
            foreach (var effect in GetComponents<Shadow>()) effect.enabled = false;

            var rootTilt = Mathf.DeltaAngle(0f, root.localEulerAngles.z);
            if (Mathf.Abs(rootTilt) > 0.01f)
            {
                _restTilt = rootTilt;
                root.localRotation = Quaternion.identity;
            }

            _sheet = RuntimeUi.CreateStretched(root, "Sheet");
            _sheet.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _sheetGroup = _sheet.gameObject.AddComponent<CanvasGroup>();
            _sheet.localRotation = Quaternion.Euler(0f, 0f, _restTilt);

            var dropShadow = CreateLayer("Drop Shadow", _sheet, PostitGraphic.LayerKind.DropShadow);

            var paper = CreateLayer("Paper", _sheet, PostitGraphic.LayerKind.Paper);
            paper.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            AddEdge(paper.gameObject, new Color(PostitStyle.Edge.r, PostitStyle.Edge.g, PostitStyle.Edge.b, 0.95f));

            _content = RuntimeUi.CreateStretched(paper.transform, "Content");
            var curlShadow = CreateLayer("Curl Shadow", paper.transform, PostitGraphic.LayerKind.CurlShadow);

            var flap = CreateLayer("Flap", _sheet, PostitGraphic.LayerKind.Flap);
            AddEdge(flap.gameObject, new Color(0.58f, 0.46f, 0.12f, 0.55f));

            _overlay = RuntimeUi.CreateStretched(_sheet, "Overlay");
            BuildPin();

            _graphics = new[] { dropShadow, paper, curlShadow, flap };
            _curl.Changed += MarkGraphicsDirty;

            AdoptChildren(root);
        }

        private PostitGraphic CreateLayer(string layerName, Transform parent, PostitGraphic.LayerKind kind)
        {
            var rect = RuntimeUi.CreateStretched(parent, layerName);
            var graphic = rect.gameObject.AddComponent<PostitGraphic>();
            graphic.Init(_curl, kind);
            return graphic;
        }

        private static void AddEdge(GameObject target, Color color)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = false;
        }

        private void MarkGraphicsDirty()
        {
            foreach (var graphic in _graphics) graphic.Dirty();
        }

        /// <summary>붙이기 전부터 있던 자식(예: 프리팹의 제목·행)을 <see cref="Content"/>로 옮기고, 그것들을 정렬하던 레이아웃 그룹도 Content가 이어받는다.</summary>
        private void AdoptChildren(RectTransform root)
        {
            var movers = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in root)
                if (child != _sheet) movers.Add(child);

            if (movers.Count == 0) return;

            var vertical = GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                var copy = _content.gameObject.AddComponent<VerticalLayoutGroup>();
                copy.padding = new RectOffset(vertical.padding.left, vertical.padding.right, vertical.padding.top, vertical.padding.bottom);
                copy.spacing = vertical.spacing;
                copy.childAlignment = vertical.childAlignment;
                copy.childControlWidth = vertical.childControlWidth;
                copy.childControlHeight = vertical.childControlHeight;
                copy.childForceExpandWidth = vertical.childForceExpandWidth;
                copy.childForceExpandHeight = vertical.childForceExpandHeight;
                copy.childScaleWidth = vertical.childScaleWidth;
                copy.childScaleHeight = vertical.childScaleHeight;
                copy.reverseArrangement = vertical.reverseArrangement;

                // 끄면 OnDisable이 자식에게 걸어 둔 구동 표시를 걷는다 — 이 프레임에 판(Sheet)의 크기를 건드리지 않게 먼저 끈다.
                vertical.enabled = false;
                Destroy(vertical);
            }

            foreach (var child in movers) child.SetParent(_content, false);
            ApplyHandFont();
        }

        private void BuildPin()
        {
            var shadowRect = RuntimeUi.CreateRect(_sheet, "Pin Shadow", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            _pinShadowGroup = shadowRect.gameObject.AddComponent<CanvasGroup>();
            shadowRect.sizeDelta = new Vector2(21f, 15f);
            shadowRect.anchoredPosition = new Vector2(5f, -_pinInset - 6f);
            RuntimeUi.CreateImage(shadowRect, "Blob", new Color(0.05f, 0.04f, 0.02f, 0.34f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, RuntimeUi.Circle);

            _pin = RuntimeUi.CreateRect(_sheet, "Pin", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            _pin.sizeDelta = new Vector2(24f, 24f);
            _pinRest = new Vector2(0f, -_pinInset);
            _pin.anchoredPosition = _pinRest;
            _pinGroup = _pin.gameObject.AddComponent<CanvasGroup>();

            RuntimeUi.CreateImage(_pin, "Rim", PostitStyle.PinRim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, RuntimeUi.Circle);
            RuntimeUi.CreateImage(_pin, "Head", PostitStyle.PinHead, Vector2.zero, Vector2.one, new Vector2(2f, 2.5f), new Vector2(-2.5f, -2f),
                RuntimeUi.Circle);
            RuntimeUi.CreateImage(_pin, "Highlight", new Color(1f, 1f, 1f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-6f, 1f), new Vector2(-1f, 6f), RuntimeUi.Circle);
        }
    }
}
