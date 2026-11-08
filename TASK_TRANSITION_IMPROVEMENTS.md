# [작업지시서] 물류 타이쿤 트랜지션 애니메이션 개선 및 버그 수정

## 1. 개요 및 목적 (Overview)
- **작업 목적**:
  1. **택배박스 닫힘 트랜지션 연출 개편**: 기존의 통통 튕기는 바운스 제거, 실제 택배 포장처럼 상/하 날개가 덮인 후 좌측에서 우측으로 운송장/라벨 테이프가 '쫘악' 붙는 자연스럽고 리얼한 연출로 변경.
  2. **가독성 향상**: UI 텍스트(라벨, 버튼, 상태창)가 작아 식별이 어려운 문제를 해결하기 위해 전반적인 폰트 크기 및 대비 상향.
  3. **배송 트럭 애니메이션 결함 해결**: 트럭 와이프 트랜지션 실행 시 화면 배경만 노출되고 트럭 본체가 보이지 않거나 엉뚱한 위치로 사라지는 버그 분석 및 수정.
- **작업 대상 파일**:
  - `Assets/Scripts/Transitions/BoxFlapTransition.cs`
  - `Assets/Scripts/Transitions/TruckWipeTransition.cs`
  - `Assets/Scripts/Transitions/Editor/TransitionPrefabBuilder.cs`
  - `Assets/Scripts/Transitions/TransitionDemoController.cs`
  - 관련 프리팹: `Assets/Prefabs/Transitions/BoxFlapTransition.prefab`, `TruckWipeTransition.prefab`

---

## 2. 결함 원인 분석 (Root Cause Analysis)

### ① 배송 트럭이 보이지 않았던 원인
1. **DOTween 트윈 충돌 (Transform Conflict)**:
   - `truckRoot.DOAnchorPosX(...)`와 `truckRoot.DOShakePosition(...)`이 동일한 `truckRoot` RectTransform에 동시에 Join되어 있음.
   - `DOShakePosition`이 내부적으로 `localPosition`을 직접 조작하면서 `DOAnchorPosX`의 앵커 좌표 트위닝을 덮어써 트럭 좌표가 화면 밖 엉뚱한 좌표로 튕겨 나감.
   - **해결책**: 트럭 본체 이동은 `truckRoot`가 담당하고, 엔진 진동 흔들림(`DOShakePosition`)은 내부 자식 객체(`CargoBody` 또는 트럭 모델)에 분리 적용.
2. **와이프 동기화 및 앵커 좌표 불일치**:
   - `curtainPanel`은 화면 왼쪽(0)에서 오른쪽(1920)으로 확장되는데, 트럭은 단순히 `-screenWidth`에서 `0`(화면 정중앙)으로만 이동하여 커튼의 선두(Leading Edge)와 따로 놀거나 가려짐.
   - **해결책**: 트럭의 X좌표가 커튼이 열리는 우측 끝 지점과 정확히 일치하여 화면 좌측 밖(`-300`)에서 우측 끝(`+screenWidth + 300`)까지 화면을 자연스럽게 쓸고 지나가도록 궤적 재설계.

### ② 택배박스 닫힘 연출 어색함의 원인
1. **과도한 바운스**: `SetEase(Ease.OutBounce)` 사용으로 인해 무거운 택배 박스가 공처럼 통통 튕김.
2. **테이핑 방향성 부재**: `tapeStrip`의 RectTransform Pivot이 `(0.5, 0.5)`로 설정되어 가운데에서 양옆으로 늘어남.
   - **해결책**: 바운스 제거(`Ease.OutQuad` 또는 `Ease.OutCubic`), 날개가 닫힌 후 `pivot.x = 0f`인 테이프/라벨이 좌 -> 우로 스케일(0->1)되거나 마스크 애니메이션으로 부착되도록 수정.

---

## 3. 세부 작업지시서 (Implementation Details)

