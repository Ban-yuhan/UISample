# [작업지시서] 지도(스테이지) 씬 전환 전용 '물류 지도 펼침 & GPS 핀(WorldMapPin)' 트랜지션 구축

## 1. 개요 및 목적 (Overview)
- **작업 목적**:
  1. **지도(스테이지 선택) 이동 전용 시네마틱 트랜지션 신설**:
     - 씬 A에서 지도/스테이지 버튼을 눌러 씬 B로 넘어갈 때, 캐주얼 타이쿤 게임 특유의 여행/탐험/지역 확장의 설렘을 주는 **'물류 배송 지도 펼침 & GPS 핀(WorldMapPinTransition)'** 트랜지션 구축.
  2. **크로스 씬 전환 파이프라인(`TransitionManager.cs`) 연동**:
     - 씬 A에서 버튼 클릭 ➡️ 지도 펼침(In) ➡️ 씬 B 비동기 로드 및 GPS 핀 통통 꽂힘 대기(Midpoint) ➡️ 지도 줌인/개방(Out) ➡️ 씬 B 노출 파이프라인 완성.
  3. **씬 A 및 쇼케이스 UI에 '🗺️ 배송 지도(스테이지)' 버튼 배치**:
     - 사용자가 바로 눌러서 테스트해 볼 수 있도록 SceneA(및 데모 UI)에 눈에 띄는 지도 버튼 연동.

---

## 2. 세부 연출 및 단계별 명세 (Cinematic Flow)

```text
┌────────────────────────────────────────────────────────┐
│  [ Phase 1 : In - 지도 펼침 ]                           │
│  격자 구역선과 배송 루트가 그려진 접이식 지도가           │
│  화면 좌우/중앙에서 촥! 촥! 펼쳐지며 전체를 덮음 (0.45s)   │
├────────────────────────────────────────────────────────┤
│  [ Phase 2 : Midpoint - GPS 핀 & 레이더 파동 ]         │
│               📍 (쿵! 통통~ 꽂힘)                       │
│           ( ( ( 🎯 레이더 원형 파동 ) ) )              │
│  "새로운 배송 구역(스테이지 B) 탐색 중..."              │
├────────────────────────────────────────────────────────┤
│  [ Phase 3 : Out - 지도 줌인 & 스테이지 오픈 ]           │
│  씬 B 로드 완료 시 핀 위치로 줌인되며                    │
│  지도가 부드럽게 좌우로 젖혀지며 씬 B 완전 노출 (0.4s)   │
└────────────────────────────────────────────────────────┘
```

### 2.1 단계별 연출 상세
1. **Phase 1 (In - 지도 펼침, 0.45s)**:
   - 연한 크림 베이지 톤의 배송 구역 격자(Grid)와 도로망/점선 배송 루트가 인쇄된 지도 시트(`MapFoldLeft`, `MapFoldRight`, `MapSheet`)가 3단 접이식이 펼쳐지듯 화면 전체를 빈틈없이 덮음 (`SetEase(Ease.OutCubic)`).
2. **Phase 2 (Midpoint / 로딩 중 대기)**:
   - 씬 B가 비동기 로드되는 동안 화면 중앙에 귀여운 붉은색 **GPS 위치 마커 핀(`📍 MarkerPin`)**이 하늘에서 쿵! 떨어져 통통(`DOPunchPosition`, `OutBounce`) 꽂힘.
   - 핀 바닥 접지면에서 동심원 레이더 펄스(Radar Wave)가 2~3회 퍼져나가며 '목적지 배송 구역 락온' 느낌 연출.
   - 핀 위에 **"스테이지 B 물류 허브"** 미니 말풍선 라벨이 퐁! 솟아오름.
3. **Phase 3 (Out - 줌인 개방, 0.4s)**:
   - 씬 B 로딩 완료 신호 시, 지도가 핀을 중심으로 줌인 스케일업(`DOScale(1.25f)`)되며 좌우로 활짝 젖혀져 사라지고 씬 B 화면이 시원하게 공개됨 (`SetEase(Ease.InCubic)`).

---

## 3. 세부 작업지시서 (Implementation Details)

