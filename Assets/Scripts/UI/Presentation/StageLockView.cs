using System.Collections;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 클리어 연출의 자물쇠 하나. 자물쇠 그림(64px 도트)을 고리(위)와 몸통(아래) 두 조각으로 나눠 그리고,
    /// 열쇠가 꽂혀 돌아가면 고리 조각이 위로 들리며 열린 자물쇠가 된다 — 그림이 한 장뿐이라 "열린" 그림을 따로 두지 않고 같은 그림을 잘라 움직인다.
    /// 표시만 한다 — 언제 열지는 <see cref="StageClearDirector"/>가 정한다.
    ///
    /// 스프라이트는 UiIconCatalog의 <see cref="LockId"/>/<see cref="KeyId"/>(Point 필터)에서 온다. 없으면 <see cref="Available"/>이 false다.
    /// </summary>
    public sealed class StageLockView : MonoBehaviour
    {
        public const string LockId = "stage_lock";
        public const string KeyId = "stage_key";

        /// <summary>자물쇠 그림에서 몸통이 시작하는 줄(위에서부터). 그 위가 고리다 — lock.png 전용 값(고리 다리가 끝나고 몸통이 넓어지는 줄).</summary>
        private const int BodyStartRow = 25;
        private const int ArtSize = 64;

        /// <summary>열쇠 그림에서 끝(이빨 쪽 끝)의 위치 — 위에서부터 줄. 열쇠는 이 끝을 열쇠 구멍에 대고 돌아간다. a_key.png 전용 값.</summary>
        private const int KeyTipRow = 55;

        /// <summary>자물쇠 그림에서 열쇠 구멍의 위치(위에서부터 줄, 가로 중앙).</summary>
        private const int KeyholeRow = 41;

        private static readonly Color Closed = new Color(1f, 1f, 1f, 1f);
        private static readonly Color Opened = new Color(1f, 0.93f, 0.74f, 1f);

        private RectTransform _shackle;   // 위 조각을 담은 틀 — 이걸 움직여 고리를 든다.
        private RectTransform _body;
        private RawImage _shackleImage;
        private RawImage _bodyImage;
        private RectTransform _key;
        private Image _keyImage;
        private Vector2 _shackleRest;    // 고리가 몸통에 맞닿은 자리(틀 기준 anchoredPosition).
        private float _scale;
        private Vector2 _keyhole;
        private Vector2 _keyRest;
        private Vector2 _keyStart;
        private float _keyRestAngle;

        public bool IsOpen { get; private set; }

        /// <summary>자물쇠·열쇠 그림을 카탈로그에서 다 얻을 수 있는가.</summary>
        public static bool Available => UiIcons.GetExact(LockId) != null && UiIcons.GetExact(KeyId) != null;

        /// <param name="pixelScale">도트 한 칸을 캔버스 단위 몇 칸으로 그릴지. 자물쇠가 들어갈 자리 폭에 맞춰 <see cref="StageLockRow"/>가 정한다.</param>
        public static StageLockView Create(Transform parent, string name, float pixelScale)
        {
            var rect = RuntimeUi.CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var view = rect.gameObject.AddComponent<StageLockView>();
            view.Build(pixelScale);
            return view;
        }

        /// <summary>한 칸의 크기(캔버스 단위, 정사각).</summary>
        public float Size => ArtSize * _scale;

        private void Build(float pixelScale)
        {
            _scale = pixelScale;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(Size, Size);

            var lockSprite = UiIcons.GetExact(LockId);
            var keySprite = UiIcons.GetExact(KeyId);

            var bodyFraction = BodyStartRow / (float)ArtSize;
            var shackleHeight = BodyStartRow * _scale;
            var bodyHeight = (ArtSize - BodyStartRow) * _scale;

            // 아래 조각(몸통): 틀은 아래쪽 (64-25)줄, 그림은 그 아래 부분만 보이게 uv를 자른다.
            _body = RuntimeUi.CreateRect(rect, "Body", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, bodyHeight));
            _bodyImage = SliceImage(_body, lockSprite, 0f, 1f - bodyFraction);

            // 위 조각(고리): 틀은 위쪽 25줄.
            _shackle = RuntimeUi.CreateRect(rect, "Shackle", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -shackleHeight), Vector2.zero);
            _shackleRest = _shackle.anchoredPosition; // 피벗이 중앙이라 0이 아니다(-높이/2) — 0으로 되돌리면 고리가 몸통에서 뜬다.
            _shackleImage = SliceImage(_shackle, lockSprite, 1f - bodyFraction, bodyFraction);

            // 열쇠: 끝을 축(pivot)으로 잡아 구멍에 댄 채 그 끝을 중심으로 돌아간다.
            var keyRect = RuntimeUi.CreateRect(rect, "Key", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            keyRect.sizeDelta = new Vector2(Size, Size);
            keyRect.pivot = new Vector2(0.5f, 1f - KeyTipRow / (float)ArtSize);
            _key = keyRect;
            _keyImage = keyRect.gameObject.AddComponent<Image>();
            _keyImage.sprite = keySprite;
            _keyImage.raycastTarget = false;
            _keyImage.color = new Color(1f, 1f, 1f, 0f);

            // 구멍 위치: 그림 정중앙(=틀 중심)에서 아래로 (KeyholeRow - 32)줄.
            _keyhole = new Vector2(0f, -(KeyholeRow - ArtSize * 0.5f) * _scale);

            // 열쇠는 아래 오른쪽에서 위 왼쪽 구멍으로 들어온다 — 끝이 위 왼쪽(45°)을 향하도록 225°.
            _keyRestAngle = 225f;
            _keyRest = _keyhole;
            var travel = Size * 1.0f;
            var toward = new Vector2(Mathf.Sin(_keyRestAngle * Mathf.Deg2Rad), -Mathf.Cos(_keyRestAngle * Mathf.Deg2Rad)); // 끝이 향하는 쪽
            _keyStart = _keyhole - toward * travel;

            ResetNow();
        }

        /// <summary>그림 한 장에서 세로 일부만 보이게 uv를 잘라 그린다. RawImage라 스프라이트의 텍스처(Point)를 그대로 쓴다. 64칸 전체(투명 여백 포함)를 그려야 다른 코드가 쓰는 칸 좌표(몸통 시작 줄 등)와 맞는다.</summary>
        private static RawImage SliceImage(RectTransform target, Sprite sprite, float uvY, float uvHeight)
        {
            var image = target.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Closed;
            if (sprite == null) return image;

            var texture = sprite.texture;
            // textureRect가 아니라 rect — Tight 메시 스프라이트의 textureRect는 불투명 영역으로 잘려 있어(폭 48칸), 그걸 64칸 자리에 펴 그리면 가로로 늘어난다.
            var rect = sprite.rect;
            var u = rect.x / texture.width;
            var v = rect.y / texture.height;
            var w = rect.width / texture.width;
            var h = rect.height / texture.height;

            image.texture = texture;
            image.uvRect = new Rect(u, v + uvY * h, w, uvHeight * h);
            return image;
        }

        /// <summary>닫힌 채로, 열쇠 없이, 원래 색으로.</summary>
        public void ResetNow()
        {
            DOTween.Kill(this);
            IsOpen = false;
            transform.localScale = Vector3.one;
            _shackle.anchoredPosition = _shackleRest;
            _shackle.localRotation = Quaternion.identity;
            _bodyImage.color = Closed;
            _shackleImage.color = Closed;
            _key.anchoredPosition = _keyStart;
            _key.localRotation = Quaternion.Euler(0f, 0f, _keyRestAngle);
            _keyImage.color = new Color(1f, 1f, 1f, 0f);
        }

        /// <summary>열쇠가 날아와 꽂히고 → 돌아가고 → 고리가 열리고 → 열쇠가 사라진다. 다 끝나면 자물쇠만 열린 채 남는다.</summary>
        public IEnumerator PlayUnlock()
        {
            var settings = UiMotion.Settings;

            // 1) 열쇠가 자물쇠 쪽으로 날아 들어간다.
            _key.anchoredPosition = _keyStart;
            var fly = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            fly.Append(_keyImage.DOFade(1f, settings.lockKeyFly * 0.25f));
            fly.Join(_key.DOAnchorPos(_keyRest, settings.lockKeyFly).SetEase(Ease.InQuad));
            yield return fly.WaitForCompletion(true);

            // 열쇠가 구멍에 박히는 순간: 열쇠 소리 + 자물쇠가 살짝 눌린다.
            UiSoundHooks.Play(UiSoundCue.Key);
            yield return transform.DOPunchScale(new Vector3(0.05f, -0.05f, 0f), 0.16f, 6, 0.6f).SetUpdate(true).SetTarget(this).WaitForCompletion(true);

            // 2) 열쇠가 끝을 축으로 돌아간다.
            yield return _key.DOLocalRotate(new Vector3(0f, 0f, _keyRestAngle - 50f), settings.lockKeyTwist)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);

            // 3) 딸깍 — 고리가 들리며 열린다. 자물쇠는 따뜻한 색으로 밝아진다.
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            IsOpen = true;

            var unlatch = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            unlatch.Append(_shackle.DOAnchorPosY(_shackleRest.y + LiftHeight,settings.lockUnlatch).SetEase(Ease.OutBack, 2.2f));
            unlatch.Join(_shackle.DOLocalRotate(new Vector3(0f, 0f, -8f), settings.lockUnlatch).SetEase(Ease.OutQuad));
            unlatch.Join(DOTween.To(() => 0f, value =>
            {
                var tint = Color.Lerp(Closed, Opened, value);
                _bodyImage.color = tint;
                _shackleImage.color = tint;
            }, 1f, settings.lockUnlatch));
            yield return unlatch.WaitForCompletion(true);

            // 4) 열쇠는 제 할 일을 마쳤다 — 열쇠 그림만 서서히 사라진다. 자물쇠는 열린 채로 화면에 남는다.
            yield return _keyImage.DOFade(0f, settings.lockKeyFade).SetEase(Ease.InSine).SetUpdate(true).SetTarget(this).WaitForCompletion(true);
        }

        /// <summary>열린 고리가 올라가는 높이(캔버스 단위) — 도트 6칸.</summary>
        private float LiftHeight => 6f * _scale;

        /// <summary>자물쇠(와 꽂힌 열쇠)를 <paramref name="amount"/>(0 = 그대로, 1 = 검게) 어둡게 한다. 실패 연출에서 자물쇠도 함께 어두워진다.</summary>
        public Tween Darken(float amount, float duration)
        {
            var from = _bodyImage.color;
            var to = Color.Lerp(IsOpen ? Opened : Closed, Color.black, amount);
            var keyFrom = _keyImage.color;
            var keyTo = new Color(Mathf.Lerp(1f, 0f, amount), Mathf.Lerp(1f, 0f, amount), Mathf.Lerp(1f, 0f, amount), keyFrom.a);

            return DOTween.To(() => 0f, value =>
            {
                var tint = Color.Lerp(from, to, value);
                _bodyImage.color = tint;
                _shackleImage.color = tint;
                _keyImage.color = Color.Lerp(keyFrom, keyTo, value);
            }, 1f, duration).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
        }
    }
}
