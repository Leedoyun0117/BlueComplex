# ESC 설정창 인수인계

작성 2026-09-27 · 담당 LSO(이시온) → 인수자

ESC 메뉴창과 그 안의 설정(볼륨·밝기), 배경 실루엣까지 만든 상태다.
**코드는 다 짰고, 유니티에서 배선하는 일이 남았다.** 지금 딱 하나 막혀 있는 게 있는데 아래 「지금 막혀 있는 것」부터 보면 된다.

---

## 지금 막혀 있는 것 ← 여기부터

**증상**: ESC를 열면 뒤 UI가 투명해지며 게임 화면이 비쳐 보인다.

**원인**: 실루엣용 `RawImage`가 **씬 인스턴스에서** 아직 `content`(내려오는 창) 안에 있다.
창 안에 있으면 창을 따라 내려와서 화면과 어긋난다.

**프리팹(`Assets/Prefabs/UI/Esc.prefab`)은 이미 고쳐져 있다.** 씬 인스턴스만 안 따라왔다.
- 씬 파일(`Assets/Scenes/LSO/LSO_room2.unity`)이 2026-09-25자 그대로다 — 저장이 안 됐다
- 씬 인스턴스에 오버라이드가 138개라 구조 변경이 자동으로 내려오지 않은 것으로 보인다

**근거** (Editor.log):
```
[LSO_EscSilhouette] 실루엣 판이 내려오는 창(content) 안에 있다.
  WarnIfInsideSlidingContent () at LSO_EscSilhouette.cs:89
  Awake () at LSO_EscSilhouette.cs:69
  UI.Esc.LSO_EscPanel:Start () at LSO_EscPanel.cs:61
```
`content.SetActive(true)`가 실루엣의 `Awake`를 깨웠다 = 실루엣이 `content` 안에 있다는 뜻.

**고치는 법**:
1. Hierarchy에서 **씬의** `Esc` 펼치기 (프리팹 말고)
2. `EscPanel` 안의 `RawImage`를 `Esc` 바로 아래로 드래그
3. 순서를 `RawImage` → `EscPanel`로 (RawImage가 위 = 뒤에 그려짐)
4. **Ctrl+S로 씬 저장** ← 이게 빠졌을 가능성이 높다

> `Prefab > Revert`로 되돌리는 방법도 있지만 **오버라이드 138개가 같이 날아간다.** 손으로 옮길 것.

확인: 다시 실행했을 때 위 경고가 안 뜨면 배치가 맞은 것이다.

---

## 구조

```
Esc  (LSO_EscPanel + LSO_EscBackdrop)
 ├─ Backdrop     런타임 생성. 전체화면 막
 ├─ RawImage     실루엣          ← 지금 여기가 아니라 EscPanel 안에 있음
 └─ EscPanel     content(내려오는 창)
      └─ 슬라이더들, 버튼 등
```

**열리는 순서**
```
ESC 누름
 → 화면 캡처(아직 안 띄움)
 → 창이 위에서 내려옴 + 배경막이 덮음        0.35초
 → OpenCompleted
 → 실루엣이 막 위로 서서히 떠오름             0.35초
```

실루엣을 창이 다 내려온 뒤에 띄우는 이유: 배경막·창·실루엣이 **같은 CanvasGroup 알파**를 받아 함께 번져 들어오는데, 그 도중엔 막이 반투명이라 진짜 화면이 비친다. 실루엣은 그 화면의 복사본이라 같이 보이면 두 겹으로 겹쳐 번진다.

---

## 파일

### 스크립트 `Assets/Scripts/UI/Esc/` (전부 내 담당, 네임스페이스 `UI.Esc`)

