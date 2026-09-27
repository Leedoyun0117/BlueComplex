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

        private CanvasGroup _group;
        private TMP_Text _toast;
        private Tween _toastTween;

        public static MainMenuView Create(Transform parent, TMP_FontAsset font, Action onStart, Action onNotes, Action onQuit)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException($"메인 화면 프리팹을 찾을 수 없다: Resources/{ResourcePath}.prefab");

            var instance = Instantiate(prefab, parent, false);
            instance.name = prefab.name;

            var view = instance.AddComponent<MainMenuView>();
            view._group = instance.AddComponent<CanvasGroup>();
            view.Wire(onStart, onNotes, onQuit);
            view.BuildToast(font);
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
    }
}
