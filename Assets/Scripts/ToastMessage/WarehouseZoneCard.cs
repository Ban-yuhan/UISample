using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToastMessageSample
{
    public sealed class WarehouseZoneCard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string _zoneId;
        [SerializeField] private Image _borderPulseImage;
        [SerializeField] private RectTransform _cardRect;

        private Tween _borderTween;
        private Tween _scaleTween;

        public string ZoneId => _zoneId;

        public void Initialize(string zoneId, Image borderPulseImage, RectTransform cardRect)
        {
            _zoneId = zoneId;
            _borderPulseImage = borderPulseImage;
            _cardRect = cardRect;
            if (_borderPulseImage != null)
            {
                Color c = _borderPulseImage.color;
                c.a = 0f;
                _borderPulseImage.color = c;
            }
        }

        public void Ping(bool isSuccess, bool isInfo = false)
        {
            Color flashColor;
            if (isInfo)
            {
                flashColor = ToastThemePalette.Hex("#4C7599");
            }
            else if (isSuccess)
            {
                flashColor = ToastThemePalette.Hex("#4E8C6F");
            }
            else
            {
                flashColor = ToastThemePalette.Hex("#C9534F");
            }

            if (_borderPulseImage != null)
            {
                _borderTween?.Kill();
                _borderPulseImage.DOKill();
                flashColor.a = 0.95f;
                _borderPulseImage.color = flashColor;
                _borderTween = _borderPulseImage
                    .DOFade(0f, 0.85f)
                    .SetEase(Ease.OutCubic)
                    .SetLink(gameObject);
            }

            if (_cardRect != null)
            {
                _scaleTween?.Kill();
                _cardRect.DOKill();
                _cardRect.localScale = Vector3.one;
                _scaleTween = DOTween.Sequence()
                    .Append(_cardRect.DOScale(1.035f, 0.14f).SetEase(Ease.OutQuad))
                    .Append(_cardRect.DOScale(1f, 0.22f).SetEase(Ease.OutCubic))
                    .SetLink(gameObject);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Ping(false, true);
        }

        private void OnDestroy()
        {
            _borderTween?.Kill();
            _scaleTween?.Kill();
        }
    }
}