| 파일 | 역할 |
|---|---|
| `LSO_EscPanel.cs` (155) | **언제** 열고 닫을지만. ESC 입력, 열림 상태, 조립 |
| `LSO_PanelSlide.cs` (100) | **어떻게** 보일지. 위→아래 슬라이드 + 페이드 |
| `LSO_GameplayLock.cs` (63) | **무엇을** 멈출지. 시간·입력 잠금과 **이전 값 복원** |
| `LSO_EscBackdrop.cs` (62) | 전체화면 막을 런타임 생성 |
| `LSO_EscSilhouette.cs` (202) | 화면 캡처 → 실루엣 표시 |
| `LSO_GameSettings.cs` (61) | 밝기 값 + PlayerPrefs 저장 |
| `LSO_BrightnessApplier.cs` (62) | 설정값 → 머티리얼 반영 |
| `LSO_BrightnessSlider.cs` (85) | 밝기 슬라이더 |
| `LSO_VolumeSlider.cs` (91) | 볼륨 슬라이더 1개 ↔ 채널 1개 |

### 셰이더·머티리얼

```
Assets/Shaders/LSO/LSO_ScreenBrightness.shader     풀스크린 밝기 패스
Assets/Shaders/LSO/LSO_ScreenSilhouette.shader     알파 → 단색 실루엣 (UI 셰이더)
Assets/Settings/LSO/LSO_ScreenBrightness.mat       렌더러 피처에 물릴 머티리얼
```

> `.meta`의 guid를 손으로 지정했다(머티리얼이 셰이더를 참조하려면 guid가 먼저 필요해서).
> 충돌은 확인했지만 유니티가 다시 부여한 적이 있으니 이상하면 guid부터 볼 것.

---

## 남은 배선 (유니티에서)

### 1. 실루엣 위치 — 위 「지금 막혀 있는 것」

### 2. 밝기 렌더러 피처 ← 안 하면 밝기가 화면에 전혀 안 먹는다

`Assets/Settings/PC_Renderer.asset` 선택 → `Add Renderer Feature` → `Full Screen Pass Renderer Feature`

```
Pass Material   : LSO_ScreenBrightness
Injection Point : After Rendering Post Processing
목록 순서       : CRT 아래   ← 중요
```

**CRT 위에 두면 씬만 어두워지고 UI는 밝은 채로 남는다.** UI가 CRT 셰이더 안에서 `_UITex`로 합성되기 때문(`UICompositorSetupTool` 참고). `Mobile_Renderer.asset`도 같이.

### 3. 밝기 적용기

씬에 빈 오브젝트 만들고 `LSO_BrightnessApplier` 붙인 뒤 `LSO_ScreenBrightness.mat` 물리기.
**ESC창 안에 두지 말 것** — 창이 꺼져 있을 때도 밝기가 유지돼야 한다.

### 4. 슬라이더

| 슬라이더 | 붙일 컴포넌트 | 설정 |
|---|---|---|
| Master | `LSO_VolumeSlider` | Channel = Master |
| BGM | `LSO_VolumeSlider` | Channel = Bgm |
| SFX | `LSO_VolumeSlider` | Channel = Sfx |
| Ambient | `LSO_VolumeSlider` | Channel = Ambient |
| 밝기 | `LSO_BrightnessSlider` | Slider, Panel(비우면 부모에서 찾음) |

**`AudioSettingsController`의 슬라이더 칸 4개는 비워 둘 것.** 양쪽에 다 물리면 리스너가 둘 붙어 같은 값을 두 번 쓴다.

### 5. Always Included Shaders ← 빌드에서만 티가 난다

`Project Settings → Graphics → Always Included Shaders`에 추가:
```
BlueComplex/LSO/ScreenBrightness
BlueComplex/LSO/ScreenSilhouette
```
`Shader.Find`로만 참조해서 씬·프리팹 어디에도 안 물려 있다 → 빌드에서 스트립된다.
에디터에서는 없어도 돌아가므로 빌드하기 전에 반드시.

### 6. AudioSettingsController가 GameScene에만 있다

`LSO_NoteScene`, `LSO_room2`, `LSO_room3`에는 없다. ESC창을 그 씬들에서도 열 거면 같이 넣어야 슬라이더가 동작하고 저장값도 복원된다.

