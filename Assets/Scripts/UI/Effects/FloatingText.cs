using UnityEngine;
using TMPro;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 버튼 클릭 시 상향 부유하며 페이드아웃되는 플로팅 텍스트 이펙트
    /// 외곽선은 TMP 머티리얼 프리셋(Outline)으로 처리하며, 정점 알파 페이드 시 외곽선도 함께 사라진다.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float floatDistance = 60f;
        [SerializeField] private float duration = 0.65f;
        [SerializeField] private Ease moveEase = Ease.OutCubic;
        [SerializeField] private Ease fadeEase = Ease.InQuad;

        [Header("Components")]
        [SerializeField] private TMP_Text messageText;

        private RectTransform rectTransform;
        private Sequence floatSequence;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (messageText == null)
            {
                messageText = GetComponentInChildren<TMP_Text>();
            }
            if (messageText != null)
            {
                messageText.raycastTarget = false;
            }
        }

        public void Play(string customMessage = null, Color? textColor = null)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>();

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
                TMP_Text txt = messageText;
                floatSequence.Join(DOTween.To(() => txt.alpha, a => txt.alpha = a, 0f, duration).SetEase(fadeEase));
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