### Task 1. `WorldMapPinTransition.cs` 신설
- **경로**: `Assets/Scripts/Transitions/WorldMapPinTransition.cs`
- `BaseTransition` 상속 및 필수 메서드 구현:
  - `PlayIn()`: 지도 양 날개 및 메인 시트 언폴드(Unfold) 전개.
  - `StartWaitingLoop()`: GPS 핀 쿵 낙하 & 바운스 꽂힘 + 레이더 링 펄스 루프.
  - `StopWaitingLoop()`: 대기 펄스 트윈 안전 Kill.
  - `PlayOut()`: 줌인 및 좌우 분할 페이드아웃 개방.
  - `OnDestroy()`: 트윈 메모리 누수 방지 Kill 처리.

### Task 2. 프리팹 제작 (`WorldMapPinTransition.prefab`)
- **경로**: `Assets/Prefabs/Transitions/WorldMapPinTransition.prefab`
- 3단 접이식 지도 시트, 도로망/루트 점선 스프라이트, 빨간색 GPS 핀(`📍`), 레이더 링, 말풍선 텍스트 오브젝트 계층 구성 및 직렬화 필드 100% 바인딩.

### Task 3. `TransitionManager.cs` 및 `SceneA` / `SceneB` 연동
- `TransitionType` 열거형에 `WorldMapPin` 추가.
- `TransitionPrefabBuilder.cs`에 `WorldMapPin` 프리팹 생성 로직 추가.
- **`SceneA.unity`**:
  - UI 상단/하단에 눈에 띄는 **[🗺️ 스테이지 지도 (Scene B 이동)]** 버튼 배치 및 클릭 시 `TransitionManager.Instance.TransitionToScene("SceneB", TransitionType.WorldMapPin)` 바인딩.
- **`SceneB.unity`**:
  - 복귀 버튼 목록에 `WorldMapPin`을 추가하여 씬 A ↔ 씬 B 양방향 지도 트랜지션 테스트 지원.

---

## 4. 검수 체크리스트 (Verification Checklist)
- [ ] Unity C# 컴파일 에러 및 경고 0건 확인
- [ ] 씬 A에서 '🗺️ 스테이지 지도' 버튼을 누르면 지도가 촥 펼쳐지며 씬 B로 전환되는가?
- [ ] 씬이 로딩되는 동안 화면 중앙에 GPS 핀이 쿵 떨어져 통통 꽂히고 레이더 펄스가 도는가?
- [ ] 씬 B가 준비되면 지도가 줌인 개방되며 씬 B 화면 위에서 자연스럽게 열리는가?
- [ ] 씬 B에서 씬 A로 복귀 시에도 지도 트랜지션이 정상 작동하는가?
- [ ] DOTween 트윈 누수 및 NullReferenceException 0건 확인.

---

## 5. 개발 에이전트 실행 프롬프트 (Prompt for Agent)

```text
작업지시서 TASK_WORLD_MAP_TRANSITION.md에 따라 지도(스테이지) 씬 전환 전용 'WorldMapPinTransition' 트랜지션을 구현하고 SceneA, SceneB, TransitionManager에 연동하세요.

1. Assets/Scripts/Transitions/WorldMapPinTransition.cs 신설:
   - BaseTransition을 상속하여 [지도가 좌우 촥 펼쳐짐(In)] -> [GPS 핀이 쿵 떨어져 통통 꽂히고 레이더 펄스(Waiting)] -> [지도 줌인 개방(Out)] 파이프라인을 구현하세요.

2. TransitionPrefabBuilder.cs 및 TransitionManager.cs 연동:
   - TransitionType에 WorldMapPin을 추가하고, WorldMapPinTransition.prefab을 자동 생성하도록 빌더에 추가하세요.
   - SceneA에 [🗺️ 스테이지 지도 (Scene B 이동)] 버튼을 추가하고 TransitionToScene("SceneB", TransitionType.WorldMapPin)을 바인딩하세요.
   - SceneB에도 지도 복귀 버튼을 추가하세요.
   - Rebuild All Transition Prefabs & Scene을 실행하여 프리팹 및 씬을 갱신하고 컴파일 에러를 검수하세요.
```
