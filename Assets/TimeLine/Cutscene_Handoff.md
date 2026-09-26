# 컷씬 인수인계

> 작성: KTH / 브랜치 `KTH`
> 컷씬은 **Unity Timeline + Signal + DOTween**으로 만들어져 있다.
> 스크립트는 `Assets/Scripts/KTH/`, 에셋은 `Assets/TimeLine/`에 있다.

---

## 1. 폴더 구조

```
Assets/
├─ Scripts/KTH/
│  ├─ KTH_TimelineSpeed.cs        타임라인 재생 속도 조절
│  ├─ KTH_TypingTextSignal.cs     대사 타이핑 (시그널마다 다음 줄)
│  ├─ KTH_FadeSystemSignal.cs     CanvasGroup 페이드
│  ├─ KTH_TimelineAudioSignal.cs  BGM 켜기/끄기 (페이드 포함)
│  └─ TimeLineCtrl/
│     ├─ KTH_TimeLinePlay.cs      타임라인 재생 진입점 (Request)
│     ├─ KTH_TimeLineTest.cs      테스트용: T키로 재생
│     └─ ITimeLinePlayer.cs       재생 인터페이스 (현재 미사용)
│
└─ TimeLine/
   ├─ Prefabs/     컷씬 프리팹 (Director + 위 컴포넌트들이 붙어 있음)
   ├─ TimeLine/    타임라인 에셋 (.playable)
   ├─ Signal/      시그널 에셋 (.signal)
   ├─ Audio/       컷씬용 오디오
   └─ CutScene/    컷씬용 리소스
```

테스트 씬: `Assets/Scenes/KTH/KTH Test Scene.unity` (여기에 `KTH_TimeLineTest`가 배치되어 있음)

---

## 2. 컷씬 확인(테스트) 방법

### 방법 A — T키로 재생 (기본)
1. `KTH Test Scene`을 연다.
2. Hierarchy에서 `KTH_TimeLineTest`가 붙은 오브젝트를 선택한다.
3. `Time Line` 칸에 보고 싶은 **타임라인 에셋(.playable)** 을 넣는다.
   (프리팹이 아니라 `Assets/TimeLine/TimeLine/`의 에셋)
4. 해당 컷씬 프리팹이 씬에 **활성 상태로** 있어야 한다.
5. 플레이 → **T키**.

> 동작 원리: `KTH_TimeLinePlay.Request(에셋)` → 씬에 있는 모든 `KTH_TimeLinePlay` 중
> `PlayableDirector.playableAsset`이 같은 것만 재생된다. (문자열 키 없이 에셋 자체가 키)

### 방법 B — 시작하자마자 재생
T키가 불편하면:
1. Hierarchy에서 `TimeLineTest` 오브젝트를 **삭제**(또는 비활성화)한다.
2. 컷씬 프리팹의 `PlayableDirector` → **Play On Awake 체크**.
3. 플레이하면 바로 컷씬이 나온다.

> ⚠️ 이 경우 `KTH_TimeLinePlay.Play()`를 거치지 않으므로 `playOnce` 체크는 무시된다. 테스트 후 **Play On Awake는 다시 꺼둘 것.**

---

## 3. 컷씬 프리팹 구성

모든 컷씬 프리팹은 대체로 같은 구성이다.

| 컴포넌트 | 역할 |
|---|---|
| `PlayableDirector` | 타임라인 재생. 설정: Wrap Mode = **None**, Update Mode = **Game Time**, Play On Awake = **Off** |
| `SignalReceiver` | 타임라인의 시그널을 받아 아래 함수들을 호출 |
| `KTH_TimeLinePlay` | 재생 요청 수신 + `activateOnPlay` 오브젝트 켜고 끄기 |
| `KTH_TimelineSpeed` | 재생 속도 (대부분 0.2, B_Father·B_HouseTerras는 0.1) |
| `KTH_TypingTextSignal` | 대사 |
| `KTH_FadeSystemSignal` | 화면/UI 페이드 (일부 컷씬만) |
| `KTH_TimelineAudioSignal` + `AudioSource` | BGM (일부 컷씬만) |

