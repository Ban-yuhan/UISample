using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 버튼 클릭 시 상향 부유하며 페이드아웃되는 플로팅 텍스트 이펙트
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float floatDistance = 60f;
        [SerializeField] private float duration = 0.65f;
        [SerializeField] private Ease moveEase = Ease.OutCubic;
        [SerializeField] private Ease fadeEase = Ease.InQuad;

        [Header("Components")]
        [SerializeField] private Text messageText;
        [SerializeField] private Outline outline;

        private RectTransform rectTransform;
        private Sequence floatSequence;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (messageText == null)
            {
                messageText = GetComponentInChildren<Text>();
            }
            if (messageText != null)
            {
                messageText.raycastTarget = false;
            }
            if (outline == null)
            {
                outline = GetComponentInChildren<Outline>();
            }
        }

        public void Play(string customMessage = null, Color? textColor = null)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (messageText == null)
                messageText = GetComponentInChildren<Text>();

            if (messageText != null)
            {
                if (!string.IsNullOrEmpty(customMessage))
                {
                    messageText.text = customMessage;
                }
                if (textColor.HasValue)
                {
                    messageText.color = textColor.Value;
                }
            }

            Vector2 startPos = rectTransform.anchoredPosition;
            float targetY = startPos.y + floatDistance;

            floatSequence?.Kill();
            floatSequence = DOTween.Sequence();
            floatSequence.SetUpdate(true);

            floatSequence.Join(rectTransform.DOAnchorPosY(targetY, duration).SetEase(moveEase));
            if (messageText != null)
            {
                floatSequence.Join(messageText.DOFade(0f, duration).SetEase(fadeEase));
            }
            if (outline != null)
            {
                Color outCol = outline.effectColor;
                floatSequence.Join(DOTween.To(() => outline.effectColor, x => outline.effectColor = x, new Color(outCol.r, outCol.g, outCol.b, 0f), duration));
            }

            floatSequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void OnDestroy()
        {
            floatSequence?.Kill();
        }
    }
}
