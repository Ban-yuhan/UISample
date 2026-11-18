# 씬 전환(Transition) 리소스 제작 요청서 — 이미지 생성 프롬프트

씬 전환 연출 14종에 쓸 이미지 리소스입니다.
각 항목의 **프롬프트를 이미지 생성 에이전트에 그대로 넣으면** 되도록 작성했습니다.

---

## 0. 사용법

1. 각 프롬프트 앞에 **[공통 스타일]**을 붙입니다.
2. 각 프롬프트 뒤에 그 항목의 배경 타입에 맞는 **[배경 A], [배경 B], [배경 C] 중 하나**를 붙입니다.
3. 생성된 이미지를 표의 **최종 크기**로 줄이고 **파일명**대로 저장합니다.

### 배경 규칙

투명 배경으로 바로 뽑을 수 없으면, 지우기 쉽게 **단색 배경**으로 뽑습니다.

| 타입 | 배경 | 대상 | 지우는 법 |
|---|---|---|---|
| **A** | 순수 검정 `#000000` | 흰색 실루엣 (색은 나중에 입힘) | 밝기를 그대로 알파로 쓰면 됨 |
| **B** | 순수 마젠타 `#FF00FF` | 컬러 일러스트 | 마젠타만 선택해서 삭제 |
| **B'** | 순수 그린 `#00FF00` | 빨강·분홍 계열이 들어간 컬러 일러스트 | 그린만 선택해서 삭제 |
| **C** | 없음 (캔버스 꽉 채움) | 반복 타일 텍스처, 전체 화면 이미지 | 지울 것 없음 |

### [공통 스타일]

```
Clean flat 2D vector game art for a cozy, warm casual logistics tycoon mobile game. Bold simple shapes, smooth anti-aliased edges, subtle soft cel shading (one highlight tone and one shadow tone only). Warm palette: cream, kraft brown, mustard yellow, truck orange, soft navy. No text, no letters, no numbers, no logos, no watermark, no signature. Orthographic view, no perspective distortion unless stated.
```

### [배경 A] 흰색 실루엣

```
Render the subject as a single pure white (#FFFFFF) silhouette with soft anti-aliased edges, on a perfectly flat solid pure black (#000000) background. No gray tones except at the anti-aliased edges, no gradients, no glow, no shadow. Subject centered with at least 6% empty margin on every side.
```

### [배경 B] 컬러 일러스트

```
Place the subject on a perfectly flat solid pure magenta (#FF00FF) background. No gradient, no vignette, no floor, no cast shadow, no reflection on the background. Do not use any magenta or pink inside the subject. Subject centered with at least 6% empty margin on every side and must not touch the edges.
```