### Director 설정이 중요한 이유
- **Wrap Mode = None**: 끝나면 `stopped` 이벤트가 발생 → `activateOnPlay` 오브젝트가 다시 꺼진다. `Hold`로 바꾸면 안 꺼진다.
- **Update Mode = Game Time**: ESC 일시정지(timeScale = 0) 시 타임라인·타이핑·페이드가 같이 멈춘다.

---

## 4. 시그널 사용법

타임라인에서 **시그널로 함수를 호출**하는 구조다.

### 시그널 에셋 (`Assets/TimeLine/Signal/`)
| 시그널 | 연결할 함수 |
|---|---|
| `TextNext Signal` | `KTH_TypingTextSignal.NextText()` |
| `FadeIn Signal` | `KTH_FadeSystemSignal.FadeIn()` |
| `FadeOut Signal` | `KTH_FadeSystemSignal.FadeOut()` |
| `SoundOnSignal` | `KTH_TimelineAudioSignal.SoundOn()` |
| `SoundOffSignal` | `KTH_TimelineAudioSignal.SoundOff()` |

### 새 시그널 연결하기
1. 타임라인 창에서 Signal Track 위 우클릭 → **Add Signal Emitter**.
2. Emitter의 `Emit Signal`에 위 시그널 에셋 중 하나를 넣는다.
3. 프리팹의 `SignalReceiver`에 해당 시그널이 없으면 **Add Reaction** → 오브젝트 지정 → 함수 선택.
   (같은 시그널은 한 번만 등록하면 이후 Emitter는 자동으로 같은 Reaction을 쓴다)

### 시그널 트랙 나누기 (가독성)
시그널을 한 트랙에 몰아넣으면 알아보기 어렵다. **Signal Track을 여러 개 만들어 역할별로 나눈다.**
예: `대사` 트랙 / `페이드·사운드` 트랙. 트랙이 달라도 같은 Director의 `SignalReceiver`로 전달되므로 동작은 같다.
(B_Father, B_Training, Car_ToDead, Uki_young, Yuki_lost는 이미 트랙 2개로 나뉘어 있음)

---

## 5. 스크립트별 상세

### 5-1. `KTH_TimeLinePlay` — 재생 진입점
```csharp
KTH_TimeLinePlay.Request(timelineAsset); // 어디서든 호출 가능 (static)
```
- `playOnce`: 체크 시 한 번만 재생 (두 번째 Request는 무시).
- `activateOnPlay`: Awake에서 **꺼두고**, 재생 시작 시 켜고, 끝나면 다시 끈다. 대사 캔버스 등을 넣는다.
- **주의**: 받는 쪽이 static 이벤트 구독 방식이라, 컷씬 오브젝트가 **비활성 상태면 Request가 조용히 무시**된다(에러 없음).

### 5-2. `KTH_TimelineSpeed` — 재생 속도
- `speed` (0.1 ~ 3). 1 = 원래 속도, 0.2 = 5배 느림.
- 재생 시작(`played`)과 Start에서 적용한다. 플레이 중 인스펙터 값을 바꾸면 즉시 반영된다(OnValidate).
- 타임라인 안의 모든 것(시그널 타이밍, 카메라 등)이 같이 느려진다.
- **사운드와 타이핑은 영향을 받지 않는다.** 사운드는 타임라인 밖의 AudioSource를 쓰기 때문에 피치가 변하지 않고, 타이핑 속도는 `charInterval`로 따로 정한다.

