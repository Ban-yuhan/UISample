using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToastMessageSample
{
    public sealed class SnackbarToastView : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private const float CardWidth = 248f;
        private const float CardHeight = 56f;
        private const float DismissSwipeThreshold = -55f;

        private RectTransform _rootRect;
        private CanvasGroup _canvasGroup;
        private Image _bgImage;
        private Image _borderImage;
        private Image _dangerPulseBorder;
        private Image _iconBoxBg;
        private Image _iconImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subText;
        private GameObject _progressTrack;
        private RectTransform _progressFillRect;
        private Image _progressFillImg;

        private ToastAlertState _state;
        private Action<ToastAlertState> _onClickCallback;
        private Action<ToastAlertState> _onDismissCallback;

        private Tween _slideTween;
        private Tween _dangerPulseTween;
        private Tween _countdownTween;
        private float _dragStartX;
        private float _currentOffsetX;
        private bool _isDragging;
        private bool _isExiting;

        public void BuildAndBind(
            ToastAlertState state,
            TMP_FontAsset font,
            bool autoDismissEnabled,
            bool playEnterAnim,
            Action<ToastAlertState> onClick,
            Action<ToastAlertState> onDismiss)
        {
            _state = state;
            _onClickCallback = onClick;
            _onDismissCallback = onDismiss;
            _isExiting = false;

            BuildVisualTree(font);
            ApplyThemeAndContent();
            StartDangerPulseIfNeeded();
            SetCountdownActive(autoDismissEnabled);

            if (playEnterAnim)
            {
                PlayEnterAnimation();
            }
            else
            {
                _canvasGroup.alpha = 1f;
                _rootRect.anchoredPosition = Vector2.zero;
            }
        }

        public void SetCountdownActive(bool enabled)
        {
            _countdownTween?.Kill();
            if (_progressTrack == null || _progressFillRect == null) return;

            _progressTrack.SetActive(enabled);
            if (!enabled) return;

            _progressFillRect.localScale = Vector3.one;
            _countdownTween = _progressFillRect
                .DOScaleX(0f, 4.0f)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }

        public void PlayExitAnimation()
        {
            if (_isExiting) return;
            _isExiting = true;

            _slideTween?.Kill();
            _countdownTween?.Kill();
            _dangerPulseTween?.Kill();
            _canvasGroup.blocksRaycasts = false;

            _slideTween = DOTween.Sequence()
                .Join(_rootRect.DOAnchorPosX(-32f, 0.26f).SetEase(Ease.InCubic))
                .Join(_canvasGroup.DOFade(0f, 0.26f).SetEase(Ease.InCubic))
                .SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isExiting || _isDragging || _state == null || _state.Closing) return;
            _onClickCallback?.Invoke(_state);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isExiting || _state == null || _state.Closing) return;
            _isDragging = true;
            _slideTween?.Kill();
            _dragStartX = eventData.position.x;
            _currentOffsetX = _rootRect.anchoredPosition.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _isExiting) return;
            float dx = eventData.position.x - _dragStartX;
            if (dx < 0f)
            {
                _currentOffsetX = dx;
                _rootRect.anchoredPosition = new Vector2(dx, 0f);
                _canvasGroup.alpha = Mathf.Max(0.2f, 1f + dx / 140f);
            }
            else
            {
                _currentOffsetX = 0f;
                _rootRect.anchoredPosition = Vector2.zero;
                _canvasGroup.alpha = 1f;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            if (_isExiting) return;

            if (_currentOffsetX <= DismissSwipeThreshold)
            {
                _onDismissCallback?.Invoke(_state);
            }
            else
            {
                _slideTween?.Kill();
                _slideTween = DOTween.Sequence()
                    .Join(_rootRect.DOAnchorPosX(0f, 0.22f).SetEase(Ease.OutCubic))
                    .Join(_canvasGroup.DOFade(1f, 0.22f).SetEase(Ease.OutCubic))
                    .SetLink(gameObject);
            }
        }

        private void BuildVisualTree(TMP_FontAsset font)
        {
            _rootRect = GetComponent<RectTransform>();
            if (_rootRect == null) _rootRect = gameObject.AddComponent<RectTransform>();
            _rootRect.anchorMin = new Vector2(0f, 1f);
            _rootRect.anchorMax = new Vector2(0f, 1f);
            _rootRect.pivot = new Vector2(0f, 1f);
            _rootRect.sizeDelta = new Vector2(CardWidth, CardHeight);
            _rootRect.anchoredPosition = Vector2.zero;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Card Background (Warm Ivory)
            _bgImage = gameObject.AddComponent<Image>();
            _bgImage.sprite = ToastProceduralSprites.GetRoundedRect();
            _bgImage.type = Image.Type.Sliced;
            _bgImage.color = ToastThemePalette.Hex("#FFFDF8");
            _bgImage.raycastTarget = true;
            gameObject.AddComponent<RectMask2D>();

            // Colored outer border (tinted per preset theme in ApplyThemeAndContent)
            var borderGo = CreateChild("Border", _rootRect);
            var sbRect = borderGo.GetComponent<RectTransform>();
            sbRect.anchorMin = Vector2.zero;
            sbRect.anchorMax = Vector2.one;
            sbRect.offsetMin = Vector2.zero;
            sbRect.offsetMax = Vector2.zero;
            _borderImage = borderGo.AddComponent<Image>();
            _borderImage.sprite = ToastProceduralSprites.GetRoundedRectOutline();
            _borderImage.type = Image.Type.Sliced;
            _borderImage.color = ToastThemePalette.Hex("#D6C9B4");
            _borderImage.raycastTarget = false;

            // Danger/Staff Pulsing Border
            var dangerBorderGo = CreateChild("DangerPulseBorder", _rootRect);
            var dbRect = dangerBorderGo.GetComponent<RectTransform>();
            dbRect.anchorMin = Vector2.zero;
            dbRect.anchorMax = Vector2.one;
            dbRect.offsetMin = Vector2.zero;
            dbRect.offsetMax = Vector2.zero;
            _dangerPulseBorder = dangerBorderGo.AddComponent<Image>();
            _dangerPulseBorder.sprite = ToastProceduralSprites.GetRoundedRectOutline();
            _dangerPulseBorder.type = Image.Type.Sliced;
            _dangerPulseBorder.color = new Color(1f, 1f, 1f, 0f);
            _dangerPulseBorder.raycastTarget = false;

            // 26x26 Icon Box
            var iconBoxGo = CreateChild("IconBox", _rootRect);
            var iconBoxRect = iconBoxGo.GetComponent<RectTransform>();
            iconBoxRect.anchorMin = new Vector2(0f, 0.5f);
            iconBoxRect.anchorMax = new Vector2(0f, 0.5f);
            iconBoxRect.pivot = new Vector2(0f, 0.5f);
            iconBoxRect.anchoredPosition = new Vector2(12f, 3f);
            iconBoxRect.sizeDelta = new Vector2(26f, 26f);
            _iconBoxBg = iconBoxGo.AddComponent<Image>();
            _iconBoxBg.sprite = ToastProceduralSprites.GetRoundedRect();
            _iconBoxBg.type = Image.Type.Sliced;
            _iconBoxBg.raycastTarget = false;

            var iconGo = CreateChild("Icon", iconBoxRect);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(16f, 16f);
            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.raycastTarget = false;

            // Title & Sub (Warm Espresso & Muted Brown)
            _titleText = CreateTmpText("Title", _rootRect, font, 12.5f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), new Vector2(47f, 11f), new Vector2(168f, 18f));
            _subText = CreateTmpText("Sub", _rootRect, font, 10.5f, FontStyles.Normal, ToastThemePalette.Hex("#756657"), new Vector2(47f, -6f), new Vector2(168f, 16f));

            // Close Button
            var closeGo = CreateChild("CloseBtn", _rootRect);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-8f, 3f);
            closeRect.sizeDelta = new Vector2(22f, 22f);

            var closeBg = closeGo.AddComponent<Image>();
            closeBg.color = new Color(0.23f, 0.19f, 0.15f, 0.06f);

            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(() =>
            {
                if (_state != null && !_state.Closing)
                {
                    _onDismissCallback?.Invoke(_state);
                }
            });

            var closeIconGo = CreateChild("CloseX", closeRect);
            var closeIconRect = closeIconGo.GetComponent<RectTransform>();
            closeIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            closeIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            closeIconRect.sizeDelta = new Vector2(10f, 10f);
            var closeIconImg = closeIconGo.AddComponent<Image>();
            closeIconImg.sprite = ToastProceduralSprites.GetIcon(ToastIconType.CloseX);
            closeIconImg.color = ToastThemePalette.Hex("#8B7B6B");
            closeIconImg.raycastTarget = false;

            // Inner Bottom Countdown Bar (inset cleanly inside panel, away from outer border)
            _progressTrack = CreateChild("ProgressTrack", _rootRect);
            var trackRect = _progressTrack.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.offsetMin = new Vector2(12f, 6f);
            trackRect.offsetMax = new Vector2(-12f, 9.5f);
            var trackImg = _progressTrack.AddComponent<Image>();
            trackImg.color = new Color(0.23f, 0.19f, 0.15f, 0.12f);
            trackImg.raycastTarget = false;

            var fillGo = CreateChild("ProgressFill", trackRect);
            _progressFillRect = fillGo.GetComponent<RectTransform>();
            _progressFillRect.anchorMin = Vector2.zero;
            _progressFillRect.anchorMax = Vector2.one;
            _progressFillRect.pivot = new Vector2(0f, 0.5f);
            _progressFillRect.offsetMin = Vector2.zero;
            _progressFillRect.offsetMax = Vector2.zero;
            _progressFillImg = fillGo.AddComponent<Image>();
            _progressFillImg.raycastTarget = false;
        }

        private void ApplyThemeAndContent()
        {
            if (_state == null || _state.Preset == null) return;
            ToastPresetData preset = _state.Preset;
            ToastThemePalette palette = ToastThemePalette.Get(preset.Tone);

            if (_borderImage != null)
            {
                _borderImage.color = palette.SnackbarBorder;
            }
            _iconBoxBg.color = palette.SnackbarIconBg;
            _iconImage.sprite = ToastProceduralSprites.GetIcon(preset.IconType);
            _iconImage.color = palette.SnackbarIconTint;
            _progressFillImg.color = palette.SnackbarProgress;
            _titleText.text = preset.Title;
            _subText.text = preset.Sub;
        }

        private void StartDangerPulseIfNeeded()
        {
            _dangerPulseTween?.Kill();
            if (_state == null || _state.Preset == null || !_state.Preset.IsDangerSeverity)
            {
                _dangerPulseBorder.gameObject.SetActive(false);
                return;
            }

            _dangerPulseBorder.gameObject.SetActive(true);
            ToastThemePalette palette = ToastThemePalette.Get(_state.Preset.Tone);
            Color baseCol = palette.BorderPulseColor;
            baseCol.a = 0.35f;
            _dangerPulseBorder.color = baseCol;

            _dangerPulseTween = _dangerPulseBorder
                .DOFade(0.95f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void PlayEnterAnimation()
        {
            _slideTween?.Kill();
            _canvasGroup.alpha = 0f;
            _rootRect.anchoredPosition = new Vector2(-32f, 0f);
            _slideTween = DOTween.Sequence()
                .Join(_rootRect.DOAnchorPosX(0f, 0.45f).SetEase(Ease.OutQuart))
                .Join(_canvasGroup.DOFade(1f, 0.45f).SetEase(Ease.OutQuart))
                .SetLink(gameObject);
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateTmpText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            float fontSize,
            FontStyles style,
            Color color,
            Vector2 anchoredPos,
            Vector2 size)
        {
            var go = CreateChild(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void OnDestroy()
        {
            _slideTween?.Kill();
            _dangerPulseTween?.Kill();
            _countdownTween?.Kill();
        }
    }
}
