# [작업 지침서] 트랜지션 4종 리뉴얼 및 퀄리티 개선 (ConveyorShutter, ForkliftLift, ReceiptRoll, FragileTape)

## 1. 개요 및 목적
유저 피드백에 따라 기존 트랜지션의 문제점을 개선하고 직관성과 연출 완성도를 극대화합니다.
1. **컨베이어 셔터 (ConveyorShutterTransition)**: 좌우 슬라이딩 사각형을 전면 폐기하고, 상단에서 묵직하게 "철컹!" 떨어지는 공장/물류창고 롤 셔터문으로 리메이크 (PlayOut은 상단으로 롤업되어 올라감).
2. **지게차 (ForkliftLiftTransition)**: 단순 사각형이 아닌, 100% 한눈에 알아볼 수 있는 **디테일한 지게차 실루엣(포크, 2단 마스트, 운전석 캐빈, 대형 바퀴, 경광등) + 팔레트에 높게 쌓인 카톤 박스 화물**을 유압 리프트로 번쩍 들어올리는 확실한 연출로 리메이크.
3. **영수증 롤 출력 (ReceiptRollTransition)**: 뽁뽁이(BubbleWrap)를 대체하여, 포스기/라벨 프린터에서 고속으로 "지지직!" 쏟아져 내려와 화면을 덮고 절취선으로 뜯어지는 롱 영수증 연출 신규 제작.
4. **취급주의 패킹 테이프 (FragileTapeTransition)**: 드론(DeliveryDrone)을 대체하여, "취급주의(FRAGILE)", "당일특송(EXPRESS)", "보안밀봉(SEALED)" 테이프들이 화면에 촥! 촥! 촥! 붙으며 밀봉하고 커팅 오픈되는 직관적 연출 신규 제작.

---

## 2. 세부 개발 명세

### [과제 1] ConveyorShutterTransition.cs (창고 롤 셔터 리메이크)
- **비주얼**:
  - `ShutterRoot`: 화면 전체 크기의 수직 이동 컨테이너 (초기 Y: +ScreenHeight 1150)
  - `ShutterSlats`: 가로 8개 이상의 다크 스틸 / 차콜 그레이 롤 셔터 슬랫 (입체적인 음영 하이라이트)
  - `BottomBar`: 하단 묵직한 강철 바 + 노랑/검정 사선 위험 스트라이프(Caution Hazard Stripe) + 손잡이
  - `WarningBadge`: "⚠️ WAREHOUSE SHUTTER CLOSED" 중앙 스텐실
- **애니메이션**:
  - **PlayIn (0.42s)**: Y +1150에서 0으로 수직 급강하! 바닥에 쿵 닿을 때 `Ease.OutBounce`(감쇠 반동 1~2회)로 묵직하게 "철컹!" 닫힘.
  - **WaitingLoop**: 셔터가 화면을 100% 닫은 채 유지되며, 경광등 점멸.
  - **PlayOut (0.38s)**: Y 0에서 +1150으로 빠르게 상단 롤업되어 올라감 (`Ease.InCubic`), 씬 오픈.

### [과제 2] ForkliftLiftTransition.cs (확실한 지게차 리메이크)
- **비주얼**:
  - `ForkliftBody`:
    - 옐로우/오렌지 지게차 차체 + 블랙 카운터웨이트
    - 튼튼한 운전석 롤 케이지 (Overhead Guard)
    - 2개의 대형 프론트/리어 휠
    - 상단 주황 회전 경광등
  - `HydraulicMast`:
    - 2단 수직 승강 마스트 강철 레일 기둥
    - 은빛 L자형 강철 포크(Forks) 2개
  - `CargoPallet`:
    - 하단 원목 팔레트 (지게차 진입 홀 2개)
    - 팔레트 위에 2~3단으로 높게 적재된 카톤 택배 박스들 (`📦`, 바코드 라벨, 테이프 자국)
    - 화면 전체를 가릴 수 있도록 넉넉한 와이드 화물 스택
