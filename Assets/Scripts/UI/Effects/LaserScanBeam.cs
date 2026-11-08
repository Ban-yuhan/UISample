using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 바코드 스캐너 레이저 빔 이펙트: 붉은 네온 바가 위에서 아래로 스캔
    /// </summary>
    public class LaserScanBeam : MonoBehaviour
    {
        [Header("Beam Settings")]
        [SerializeField] private float scanDuration = 0.22f;
        [SerializeField] private Color beamColor = new Color(1f, 0.15f, 0.15f, 0.95f);
        [SerializeField] private Color coreColor = new Color(1f, 0.92f, 0.92f, 1f);

        private Sequence scanSequence;

        public void Play(RectTransform targetRect, Sprite roundedSprite = null, Action onComplete = null)
        {
            RectTransform beamRoot = GetComponent<RectTransform>();
            if (beamRoot == null || targetRect == null)
            {
                onComplete?.Invoke();
                Destroy(gameObject);
                return;
            }

            // 부모를 targetRect로 맞춰 버튼 영역 내에서 정확히 스캔되도록 설정
            beamRoot.SetParent(targetRect, false);
            beamRoot.anchorMin = new Vector2(0f, 1f);
            beamRoot.anchorMax = new Vector2(1f, 1f);
            beamRoot.pivot = new Vector2(0.5f, 0.5f);
            beamRoot.offsetMin = new Vector2(4f, 0f);
            beamRoot.offsetMax = new Vector2(-4f, 0f);

            float totalHeight = targetRect.rect.height;
            float startY = 8f;
            float endY = -totalHeight - 8f;

            beamRoot.anchoredPosition = new Vector2(0, startY);
            beamRoot.sizeDelta = new Vector2(0, 10f);

            // 레이저 본체 (Red Neon Glow)
            Image beamImg = GetComponent<Image>();
            if (beamImg == null)
            {
                beamImg = gameObject.AddComponent<Image>();
            }
            beamImg.sprite = roundedSprite;
            beamImg.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            beamImg.color = beamColor;
            beamImg.raycastTarget = false;

            // 레이저 코어 (White Hot Center)
            GameObject coreObj = new GameObject("Core", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            coreObj.transform.SetParent(transform, false);
            RectTransform coreRect = coreObj.GetComponent<RectTransform>();
            coreRect.anchorMin = new Vector2(0.05f, 0.3f);
            coreRect.anchorMax = new Vector2(0.95f, 0.7f);
            coreRect.sizeDelta = Vector2.zero;
            coreRect.anchoredPosition = Vector2.zero;

            Image coreImg = coreObj.GetComponent<Image>();
            coreImg.sprite = roundedSprite;
            coreImg.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            coreImg.color = coreColor;
            coreImg.raycastTarget = false;

            scanSequence?.Kill();
            scanSequence = DOTween.Sequence();
            scanSequence.SetUpdate(true);

            // 상단 -> 하단 스캔 이동
            scanSequence.Join(beamRoot.DOAnchorPosY(endY, scanDuration).SetEase(Ease.InOutSine));
            // 끝에 도달하며 살짝 얇아짐
            scanSequence.Join(beamRoot.DOSizeDelta(new Vector2(0, 2f), scanDuration * 0.4f).SetDelay(scanDuration * 0.6f));
            scanSequence.Join(beamImg.DOFade(0.2f, scanDuration * 0.3f).SetDelay(scanDuration * 0.7f));
            scanSequence.Join(coreImg.DOFade(0.2f, scanDuration * 0.3f).SetDelay(scanDuration * 0.7f));

            scanSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void OnDestroy()
        {
            scanSequence?.Kill();
        }
    }
}