---

## 이 프로젝트에서 걸리는 함정

### UI는 화면에 직접 안 그려진다
```
UI Camera (UI 레이어만) → RT_UI → CRT 셰이더가 _UITex 로 합성 → 화면
```
- **UI 레이어(5)가 아니면 아예 안 보인다.** 코드로 만드는 오브젝트는 레이어를 부모에서 상속시킬 것
- URP Volume의 Color Adjustments로 밝기를 하면 **씬만 어두워지고 UI는 안 어두워진다** (CRT가 포스트 처리 뒤에 돌기 때문)

### 중첩 Canvas를 쓸 거면 레이캐스터 주의
`overrideSorting`용으로 Canvas를 중첩하면 자체 `GraphicRaycaster`가 필요한데,
**일반 것을 붙이면 CRT 배럴 왜곡 보정이 빠져 클릭 좌표가 어긋난다.**
`DistortionCorrectedGraphicRaycaster`를 붙일 것.

### 유니티가 열린 채로 프리팹 YAML을 고치지 말 것
`ClueBookPanel.prefab`이 6761줄 → 5655줄, 오브젝트 288개 → 232개로 날아간 적이 있다.
에디터 리임포트와 파일 쓰기가 겹쳐서 생겼다. 인스펙터에서 하거나 유니티를 끄고 할 것.

### ESC창은 열 때마다 맨 앞으로 간다
`LSO_EscPanel.BringToFront()` = `transform.SetAsLastSibling()`.
이 프로젝트는 런타임에 패널을 만들며 스스로를 앞에 세우는 게 관행이라(`ClueBookPanel`, `StageDialogueOverlay`, `KeyTurnOverlay` 등), 미리 놓아둔 창은 계속 뒤로 밀린다.

**ESC창이 Canvas 루트(MainHud)의 직속 자식이어야 이 한 줄이 통한다.** 더 깊이 옮기면 형제 순서로는 부족해지고 Canvas + `overrideSorting`으로 가야 한다(위 레이캐스터 주의 참고).

---

## 설계 의도 (고칠 때 참고)

### 왜 SetActive가 아니라 CanvasGroup인가
창을 `SetActive(false)`로 끄면 그동안 `Update`가 안 돌아 **ESC 키를 못 받는다.**
그래서 뿌리의 CanvasGroup으로 `alpha`/`blocksRaycasts`를 여닫는다. 없으면 런타임에 붙인다.

### 왜 잠금이 "이전 값 복원"인가
`LSO_GameplayLock`은 `Time.timeScale`과 `StageBootstrapper.InputBlocked`를 잡을 때의 값을 기억했다가 그대로 되돌린다. **무조건 `timeScale=1`, `InputBlocked=false`로 풀면** 대사 재생처럼 이미 잠가 둔 쪽의 잠금까지 풀어 버린다.

잡지 않은 상태에서 풀거나 두 번 잡아도 아무 일이 없게 해놨다(`OnDestroy`에서 그냥 불러도 안전).

### 왜 설정 항목이 EscPanel을 안 고치고 붙는가
`LSO_EscPanel`이 `Opened` / `Closed` / `OpenCompleted` 이벤트를 연다.
설정 UI는 이걸 구독만 하면 되고, 항목이 늘어도 `LSO_EscPanel`은 안 고친다.
실제로 볼륨·밝기·실루엣을 다 붙이는 동안 그 파일을 건드리지 않았다.

### 실루엣이 스냅샷인 이유
창이 열리면 `timeScale = 0`이라 뒤 UI가 멈춘다 → 실시간으로 떠와도 결과가 같다.
**`stopTime`을 끄면 이 그림이 굳어 보인다.** 그때는 UI 전용 레이어를 하나 더 파고 실루엣 카메라가 그 레이어만 RT에 계속 그리게 해야 한다. **셰이더는 그대로 쓰고 `_image.texture`만 그 RT로 바꾸면 된다.**