### Task 1. `BoxFlapTransition.cs` 리얼 포장 연출 구현
1. **날개 닫힘 연출**:
   - `topFlap`과 `bottomFlap`의 Ease를 `Ease.OutBounce`에서 `Ease.OutQuad` 또는 `Ease.OutCubic`으로 변경 (착! 하고 빈틈없이 닫히는 감각).
   - 필요 시 상단 날개와 하단 날개의 닫힘 완료 시점에 미세한 시차(0.05s)를 두어 실제 날개가 맞물려 덮이는 느낌 강화.
2. **좌 -> 우 테이핑 라벨 연출**:
   - `tapeStrip`의 Pivot을 `Vector2(0f, 0.5f)`로 고정 (왼쪽 기준점).
   - 날개가 완전히 닫힌 직후(`Append`), 테이프를 활성화하고 `DOScaleX(1f, tapeStickDuration).SetEase(Ease.OutQuad)`로 좌측에서 우측으로 테이프가 발라지듯 전개.
   - 테이프가 붙는 동안 라벨 텍스트(`boxLabelText`)가 부드럽게 페이드인되거나 테이프와 함께 드러나도록 연동.
3. **오픈(화면 전환 완료) 연출**:
   - 테이프가 뜯기거나 가운데가 갈라지며 상/하 날개가 화면 밖으로 열리도록 처리 (`Ease.InCubic`).

### Task 2. `TruckWipeTransition.cs` 트럭 시각화 및 경로 수정
1. **트럭 궤적 및 와이프 동기화**:
   - 화면 해상도 기준 폭(약 1920px 기준 여유값 2200px 설정).
   - 트럭 출발점: 화면 왼쪽 바깥 (`-1100f` ~ `-1200f`).
   - 트럭 중간점: 커튼이 화면 전체를 완전히 덮었을 때 트럭은 화면 오른쪽 끝을 지나감.
   - 트럭 도착점: 화면 오른쪽 바깥 (`+1100f` ~ `+1200f`).
2. **DOTween 경합 제거 (Shake 분리)**:
   - `truckRoot`에는 오직 `DOAnchorPosX`만 적용.
   - 흔들림 애니메이션은 `[SerializeField] private RectTransform truckBodyVisual;` 필드를 신설하여 자식 비주얼 객체에만 `DOShakePosition` 적용.
3. **렌더링 레이어 보장**:
   - `truckRoot`가 `curtainPanel`보다 Hierarchy 상 아래(화면상 맨 앞)에 위치하도록 `SetAsLastSibling()` 보장.

### Task 3. 텍스트 가독성 및 UI 폰트 크기 상향
1. **박스 테이프 라벨 (`boxLabelText`)**:
   - 기존 폰트 크기: `24` -> **`36`** 상향.
   - 텍스트 가독성을 위해 볼드체 및 명확한 크라프트지 테이프 대비 색상 적용 (어두운 브라운/블랙 잉크 느낌 `#2A1808`).
2. **트랜지션 데모 패널 (`TransitionPrefabBuilder.cs` / `TransitionDemoController.cs`)**:
   - 상태 텍스트(`StatusText`): 기존 `20` -> **`26`** 상향, 패딩 확보.
   - 버튼 텍스트: 기존 `16` -> **`22`** 상향, 줄바꿈 간격 및 가독성 최적화.

### Task 4. 프리팹 빌더 실행 및 자동 바인딩 갱신
- 스크립트 수정 후 `TransitionPrefabBuilder.cs`의 메뉴 기능(`Tools > Logistics Transitions > Rebuild All Transition Prefabs & Scene`)을 호출하거나 에디터 코드를 통해 프리팹 및 씬을 최신 변경사항으로 갱신.

---

## 4. 검수 및 컴파일 체크리스트 (Verification Checklist)

### 1) 컴파일 및 빌드 검수
- [ ] Unity 에디터 콘솔에 C# 컴파일 에러가 전혀 없는가? (0 Errors)
- [ ] DOTween 관련 네임스페이스 및 메서드 호출에 경고가 없는가?

