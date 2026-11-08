# [작업지시서] 코인 분수 제거 및 '상차 완료 배송 출발(Dispatch Rev-Up)' 버튼 인터랙션 신설

## 1. 개요 및 목적 (Overview)
- **작업 목적**:
  1. **기존 코인 분수(`CoinFountain`) 제거**:
     - 피드백이 불만족스러웠던 코인 분수 프리셋 및 관련 컴포넌트를 UI에서 완전히 제외.
  2. **'상차 완료 배송 출발(Dispatch Rev-Up)' 인터랙션 신설**:
     - 상차를 마치고 출발 버튼을 누르는 순간의 쾌감을 극대화.
     - 다른 UI를 침범하지 않도록 **제자리(In-Place)**에서 트럭이 앞으로 기울며 부르릉~ 들썩들썩(Suspension Jolt) 엔진 시동을 거는 연출 구현.
     - 트럭 뒤쪽(왼쪽 하단)으로 몽글몽글한 **배기 가스 연기(Exhaust Puff Smoke) 파티클 3~4개**와 타이어 마찰 효과가 뒤로 퐁퐁퐁 뿜어져 나오는 역동적인 디테일 연출.

---

## 2. 세부 연출 설계 명세 (Motion & FX Spec)

### 2.1 제자리 시동 및 들썩임 모션 (`TruckRevUp`)
- **타이밍 & 감속 곡선**:
  - `OnPointerDown`: 출발 대기 준비로 살짝 뒤로 당겨짐 (`X -6px, Scale 0.96f`, 0.08s).
  - `OnPointerClick`:
    - 전방 급발진 틸트: Z축 `-4도` 회전 쏠림 (`0.1s, OutQuad`).
    - 제자리 서스펜션 들썩임: `DOPunchPosition(new Vector3(0, 14f, 0), 0.35s, 12, 0.8f)`로 덜덜덜덜~ 강력한 엔진 진동.
    - 자세 복귀: 회전이 `0도`로 자연스럽게 스냅 (`0.2s, OutBack`).

### 2.2 배기 가스 및 타이어 마찰 FX (`ExhaustSmokePuff.prefab`)
- **위치**: 버튼의 왼쪽 하단(Left-Bottom, 배기통 위치).
- **파티클 구성**:
  - 3~4개의 둥글둥글한 연기 구름(Puff Cloud) 오브젝트.
  - 색상: 따뜻한 물류센터 톤에 어울리는 소프트 웜 그레이/크림 화이트 (`Color(0.85f, 0.83f, 0.80f, 0.8f)`).
  - 모션: 뒤쪽(왼쪽)으로 가속 이동하며 스케일이 `0.5f ➡️ 1.4f`로 몽글몽글 커지다가 알파가 `0f`로 부드럽게 페이드아웃 후 자동 파괴 (`0.45s`).
  - 타이어 마찰: 바퀴 아래쪽에 살짝 흰색/오렌지색 마찰 스파크 라인이 스치듯 사라짐.

---

## 3. 세부 작업지시서 (Implementation Details)

### Task 1. `UIButtonFeedback.cs` 프리셋 개편
- `MotionPreset.CoinFountain` 열거형을 `MotionPreset.TruckRevUp`으로 대체/개편.
- `TruckRevUp` 모션 로직 구현:
  - 제자리 서스펜션 점핑 및 전방 틸트 펀치.
  - 클릭 순간 버튼 왼쪽 하단 좌표에 `ExhaustSmokePuff.prefab` 자동 스폰 연동.

### Task 2. `ExhaustSmokePuff.cs` 및 프리팹 제작
- **경로**: `Assets/Scripts/UI/Effects/ExhaustSmokePuff.cs`, `Assets/Prefabs/UI/ExhaustSmokePuff.prefab`
- 3~4개의 연기 버블이 왼쪽 뒤로 퐁퐁 뿜어져 나가는 DOTween 시퀀스 및 수명 주기 자동 파괴(`Destroy`) 구현.

### Task 3. 쇼케이스 컨트롤러 및 씬 갱신
- **스크립트**: `ButtonShowcaseController.cs`, `ButtonSceneBuilder.cs`
- 9번 버튼 명칭을 **`[9. 🚚 배송 출발 (Rev-Up)]`**으로 변경하고 색상을 유니폼 소프트 네이비/트럭 오렌지로 갱신.
- 클릭 시 상단 배너에 `🚚 부르릉~! 상차 완료, 배송 트럭 출발 대기 중!` 피드백 노출.
- `Setup ButtonScene` 실행하여 씬 갱신 및 저장.

---

## 4. 검수 체크리스트 (Verification Checklist)
- [ ] 기존 코인 분수가 완전히 제거되고 '배송 출발(TruckRevUp)' 모드로 정상 대체되었는가?
- [ ] 버튼 클릭 시 옆 UI를 침범하지 않고 제자리에서 트럭이 덜덜거리며 들썩이는가?
- [ ] 버튼 왼쪽 뒤편에서 몽글몽글한 배기 연기가 퐁퐁 뿜어져 나오며 자연스럽게 사라지는가?
- [ ] C# 컴파일 에러 0건 및 프리팹 자동 파괴(`Destroy`) 확인.

---

## 5. 개발 에이전트 실행 프롬프트 (Prompt for Agent)

```text
작업지시서 TASK_TRUCK_REV_DEPARTURE_EFFECT.md에 따라 코인 분수를 제거하고 '상차 완료 배송 출발(TruckRevUp)' 버튼 인터랙션을 구현하세요.

1. ExhaustSmokePuff.cs 및 프리팹 신설:
   - 버튼 왼쪽 하단에서 3~4개의 몽글몽글한 배기 연기 버블이 왼쪽 뒤로 퍼져나가며 페이드아웃 후 파괴되는 FX를 제작하세요.

2. UIButtonFeedback.cs 수정:
   - CoinFountain 프리셋을 TruckRevUp으로 대체하세요.
   - 클릭 시 제자리에서 Z축 -4도 틸트 및 Y축 서스펜션 들썩임(DOPunchPosition)을 주고, 왼쪽 뒤에 ExhaustSmokePuff를 스폰하세요.

3. ButtonShowcaseController.cs & ButtonSceneBuilder.cs 갱신:
   - 9번 버튼을 [9. 🚚 배송 출발 (Rev-Up)]으로 변경하고, 상단 배너에 배송 출발 메시지를 연동하세요.
   - Setup ButtonScene을 실행하여 씬을 갱신하고 컴파일 에러를 검수하세요.
```
