# [작업지시서] 물류 센터 테마 배경 적용 및 신규 버튼 인터랙션 5종 추가

## 1. 개요 및 목적 (Overview)
- **작업 목적**:
  1. **물류 센터 감성 인게임 배경 적용**:
     - 사용자가 제공한 캐주얼 쿼터뷰 물류 타이쿤 인게임 레퍼런스 이미지(`Assets/Prefabs/UI/Sprites/LogisticsCenterBg.jpg`)를 `ButtonScene`의 배경에 장착.
     - 중앙 테스트 버튼과 UI의 가독성을 보장하기 위해 부드러운 반투명 틴트(Soft Dark Overlay & Vignette) 결합.
  2. **물류 타이쿤 맞춤형 신규 버튼 인터랙션 5종 구현 (총 10종 프리셋 완성)**:
     - 물류 센터의 컨베이어 벨트, 택배 상자, 바코드 스캐너, 배송 보상 코인, 테이핑 액션을 테마로 한 특화 인터랙션 5종 신설.
  3. **쇼케이스 컨트롤러 및 에디터 빌더 확장**:
     - 총 10가지 모션 프리셋을 한눈에 누를 수 있는 2열 그리드 컨트롤 패널 구축.
     - 신규 FX 토글(코인 분수, 바코드 레이저) 추가.

---

## 2. 신규 버튼 인터랙션 5종 상세 기획 (Presets 6 ~ 10)

| 번호 | 모션/FX 명칭 | 연출 상세 (손맛 & 테마 포인트) | 구현 방식 |
|:---:|---|---|---|
| **6** | **컨베이어 스냅<br>(ConveyorSnap)** | 컨베이어 벨트 위의 화물처럼 누를 때 우측으로 스르륵 밀렸다가(`X +12px`), 뗄 때 기어가 찰칵 맞물리듯 착 달라붙음 | `OnPointerDown`: `DOAnchorPosX(+12px, 0.1s)`<br>`OnPointerUp`: `DOAnchorPosX(0, 0.25s, OutBack)` |
| **7** | **박스 덜컹 들썩임<br>(BoxJolt)** | 배송 트럭이 방지턱을 넘을 때처럼 상자가 위로 퐁 튀었다가(Y +16px) 좌우로 덜컹뒤뚱거리며 착지 | `OnPointerClick`: `DOJump/PunchPosition(Y +18px)` + `DOPunchRotation(Z 8도, 0.35s)` |
| **8** | **바코드 스캔 레이저<br>(BarcodeLaser)** | 버튼 중앙을 붉은 레이저 빔이 위에서 아래로 찌잉- 훑고 지나간 뒤, '스캔 승인'을 알리는 에메랄드 그린 섬광 펄스 | `LaserScanBeam`: 상->하 이동 레이저 라인 + `GlowGraphic` 녹색 플래시 |
| **9** | **황금 코인 분수<br>(CoinFountain)** | 배송 완료 시 상단 재화 카운터처럼 황금빛 동전(🟡) 6~8개가 분수처럼 퐁퐁 솟아올라 곡선 궤적으로 퍼짐 | `CoinFountain.prefab`: 방사형 점프 + 물리 중력 낙하 트윈 |
| **10** | **박스 테이핑 라벨<br>(TapeStripWipe)** | 클릭 순간 투명 크라프트 테이프가 버튼 중앙을 좌 ➡️ 우로 '쫘악' 가로지르며 붙었다가 서서히 스며듦 | `TapeWipeOverlay`: Pivot.x=0 기준 `DOScaleX(1f, 0.25s)` 후 페이드아웃 |

---

## 3. 세부 작업지시서 (Implementation Details)

### Task 1. 배경 이미지 교체 및 비네팅 처리
- **스크립트**: `Assets/Scripts/UI/Editor/ButtonSceneBuilder.cs`
- **구현 내용**:
  - `Assets/Prefabs/UI/Sprites/LogisticsCenterBg.jpg` 스프라이트를 로드하여 `Background` Image의 sprite로 설정 (`PreserveAspect = false`, 1920x1080 꽉 차게 스트레칭).
  - 배경 위에 소프트 틴트 오버레이(`OverlayTint`) 오브젝트를 추가하여 색상 `Color(0.10f, 0.12f, 0.18f, 0.45f)`를 적용해 뒤편의 귀여운 물류 센터 인게임 뷰가 보이면서도 전면 UI 버튼의 텍스트가 또렷하게 강조되도록 처리.