### 2) 런타임 안정성 검수 (Null & Exception Check)
- [ ] `BoxFlapTransition` 실행 시 `topFlap`, `bottomFlap`, `tapeStrip`, `boxLabelText`가 null이 아닌지 방어 코드가 작동하는가?
- [ ] `TruckWipeTransition` 실행 시 `truckBodyVisual` 및 `curtainPanel` 참조 누락으로 인한 `NullReferenceException`이 발생하지 않는가?
- [ ] `DOShakePosition` 및 좌표 트윈 간의 충돌로 콘솔에 Tween Warning이 뜨지 않는가?

### 3) 연출 및 UX 검수 (Visual QA)
- [ ] **택배박스 연출**:
  - [ ] 상하 날개가 닫힐 때 공 튀기듯 튕기지 않고 단단하게 탁 닫히는가?
  - [ ] 날개가 닫힌 후 테이프/라벨이 왼쪽에서 오른쪽으로 부드럽게 붙어나오는가?
  - [ ] 테이프 중앙 라벨 텍스트가 멀리서도 한눈에 알아볼 수 있을 만큼 크고 또렷한가?
- [ ] **배송 트럭 연출**:
  - [ ] 트랜지션 실행 시 화면 뒤로 숨지 않고, 노란색 커튼의 선두에서 붉은 트럭이 선명하게 달리는 모습이 보이는가?
  - [ ] 트럭이 덜덜거리는 주행 흔들림 효과가 좌표 이동과 간섭 없이 부드럽게 재생되는가?
- [ ] **UI 텍스트**:
  - [ ] 데모 화면 하단 컨트롤 패널의 버튼 글씨 및 상태 텍스트가 시원하게 커졌는가?

---

## 5. AI 개발 에이전트 실행 프롬프트 (Prompt for Agent)

```text
다음 지시사항에 따라 물류 타이쿤의 트랜지션 애니메이션 시스템을 리팩토링하고 버그를 수정해주세요.

1. BoxFlapTransition.cs 수정:
   - topFlap과 bottomFlap의 닫힘 Ease를 OutBounce에서 OutQuad로 변경하고, 바운스 효과를 완전히 제거하세요.
   - tapeStrip의 Pivot을 (0f, 0.5f)로 설정하여 왼쪽에서 오른쪽으로 테이프가 발라지듯 DOScaleX(1f) 애니메이션을 적용하세요.
   - boxLabelText의 폰트 크기를 대폭 상향(기존 24 -> 36 이상)하고, 색상 대비를 강화해 크라프트 박스 위에서 또렷하게 보이도록 하세요.

2. TruckWipeTransition.cs 수정:
   - truckRoot에 걸려있던 DOShakePosition과 DOAnchorPosX의 충돌을 제거하세요.
   - 진동 효과를 전담할 truckBodyVisual(또는 자식 RectTransform)을 참조받아 본체에만 DOShakePosition을 적용하세요.
   - 트럭이 화면 좌측 바깥(-1200f)에서 우측 바깥(+1200f)으로 커튼의 와이프 속도에 맞춰 자연스럽게 통과하도록 좌표 애니메이션을 정상화하세요.
   - 트럭이 커튼에 가려지지 않도록 truckRoot.SetAsLastSibling()을 호출하여 최상단 렌더링을 보장하세요.

3. TransitionPrefabBuilder.cs & UI 텍스트 상향:
   - 프리팹 빌더에서 BoxFlap의 텍스트 크기(36), Demo 패널의 상태 텍스트(26), 버튼 텍스트(22)로 폰트 크기를 상향 조정하세요.
   - TruckWipe 프리팹 생성 로직에 truckBodyVisual 바인딩을 추가하세요.
   - 변경된 프리팹과 데모 씬을 빌드 및 갱신하세요.

작업 완료 후 반드시 컴파일 에러 검수 및 기능 동작 검수를 수행해주세요.
```