### 5-3. `KTH_TypingTextSignal` — 대사
- 인스펙터 `lines` 배열에 대사를 **순서대로** 적는다.
- `TextNext Signal`이 올 때마다 다음 줄을 한 글자씩 타이핑한다. 마지막 줄 이후엔 마지막 줄을 반복한다.
- `charInterval`: 글자당 시간 (기본 0.05초).
- `clearOnEnable`: 켜질 때 텍스트 비우기.
- 리치 텍스트 태그(`<color>` 등)를 써도 태그는 글자 수에서 제외된다.
- 그 밖의 함수: `ShowText(int)` 특정 줄 출력, `Type(string)` 문자열 직접 출력, `Complete()` 즉시 전체 표시.
- ⚠️ **시그널 개수와 `lines` 개수를 맞춰야 한다.** 대사를 중간에 추가하면 시그널도 같은 위치에 추가해야 한다.

### 5-4. `KTH_FadeSystemSignal` — 페이드
- `canvasGroups` 배열을 **시그널이 올 때마다 순서대로 하나씩** 사용한다.
  (FadeIn이든 FadeOut이든 호출될 때마다 다음 그룹으로 넘어감, 마지막 이후엔 마지막 그룹 반복)
- 이름 주의 — **화면을 가리는 검은 판 기준**이다.
  - `FadeIn()` → alpha **0** (판이 사라짐 = 화면이 보임)
  - `FadeOut()` → alpha **1** (판이 나타남 = 화면이 가려짐)
- `fadeDuration`: 페이드 시간 (B_Father는 1.2초).
- ⚠️ 시그널 순서 = 배열 순서. 페이드 시그널을 추가/삭제하면 배열도 같이 고쳐야 한다. 같은 그룹을 두 번 쓰려면 배열에 두 번 넣는다.

### 5-5. `KTH_TimelineAudioSignal` — 사운드
- `clip`: 재생할 곡. `volume`: 곡 기본 볼륨.
- `SoundOn()`: AudioSource를 켜고 0에서 `volume`까지 페이드인.
- `SoundOff()`: 0까지 페이드아웃한 뒤 AudioSource를 끈다.
- `SoundToggle()`: 시그널 하나로 켜고 끌 때 사용.
- **믹서 연결**: `channel`(기본 Bgm) 그룹에 자동 연결 → 옵션 메뉴의 BGM 볼륨 슬라이더가 그대로 적용된다. `mixer`를 비우면 `Resources/GameAudioMixer`를 쓴다.
- 페이드는 timeScale을 무시하므로 **ESC 일시정지 중에도 음악은 계속 나온다**(의도된 동작).
- AudioSource의 `volume`은 이 컴포넌트만 건드려야 한다. 볼륨을 바꾸려면 `Volume` 프로퍼티를 쓴다.

---

## 6. 카메라 워킹 (Cinemachine Track)

- 타임라인에 **Cinemachine Track**을 추가하고, 트랙 바인딩에 `CinemachineBrain`(메인 카메라)을 넣는다.
- 트랙 위에 Cinemachine Shot 클립을 놓고 각 클립에 가상 카메라를 지정한다.
- **클립 두 개를 서로 겹치면** 겹친 구간 동안 두 카메라 사이를 블렌딩한다 → 부드러운 카메라 이동.
  겹친 길이 = 블렌드 시간.
- 현재 Cinemachine Track이 있는 컷씬: **B_HouseTerras** (Activation Track도 같이 사용)

---

## 7. 컷씬별 현황

| 컷씬 | 속도 | 대사 | 페이드 | 사운드 | 시그널 트랙 | 비고 |
|---|---|---|---|---|---|---|
| B_Father | 0.1 | ✅ | ✅ | ❌ | 2 | |
| B_HouseTerras | 0.1 | ✅ | ✅ (In/Out) | ✅ | 1 | Cinemachine + Activation 트랙 |
| B_Training | 0.2 | ✅ | ✅ | ✅ | 2 | |
| Car_ToDead | 0.2 | ✅ | ✅ | ❌ | 2 | Marker 트랙 있음 |
| Conference | 0.2 | ❌ (임시값) | ✅ | ❌ | 1 | `lines`에 `b, v, f, s` 임시값만 있음 |
| Uki_young | 0.2 | ✅ | ✅ | ❌ | 2 | |
| Yuki_FamilyMomory | 0.2 | ✅ | ❌ | ✅ | 1 | |
| Yuki_InSchool | 0.2 | ✅ | ❌ | ❌ | 1 | |
| Yuki_Requiem | 0.2 | ✅ | ❌ | ✅ | 1 | |
| Yuki_lost | 0.2 | ✅ | ✅ | ✅ | 2 | Group 트랙 사용 |

