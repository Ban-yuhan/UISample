# [이식 지침서] 물류 타이쿤 트랜지션 & 모듈형 버튼 인터랙션 시스템 이식 가이드

> **문서 목적**: 본 프로젝트(`UISample`)에서 완성된 **11종 크로스 씬 트랜지션 시스템**, **10종 모듈형 버튼 인터랙션 컴포넌트**, **주문 폭주(Order Rush) 피버 모드**를 다른 신규/기존 유니티 프로젝트에 오류 없이 100% 온전하게 이식하고 실전에 즉시 활용할 수 있도록 정리한 표준 지침서입니다.

---

## 📋 1. 시스템 구성 품목 요약

| 시스템 분류 | 포함 스크립트 | 포함 프리팹 및 에셋 | 핵심 특징 |
|---|---|---|---|
| **크로스 씬 트랜지션**<br>(11종) | `BaseTransition.cs`<br>`TransitionManager.cs`<br>개별 트랜지션 11종 스크립트 | `Assets/Prefabs/Transitions/`<br>(11종 `.prefab`) | 씬 A ↔ 씬 B 간 화면 덮기 ➡️ 백그라운드 비동기 로드 ➡️ 새 씬 위에서 열기 자동 총괄 (`DontDestroyOnLoad` 싱글톤) |
| **모듈형 버튼 인터랙션**<br>(10종 모션 + 4종 FX) | `UIButtonFeedback.cs`<br>FX 스크립트 5종 | `Assets/Prefabs/UI/`<br>(FX 프리팹 5종 및 스프라이트) | 기존 버튼 디자인/이벤트를 건드리지 않고 스크립트 컴포넌트 부착만으로 쫀득한 손맛 구현 |
| **주문 폭주 피버 모드** | `OrderRushOverlay.cs` | `Assets/Prefabs/UI/Sprites/`<br>(비네팅 테두리 스프라이트) | 화면 4면 테두리에 붉은 네온 펄스 점멸 (버튼 터치 100% 무방해 논블로킹 레이어) |

---

## 📦 2. Step 1 : 현재 프로젝트에서 패키지 추출하기 (Export)

새 프로젝트에 이미 에셋 스토어로 DOTween을 받으실 수 있으므로, **충돌 방지를 위해 아래 딱 2개 폴더만 추출**합니다.

1. 현재 프로젝트(`UISample`)의 **Project 창**에서 `Ctrl` 키를 누른 채 아래 2개 폴더를 다중 선택합니다:
   - **`Assets/Scripts`**
   - **`Assets/Prefabs`**
2. 선택한 폴더 위에서 마우스 우클릭 ➡️ **`Export Package...`** 클릭.
3. 팝업창에서 두 폴더 하위의 모든 파일에 체크가 되어 있는지 확인하고 **[Export...]** 버튼 클릭.
4. 파일 이름을 `Logistics_UI_System.unitypackage` 등으로 지정하여 원하는 경로에 저장합니다.

---

## 🚀 3. Step 2 : 새 프로젝트에 가져오기 및 초기 세팅 (Import & Setup)

가장 안전하고 오류가 발생하지 않는 표준 순서입니다:

### 1) DOTween 사전 설치 (필수)
1. 새 프로젝트의 Package Manager 또는 Asset Store에서 **`DOTween (HOTween v2)`** 에셋을 임포트합니다.
2. 유니티 상단 메뉴에서 **`Tools > Demigiant > DOTween Utility Panel`**을 엽니다.
3. 파란색 **`[Setup DOTween...]`** 버튼을 1회 클릭하여 모듈 설정을 완료합니다.

### 2) 추출한 패키지 임포트
1. 위 Step 1에서 저장해 둔 `Logistics_UI_System.unitypackage` 파일을 더블 클릭합니다.
2. 유니티 임포트 창에서 **[Import]** 버튼을 클릭합니다.
3. C# 컴파일 에러 0건으로 깔끔하게 설치가 완료됩니다.

### 3) Build Settings에 씬 등록 (씬 전환용)
1. 유니티 상단 메뉴: **`File > Build Settings...`** 열기.
2. 전환 대상이 될 씬들(예: `LobbyScene`, `StageScene`, `InGameScene` 등)을 **`Scenes In Build`** 목록에 드래그하여 등록합니다.

---

## 🎬 4. Step 3 : 트랜지션(화면 전환) 실전 사용법

새 프로젝트의 어떤 C# 스크립트에서든, 기존의 `SceneManager.LoadScene("다음씬")` 대신 **아래 딱 1줄**을 호출하시면 시네마틱 트랜지션이 발동됩니다!

