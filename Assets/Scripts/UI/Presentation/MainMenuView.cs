using System;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 메인 화면(이시온 프리팹, Resources/UI/MainMenu.prefab). 오프닝 19단계에서 <see cref="IntroSequencePlayer"/>가 띄운다.
    /// 배경·제목·버튼 셋과 마우스 패럴랙스(<c>LSO_MouseParallax</c>)는 프리팹 안에 다 들어있다 — 이 컴포넌트는 버튼 눌림을 콜백으로
    /// 잇고, 시작을 누른 뒤 두 번 눌리지 않게 잠그고, 안내 토스트를 띄우는 것만 한다(토스트 자리는 프리팹에 없어 여기서 덧붙인다).
    /// </summary>
    internal sealed class MainMenuView : MonoBehaviour
    {
        private const string ResourcePath = "UI/MainMenu";
        private const string ResetLogoResourcePath = "UI/zentonLogo";
        private const float ResetButtonSize = 56f;
        private const float ResetButtonMargin = 24f;

        private CanvasGroup _group;
        private TMP_Text _toast;
        private Tween _toastTween;
        private TMP_FontAsset _font;
        private Action _onReset;
        private RectTransform _resetConfirmRoot;

        public static MainMenuView Create(Transform parent, TMP_FontAsset font, Action onStart, Action onNotes, Action onQuit, Action onReset)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException($"메인 화면 프리팹을 찾을 수 없다: Resources/{ResourcePath}.prefab");

            var instance = Instantiate(prefab, parent, false);
            instance.name = prefab.name;

            var view = instance.AddComponent<MainMenuView>();
            view._font = font;
            view._group = instance.AddComponent<CanvasGroup>();
            view.Wire(onStart, onNotes, onQuit);
            view.BuildToast(font);
            view.BuildResetButton(onReset);
            return view;
        }

        /// <summary>버튼을 잠그거나 푼다(시작을 누른 뒤 두 번 눌리지 않게).</summary>
        public void SetInteractable(bool interactable)
        {
            _group.interactable = interactable;
            _group.blocksRaycasts = interactable;
        }

        /// <summary>버튼 아래에 짧은 안내 문구를 띄웠다가 지운다.</summary>
        public void ShowToast(string message, float seconds = 1.6f)
        {
            _toastTween?.Kill();
            _toast.text = message;
            _toast.alpha = 1f;
            _toastTween = DOTween.To(() => _toast.alpha, value => _toast.alpha = value, 0f, 0.4f)
                .SetDelay(seconds).SetUpdate(true).SetTarget(this);
        }

        private void OnDestroy() => DOTween.Kill(this);

        private void Wire(Action onStart, Action onNotes, Action onQuit)
        {
            BindButton("Buttons/StartBtn", onStart);
            BindButton("Buttons/NoteBtn", onNotes);
            BindButton("Buttons/QuitBtn", onQuit);
        }

        private void BindButton(string path, Action onClick)
        {
            var button = transform.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[MainMenuView] 버튼 '{path}'를 프리팹에서 찾지 못했다.", this);
                return;
            }

            button.onClick.AddListener(() =>
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                onClick?.Invoke();
            });
        }

        /// <summary>Buttons 판(오른쪽, 앵커 0.5/0.5·자리 697,30·크기 250×450) 바로 아래에 같은 앵커 방식으로 놓는다.</summary>
        private void BuildToast(TMP_FontAsset font)
        {
            var toastRect = RuntimeUi.CreateRect((RectTransform)transform, "Toast", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.sizeDelta = new Vector2(250f, 50f);
            toastRect.anchoredPosition = new Vector2(697f, -235f);

            _toast = toastRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) _toast.font = font;
            _toast.fontSize = 22f;
            _toast.color = MockupStyle.Paper;
            _toast.alignment = TextAlignmentOptions.Center;
            _toast.textWrappingMode = TextWrappingModes.Normal;
            _toast.raycastTarget = false;
            _toast.alpha = 0f;
        }

        /// <summary>화면 오른쪽 아래 구석의 "세이브 데이터 초기화" 버튼 — zenton 로고를 그대로 버튼 얼굴로 쓴다.
        /// 프리팹에는 자리가 없어 토스트처럼 여기서 덧붙인다. 확인 팝업 없이 누르면 되돌릴 수 없어 확인을 한 번 거친다.</summary>
        private void BuildResetButton(Action onReset)
        {
            _onReset = onReset;

            var rect = RuntimeUi.CreateRect((RectTransform)transform, "ResetSaveBtn", new Vector2(1f, 0f), new Vector2(1f, 0f),
                Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(ResetButtonSize, ResetButtonSize);
            rect.anchoredPosition = new Vector2(-ResetButtonMargin, ResetButtonMargin);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(ResetLogoResourcePath);
            image.color = Color.white;
            image.raycastTarget = true;
            if (image.sprite == null)
                Debug.LogWarning($"[MainMenuView] 초기화 버튼 로고를 찾지 못했다: Resources/{ResetLogoResourcePath}.png", this);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                ShowResetConfirm();
            });
        }

        private void ShowResetConfirm()
        {
            if (_resetConfirmRoot == null) BuildResetConfirm();
            _resetConfirmRoot.gameObject.SetActive(true);
            _resetConfirmRoot.SetAsLastSibling();
        }

        private void HideResetConfirm()
        {
            if (_resetConfirmRoot != null) _resetConfirmRoot.gameObject.SetActive(false);
        }

        /// <summary>메인 화면 전체를 덮는 확인 팝업 — 딤 배경 + 문구 + 초기화/취소 두 버튼. 처음 열 때 한 번만 짓는다.</summary>
        private void BuildResetConfirm()
        {
            _resetConfirmRoot = RuntimeUi.CreateStretched(transform, "ResetSaveConfirm");
            RuntimeUi.CreateImage(_resetConfirmRoot, "Dim", new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, raycastTarget: true);

            var box = RuntimeUi.CreateRect(_resetConfirmRoot, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            box.sizeDelta = new Vector2(560f, 220f);
            RuntimeUi.CreateImage(box, "Panel", new Color(0.12f, 0.12f, 0.14f, 0.97f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, raycastTarget: true);

            var message = RuntimeUi.CreateText(box, "Message",
                "세이브 데이터를 초기화합니다.\n진행도와 단서 기록이 모두 사라집니다.", _font, 24f, Color.white,
                TextAlignmentOptions.Center, new Vector2(0f, 0.4f), new Vector2(1f, 1f));
            message.textWrappingMode = TextWrappingModes.Normal;

            BuildConfirmButton(box, "ConfirmBtn", "초기화", new Vector2(0.1f, 0.1f), new Vector2(0.46f, 0.32f), ConfirmReset);
            BuildConfirmButton(box, "CancelBtn", "취소", new Vector2(0.54f, 0.1f), new Vector2(0.9f, 0.32f), HideResetConfirm);
        }

        private void BuildConfirmButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            UnityEngine.Events.UnityAction onConfirm)
        {
            var background = RuntimeUi.CreateImage(parent, name, new Color(1f, 1f, 1f, 0.12f), anchorMin, anchorMax,
                Vector2.zero, Vector2.zero, raycastTarget: true);

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.24f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.34f);
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                onConfirm();
            });

            RuntimeUi.CreateText(background.transform, "Label", label, _font, 22f, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        }

        private void ConfirmReset()
        {
            HideResetConfirm();
            _onReset?.Invoke();
        }
    }
}
