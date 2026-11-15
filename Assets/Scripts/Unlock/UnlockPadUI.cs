using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnlockSystem
{
    /// <summary>
    /// 바닥에 평행하게 놓인 해금 발판 World-Space UI를 제어합니다.
    /// 게이지 Fill(아래->위), 금액 카운트다운(200->0), 테두리 강조를 담당합니다.
    /// </summary>
    public class UnlockPadUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image fillHighlightImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private int totalCost = 200;
        [SerializeField] private Color normalBorderColor = new Color(0.231f, 0.137f, 0.078f, 1f); // #3B2314
        [SerializeField] private Color activeBorderColor = new Color(0.478f, 0.780f, 0.118f, 1f); // #7AC71E

        private Tween _pulseTween;
        private Vector3 _initialScale = Vector3.one;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
            ResetPad();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _initialScale = transform.localScale;
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void KillTweens()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
        }

        /// <summary>
        /// 충전 진행도(0~1)에 따라 게이지 높이와 남은 금액을 갱신합니다.
        /// </summary>
        public void SetProgress(float progress)
        {
            float clamped = Mathf.Clamp01(progress);

            if (fillImage != null)
            {
                fillImage.fillAmount = clamped;
            }

            if (fillHighlightImage != null)
            {
                RectTransform rt = fillHighlightImage.rectTransform;
                float parentHeight = rt.parent is RectTransform parentRt ? parentRt.rect.height : 100f;
                rt.anchoredPosition = new Vector2(0f, clamped * parentHeight);
                fillHighlightImage.enabled = clamped > 0.03f && clamped < 0.99f;
            }

            if (costText != null)
            {
                int remaining = Mathf.Max(0, Mathf.RoundToInt(totalCost * (1f - clamped)));
                costText.text = remaining.ToString();
            }

            if (borderImage != null)
            {
                borderImage.color = Color.Lerp(normalBorderColor, activeBorderColor, clamped);
            }
        }

        public void PlayChargeCompletePulse()
        {
            EnsureInitialized();
            KillTweens();
            transform.localScale = _initialScale;
            _pulseTween = transform
                .DOPunchScale(_initialScale * 0.08f, 0.25f, 4, 0.5f)
                .SetUpdate(false);
        }

        public void SetPadDimmed(bool dimmed)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = dimmed ? 0.55f : 1f;
            }
        }

        public void ResetPad()
        {
            EnsureInitialized();
            KillTweens();
            transform.localScale = _initialScale;
            SetProgress(0f);
            SetPadDimmed(false);
            if (borderImage != null)
                borderImage.color = normalBorderColor;
        }
    }
}