```csharp
using LogisticsTycoon.Transitions; // 네임스페이스 추가

public class LobbyController : MonoBehaviour
{
    // 지도(스테이지) 선택 버튼에 연결
    public void OnClickStageMapButton()
    {
        // 🗺️ 물류 지도 펼침 & GPS 핀 쿵! 꽂힘 연출로 "StageScene" 이동
        TransitionManager.Instance.TransitionToScene("StageScene", TransitionType.WorldMapPin);
    }

    // 창고/상점 버튼에 연결
    public void OnClickShopButton()
    {
        // 📦 택배 박스 상하 날개 닫힘 & 테이핑 연출로 "ShopScene" 이동
        TransitionManager.Instance.TransitionToScene("ShopScene", TransitionType.BoxFlap);
    }

    // 출동/배송 버튼에 연결
    public void OnClickDispatchButton()
    {
        // 🚚 배송 트럭 주행 와이프 연출로 "InGameScene" 이동
        TransitionManager.Instance.TransitionToScene("InGameScene", TransitionType.TruckWipe);
    }
}
```

### 💡 사용 가능한 11종 트랜지션 타입 목록 (`TransitionType`)

| Enum 값 | 연출 명칭 | 추천 사용 상황 |
|---|---|---|
| **`TransitionType.WorldMapPin`** | **🗺️ 물류 배송 지도 & GPS 핀** | **스테이지 선택, 월드맵, 구역 이동** |
| **`TransitionType.BoxFlap`** | **📦 택배박스 닫힘 & 테이핑** | **상점, 보관함, 패키지 구매, 정산 완료** |
| **`TransitionType.TruckWipe`** | **🚚 배송 트럭 와이프** | **출발, 로비 ➡️ 인게임 진입, 배송 시작** |
| **`TransitionType.GearCircle`** | **⚙️ 톱니바퀴 180도 회전 원형** | **공장 가동, 본사 업그레이드, 시설 확장** |
| **`TransitionType.ForkliftLift`** | **🚜 지게차 팔레트 리프트** | **물류 창고 입고, 대량 자재 이동** |
| **`TransitionType.BarcodeScan`** | **📟 바코드 레이저 스캔** | **검수 완료, 송장 승인, 라이선스 획득** |
| **`TransitionType.BubbleWrap`** | **🫧 에어캡 뽁뽁이 팡팡** | **포장 완료, 캐주얼 보너스 스테이지** |
| **`TransitionType.ContainerDoor`** | **🚪 컨테이너 도어 쾅! 닫힘** | **수출/무역 진입, 보스/대형 계약 완료** |
| **`TransitionType.DeliveryDrone`** | **🛸 배송 드론 급강하 착륙** | **특급 배송, VIP 주문, 항공 물류** |
| **`TransitionType.ConveyorShutter`** | **🏭 컨베이어 셔터 슬랫** | **작업장 변경, 교대 근무 시작** |
| **`TransitionType.StampInvoice`** | **📑 송장 도장 쾅! 스탬프** | **계약 체결, 일일 퀘스트 완료** |
| **`TransitionType.RandomReturn`** | **🎲 복귀 5종 중 무작위** | **로비/본사로 되돌아올 때 (지루함 방지)** |

---

## 🕹️ 5. Step 4 : 모듈형 버튼 인터랙션(`UIButtonFeedback`) 사용법

새 프로젝트에 이미 만들어져 있는 어떤 UI 버튼이든 **디자인이나 기존 이벤트를 전혀 수정할 필요 없이** 컴포넌트 하나만 붙이시면 됩니다!

### 1) 기본 장착 방법 (10초 완료)
1. 계층(Hierarchy) 창에서 손맛을 넣고 싶은 **버튼 오브젝트를 클릭**합니다.
2. 인스펙터(Inspector) 창 맨 아래에서 **`Add Component` ➡️ `UIButtonFeedback`**을 추가합니다.
3. 인스펙터의 **`Motion Preset` 드롭다운**에서 원하는 손맛을 선택합니다. 끝!

### 2) 10대 시그니처 모션 프리셋 설명

