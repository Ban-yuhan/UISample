using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 배송 완료/보상 수령 시 황금 코인 6~8개가 분수처럼 퐁퐁 솟아올라 흩어지는 이펙트
    /// </summary>
    public class CoinFountain : MonoBehaviour
    {
        [Header("Coin Fountain Settings")]
        [SerializeField] private int coinCount = 7;
        [SerializeField] private Vector2 coinSize = new Vector2(26f, 26f);
        [SerializeField] private float jumpDuration = 0.55f;

        private Sequence fountainSequence;

        public void Play(Vector2 centerPosition, Sprite circleSprite = null)
        {
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchoredPosition = centerPosition;
            }

            fountainSequence?.Kill();
            fountainSequence = DOTween.Sequence();
            fountainSequence.SetUpdate(true);

            Color goldPrimary = new Color(1f, 0.82f, 0.12f, 1f);     // 진한 황금색
            Color goldRim = new Color(1f, 0.96f, 0.45f, 1f);         // 밝은 하이라이트

            for (int i = 0; i < coinCount; i++)
            {
                // 코인 외곽/본체
                GameObject coin = new GameObject($"Coin_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                coin.transform.SetParent(transform, false);

                RectTransform coinRect = coin.GetComponent<RectTransform>();
                Image coinImg = coin.GetComponent<Image>();
                coinImg.raycastTarget = false;
                coinImg.color = goldPrimary;
                if (circleSprite != null)
                {
                    coinImg.sprite = circleSprite;
                }

                coinRect.sizeDelta = coinSize;
                coinRect.anchoredPosition = Vector2.zero;
                coinRect.localScale = Vector3.zero;

                // 코인 내부 디테일 (밝은 코어)
                GameObject inner = new GameObject("Core", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                inner.transform.SetParent(coin.transform, false);
                RectTransform innerRect = inner.GetComponent<RectTransform>();
                innerRect.anchorMin = Vector2.zero;
                innerRect.anchorMax = Vector2.one;
                innerRect.sizeDelta = new Vector2(-6f, -6f);
                innerRect.anchoredPosition = Vector2.zero;
                Image innerImg = inner.GetComponent<Image>();
                innerImg.raycastTarget = false;
                innerImg.color = goldRim;
                if (circleSprite != null)
                {
                    innerImg.sprite = circleSprite;
                }

                // 궤적 파라미터 계산 (위쪽 부채꼴 방사)
                float spreadAngle = Random.Range(-45f, 45f); // 위쪽을 기준으로 좌우 분산
                float rad = (90f + spreadAngle) * Mathf.Deg2Rad;
                float force = Random.Range(100f, 160f);
                float targetX = Mathf.Cos(rad) * force * 1.1f + Random.Range(-15f, 15f);
                float peakY = Mathf.Sin(rad) * force;
                float fallY = peakY - Random.Range(60f, 110f);

                float delay = i * 0.03f; // 순차적 퐁퐁 분수 발사감

                Sequence coinSeq = DOTween.Sequence();
                coinSeq.SetUpdate(true);
                coinSeq.AppendInterval(delay);

                // 스케일 팝 (0 -> 1.15 -> 1.0)
                coinSeq.Join(coinRect.DOScale(1.15f, 0.12f).SetEase(Ease.OutBack));
                coinSeq.Append(coinRect.DOScale(1.0f, 0.1f));

                // X축 이동 (수평 확산)
                coinSeq.Join(coinRect.DOAnchorPosX(targetX, jumpDuration).SetEase(Ease.OutQuad));

                // Y축 포물선 (올라갔다가 중력 낙하)
                float upDuration = jumpDuration * 0.42f;
                float downDuration = jumpDuration * 0.58f;
                coinSeq.Join(coinRect.DOAnchorPosY(peakY, upDuration).SetEase(Ease.OutCubic));
                coinSeq.Insert(delay + upDuration, coinRect.DOAnchorPosY(fallY, downDuration).SetEase(Ease.InQuad));

                // 회전 (엽전 돌리기)
                coinSeq.Join(coinRect.DORotate(new Vector3(0, 0, Random.Range(-270f, 270f)), jumpDuration, RotateMode.FastBeyond360));

                // 소멸 페이드아웃
                coinSeq.Insert(delay + jumpDuration - 0.15f, coinImg.DOFade(0f, 0.15f));
                coinSeq.Insert(delay + jumpDuration - 0.15f, innerImg.DOFade(0f, 0.15f));

                fountainSequence.Join(coinSeq);
            }

            fountainSequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void OnDestroy()
        {
            fountainSequence?.Kill();
        }
    }
}