### 남은 작업
**대사가 안 들어간 컷씬**
- Conference (`KTH_TypingTextSignal.lines`에 실제 대사를 넣고 시그널 개수를 맞춰야 함)

**사운드가 안 들어간 컷씬**
- B_Father
- Car_ToDead
- Conference
- Uki_young
- Yuki_InSchool

**사운드 넣는 법**
1. 컷씬 프리팹에 `KTH_TimelineAudioSignal`을 추가한다 (AudioSource는 자동으로 추가됨).
2. `clip`에 곡을 넣는다 (`Assets/TimeLine/Audio/`).
3. `SignalReceiver`에 `SoundOnSignal → SoundOn()`, `SoundOffSignal → SoundOff()` Reaction을 추가한다.
4. 타임라인에서 시작 지점에 `SoundOnSignal`, 끝 지점에 `SoundOffSignal` Emitter를 놓는다.

---

## 8. 알려진 버그 / 주의사항

### ⚠️ 재생 중 타임라인 바를 움직이면 속도가 1배로 돌아감
- **증상**: 플레이 중 Timeline 창에서 재생 바(Playhead)를 드래그하면, 0.2배였던 속도가 1배로 돌아갈 수 있다.
- **원인**: 바를 움직이면 Timeline이 내부 PlayableGraph를 다시 만드는데, `KTH_TimelineSpeed`는 재생 시작(`played`)과 Start에서만 속도를 적용하기 때문이다.
- **임시 해결**: 플레이 중 `KTH_TimelineSpeed`의 `speed` 값을 살짝 바꿨다가 되돌리면 다시 적용된다(OnValidate). 또는 컷씬을 처음부터 다시 재생한다.
- 실제 게임(바를 건드리지 않는 상황)에서는 발생하지 않는다.

### 그 밖의 주의사항
- **시그널 순서 의존**: 대사(`lines`)와 페이드(`canvasGroups`)는 모두 "시그널이 몇 번째로 왔는지"로 동작한다. 시그널을 추가하거나 삭제하면 배열도 같이 맞춰야 한다.
- **같은 타임라인 재재생**: `KTH_FadeSystemSignal`의 순번은 초기화되지 않는다. `playOnce`를 끄고 같은 컷씬을 다시 재생하면 페이드 대상이 어긋난다.
- **Request 무시**: 컷씬 오브젝트가 비활성 상태이거나 씬에 없으면 `Request`가 아무 일도 하지 않는다. 컷씬이 안 나오면 먼저 오브젝트가 켜져 있는지, Director에 올바른 `.playable`이 들어 있는지 확인한다.
- **`KTH_TimeLineTest`는 빌드에서 제거**: 테스트용이라 빌드에 남아 있으면 T키로 컷씬이 재생된다.

---

## 9. 아직 구현되지 않은 것 (다음 작업자 참고)

- **컷씬 종료 콜백**: 컷씬이 끝났을 때 게임 흐름(플레이어 조작 복구, 다음 스테이지 진입 등)에 알려주는 방법이 없다. 현재 `Request`는 테스트 스크립트에서만 호출된다.
  → `KTH_TimeLinePlay`의 `OnStopped`에서 static 이벤트나 콜백을 호출하는 방식을 추천한다.
- **스킵 기능**: 없음.
- **`ITimeLinePlayer`**: 인터페이스만 있고 사용하는 곳이 없다.
