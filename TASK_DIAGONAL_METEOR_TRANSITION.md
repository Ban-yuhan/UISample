# [작업지시서] 대각선 굵은 스트라이프 & 유성 택배(DiagonalMeteor) 트랜지션 구축

## 1. 개요 및 목적 (Overview)
- **작업 목적**:
  1. **역동적인 '대각선 굵은 스트라이프 & 유성 택배(DiagonalMeteorTransition)' 신설**:
     - 대각선 각도(-40도)의 끝이 둥글고 뭉툭한 굵은 캡슐형 선(Stripe) 약 7개가 서로 다른 시차와 속도로 화면을 슉! 슉! 덮음.
     - 화면이 덮인 후, 택배 상자 이모지(`📦`)가 빛의 꼬리(Speed Trail)를 달고 유성(별똥별)처럼 대각선 궤적을 그리며 화면을 "슝~!" 가로질러 떨어짐.
     - 덮이던 진행 방향 그대로, 7개의 굵은 선들이 각자 다른 속도로 빠져나가며 다음 씬이 시원하게 공개되는 스피디하고 세련된 시네마틱 트랜지션 구현.
  2. **크로스 씬 전환 시스템(`TransitionManager.cs`) 완벽 연동**:
     - `TransitionType.DiagonalMeteor` 신설 및 `SceneA`, `SceneB`에 테스트 버튼 연동.

---

## 2. 세부 연출 및 단계별 명세 (Cinematic Flow)

```text
┌────────────────────────────────────────────────────────┐
│  [ Phase 1 : In - 7개 대각선 뭉툭한 선 차례로 덮기 ]      │
│  끝이 둥근 굵은 캡슐 스트라이프 7개가                    │
│  서로 다른 속도(0.35s~0.5s)로 슉! 슉! 들어와 화면 밀폐   │
├────────────────────────────────────────────────────────┤
│  [ Phase 2 : Midpoint - 유성 택배 슈팅 (Shooting 📦) ]   │
│   ↖ 📦 ━━━━━ ═ ═ ═  (빛의 꼬리를 달고 대각선으로 슝~!) │
│  "특급 로켓 배송 화물 통과 중!"                         │
├────────────────────────────────────────────────────────┤
│  [ Phase 3 : Out - 동일한 방향으로 선들 퇴장 ]          │
│  덮이던 방향 그대로 7개 선들이 서로 다른 속도로          │
│  우하단 밖으로 빠져나가며 다음 씬 시원하게 오픈 (0.45s)  │
└────────────────────────────────────────────────────────┘
```

### 2.1 단계별 연출 상세
1. **Phase 1 (In - 대각선 뭉툭한 선 7개 덮기, 0.45s)**:
   - 각도: 약 `-40도` 대각선 기울기.
   - 구성: 끝이 뭉툭한 둥근 캡슐형(Rounded Capsule) 굵은 막대 7개.
   - 컬러: 물류 타이쿤 테마의 따뜻한 웜 톤 팔레트:
     - Stripe 1: 꿀벌 옐로우 (`#FFC837`)
     - Stripe 2: 크라프트 브라운 (`#C88A58`)
     - Stripe 3: 웜 오렌지 (`#F37A3E`)
     - Stripe 4: 밀크 크림 아이보리 (`#F7F3E9`)
     - Stripe 5: 세이지 그린 (`#62B358`)
     - Stripe 6: 소프트 네이비 (`#32435F`)
     - Stripe 7: 딥 테라코타 (`#D35400`)
   - 모션: 좌상단 화면 밖에서 서로 다른 딜레이(`0.03s ~ 0.05s` 간격)와 속도로 슬라이드 인하여 화면 전체를 오버랩하며 빈틈없이 100% 밀폐 (`Ease.OutCubic`).
2. **Phase 2 (Midpoint - 유성 택배 슝~! 0.40s)**:
   - 씬 전환 로딩 완료 후, 깨끗한 60fps 상태에서 발동.
   - 택배 상자 이모지(`📦`, 폰트 크기 `90pt`)가 대각선 유성 궤적을 타고 좌상단 밖(`-1200, 700`)에서 우하단 밖(`1200, -700`)으로 초고속으로 **"슝~~~!"** 횡단 (`Ease.InCubic` or `OutQuad`).
   - 택배 뒤로 반투명 화이트/골드 스피드 라인 꼬리(Trail)와 스파클 잔상이 스르륵 스치며 지나감.
