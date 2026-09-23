using System;

namespace BlueComplex.UI.Motion
{
    /// <summary>새 값은 반드시 맨 끝에 추가할 것 — SoundLibrary.asset이 각 항목을 이 enum의 순번(int)으로 저장하므로,
    /// 중간에 끼워 넣으면 뒤에 있던 값들의 순번이 밀려서 이미 저장된 에셋의 클립-큐 연결이 통째로 어긋난다
    /// (실제로 한 번 이 문제가 나서 ButtonClick을 누르면 ClockTick 클립이 나온 적 있다 — SoundLibrary.asset을
    /// 다시 만들어야 했다). 값을 지울 때도 마찬가지로 뒤 순번이 밀리니 끝에서만 빼거나, 지우는 대신 안 쓰기만 한다.</summary>
    public enum UiSoundCue
    {
        /// <summary>종이가 스치거나 놓이는 소리(카드 집기·안착, 아이템 삽입·구겨짐).</summary>
        Paper,

        /// <summary>압정을 꽂는 소리(포스트잇이 붙을 때 압정이 박히는 순간) — 키 카드는 테이프로 붙는 소리로도 쓴다.</summary>
        Pin,

        /// <summary>타자기 한 글자.</summary>
        Type,

        /// <summary>펜으로 선을 긋거나 지우개로 지우는 소리(기억 풍선).</summary>
        Pen,

        /// <summary>열쇠가 찍히는 소리.</summary>
        Key,

        /// <summary>포스트잇을 떼는 소리(끈적이는 종이가 벗겨지는 소리) — 턴이 넘어갈 때 두 포스트잇이 떼어진다.</summary>
        PostitPeel,

        /// <summary>글자 쓰는 소리(펜으로 슥슥 적는 소리) — 떼어진 사이에 포스트잇 내용이 새로 쓰인다.</summary>
        Write,

        /// <summary>아이템을 사용하는 소리(카드가 구겨져 사라지는 순간).</summary>
        ItemUse,

        /// <summary>종이 집는 소리 — 단서 카드에 마우스를 올릴 때 난다.</summary>
        ClueTake,

        /// <summary>엑스레이 판넬을 매단 관절 팔이 펼쳐지는 소리.</summary>
        MechanicalJointOpen,

        /// <summary>엑스레이 판넬을 매단 관절 팔이 접히는 소리.</summary>
        MechanicalJointClose,

        /// <summary>버튼 클릭음 — 엑스레이 접기 버튼, 단서를 짧게 눌러 스토리 보기, 그 외 일반 UI 버튼.</summary>
        ButtonClick,

        /// <summary>벽시계 바늘이 시간을 건너뛰며 움직이는 소리. 쿼터가 넘어가는 턴에서만 난다.</summary>
        ClockTick,
    }

    /// <summary>
    /// 사운드 훅. 연출 코드는 "지금 이 소리가 나야 한다"는 시점만 여기로 알리고, 실제 재생은
    /// <see cref="BlueComplex.Audio.SoundManager"/>가 <see cref="Cue"/>를 구독해서 담당한다(구독이 없으면 아무 일도 없다).
    /// </summary>
    public static class UiSoundHooks
    {
        public static event Action<UiSoundCue> Cue;

        public static void Play(UiSoundCue cue) => Cue?.Invoke(cue);
    }
}