### Task 2. `UIButtonFeedback.cs` 신규 프리셋 확장
- **경로**: `Assets/Scripts/UI/UIButtonFeedback.cs`
- **열거형 확장**:
  ```csharp
  public enum MotionPreset
  {
      JellySquash,
      DepthPress,
      StampHit,
      NeonPulse,
      GentlePop,
      ConveyorSnap,    // [신규 6] 컨베이어 밀림 & 찰칵 복귀
      BoxJolt,         // [신규 7] 택배 박스 위로 퐁 튀고 덜컹임
      BarcodeLaser,    // [신규 8] 붉은 레이저 스캔 빔 & 그린 승인
      CoinFountain,    // [신규 9] 보상 황금 코인 분수
      TapeStripWipe    // [신규 10] 좌->우 테이프 쫘악 붙이기
  }
  ```
- **신규 로직 구현**:
  - `ConveyorSnap`: X축 오프셋 트위닝 + 기어 스냅.
  - `BoxJolt`: 점프 앤 로테이션 펀치.
  - `BarcodeLaser`: 붉은 레이저 바 트윈 및 그린 플래시.
  - `CoinFountain`: 코인 파티클 오브젝트 풀/스폰.
  - `TapeStripWipe`: 좌->우 스케일 테이프 연출.

### Task 3. 동적 FX 프리팹 확장
- `CoinFountain.prefab`: 원형 골드 스프라이트 6~8개가 위로 솟구치며 아래로 흩어지는 이펙트.
- `LaserScanBeam.prefab`: 가로형 레드 네온 라인이 상 ➡️ 하로 빠르게 스캔하는 이펙트.

### Task 4. 쇼케이스 컨트롤러 및 2열 UI 레이아웃 개편
- **스크립트**: `Assets/Scripts/UI/ButtonShowcaseController.cs`, `ButtonSceneBuilder.cs`
- **우측 패널 레이아웃**:
  - 10개 프리셋 버튼을 컴팩트한 2열 그리드(2 Columns x 5 Rows) 또는 세로 스크롤로 재배치.
  - FX 토글 버튼에 코인 분수, 레이저 토글 연동.
  - 상단 상태 텍스트에 신규 10개 모드 실시간 표기.

---

## 4. 검수 및 컴파일 체크리스트 (Verification Checklist)

### 1) 컴파일 및 리소스 검수
- [ ] Unity C# 컴파일 에러 및 경고 0건
- [ ] `LogisticsCenterBg.jpg`가 스프라이트로 정상 로드되어 배경에 깨짐 없이 채워지는가?

### 2) 런타임 안정성 & Null-Safety
- [ ] 신규 5종 모션 실행 시 `NullReferenceException` 없이 완벽히 방어되는가?
- [ ] 코인 분수 및 레이저 빔이 실행 후 지정 시간(0.6s) 뒤 정상적으로 파괴(`Destroy`)되는가?
- [ ] 연타(스팸 클릭) 시 컨베이어나 박스 덜컹임 트윈이 꼬이지 않고 원위치로 복귀하는가?

### 3) 비주얼 및 손맛 QA
- [ ] 레퍼런스 이미지(해피로지스 물류센터)가 배경에 깔려 타이쿤 게임 분위기가 물씬 풍기는가?
- [ ] 컨베이어 스냅: 벨트가 굴러가듯 찰칵 걸리는 기계적 손맛이 느껴지는가?
- [ ] 박스 덜컹임: 택배 박스가 덜컹거리는 경쾌한 입체감이 느껴지는가?
- [ ] 코인 분수: 클릭할 때마다 골드 코인이 퐁퐁 솟아올라 보상 수령 쾌감이 느껴지는가?

---

## 5. AI 개발 에이전트 실행 프롬프트 (Prompt for Agent)

```text
작업지시서 TASK_LOGISTICS_BUTTON_EFFECTS.md에 따라 ButtonScene에 물류 센터 테마 배경을 적용하고, 신규 버튼 인터랙션 5종을 추가 구현하세요.

1. 배경 이미지 적용 (ButtonSceneBuilder.cs):
   - Assets/Prefabs/UI/Sprites/LogisticsCenterBg.jpg를 로드하여 Background 이미지로 설정하고, 반투명 오버레이 틴트(Color(0.1f, 0.12f, 0.18f, 0.45f))를 결합하세요.

2. 신규 모션 5종 구현 (UIButtonFeedback.cs):
   - MotionPreset에 ConveyorSnap, BoxJolt, BarcodeLaser, CoinFountain, TapeStripWipe를 추가하세요.
   - 각 프리셋별 개성 넘치는 물류 테마 DOTween 애니메이션 및 FX를 완성하세요.
   - 코인 분수(CoinFountain) 및 바코드 스캔 레이저 FX 프리팹을 작성/연동하세요.

3. 쇼케이스 컨트롤러 및 2열 그리드 패널 구축:
   - ButtonShowcaseController.cs와 ButtonSceneBuilder.cs를 갱신하여 10개 프리셋 버튼(2열 그리드) 및 FX 토글을 연동하세요.
   - Setup ButtonScene을 실행하여 최신 씬을 저장하고 컴파일 에러를 검수하세요.
```
