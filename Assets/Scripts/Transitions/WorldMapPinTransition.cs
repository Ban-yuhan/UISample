using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 물류 지도 펼침 & GPS 핀(WorldMapPin) 트랜지션
    /// In (0.45s): 3단 접이식 물류 배송 지도가 좌우에서 촥! 촥! 펼쳐지며 화면 전체 밀폐 (OutCubic)
    /// Midpoint: 하늘에서 붉은색 GPS 위치 마커 핀(📍)이 쿵! 통통~ 꽂힘 + 레이더 펄스 파동 + 말풍선 라벨 팝업
    /// Out (0.40s): 핀을 중심으로 지도가 줌인(1.25배)되며 좌우로 젖혀져 씬 B 오픈 (InCubic)
    /// </summary>
    public class WorldMapPinTransition : BaseTransition
    {
        [Header("Map Sheets (3-Fold Map)")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private RectTransform mapSheet;
        [SerializeField] private RectTransform mapFoldLeft;
        [SerializeField] private RectTransform mapFoldRight;
        [SerializeField] private RectTransform mapFoldCenter;
        [SerializeField] private CanvasGroup mapCanvasGroup;

        [Header("GPS Pin & Radar Components")]
        [SerializeField] private RectTransform pinRoot;
        [SerializeField] private RectTransform pinVisual;
        [SerializeField] private Image pinShadow;
        [SerializeField] private RectTransform[] radarRings;
        [SerializeField] private RectTransform labelBubble;
        [SerializeField] private Text labelBubbleText;

        [Header("Decorations (Delivery Routes & Grid)")]
        [SerializeField] private CanvasGroup gridGroup;
        [SerializeField] private CanvasGroup routeLinesGroup;

        [Header("Timings")]
        [SerializeField] private float unfoldDuration = 0.45f;
        [SerializeField] private float zoomOutDuration = 0.40f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence waitSequence;
        private Sequence radarLoopSequence;
        private Tween pinFloatTween;

        private const float FoldSlideOffset = 640f;

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            waitSequence?.Kill();
            radarLoopSequence?.Kill();
            pinFloatTween?.Kill();
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (container != null)
            {
                container.anchorMin = Vector2.zero;
                container.anchorMax = Vector2.one;
                container.offsetMin = Vector2.zero;
                container.offsetMax = Vector2.zero;
                container.localScale = Vector3.one;
            }

            if (mapContainer != null)
            {
                mapContainer.localScale = Vector3.one;
                mapContainer.anchoredPosition = Vector2.zero;
            }

            if (mapSheet != null)
            {
                mapSheet.gameObject.SetActive(false);
            }

            if (mapCanvasGroup != null)
            {
                mapCanvasGroup.alpha = 1f;
            }

            if (mapFoldLeft != null)
            {
                mapFoldLeft.anchoredPosition = new Vector2(-FoldSlideOffset, 0);
                mapFoldLeft.localScale = new Vector3(0.2f, 1f, 1f);
            }

            if (mapFoldRight != null)
            {
                mapFoldRight.anchoredPosition = new Vector2(FoldSlideOffset, 0);
                mapFoldRight.localScale = new Vector3(0.2f, 1f, 1f);
            }

            if (mapFoldCenter != null)
            {
                mapFoldCenter.localScale = new Vector3(0.85f, 0.85f, 1f);
            }

            if (gridGroup != null)
            {
                gridGroup.alpha = 0f;
            }

            if (routeLinesGroup != null)
            {
                routeLinesGroup.alpha = 0f;
            }

            if (pinRoot != null)
            {
                pinRoot.gameObject.SetActive(false);
                pinRoot.localScale = Vector3.one;
            }

            if (pinVisual != null)
            {
                pinVisual.anchoredPosition = new Vector2(0, 420);
                pinVisual.localScale = new Vector3(1.4f, 1.4f, 1f);
            }

            if (pinShadow != null)
            {
                pinShadow.color = new Color(0, 0, 0, 0);
                pinShadow.transform.localScale = Vector3.zero;
            }

            if (radarRings != null)
            {
                foreach (var ring in radarRings)
                {
                    if (ring != null)
                    {
                        ring.localScale = Vector3.zero;
                        var img = ring.GetComponent<Image>();
                        if (img != null)
                        {
                            var c = img.color;
                            c.a = 0;
                            img.color = c;
                        }
                    }
                }
            }

            if (labelBubble != null)
            {
                labelBubble.localScale = Vector3.zero;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }

            IsPlaying = false;
        }

        public override void PlayIn(Action onInComplete = null)
        {
            if (IsPlaying) return;
            IsPlaying = true;

            KillAllTweens();

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            if (mapSheet != null)
                mapSheet.gameObject.SetActive(true);

            // 초기 위치 리셋
            if (mapContainer != null)
                mapContainer.localScale = Vector3.one;

            if (mapFoldLeft != null)
            {
                mapFoldLeft.anchoredPosition = new Vector2(-FoldSlideOffset, 0);
                mapFoldLeft.localScale = new Vector3(0.15f, 1f, 1f);
            }

            if (mapFoldRight != null)
            {
                mapFoldRight.anchoredPosition = new Vector2(FoldSlideOffset, 0);
                mapFoldRight.localScale = new Vector3(0.15f, 1f, 1f);
            }

            if (mapFoldCenter != null)
            {
                mapFoldCenter.localScale = new Vector3(0.85f, 0.85f, 1f);
            }

            if (gridGroup != null) gridGroup.alpha = 0f;
            if (routeLinesGroup != null) routeLinesGroup.alpha = 0f;
            if (pinRoot != null) pinRoot.gameObject.SetActive(false);

            inSequence = DOTween.Sequence();

            // 1. 좌우 및 중앙 날개가 촥! 촥! 펼쳐지며 전면 밀폐 (0.45s, Ease.OutCubic)
            if (mapFoldCenter != null)
            {
                inSequence.Join(mapFoldCenter.DOScale(Vector3.one, unfoldDuration * 0.7f).SetEase(Ease.OutCubic));
            }

            if (mapFoldLeft != null)
            {
                inSequence.Join(mapFoldLeft.DOAnchorPosX(0f, unfoldDuration).SetEase(Ease.OutCubic));
                inSequence.Join(mapFoldLeft.DOScaleX(1f, unfoldDuration).SetEase(Ease.OutCubic));
            }

            if (mapFoldRight != null)
            {
                inSequence.Join(mapFoldRight.DOAnchorPosX(0f, unfoldDuration).SetEase(Ease.OutCubic));
                inSequence.Join(mapFoldRight.DOScaleX(1f, unfoldDuration).SetEase(Ease.OutCubic));
            }

            // 지도 격자 및 배송 노선이 선명하게 나타남
            if (gridGroup != null)
            {
                inSequence.Join(gridGroup.DOFade(1f, unfoldDuration * 0.8f).SetEase(Ease.OutQuad));
            }

            if (routeLinesGroup != null)
            {
                inSequence.Join(routeLinesGroup.DOFade(1f, unfoldDuration).SetEase(Ease.OutQuad));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            if (pinRoot == null) return;

            pinRoot.gameObject.SetActive(true);
            waitSequence?.Kill();
            radarLoopSequence?.Kill();
            pinFloatTween?.Kill();

            waitSequence = DOTween.Sequence();

            // 1. 하늘 상공에서 빨간색 GPS 핀이 시원하고 빠르게 쿵! 낙하 (0.15s, InQuad)
            if (pinVisual != null)
            {
                pinVisual.anchoredPosition = new Vector2(0, 380);
                pinVisual.localScale = new Vector3(1.35f, 1.35f, 1f);

                waitSequence.Append(pinVisual.DOAnchorPosY(0f, 0.15f).SetEase(Ease.InQuad));
                waitSequence.Join(pinVisual.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad));

                // 2. 바닥 꽂힘 찰진 바운스 (0.18s)
                waitSequence.Append(pinVisual.DOPunchPosition(new Vector3(0, 16, 0), 0.18f, 8, 0.5f));
            }

            // 바닥 그림자 확장
            if (pinShadow != null)
            {
                pinShadow.color = new Color(0, 0, 0, 0);
                pinShadow.transform.localScale = Vector3.zero;

                waitSequence.Insert(0.08f, pinShadow.DOColor(new Color(0, 0, 0, 0.45f), 0.10f));
                waitSequence.Insert(0.08f, pinShadow.transform.DOScale(Vector3.one, 0.10f).SetEase(Ease.OutBack));
            }

            // 3. "스테이지 B 물류 허브" 미니 말풍선 퐁! 솟아오름 (0.16s, OutBack)
            if (labelBubble != null)
            {
                labelBubble.localScale = Vector3.zero;
                waitSequence.Insert(0.16f, labelBubble.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack));
            }

            // 4. 바닥 레이더 링 파동 무한 루프 시작
            waitSequence.OnComplete(() =>
            {
                // 착지 완료 위치 보장
                if (pinVisual != null)
                {
                    pinVisual.anchoredPosition = Vector2.zero;
                    pinVisual.localScale = Vector3.one;
                }

                StartRadarWaveLoop();

                // 핀 미세 호흡 유휴 트윈
                if (pinVisual != null)
                {
                    pinFloatTween = pinVisual.DOAnchorPosY(5f, 0.55f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                }
            });
        }

        private void StartRadarWaveLoop()
        {
            if (radarRings == null || radarRings.Length == 0) return;

            radarLoopSequence?.Kill();
            radarLoopSequence = DOTween.Sequence();

            float ringDuration = 0.95f;
            float interval = 0.30f;

            for (int i = 0; i < radarRings.Length; i++)
            {
                var ring = radarRings[i];
                if (ring == null) continue;

                var img = ring.GetComponent<Image>();
                if (img == null) continue;

                float startTime = i * interval;

                // 시퀀스 내부에 중첩 무한루프 대신, 단일 시퀀스 루프로 깔끔하게 처리
                radarLoopSequence.InsertCallback(startTime, () =>
                {
                    if (ring != null)
                    {
                        ring.localScale = Vector3.zero;
                        if (img != null)
                        {
                            Color c = img.color;
                            c.a = 0.85f;
                            img.color = c;
                        }
                    }
                });

                radarLoopSequence.Insert(startTime, ring.DOScale(2.8f, ringDuration).SetEase(Ease.OutCubic));
                radarLoopSequence.Insert(startTime, img.DOFade(0f, ringDuration).SetEase(Ease.InQuad));
            }

            radarLoopSequence.SetLoops(-1, LoopType.Restart);
        }

        public override void StopWaitingLoop()
        {
            waitSequence?.Kill();
            radarLoopSequence?.Kill();
            pinFloatTween?.Kill();

            // 중단되더라도 핀이 공중에 떠 있지 않고 반드시 바닥에 착지하도록 강제 보정!
            if (pinVisual != null)
            {
                pinVisual.anchoredPosition = Vector2.zero;
                pinVisual.localScale = Vector3.one;
            }
            if (pinShadow != null)
            {
                pinShadow.color = new Color(0, 0, 0, 0.45f);
                pinShadow.transform.localScale = Vector3.one;
            }
            if (labelBubble != null)
            {
                labelBubble.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            KillAllTweens();

            outSequence = DOTween.Sequence();

            // 1. 핀을 중심으로 지도가 줌인 스케일업 (1.0 -> 1.25배, Ease.InCubic)
            if (mapContainer != null)
            {
                outSequence.Join(mapContainer.DOScale(1.25f, zoomOutDuration).SetEase(Ease.InCubic));
            }

            // 2. 좌우 지도가 부드럽게 젖혀지며 밖으로 슬라이드 (InCubic)
            if (mapFoldLeft != null)
            {
                outSequence.Join(mapFoldLeft.DOAnchorPosX(-1100f, zoomOutDuration).SetEase(Ease.InCubic));
            }

            if (mapFoldRight != null)
            {
                outSequence.Join(mapFoldRight.DOAnchorPosX(1100f, zoomOutDuration).SetEase(Ease.InCubic));
            }

            // 3. 핀 및 라벨도 확대되며 부드럽게 퇴장
            if (pinRoot != null)
            {
                outSequence.Join(pinRoot.DOScale(1.4f, zoomOutDuration * 0.7f).SetEase(Ease.InCubic));
            }

            // 4. 캔버스 그룹 페이드아웃으로 씬 B 전면 노출 보장
            if (mapCanvasGroup != null)
            {
                outSequence.Join(mapCanvasGroup.DOFade(0f, zoomOutDuration * 0.8f).SetDelay(zoomOutDuration * 0.2f).SetEase(Ease.InQuad));
            }

            if (canvasGroup != null)
            {
                outSequence.Join(canvasGroup.DOFade(0f, zoomOutDuration * 0.6f).SetDelay(zoomOutDuration * 0.4f).SetEase(Ease.InQuad));
            }

            outSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.blocksRaycasts = false;
                }

                if (mapSheet != null)
                    mapSheet.gameObject.SetActive(false);

                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
