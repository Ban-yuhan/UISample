# BillScene (정산 씬) 기술 & 리소스 Wiki

다른 에이전트나 새 채팅에서 `BillScene` 작업을 이어받을 때 바로 참고할 수 있도록 **현재 구현 구조**, **핵심 스크립트**, **확정된 2D 리소스 요청 스펙**을 정리한 문서입니다.

---

## 1. 씬 및 폴더 위치
- **씬 경로**: `Assets/SettlementReceipt/Scenes/BillScene.unity`
- **에디터 빌더**: `Assets/SettlementReceipt/Scripts/Editor/BillSceneBuilder.cs` (메뉴: `Tools/UI Showcase/Setup BillScene (Settlement Receipt)`)
- **런타임 스크립트**: `Assets/SettlementReceipt/Scripts/`
- **현재 임시 스프라이트**: `Assets/SettlementReceipt/Sprites/`
- **대응 HTML 샘플**: `Assets/UI샘플/end_of_day_settlement_receipt_ui.html`

---

## 2. 확정된 기획/디자인 결정 사항
1. **영수증 말림(Curl) 연출 유지**:
   - 정적 스프라이트로 단순화하지 않고, 기존의 **커스텀 정점 변형 기반 3D 말림(Curl)** 시스템을 그대로 유지합니다.
   - 따라서 감열지 리소스는 고정 톱니 통이미지가 아니라 **종이 질감 패턴 타일**을 사용합니다(상단 톱니 절단면과 말림 곡률은 `ReceiptPaperGraphic`이 메시로 직접 생성).
2. **날씨(Weather) 요소 제거 예정**:
   - `BillSceneBuilder.cs`의 익일 예보(`Row_Forecast`) 내 날씨 칩 및 날씨 아이콘 4종(`Icon_sun`, `Icon_cloud`, `Icon_cloud_rain`, `Icon_snow`)은 추후 제거될 예정이므로 **신규 2D 리소스 요청 목록에서 제외**합니다.
3. **LED 글로우(Glow) 구현 방식**:
   - 색상별로 글로우 이미지를 여러 장 받지 않고, **흰색 방사형(Radial) 글로우 스프라이트 1장**만 리소스로 받아 Unity `Image` 오브젝트의 `Color`, `Alpha`, `Scale`을 DOTween으로 제어합니다.

---

## 3. 확정 2D 아트 리소스 요청 스펙
> 색상을 코드에서 바꿔 재사용하는 요소(글로우, 공용 9-slice, 점선 등)는 **순수 흰색(`#FFFFFF`, 투명 배경)**으로 요청합니다.

| 번호 | 항목명 | 규격 / 분리 조건 | 비고 |
| :---: | :--- | :--- | :--- |
| **1** | **프린터 본체** | 단품 | 영수증 뒤 레이어에 배치 |
| **2** | **배출구 상단 덮개** | **프린터 본체와 분리** | 영수증 앞 레이어에서 배출구 가림 처리(커터 날 포함 가능) |
| **3** | **LED 램프** | 단품 | 프린터 상태 표시등 |
| **3-1** | **흰색 방사형 글로우 스프라이트** | **순수 흰색 (틴트/알파용)** | 대기(초록) ↔ 출력(주황 핑, Scale `1→2.2`) DOTween 제어 |
| **4** | **감열지 질감 타일** | 반복 타일(Repeat) | `ReceiptPaperGraphic` Curl 유지용 도트/종이 질감 |
| **5** | **정산완료 인주자국** | 완성형 또는 빈 프레임(흰색)+TMP | `SettledStamp` 연출용 |
| **6** | **아이콘 세트** | 개별 2D 아이콘 | **수입/지출 추세(`trend_up`, `trend_down`), 배송 택배 수(`truck`), 반품 택배 수(`rotate_ccw`), 달력(`calendar`), 택배 박스(`package`)** *(날씨 아이콘 제외)* |

- **공용 재사용 파츠** *(ClipBoardScene 등 타 씬과 공용 가능)*:
  - 가로 점선 타일(`Image.Type.Tiled`), 흰색 9-slice 라운드 박스, 원형/별 파티클

---

## 4. 핵심 스크립트 아키텍처

| 스크립트 | 역할 |
| :--- | :--- |
| `SettlementReceiptView.cs` | 전체 출력 시퀀스, 행 순차 등장, 스킵/뜯기 인터랙션 총괄 |
| `PrinterBodyFx.cs` | 출력 중 프린터 미세 진동(`shakePeriod = 0.11f`), `led` 색상 전환(대기 초록 ↔ 출력 앰버), `ledPing` 스케일/알파 루프 제어 |
| `ReceiptCurl.cs` | 종이 상단 `curlLength` 구간의 꺾임 각도(`degrees`), 반지름(`curlRadius`), 원근 초점(`perspective`), 앞/뒤 말림 방향(`towardViewer`) 관리 및 하위 `Graphic`에 `ReceiptCurlVertex` 자동 부착 |
| `ReceiptPaperGraphic.cs` | `MaskableGraphic` 상속 커스텀 메시. 상단 톱니 절단면 + 도트 질감 + `Part.Flat`(평평한 본체) / `Part.Curl`(앞으로 넘어와 글씨를 덮는 말린 부분 + 그림자) 분할 렌더링 |
| `ReceiptCurlVertex.cs` | `BaseMeshEffect.ModifyMesh`로 일반 `Image` 정점을 휘게 하고, `TMPro_EventManager.TEXT_CHANGED_EVENT` 콜백으로 **TMP 텍스트 정점(`TMP_TextInfo`)까지 실시간 곡률 변형 및 기울기 알파 감쇠** 적용 |
| `UIVerticalGradient.cs` | `BaseMeshEffect` 기반 수직 그라데이션(예: 배출구 슬롯 깊이감 음영) |
| `ReceiptParticlePool.cs` | 수치 행 등장/순수익 달성 시 터지는 파티클 풀 |
| `BillSceneBuilder.cs` | 에디터에서 씬 계층, 레이어(`PrinterAssembly` → `ReceiptViewport` → `SlotUpper`), 폰트, 스프라이트를 일괄 조립 |

---

## 5. 레이어 및 주의사항
1. **레이어 순서 (가림 처리)**:
   - 영수증(`ReceiptPaper`)이 프린터 슬롯 안에서 밀려 나오므로 반드시 `프린터 본체(뒤) → ReceiptViewport(중간) → 배출구 상단 덮개(앞)` 순서를 유지해야 합니다.
2. **TMP 정점 변형 주의**:
   - `ReceiptPaper` 하위의 모든 텍스트/이미지는 `ReceiptCurl.RegisterChildren()`을 통해 `ReceiptCurlVertex`가 부착되므로, 런타임에 행이나 아이콘을 동적으로 추가할 경우 `RegisterChildren()`을 다시 호출해야 말림 변형이 적용됩니다.
3. **남은 작업 (Next Steps)**:
   - `BillScene` 2D 리소스 시트 이미지 생성 (`Assets/UI샘플/ResourceSheets/BillScene_2D_Resources.png` — 이전 요청 시 `429 RESOURCE_EXHAUSTED` 쿼터 제한으로 미생성)
   - `BillSceneBuilder.cs` 및 데이터에서 날씨 UI(`WeatherChip`, 날씨 아이콘 4종) 제거