캡처는 프레임 끝을 기다리지 않는다. 창이 나타나기 **전** 화면이 필요한데, 카메라는 Update 뒤에 그리므로 그 시점 RT_UI에는 지난 프레임(창 없는 화면)이 들어 있다. 프레임 끝까지 기다리면 그새 창이 번져 들어와 자기 자신이 실루엣에 찍힌다.

---

## 남의 코드 (건드리려면 승인 필요)

| 파일 | 담당 | 비고 |
|---|---|---|
| `Assets/Scripts/Audio/AudioSettingsController.cs` | LDY | 볼륨 설정 전체를 이미 담당. 건드릴 필요 없음 |
| `Assets/Shaders/CrtEffect.shader` | LDY | `_Brightness`는 `CrtEffectDriver`가 심박수로 매 프레임 덮어씀. **뺏어 쓰면 안 됨** |
| `Assets/Prefabs/UI/MainHud.prefab` | 공용 | 에디터 툴로 재생성 금지(손으로 다듬어진 상태) |

### 알려만 두고 안 고친 것

`AudioSettingsController.SetVolume()`이 값이 바뀔 때마다 `PlayerPrefs.Save()`를 부른다.
슬라이더를 드래그하면 **프레임마다 디스크 플러시**다. 창 닫을 때 한 번으로 빼면 되는데 한 줄짜리라, LDY 승인받으면 바로 고칠 수 있다. 당장 터지는 문제는 아니다.

### 설정 시스템이 두 벌이다
```
볼륨   AudioSettingsController  →  PlayerPrefs "Volume_*"        (LDY)
밝기   LSO_GameSettings         →  PlayerPrefs "LSO.Settings.*"  (나)
```
둘 다 PlayerPrefs라 기술적 문제는 없다. 합치려면 남의 파일을 건드려야 해서 그대로 뒀다.
나중에 설정이 더 늘면 그때 한 번에 정리하는 게 낫다.

---

## 검증 안 된 것 ← 반드시 읽을 것

**나는 유니티를 실행할 수 없어서, 아래는 전부 확인 못 했다.**

- **컴파일** — 괄호 균형·API 시그니처 일치만 정적으로 확인했다
- **밝기 패스** — 렌더러 피처를 아직 안 붙여서 화면에 먹는지 확인 못 했다.
  특히 **풀스크린 패스 2개(CRT + 밝기)가 연달아 도는 게 실제로 괜찮은지** 확인이 필요하다.
  이 프로젝트에 "두 번째 패스의 좌표계가 깨져 화면이 새까맣게" 된 전례가 있다(`CrtEffect.shader` 상단 주석).
  이론상 밝기 패스는 제자리 곱하기뿐이라 안전하지만 **실제로 돌려봐야 안다.**
- **실루엣** — 배치가 안 끝나서 제대로 보이는지 확인 못 했다
- **볼륨** — 슬라이더를 안 붙여서 믹서에 실제로 먹는지 확인 못 했다

### 덤으로: 상태를 모르는 다른 작업

같은 기간에 만든 것 중 동작 확인을 못 받은 게 있다.

- `LSO_PageTurn.shader` — 단서책 페이지 넘김. **한 번 핑크(셰이더 미지원)로 나왔고** 그 뒤 기본 UI 셰이더 뼈대로 다시 써서 `Fallback "UI/Default"`를 달았다. 지금 동작하는지 확인 못 받았다.
  **Fallback이 문제를 숨기고 있을 수 있다** — 연출이 안 보이면 Fallback으로 떨어진 것이다.
  `LSO_ScreenSilhouette.shader`도 같은 뼈대라 같이 의심해야 한다.
- `LSO_BackgroundFly.shader` — 날벌레. `_Seed`를 머티리얼마다 0~7로 다르게 줘야 따로 움직인다.
- `Assets/Scripts/UI/Menu/LSO_MouseParallax.cs` — 메인화면 마우스 패럴랙스. 아직 안 붙였다.
