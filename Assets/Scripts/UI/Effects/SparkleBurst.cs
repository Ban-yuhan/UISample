using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 클릭 지점에서 5~8개의 작은 스파클 파티클이 방사형으로 퍼져나가는 연출
    /// </summary>
    public class SparkleBurst : MonoBehaviour
    {
        [Header("Sparkle Settings")]
        [SerializeField] private int particleCount = 7;
        [SerializeField] private float minDistance = 35f;
        [SerializeField] private float maxDistance = 75f;
        [SerializeField] private float duration = 0.5f;

        private Sequence burstSequence;

        public void Play(Vector2 centerPosition, Sprite sparkleSprite = null, Color? sparkleColor = null)
        {
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchoredPosition = centerPosition;
            }

            burstSequence?.Kill();
            burstSequence = DOTween.Sequence();
            burstSequence.SetUpdate(true);

            Color baseColor = sparkleColor ?? new Color(1f, 0.92f, 0.23f, 1f); // Gold/Yellow sparkle

            for (int i = 0; i < particleCount; i++)
            {
                GameObject p = new GameObject($"Sparkle_{i}", typeof(RectTransform), typeof(Image));
                p.transform.SetParent(transform, false);

                RectTransform pRect = p.GetComponent<RectTransform>();
                Image pImg = p.GetComponent<Image>();
                pImg.raycastTarget = false;
                pImg.color = baseColor;
                if (sparkleSprite != null)
                {
                    pImg.sprite = sparkleSprite;
                }

                pRect.anchoredPosition = Vector2.zero;
                pRect.sizeDelta = new Vector2(16f, 16f);
                pRect.localScale = Vector3.one * Random.Range(0.8f, 1.3f);

                float angle = (360f / particleCount) * i + Random.Range(-15f, 15f);
                float rad = angle * Mathf.Deg2Rad;
                float dist = Random.Range(minDistance, maxDistance);
                Vector2 targetPos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;

                burstSequence.Join(pRect.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCubic));
                burstSequence.Join(pRect.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad));
                burstSequence.Join(pImg.DOFade(0f, duration).SetEase(Ease.InQuad));
                burstSequence.Join(pRect.DORotate(new Vector3(0, 0, Random.Range(-180f, 180f)), duration, RotateMode.FastBeyond360));
            }

            burstSequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void OnDestroy()
        {
            burstSequence?.Kill();
        }
    }
}