- **애니메이션**:
  - **PlayIn (0.48s)**: 지게차가 포크에 거대한 화물 팔레트를 싣고 아래에서 위로 "위이잉~ 쿵!" 유압으로 번쩍 들어올려 화면을 100% 가림 (`Ease.OutBack`).
  - **WaitingLoop**: 상단 주황 경광등이 삐뽀삐뽀 회전 점멸.
  - **PlayOut (0.40s)**: 포크를 쿵 내리고 지게차가 하단/후방으로 하강 퇴장 (`Ease.InQuad`).

### [과제 3] ReceiptRollTransition.cs (영수증 롤 출력 - 뽁뽁이 대체)
- **비주얼**:
  - `PrinterSlot`: 상단 블랙 메탈 영수증 배출구
  - `ReceiptPaper`: 상단 앵커 크림 화이트 영수증 종이
  - 영수증 텍스트 및 그래픽:
    - `[ LOGISTICS TYCOON EXPRESS ]`
    - 대형 흑백 바코드 줄무늬
    - 품목: `★ 물류 허브 화물 이송 완료`
    - `TOTAL: ₩ 158,000 / 승인 완료`
    - 붉은색 승인 도장 `[ PAID ]`
    - 하단 톱니모양 절취선 (Perforated Line)
- **애니메이션**:
  - **PlayIn (0.42s)**: 영수증 종이가 프린터에서 "지지직!" 쏟아져 나오듯 상단에서 하단으로 빠르게 길이(`sizeDelta.y` or `anchoredPosition.y`)가 늘어나며 화면 100% 밀폐 (`Ease.OutCubic`).
  - **WaitingLoop**: 붉은 도장이 쾅! 찍히거나 결제 완료 체크 반짝임.
  - **PlayOut (0.38s)**: 절취선이 뜯어지며 아래로 스르륵 떨어져 퇴장 (`Ease.InQuad`).

### [과제 4] FragileTapeTransition.cs (취급주의 패킹 테이프 - 드론 대체)
- **비주얼**:
  - `CardboardBackground`: 크라프트 박스 브라운 배경 패널
  - `TapeStripes`:
    - Tape 1 (사선 -35도, 레드): `⚠️ FRAGILE / 취급주의 / HANDLE WITH CARE`
    - Tape 2 (사선 +35도, 옐로우): `⚡ PRIORITY EXPRESS / 당일 로켓 특송`
    - Tape 3 (가로, 오렌지/블랙): `🔒 SEALED CARGO / 검수 완료`
    - Tape 4 & 5: 모서리 봉인 테이프
- **애니메이션**:
  - **PlayIn (0.45s)**: 배경과 함께 4~5개의 테이프가 시차를 두고 "촥! 촥! 촥!" 대각선/가로로 뻗어나와 화면을 100% 밀봉 (`Ease.OutQuad`).
  - **WaitingLoop**: 중앙에 `[ EXAMINED & VERIFIED ]` 스탬프 박동.
  - **PlayOut (0.40s)**: 중앙 절개선을 따라 테이프와 박스가 좌우로 쫘악 찢어지며 씬 노출 (`Ease.InCubic`).

---

## 3. 코드 및 씬 연동
1. `TransitionManager.cs`:
   - `TransitionType`에 `ReceiptRoll`, `FragileTape` 추가 및 기존 `BubbleWrap = ReceiptRoll`, `DeliveryDrone = FragileTape` 호환 매핑.
   - 신규 프리팹 필드 등록.
2. `TransitionPrefabBuilder.cs`:
   - 신규 4종 빌더 메서드 구현 및 프리팹 생성.
   - `SceneA` 및 `SceneB` 버튼 텍스트 및 리스너 업데이트:
     - 셔터 버튼: `[🪟 창고 롤 셔터]`
     - 지게차 버튼: `[🚜 지게차 리프트]`
     - 영수증 버튼: `[🧾 영수증 롤 출력]` (구 뽁뽁이 대체)
     - 테이프 버튼: `[📦 취급주의 테이프]` (구 드론 대체)
3. 빌드 및 에러 0건 검증.