3. **Phase 3 (Out - 진행 방향 그대로 퇴장, 0.45s)**:
   - 덮이던 진행 방향(우하단) 그대로, 7개의 대각선 선들이 각자 다른 딜레이와 가속도(`Ease.InCubic`)로 화면 밖으로 퇴장하며 다음 씬이 활짝 열림.

---

## 3. 세부 작업지시서 (Implementation Details)

### Task 1. `DiagonalMeteorTransition.cs` 신설
- **경로**: `Assets/Scripts/Transitions/DiagonalMeteorTransition.cs`
- `BaseTransition` 상속:
  - `PlayIn()`: 7개 대각선 캡슐 스트라이프의 차등 속도 슬라이드 인.
  - `StartWaitingLoop()`: 대각선 유성 택배(`📦`) 및 스피드 트레일 쾌속 슈팅 연출.
  - `StopWaitingLoop()`: 유성 트윈 정리.
  - `PlayOut()`: 진행 방향 그대로 7개 스트라이프 순차 퇴장.

### Task 2. 프리팹 제작 (`DiagonalMeteorTransition.prefab`)
- **경로**: `Assets/Prefabs/Transitions/DiagonalMeteorTransition.prefab`
- 7개의 대각선 캡슐 막대(`Stripe_0` ~ `Stripe_6`), 9-슬라이스 둥근 캡슐 스프라이트, 유성 택배 루트 오브젝트(`MeteorParcel`), 트레일 라인 오브젝트 계층 구성 및 직렬화 필드 바인딩.

### Task 3. `TransitionManager.cs` 및 씬 연동
- `TransitionType` 열거형에 `DiagonalMeteor` 추가.
- `TransitionPrefabBuilder.cs`에 프리팹 생성 로직 추가.
- `SceneA` 및 `SceneB` 데모 컨트롤러에 **[7. 📦 유성 택배&스트라이프]** 버튼 연동.

---

## 4. 검수 체크리스트 (Verification Checklist)
- [ ] Unity C# 컴파일 에러 및 경고 0건 확인.
- [ ] 끝이 뭉툭한 굵은 대각선 선 7개가 서로 다른 속도로 화면을 100% 빈틈없이 덮는가?
- [ ] 화면이 덮인 후 택배 상자 이모지(`📦`)가 유성처럼 꼬리를 달고 대각선으로 시원하게 가로지르는가?
- [ ] 선들이 덮이던 방향 그대로 빠져나가며 다음 씬이 열리는가?
- [ ] 씬 A ↔ 씬 B 전환 시 끊김이나 좌표 왜곡 없이 완벽하게 작동하는가?

---

## 5. 개발 에이전트 실행 프롬프트 (Prompt for Agent)

```text
작업지시서 TASK_DIAGONAL_METEOR_TRANSITION.md에 따라 '대각선 굵은 스트라이프 & 유성 택배(DiagonalMeteorTransition)' 트랜지션을 구현하고 씬에 연동하세요.

1. Assets/Scripts/Transitions/DiagonalMeteorTransition.cs 신설:
   - BaseTransition 상속.
   - 7개의 끝이 뭉툭한 대각선 굵은 스트라이프가 차등 속도로 화면을 덮는 PlayIn 구현.
   - 택배 이모지(📦)가 빛의 꼬리를 달고 대각선으로 슝~ 가로지르는 유성 연출(StartWaitingLoop) 구현.
   - 덮이던 방향 그대로 7개 선들이 빠져나가는 PlayOut 구현.

2. TransitionPrefabBuilder.cs & TransitionManager.cs 연동:
   - TransitionType.DiagonalMeteor 추가 및 프리팹 자동 생성 빌더 구현.
   - SceneA, SceneB에 [7. 📦 유성 택배] 버튼 연동.
   - 프리팹 및 씬을 리빌드하고 컴파일 에러를 검수하세요.
```
