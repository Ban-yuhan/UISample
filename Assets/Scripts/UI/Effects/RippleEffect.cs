using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 버튼 클릭 지점에서 원형으로 퍼져나가는 물결(Ripple) 이펙트 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class RippleEffect : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float targetScale = 3.2f;
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private Ease scaleEase = Ease.OutQuad;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;

        private Image rippleImage;
        private RectTransform rectTransform;
        private Sequence rippleSequence;

        private void Awake()
        {
            rippleImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            if (rippleImage != null)
            {
                rippleImage.raycastTarget = false;
            }
        }

        public void Play(Vector2 localPosition, Color? baseColor = null)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (rippleImage == null)
                rippleImage = GetComponent<Image>();

            rectTransform.anchoredPosition = localPosition;
            rectTransform.localScale = Vector3.zero;

            // 따뜻하고 부드러운 소프트 밀크 화이트/웜 크림 반투명 물결
            Color col = baseColor ?? new Color(1f, 0.98f, 0.92f, 0.65f);
            rippleImage.color = col;

            rippleSequence?.Kill();
            rippleSequence = DOTween.Sequence();
            rippleSequence.SetUpdate(true); // Ignore timeScale if needed

            rippleSequence.Join(rectTransform.DOScale(Vector3.one * targetScale, duration).SetEase(scaleEase));
            rippleSequence.Join(rippleImage.DOFade(0f, duration).SetEase(fadeEase));
            rippleSequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void OnDestroy()
        {
            rippleSequence?.Kill();
        }
    }
}
