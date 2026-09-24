using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueComplex.Core.Stability;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 나츠 초상화("Natsu Portrait", MainHud)의 표정. 상태는 <see cref="NatsuExpression"/> 넷(Normal·Fury 당황·Focus 집중·Relief 안도)이고,
    /// 프레임은 <see cref="NatsuExpressionSet"/>(Assets/Art/Portraits/Natsu의 시트 4장에서 자른 것)에서 온다.
    ///
    /// 이 컴포넌트는 표정을 "언제" 바꿀지 스스로 정하지 않는다 — <see cref="SetExpression"/>은 CinematicTurnResultPresenter가
    /// 심박수 구간이 바뀌는 시점·키 턴이 시작되는 시점에 부른다(판정은 <see cref="PortraitReactionRules"/>). 여기서 스스로 하는 건
    /// 정해진 표정 안의 프레임 재생뿐이다:
    ///   Normal: 뜬 눈 한 장, 3초마다 눈을 깜박인다(CloseEyes 1→2→3→2→1).
    ///   Fury: Confused 5장을 왕복하며 계속 식은땀·입꼬리가 흔들린다.
    ///   Focus: Focus 1→5(손을 올려 턱을 짚음) 후 마지막 장에서 유지.
    ///   Relief: 집중 중이었다면 TakeOffHand 1→5(손을 뗌)를 먼저 재생하고, 이어서 CloseEyes 1,2,3,4,5,6,5,4,2,1 시퀀스 뒤 Normal로 돌아간다.
    /// 집중에서 Normal로 돌아올 때도 손 떼기(TakeOffHand)를 재생한다.
    ///
    /// 프리팹에 미리 안 붙어 있다 — <see cref="GetOrAdd"/>가 "Natsu Portrait" 오브젝트에 붙인다(런타임 자동 부착, 프리팹 굽기 불필요).
    /// 표정 세트 에셋이 없으면 조용히 그 자리에 구워진 스프라이트를 유지한다.
    /// </summary>
    public sealed class NatsuPortraitView : MonoBehaviour
    {
        /// <summary>한 프레임을 보여주는 시간(초). 안도 시퀀스(10스텝)가 1초 안팎이 되게 잡았다.</summary>
        private const float FrameSeconds = 0.1f;
        private const float BlinkFrameSeconds = 0.05f;
        private const float BlinkPeriodSeconds = 3f;
        private const float FuryFrameSeconds = 0.18f;

        /// <summary>안도 시퀀스의 CloseEyes 프레임(0부터). 기획 원문 "1,2,3,4,5,6,5,4,2,1"을 그대로 옮겼다.</summary>
        private static readonly int[] ReliefOrder = { 0, 1, 2, 3, 4, 5, 4, 3, 1, 0 };
        private static readonly int[] BlinkOrder = { 0, 1, 2, 1, 0 };

        [SerializeField] private Image _image;

        private NatsuExpressionSet _set;
        private Coroutine _routine;

        public NatsuExpression Current { get; private set; } = NatsuExpression.Normal;

        /// <summary>"Natsu Portrait" 오브젝트를 이름으로 찾아 이 컴포넌트를 붙여서(이미 있으면 그대로) 돌려준다. 그 오브젝트가 없으면 null.</summary>
        public static NatsuPortraitView GetOrAdd(Transform root)
        {
            var rect = root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Natsu Portrait");
            if (rect == null) return null;

            var view = rect.GetComponent<NatsuPortraitView>();
            return view != null ? view : rect.gameObject.AddComponent<NatsuPortraitView>();
        }

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            _set = NatsuExpressionSet.Load();
        }

        private void OnEnable() => Begin(Current, fromFocus: false);

        /// <summary>표정을 바꾼다. 이미 그 표정이면(안도가 도는 중에 안도 요청이 또 온 경우 포함) 아무것도 안 한다.</summary>
        public void SetExpression(NatsuExpression next)
        {
            if (next == Current) return;

            var fromFocus = Current == NatsuExpression.Focus;
            Current = next;
            Begin(next, fromFocus);
        }

        /// <summary>애니메이션 없이 평소 표정으로 되돌린다(스테이지 시작·재시작).</summary>
        public void ResetToNormal()
        {
            Current = NatsuExpression.Normal;
            Begin(NatsuExpression.Normal, fromFocus: false);
        }

        private void Begin(NatsuExpression expression, bool fromFocus)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;

            if (_image == null || !isActiveAndEnabled || !HasFrames) return;

            _routine = StartCoroutine(expression switch
            {
                NatsuExpression.Fury => FuryRoutine(),
                NatsuExpression.Focus => FocusRoutine(),
                NatsuExpression.Relief => ReliefRoutine(fromFocus),
                _ => NormalRoutine(fromFocus)
            });
        }

        private bool HasFrames =>
            _set != null && _set.closeEyes is { Length: >= 6 } && _set.confused is { Length: > 0 } &&
            _set.focus is { Length: > 0 } && _set.takeOffHand is { Length: > 0 };

        private void Show(Sprite sprite)
        {
            if (sprite != null) _image.sprite = sprite;
        }

        private IEnumerator Play(Sprite[] frames, IEnumerable<int> order, float seconds)
        {
            var wait = new WaitForSecondsRealtime(seconds);
            foreach (var index in order)
            {
                Show(frames[index]);
                yield return wait;
            }
        }

        private IEnumerator NormalRoutine(bool fromFocus)
        {
            if (fromFocus) yield return Play(_set.takeOffHand, Enumerable.Range(0, _set.takeOffHand.Length), FrameSeconds);

            Show(_set.closeEyes[0]);
            var period = new WaitForSecondsRealtime(BlinkPeriodSeconds);
            while (true)
            {
                yield return period;
                yield return Play(_set.closeEyes, BlinkOrder, BlinkFrameSeconds);
            }
        }

        private IEnumerator FuryRoutine()
        {
            // 1→5→1 왕복. 양 끝 장이 두 번 연달아 나오지 않게 한 주기는 1..5, 4..2로 만든다.
            var cycle = Enumerable.Range(0, _set.confused.Length).Concat(Enumerable.Range(1, _set.confused.Length - 2).Reverse()).ToArray();
            while (true) yield return Play(_set.confused, cycle, FuryFrameSeconds);
        }

        private IEnumerator FocusRoutine()
        {
            yield return Play(_set.focus, Enumerable.Range(0, _set.focus.Length), FrameSeconds);
        }

        private IEnumerator ReliefRoutine(bool fromFocus)
        {
            if (fromFocus) yield return Play(_set.takeOffHand, Enumerable.Range(0, _set.takeOffHand.Length), FrameSeconds);

            yield return Play(_set.closeEyes, ReliefOrder, FrameSeconds);

            // 안도가 끝나면 평소로 — 이후 판단은 다음 심박수 갱신 때 Presenter가 한다.
            Current = NatsuExpression.Normal;
            yield return NormalRoutine(fromFocus: false);
        }
    }
}
