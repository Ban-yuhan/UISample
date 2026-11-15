# [작업지시서] BouncingBallTransition 추가 및 SceneA·SceneB 버튼 확장

## 1. 프로젝트 및 참조 정보
- **활성 유니티 프로젝트**: `C:\YuHan\Project\UISample`
- **참조 모션 HTML**: `c:\Users\김진웅\Downloads\bouncing-ball.html`
- **대상 씬**: `Assets/Scenes/SceneA.unity`, `Assets/Scenes/SceneB.unity`

---

## 2. `BouncingBallTransition` 구현 명세 (`Assets/Scripts/Transitions/BouncingBallTransition.cs`)

### 2.1 비주얼 & 컬러 팔레트
- **크림 배경 패널 (`bgPanel`)**: `#EFEAE0` (`new Color(0.937f, 0.918f, 0.878f, 1f)`), `PlayIn` 시 빠르게 페이드인(`0 → 1`), `PlayOut` 시작 시 숨김/페이드아웃.
- **바닥 라인 (`floorLine`)**: `#DCD4C5` (`new Color(0.863f, 0.831f, 0.773f, 1f)`), `y = -400` (1920×1080 중앙 기준, HTML의 `FLOOR = 940` 대응), 두께 `3px`.
- **점선 궤적 (`trailDots`)**: `#C4B8A5` (`new Color(0.769f, 0.722f, 0.647f, 1f)`), `6×6` 원형 도트 풀. 공의 중심이 이동한 경로 뒤로만 순차적으로 점 찍힘.
- **메인 공 (`ballRect`)**: `#E4462B` (`new Color(0.894f, 0.275f, 0.169f, 1f)`), 원형 스프라이트, 시작 지름 `100×100` (`R0 = 50`).

### 2.2 DOTween 물리 모션 타임라인 (HTML 로직 충실 이식)
- `DOTween.To(() => _elapsed, t => UpdateMotionAtTime(t), totalInDuration, totalInDuration).SetEase(Ease.Linear)` 방식으로 HTML의 결정론적 물리 함수(`ballAt(t)`)를 그대로 재현하면 60fps/고주사율 관계없이 완벽한 스쿼시·스트레치·포물선·면적 보존(`sx = 1 / sy`)·도트 궤적이 구현됨!
- **타임라인**:
  1. **등장 (`0.00s ~ 0.12s`)**: 좌측 상단(`x = -700, y = 120` 부근, 1920×1080 중앙 원점 기준)에서 `Ease.OutBack` 스케일 팝업.
  2. **낙하 + 3회 바운스 (`0.15s ~ T_LAND`)**: `G = 5200`, `H0 = 520`, `REST = {0.72f, 0.80f, 0.66f}` 물리 계산으로 X(`-700 → +290`) 지수 전진 + 3회 톡-톡-톡 바운스 + 속도 비례 세로 스트레치 & 접촉 스쿼시 + 지나간 경로 뒤로 도트 활성화.
  3. **4번째 착지 기 모으기 (`CHARGE = 0.26s`)**: 바닥에서 납작하게 웅크림 (`sy ≈ 0.51`, `sx ≈ 1.96`).
  4. **화면 돌진 (`LAUNCH = 0.34s`)**: 세로 스트레치(`sy = 1.45`) 후 살짝 도약하며 화면 중앙 `(0, 0)`으로 폭발적 확대(`R_END = 2200`, 지름 `4400`), 화면 전체를 `#E4462B`로 100% 덮은 직후 `onInComplete?.Invoke()`.
- **WaitingLoop & PlayOut**:
  - `StartWaitingLoop()`: 화면을 덮은 `#E4462B` 공 유지 (필요 시 미세 1.02 펄스).
  - `PlayOut(onOutComplete)`: `bgPanel`, `floorLine`, `trailDots`를 비활성화/투명 처리해 새 씬을 뒤로 준비시킨 뒤, 화면을 덮고 있던 붉은 공 `ballRect`가 `DOScale(Vector3.zero, 0.42f).SetEase(Ease.InBack)`으로 중앙으로 쏙 축소되며 새 씬을 시원하게 공개!
- **안전성**: `OnDestroy()` 및 `ResetState()`에서 모든 DOTween `Kill()` 및 상태 초기화 완벽 처리.

---

## 3. 시스템 & SceneA / SceneB 버튼 연동
1. **`TransitionManager.cs`**:
   - `TransitionType`에 `BouncingBall` 추가.
   - `[SerializeField] private GameObject bouncingBallPrefab;` 추가 및 `LoadPrefabsIfNull()` (`Resources.Load<GameObject>("Transitions/BouncingBallTransition")`), `ResolvePrefab()`, `GetRandomType()`(RandomAll 포함), `SetPrefabs(...)` 연동.
2. **`TransitionDemoController.cs`**:
   - `[SerializeField] private Button btn14BouncingBall;` 추가, `btn14RandomAll` -> `btn15RandomAll` (또는 `btn14BouncingBall` + `btn15RandomAll`)으로 명확히 정리.
   - `BindButtons()`에 `14. 🔴 바운싱 볼` (`TransitionType.BouncingBall`) 및 `15. 🎲 전체 랜덤` (`TransitionType.RandomAll`) 바인딩.
3. **`TransitionPrefabBuilder.cs`**:
   - `CreateBouncingBallPrefab(Sprite circleSprite)` 구현하여 `Assets/Prefabs/Transitions/BouncingBallTransition.prefab` 및 `Assets/Resources/Transitions/BouncingBallTransition.prefab` 양쪽 모두 생성·동기화.
   - `SetupTransitionControlPanel` 및 `SceneA.unity`, `SceneB.unity` 빌드 로직에 `14. 🔴 바운싱 볼` (`#E4462B`) & `15. 🎲 전체 랜덤` 버튼 추가(그리드/레이아웃 깨짐 없이 깔끔히 배치).
   - 메뉴 실행으로 프리팹 및 `SceneA.unity`, `SceneB.unity` 재빌드 및 저장 완료.

---

## 4. 검수 체크리스트
- [ ] 컴파일 에러 및 경고 0건
- [ ] `Assets/Prefabs/Transitions/BouncingBallTransition.prefab` 및 `Assets/Resources/Transitions/BouncingBallTransition.prefab` 동시 존재 및 무결성 확인
- [ ] `SceneA.unity`와 `SceneB.unity` 양쪽 컨트롤 패널에 `14. 🔴 바운싱 볼` 버튼 존재 및 `TransitionDemoController` / `TransitionManager` 직렬화 레퍼런스 0 Missing
- [ ] Play Mode에서 `BouncingBallTransition` `PlayIn` -> `PlayOut` 동작 및 DOTween `Kill()` 안전성 확인
