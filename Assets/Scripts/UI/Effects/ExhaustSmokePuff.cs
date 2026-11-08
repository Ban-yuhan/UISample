using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 배송 출발(TruckRevUp) 시 트럭 왼쪽 하단(배기통/바퀴)에서 퐁퐁 뿜어져 나오는 배기 연기 및 타이어 마찰 FX
    /// </summary>
    public class ExhaustSmokePuff : MonoBehaviour
    {
        [Header("Smoke Settings")]
        [SerializeField] private int smokeCount = 4;
        [SerializeField] private Vector2 baseSmokeSize = new Vector2(24f, 24f);
        [SerializeField] private float puffDuration = 0.45f;
        [SerializeField] private Color smokeColor = new Color(0.85f, 0.83f, 0.80f, 0.8f); // 소프트 웜 그레이/크림 화이트

        private Sequence puffSequence;

        public void Play(Vector2 startPosition, Sprite circleSprite = null)
        {
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchoredPosition = startPosition;
            }

            puffSequence?.Kill();
            puffSequence = DOTween.Sequence();
            puffSequence.SetUpdate(true);

            // 1. 타이어 마찰 스파크 라인 (바퀴 아래쪽에서 스치듯 사라짐)
            CreateTireFrictionEffect();

            // 2. 3~4개의 배기 연기 버블 (뒤쪽/왼쪽으로 퐁퐁 뿜어짐)
            int count = Mathf.Clamp(smokeCount, 3, 5);
            for (int i = 0; i < count; i++)
            {
                CreateSmokeBubble(i, count, circleSprite);
            }

            puffSequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void CreateSmokeBubble(int index, int totalCount, Sprite circleSprite)
        {
            GameObject bubbleObj = new GameObject($"Smoke_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bubbleObj.transform.SetParent(transform, false);

            RectTransform bubbleRt = bubbleObj.GetComponent<RectTransform>();
            Image bubbleImg = bubbleObj.GetComponent<Image>();
            bubbleImg.raycastTarget = false;
            bubbleImg.color = smokeColor;
            if (circleSprite != null)
            {
                bubbleImg.sprite = circleSprite;
            }

            // 개별 크기 살짝 랜덤화
            float sizeVariation = Random.Range(0.85f, 1.25f);
            bubbleRt.sizeDelta = baseSmokeSize * sizeVariation;
            bubbleRt.anchoredPosition = Vector2.zero;
            bubbleRt.localScale = Vector3.one * 0.5f; // 초기 스케일 0.5f

            // 뒤쪽(왼쪽)으로 퍼져나가는 궤적 계산
            float delay = index * 0.045f; // 퐁! 퐁! 퐁! 연속 방출감
            float speedFactor = 1f + (index * 0.15f);
            float targetX = -Random.Range(32f, 65f) * speedFactor; // 왼쪽으로 분사
            float targetY = Random.Range(-6f, 22f) + (index * 4f);  // 살짝 위로 흩날림

            Sequence bubbleSeq = DOTween.Sequence();
            bubbleSeq.SetUpdate(true);
            bubbleSeq.AppendInterval(delay);

            // 스케일 업: 0.5f -> 1.4f (몽글몽글 커짐)
            bubbleSeq.Join(bubbleRt.DOScale(Vector3.one * 1.4f, puffDuration).SetEase(Ease.OutQuad));

            // 위치 이동: 뒤쪽(왼쪽)으로 가속 이동
            bubbleSeq.Join(bubbleRt.DOAnchorPosX(targetX, puffDuration).SetEase(Ease.OutQuad));
            bubbleSeq.Join(bubbleRt.DOAnchorPosY(targetY, puffDuration).SetEase(Ease.OutSine));

            // 페이드아웃: 마지막 0.25초 동안 알파 0f로 부드럽게 페이드
            bubbleSeq.Insert(delay + (puffDuration * 0.45f), bubbleImg.DOFade(0f, puffDuration * 0.55f).SetEase(Ease.InQuad));

            puffSequence.Join(bubbleSeq);
        }

        private void CreateTireFrictionEffect()
        {
            // 타이어 마찰 스파크 라인 (오렌지/화이트 마찰선)
            GameObject frictionObj = new GameObject("TireFrictionSpark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frictionObj.transform.SetParent(transform, false);

            RectTransform frictionRt = frictionObj.GetComponent<RectTransform>();
            Image frictionImg = frictionObj.GetComponent<Image>();
            frictionImg.raycastTarget = false;

            // 바닥 접지면 마찰 라인
            frictionRt.anchoredPosition = new Vector2(8f, -6f);
            frictionRt.sizeDelta = new Vector2(30f, 4f);
            frictionRt.localScale = new Vector3(0.3f, 1f, 1f);

            // 따뜻한 오렌지/골드 마찰광
            frictionImg.color = new Color(1f, 0.65f, 0.25f, 0.85f);

            Sequence frictionSeq = DOTween.Sequence();
            frictionSeq.SetUpdate(true);

            // 뒤로 슥 미끄러지며 스케일 늘어났다가 페이드아웃
            frictionSeq.Join(frictionRt.DOAnchorPosX(-12f, 0.22f).SetEase(Ease.OutQuad));
            frictionSeq.Join(frictionRt.DOScaleX(1.3f, 0.1f).SetEase(Ease.OutQuad));
            frictionSeq.Insert(0.08f, frictionImg.DOFade(0f, 0.16f).SetEase(Ease.InQuad));

            puffSequence.Join(frictionSeq);
        }

        private void OnDestroy()
        {
            puffSequence?.Kill();
        }
    }
}