| 프리셋 이름 | 연출 특징 | 추천 용도 |
|---|---|---|
| **`JellySquash`** | 누를 때 쫀득하게 납작 ➡️ 뗄 때 띠용~ 튀어오름 | 보상 받기, 일반 확인, 캐주얼 버튼 |
| **`DepthPress`** | 버튼이 -6px 아래로 쑥 내려가는 실제 3D 물리 스위치 감각 | 메인 메뉴, 시설 가동 스위치 |
| **`TruckRevUp` ⭐** | 제자리에서 틸트되며 부르릉~ 들썩들썩 엔진 진동 (인접 UI 무침범) | **상차 완료, 배송 보내기, 출발 버튼** |
| **`StampHit`** | 살짝 들렸다가 아래로 쾅! 찍히며 회전/스케일 충격파 | 계약 확정, 송장 승인, 결제 버튼 |
| **`ConveyorSnap`** | 화물처럼 우측으로 밀렸다가 찰칵 맞물려 복귀 | 컨베이어 조작, 기계 레버 버튼 |
| **`BoxJolt`** | 방지턱 넘듯 퐁 튀며 좌우로 뒤뚱뒤뚱 덜컹거림 | 택배 상자 열기, 창고 분류 버튼 |
| **`BarcodeLaser`** | 붉은 레이저가 스윽 지나가고 에메랄드 그린 플래시 | 스캔, 조회, 검수 확인 버튼 |
| **`TapeStripWipe`** | 크라프트 테이프가 좌 ➡️ 우로 '쫘악' 발리며 밀착 | 포장 완료, 봉인 버튼 |
| **`NeonPulse`** | 테두리 섬광 알파 페이드 + 1.08f 스케일 펄스 | 특수 상점, VIP 기능, 가챠 버튼 |
| **`GentlePop`** | 0.94f 압축 ➡️ 0.12초 만에 단정하게 원복 | 닫기(X), 취소, 서브 메뉴 탭 버튼 |

---

### 💡 3) 동적 추가 FX (물결 리플, 배기 연기 등) 연결하는 법

버튼에 시각 효과를 추가하고 싶을 때는 `UIButtonFeedback` 인스펙터의 체크박스를 켜고 프리팹을 연결해 주시면 됩니다:

1. **마우스 찍은 위치에 하얀 물결 파동을 보고 싶을 때**:
   - `Enable Ripple` 체크 ➡️ `Ripple Prefab` 슬롯에 **`Assets/Prefabs/UI/RippleEffect.prefab`** 드래그 연결.
2. **배송 출발 시 트럭 뒤에서 뽀얀 배기 연기(💨)가 퐁퐁 나오게 할 때**:
   - `Motion Preset`을 **`TruckRevUp`**으로 설정 ➡️ `Exhaust Smoke Prefab` 슬롯에 **`Assets/Prefabs/UI/ExhaustSmokePuff.prefab`** 드래그 연결.
3. **클릭 시 "+100 EXP" 같은 글자가 퐁 떠오르게 할 때**:
   - `Enable Floating Text` 체크 ➡️ `Floating Text Prefab` 슬롯에 **`Assets/Prefabs/UI/FloatingText.prefab`** 드래그 연결.

---

## 🚨 6. Step 5 : '주문 폭주(Order Rush)' 피버 모드 적용법

화면 가장자리에 긴장감 넘치는 붉은 네온 펄스 라인을 띄우는 방법입니다:

1. 씬의 메인 UI Canvas 아래에 **빈 게임오브젝트** 생성 ➡️ 이름을 `OrderRushOverlay`로 변경.
2. **`Add Component ➡️ OrderRushOverlay`** 부착.
3. RectTransform을 **전체 화면 스트레칭(Alt 누르고 맨 우측 하단 Stretch-Stretch 클릭)**으로 설정.
4. 어떤 버튼에서든 아래 코드로 피버 모드를 켜고 끌 수 있습니다:
   ```csharp
   // 피버 모드 켜기
   orderRushOverlay.Show();

   // 피버 모드 끄기 (부드러운 페이드아웃)
   orderRushOverlay.Hide();

   // 원클릭 토글
   orderRushOverlay.Toggle();
   ```
   *(외곽 라인이 번쩍이는 동안에도 `blocksRaycasts = false` 설정이 되어 있어 화면 내 모든 버튼 조작에 일절 방해를 주지 않습니다!)*

---

## 🛠️ 7. 문제 해결 및 FAQ (Troubleshooting)

### Q1. 새 프로젝트에 패키지를 넣었는데 `DOTween` 빨간 에러가 뜹니다!
- **해결**: 새 프로젝트 상단 메뉴 **`Tools > Demigiant > DOTween Utility Panel`**을 열고 **`[Setup DOTween...]`** 버튼을 1회 눌러주시면 즉시 정상 컴파일됩니다.

### Q2. 씬을 넘길 때 `Scene 'XXX' couldn't be loaded because it has not been added to the build settings` 에러가 납니다!
- **해결**: 유니티 상단 **`File > Build Settings`**를 열고, 이동할 씬 파일(`.unity`)을 상단 **`Scenes In Build`** 창으로 드래그해 넣으시면 해결됩니다.

### Q3. 버튼에 3D 뎁스(DepthPress)를 넣었는데 그림자가 안 움직입니다.
- **해결**: `DepthPress`는 버튼 본체만 있어도 알아서 Y축으로 쑥 내려가지만, 버튼 뒤에 어두운 색상의 그림자 오브젝트(Shadow Image)가 있다면 `UIButtonFeedback` 인스펙터의 **`Shadow Transform` 슬롯**에 드래그해 주시면 그림자가 줄어드는 깊이감이 2배로 살아납니다!