> **[배경 B']**: 위 문장에서 `pure magenta (#FF00FF)`를 `pure green (#00FF00)`로, `magenta or pink`를 `bright green`으로 바꿔서 씁니다.

### [배경 C] 타일 텍스처

```
Full-bleed texture filling the entire canvas edge to edge, no border, no background, no vignette. Seamlessly tileable: the left edge must continue perfectly into the right edge (and top into bottom where stated). Even lighting, no directional shadow across the image.
```

---

## 1. 흰색 심볼 — 배경 A

색은 나중에 입히므로 **전부 흰색 실루엣**으로 받습니다.

### 1-1. 톱니바퀴 — `tr_gear_12.png`

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 1:1 | 1024 × 1024 | 톱니 트랜지션 2종의 주인공. 화면 중앙에서 회전 (화면상 약 420px) |

```
A single industrial mechanical gear, front view, perfectly circular and symmetric. Exactly 12 evenly spaced chunky trapezoid teeth with slightly rounded corners. A round hole in the exact center about 26% of the gear's total diameter. A thin inner ring groove between the teeth and the center hole. Flat and graphic, no bevel, no 3D.
```

### 1-2. 도장 테두리 — `tr_stamp_frame.png`

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 3:1 | 1536 × 512 | 송장 합격 도장, 영수증 PAID 도장, 취급주의 검수 도장 (3곳 공용). 글자는 위에 따로 올림 |

```
An empty rubber ink stamp frame: a wide horizontal rounded rectangle made of a thick outer border and a thin inner border with a small gap between them. Rough grunge ink texture: slightly uneven edges, small speckles, a few faint broken gaps where the ink did not press fully. The inside of the frame is completely empty. Slightly imperfect hand-stamped feel but still a clean rectangle shape.
```

### 1-3. 우표 톱니 테두리 — `tr_postage_frame.png`

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 7:6 | 700 × 600 | 송장 오른쪽 위 우표 (화면상 105 × 90) |

```
A postage stamp shape: a solid rectangle whose four edges are perforated with evenly spaced small semicircular notches, like a real tear-off postage stamp. Inside it, a second thin rectangular border line inset from the edge. The center is solid (filled). Perforation notches evenly spaced and identical on all sides.
```

### 1-4. 말풍선 — `tr_label_bubble.png`

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 4:1 | 1024 × 256 | 지도 핀 위에 뜨는 목적지 이름표 (화면상 340 × 60) |

```
A wide horizontal speech bubble: a pill-shaped rounded rectangle with fully rounded ends, and one small triangular tail pointing straight down from the exact bottom center. Solid filled. The long middle section must be perfectly straight and uniform so it can be stretched horizontally.
```

### 1-5. 작은 아이콘 5종 — `tr_icon_[이름].png`

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 1:1 | 256 × 256 (각각) | 전환 화면 안내 문구 앞에 붙는 아이콘 (화면상 약 36px) |

모든 아이콘은 같은 크기감, 같은 선 굵기로 맞춥니다. 내부 디테일은 투명 구멍(검정)으로 표현합니다.

| 파일명 | 프롬프트 |
|---|---|
| `tr_icon_forklift.png` | `A simple forklift icon, side view facing right: body with driver cage, vertical mast at the front, forks pointing right, two wheels shown as holes.` |
| `tr_icon_bolt.png` | `A simple bold lightning bolt icon, slightly tilted, sharp zigzag.` |
| `tr_icon_warning.png` | `A warning sign icon: a rounded-corner triangle with an exclamation mark cut out as a hole in the middle.` |
| `tr_icon_check.png` | `A bold check mark icon with rounded line ends.` |
| `tr_icon_scissors.png` | `A simple scissors icon, side view, blades slightly open pointing right, two round finger holes cut out.` |

각 프롬프트 끝에 아래 문장을 붙입니다.
```
Icon fills a centered square safe area of about 75% of the canvas. Uniform thick strokes, minimal detail.
```

---

## 2. 반복 타일 텍스처 — 배경 C

화면 전체를 덮는 판에 반복해서 깝니다. **이음매가 보이면 안 됩니다.**

### 2-1. 골판지 — `tr_tex_cardboard.png`

| 생성 비율 | 최종 크기 | 반복 방향 | 쓰는 곳 |
|---|---|---|---|
| 1:1 | 1024 × 1024 | 가로·세로 | 박스 플랩, 취급주의 테이프 배경 판, 지게차 카톤 박스 (3곳 공용) |

```
Kraft corrugated cardboard surface texture, flat top-down view. Warm light kraft brown (#C8915A range). Very subtle horizontal corrugation ridges faintly visible through the paper, fine paper fiber grain, a few tiny darker specks. Low contrast so text and tape can sit on top. Tileable in both directions.
```

### 2-2. 컨테이너 철문 — `tr_tex_container_steel.png`

| 생성 비율 | 최종 크기 | 반복 방향 | 쓰는 곳 |
|---|---|---|---|
| 1:1 | 1024 × 1024 | 가로·세로 | 컨테이너 문 양쪽 판 (각각 화면 절반) |

```
Painted shipping container corrugated steel wall texture, front view. Ocean blue paint (#2E70B8 range). Evenly spaced vertical trapezoid ribs running top to bottom, exactly 4 ribs across the canvas, each with a light highlight edge on the left and a darker shadow edge on the right. Very light paint wear and tiny scratches, no rust, no stickers. Tileable in both directions.
```

### 2-3. 롤 셔터 슬랫 — `tr_tex_shutter_slat.png`

| 생성 비율 | 최종 크기 | 반복 방향 | 쓰는 곳 |
|---|---|---|---|
| 4:1 | 1024 × 256 | 가로·세로 | 창고 롤 셔터. 위에서 떨어져 화면을 덮음 (슬랫 1개 화면상 약 108px 높이) |

```
Warehouse steel roller shutter texture, front view, made of exactly 2 identical horizontal slats stacked vertically, each slat half the image height. Dark charcoal steel (#3A3F47 range). Each slat has a thin bright embossed highlight line along its top edge and a thin dark shadow groove along its bottom edge. Subtle brushed metal grain running horizontally. Tileable horizontally and vertically.
```

### 2-4. 경고 사선 띠 — `tr_tex_hazard_stripe.png`

| 생성 비율 | 최종 크기 | 반복 방향 | 쓰는 곳 |
|---|---|---|---|
| 8:1 | 1024 × 128 | 가로 | 셔터 하단 바, 지게차 바닥 주행선 (2곳 공용) |

```
Industrial hazard warning stripe band: alternating diagonal stripes at 45 degrees, mustard yellow (#F2C230) and near-black (#22201E), equal stripe widths, exactly 8 yellow and 8 black stripes across the width. Very light worn paint texture. Tileable horizontally.
```

### 2-5. 바코드 — `tr_tex_barcode.png`

| 생성 비율 | 최종 크기 | 반복 방향 | 쓰는 곳 |
|---|---|---|---|
| 4:1 | 1024 × 256 | 가로 | 바코드 스캔 블라인드, 송장 바코드, 영수증 바코드 (3곳 공용) |

```
Barcode pattern band: vertical black bars of varying widths (thin, medium, thick) with varying gaps, like a real product barcode, on a plain white background. Bars run the full height of the image. Crisp, no numbers underneath. Tileable horizontally.
```

### 2-6. 종이 — `tr_tex_paper.png`

| 생성 비율 | 최종 크기 | 반복 방향 | 쓰는 곳 |
|---|---|---|---|
| 1:1 | 1024 × 1024 | 가로·세로 | 송장 용지, 영수증 용지 |

```
Plain warm cream paper texture (#FBF6EA range), flat top-down view. Very subtle paper fiber grain and soft mottling, extremely low contrast. No folds, no stains, no lines. Tileable in both directions.
```

### 2-7. 취급주의 테이프 — `tr_tex_fragile_tape.png`

| 생성 비율 | 최종 크기 | 반복 방향 | 쓰는 곳 |
|---|---|---|---|
| 6:1 | 1536 × 256 | 가로 | 취급주의 패킹 테이프. 화면을 가로지르며 붙음. "FRAGILE" 글자는 나중에 따로 올림 |

```
A strip of red "fragile" packing tape seen flat from above, filling the full height. Warm red tape (#D64534 range) with a slightly glossy plastic feel and faint horizontal stretch lines. Repeating white symbols spaced evenly along the strip: a wine glass, an umbrella, and a pair of upward arrows, then repeating. Leave a clear empty band in the middle third of the height with no symbols (text will be placed there later). Thin white border lines along the top and bottom edges. Tileable horizontally.
```

---

## 3. 컬러 단품 — 배경 B / B'

### 3-1. 지도 핀 — `tr_map_pin.png` (배경 B')

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 5:7 | 500 × 700 | 지도 위에 위에서 꽂히는 GPS 핀 (화면상 100 × 140) |

```
A classic map location pin, straight front view, standing upright. A glossy red round pin head (#E04A3A) with a small white circle in its center, tapering into a point at the bottom (teardrop map marker shape). Simple soft highlight on the upper left of the head. Nothing else.
```

### 3-2. 경광등 — `tr_beacon.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 1:1 | 512 × 512 | 셔터 중앙 배지, 지게차 지붕 위 경광등 (화면상 약 46px) |

```
An amber rotating warning beacon light, front view: a semi-transparent-looking amber orange dome (#FF9A2E) with a brighter inner glow core, sitting on a short dark gray cylindrical base. Simple stylized, symmetric.
```

### 3-3. 영수증 뜯긴 끝 — `tr_receipt_tear_edge.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 8:1 | 1024 × 128 | 영수증 용지 맨 아래 지그재그 절단면 (영수증 폭 화면상 920px) |

```
A horizontal strip of cream receipt paper (#FBF6EA) seen flat from above. The top edge is perfectly straight. The bottom edge is a regular sharp zigzag (sawtooth) tear, exactly 24 evenly sized triangle teeth across the width. Flat, no shadow. The zigzag must repeat evenly so the strip can be tiled horizontally.
```

### 3-4. 택배 상자 — `tr_parcel_box.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 1:1 | 512 × 512 | 별똥별 머리에 달린 택배 상자 (화면상 약 60~90px) |

```
A cute small cardboard parcel box in three-quarter view, kraft brown with a strip of light tan packing tape across the top and down the front. Chunky rounded proportions, slightly toy-like. Simple soft shading.
```

### 3-5. 지도 전체 — `tr_map_sheet.png` (배경 C)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 16:9 | 1920 × 1080 | 3단 접이식 지도가 펼쳐지며 화면 전체를 덮음 |

```
A full-screen stylized paper logistics region map, flat top-down view, filling the whole canvas. Warm cream paper base with soft muted color areas: pale green parks, pale blue river winding across, light beige city blocks in a loose grid. Thin light gray secondary roads. Two subtle vertical fold creases dividing the map into three equal vertical panels, each crease shown as a soft darker line with a faint highlight next to it. Calm and low contrast, no text, no labels, no icons, no pins.
```

---

## 4. 일러스트 부품 — 배경 B / B'

움직이는 부분(바퀴, 포크, 마스트, 경광등)은 **따로 뽑습니다.**
트럭과 지게차 모두 **측면, 오른쪽을 향하게** 그립니다.

### 4-1. 배송 트럭 몸체 — `tr_truck_body.png` (배경 B')

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 5:3 | 1500 × 900 | 화면을 원을 그리며 가로질러 달리는 트럭 (화면상 300 × 180) |

```
A cute chunky delivery truck, exact side view facing right. A large boxy cargo container at the back in warm red-orange (#E85A3D) with rounded corners, and a shorter rounded cab at the front in the same color with a light blue window. A small round blank emblem circle on the side of the cargo box (empty, no logo). IMPORTANT: draw the truck WITHOUT wheels — leave two empty semicircular wheel arches at the bottom (filled with background color), one under the cab and one under the cargo box.
```

### 4-2. 바퀴 — `tr_wheel.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 1:1 | 512 × 512 | 트럭 바퀴, 지게차 앞·뒤 바퀴 (공용, 회전함) |

```
A single vehicle wheel, exact front-on circular view, perfectly round and centered. Dark charcoal rubber tire (#2B2A2E) with a few short tread notches around the outer edge, and a light gray metal rim hub in the center about 50% of the diameter with 5 small round bolt dots. Rotationally symmetric so it looks correct when spun.
```

### 4-3. 지게차 몸체 — `tr_forklift_body.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 3:2 | 1500 × 1000 | 화면 왼쪽에서 달려와 팔레트를 관통하는 지게차 (화면상 약 900 × 500) |

```
A cute chunky warehouse forklift body, exact side view facing right. Mustard yellow chassis (#F2B630), a heavy black counterweight at the rear with one diagonal yellow stripe, a black steel tube roll cage over the driver area with a simple seat inside. IMPORTANT: draw it WITHOUT wheels (leave empty wheel arches), WITHOUT the front mast, WITHOUT forks, and WITHOUT any light on the roof. The front (right side) of the body ends flat where the mast will attach.
```

### 4-4. 지게차 마스트 — `tr_forklift_mast.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 1:4 | 256 × 1024 | 지게차 앞 세로 기둥 (화면상 44 × 400) |

```
A forklift vertical mast, exact side view: two parallel tall steel rails in dark gunmetal gray joined by three short horizontal crossbars, with a thin silver hydraulic cylinder running up the middle. Tall and narrow, perfectly vertical.
```

### 4-5. 지게차 포크 — `tr_forklift_forks.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 4:1 | 1024 × 256 | 앞으로 뻗은 포크 날 (화면상 280 × 22) |

```
A forklift fork blade, exact side view: a long flat silver steel bar pointing right, with a short vertical heel section on the left end forming an L shape. The tip on the right is slightly tapered. Clean metallic gray with one light highlight line along the top.
```

### 4-6. 나무 팔레트 앞판 — `tr_pallet_front.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 15:2 | 1500 × 200 | 지게차 포크가 뚫고 들어가는 팔레트의 앞면 (화면상 420 × 56) |

```
A wooden shipping pallet seen exactly from the front (end-on view): one horizontal top deck board, one horizontal bottom deck board, and three vertical wooden blocks connecting them (left, center, right). Between the blocks are two rectangular fork pocket openings — these openings must be EMPTY and filled with the background color. Warm light wood (#C99A5E) with simple wood grain lines.
```

### 4-7. 나무 팔레트 뒷면 그림자 — `tr_pallet_back.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 15:2 | 1500 × 200 | 팔레트 앞판 뒤에 깔리는 어두운 안쪽 (포크 구멍 너머로 보임) |

```
The same wooden pallet shape as a front view, but representing the dark shadowed interior behind it: a solid rectangle of very dark brown (#2E2014) with two slightly darker rectangular recesses where the fork pockets are. Flat, no wood grain, no highlights.
```

> 4-6과 **같은 비율, 같은 위치**로 겹쳐야 합니다. 4-6을 먼저 뽑고, 그 이미지를 참고 이미지로 넣어 같은 구도로 뽑으세요.

### 4-8. 카톤 박스 — `tr_carton_box.png` (배경 B)

| 생성 비율 | 최종 크기 | 쓰는 곳 |
|---|---|---|
| 8:5 | 1024 × 640 | 팔레트 위에 실린 큰 택배 박스 (화면상 390 × 250) |

```
A large cardboard shipping carton, exact front view (flat, no perspective). Kraft brown with a vertical strip of light tan packing tape down the center of the top flap, and a blank white rectangular shipping label area on the front lower right (empty, no text). Simple soft shading, slightly rounded corners.
```

---

## 5. 다른 요청서에서 재사용 (이 요청서에서 생략)

| 리소스 | 쓰는 곳 | 출처 |
|---|---|---|
| 박스 테이프 띠 | 박스 플랩 트랜지션의 테이핑 | ButtonScene 요청서 `fx_tape_strip.png` |
| 레이저 빔 | 바코드 스캔 트랜지션의 스캔선 | ButtonScene 요청서 `fx_laser_beam.png` |
| 반짝이 | 별똥별 주변 별 | ButtonScene 요청서 `fx_sparkle_star.png` |

---

## 6. 납품 목록 요약

| # | 파일명 | 최종 크기 | 배경 | 비고 |
|---|---|---|---|---|
| 1 | `tr_gear_12.png` | 1024 × 1024 | A | **최우선** (지금은 ⚙ 글자로 대체 중) |
| 2 | `tr_stamp_frame.png` | 1536 × 512 | A | 3곳 공용 |
| 3 | `tr_postage_frame.png` | 700 × 600 | A | |
| 4 | `tr_label_bubble.png` | 1024 × 256 | A | |
| 5 | `tr_icon_*.png` (5종) | 256 × 256 | A | |
| 6 | `tr_tex_cardboard.png` | 1024 × 1024 | C | 3곳 공용 |
| 7 | `tr_tex_container_steel.png` | 1024 × 1024 | C | |
| 8 | `tr_tex_shutter_slat.png` | 1024 × 256 | C | |
| 9 | `tr_tex_hazard_stripe.png` | 1024 × 128 | C | 2곳 공용 |
| 10 | `tr_tex_barcode.png` | 1024 × 256 | C | 3곳 공용 |
| 11 | `tr_tex_paper.png` | 1024 × 1024 | C | |
| 12 | `tr_tex_fragile_tape.png` | 1536 × 256 | C | |
| 13 | `tr_map_pin.png` | 500 × 700 | B' | **최우선** (지금은 📍 글자로 대체 중) |
| 14 | `tr_beacon.png` | 512 × 512 | B | |
| 15 | `tr_receipt_tear_edge.png` | 1024 × 128 | B | |
| 16 | `tr_parcel_box.png` | 512 × 512 | B | **최우선** (지금은 📦 글자로 대체 중) |
| 17 | `tr_map_sheet.png` | 1920 × 1080 | C | |
| 18 | `tr_truck_body.png` | 1500 × 900 | B' | 바퀴 없이 |
| 19 | `tr_wheel.png` | 512 × 512 | B | 트럭·지게차 공용 |
| 20 | `tr_forklift_body.png` | 1500 × 1000 | B | 바퀴·마스트·포크·경광등 없이 |
| 21 | `tr_forklift_mast.png` | 256 × 1024 | B | |
| 22 | `tr_forklift_forks.png` | 1024 × 256 | B | |
| 23 | `tr_pallet_front.png` | 1500 × 200 | B | 포크 구멍은 배경색 |
| 24 | `tr_pallet_back.png` | 1500 × 200 | B | 23과 같은 구도 |
| 25 | `tr_carton_box.png` | 1024 × 640 | B | |

## 7. 체크리스트 (생성 후 확인)

- [ ] 배경이 **완전히 균일한 단색**인가? (그라데이션, 바닥 그림자, 비네트 없음)
- [ ] 피사체 안에 배경색(마젠타 / 그린)과 비슷한 색이 섞이지 않았는가?
- [ ] 배경 A 항목이 **흰색 하나**로만 되어 있는가? (회색 명암 없음)
- [ ] 글자, 숫자, 로고가 하나도 들어가지 않았는가? (AI가 자주 넣으니 꼭 확인)
- [ ] 타일 텍스처를 2×2로 붙여 봤을 때 이음매가 안 보이는가?
- [ ] 톱니가 정확히 12개이고, 바퀴가 돌려도 어색하지 않게 대칭인가?
- [ ] 트럭·지게차 몸체에 바퀴, 마스트, 포크가 **안 그려져** 있는가?
- [ ] 트럭·지게차가 모두 **오른쪽**을 향하는가?
- [ ] 팔레트 앞판의 포크 구멍 2개가 배경색으로 뚫려 있는가?
