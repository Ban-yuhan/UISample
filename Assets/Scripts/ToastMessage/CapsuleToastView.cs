using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToastMessageSample
{
    public sealed class CapsuleToastView : MonoBehaviour, IPointerClickHandler
    {
        private const float CollapsedWidth = 54f;
        private const float ExpandedWidth = 340f;
        private const float PillHeight = 54f;

        private RectTransform _rootRect;
        private CanvasGroup _rootCanvasGroup;
        private RectTransform _pillBodyRect;
        private Image _pillBgImage;
        private Image _auraGlowImage;
        private RectTransform _auraGlowRect;
        private Image _auraRingImage;
        private RectTransform _auraRingRect;
        private RectTransform _glintRect;
        private Image _glintImage;
        private Image _iconImage;
        private CanvasGroup _textCanvasGroup;
        private RectTransform _textBoxRect;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subText;
        private Button _closeMiniBtn;
        private Image[] _sparkles;

        private ToastAlertState _state;
        private float _pulseDuration = 2.1f;
        private Action<ToastAlertState> _onClickCallback;
        private Action<ToastAlertState> _onDismissCallback;

        private Tween _enterTween;
        private Tween _morphTween;
        private Tween _textTween;
        private Tween _auraGlowTween;
        private Tween _auraExpandTween;
        private Tween _auraRingTween;
        private Sequence _glintSequence;
        private Tween[] _sparkleTweens;

        public void BuildAndBind(
            ToastAlertState state,
            TMP_FontAsset font,
            float pulseDuration,
            bool playEnterAnim,
            Action<ToastAlertState> onClick,
            Action<ToastAlertState> onDismiss)
        {
            _state = state;
            _pulseDuration = Mathf.Clamp(pulseDuration, 0.9f, 3.6f);
            _onClickCallback = onClick;
            _onDismissCallback = onDismiss;

            BuildVisualTree(font);
            ApplyThemeAndContent();
            SetExpanded(state.Expanded, animate: false);
            StartAmbientTweens();

            if (playEnterAnim)
            {
                PlayEnterAnimation();
            }
            else
            {
                _rootCanvasGroup.alpha = 1f;
                _rootRect.anchoredPosition = Vector2.zero;
            }
        }

        public void SetPulseDuration(float duration)
        {
            _pulseDuration = Mathf.Clamp(duration, 0.9f, 3.6f);
            RestartAuraTweens();
        }

        public void SetExpanded(bool expanded, bool animate = true)
        {
            if (_pillBodyRect == null) return;

            _morphTween?.Kill();
            _textTween?.Kill();

            float targetWidth = expanded ? ExpandedWidth : CollapsedWidth;

            if (_closeMiniBtn != null)
            {
                _closeMiniBtn.gameObject.SetActive(expanded);
            }

            if (_auraRingImage != null)
            {
                _auraRingImage.gameObject.SetActive(!expanded);
            }

            if (!animate)
            {
                _pillBodyRect.sizeDelta = new Vector2(targetWidth, PillHeight);
                _rootRect.sizeDelta = new Vector2(targetWidth, PillHeight);
                _textCanvasGroup.alpha = expanded ? 1f : 0f;
                _textBoxRect.anchoredPosition = new Vector2(expanded ? 54f : 46f, 0f);
                UpdateAuraForExpandedState(expanded, 0f);
                return;
            }

            float duration = expanded ? 0.5f : 0.4f;
            Ease ease = expanded ? Ease.OutQuart : Ease.OutCubic;

            _morphTween = DOTween.Sequence()
                .Join(_pillBodyRect.DOSizeDelta(new Vector2(targetWidth, PillHeight), duration).SetEase(ease))
                .Join(_rootRect.DOSizeDelta(new Vector2(targetWidth, PillHeight), duration).SetEase(ease))
                .SetLink(gameObject);

            if (expanded)
            {
                _textBoxRect.anchoredPosition = new Vector2(46f, 0f);
                _textTween = DOTween.Sequence()
                    .SetDelay(0.08f)
                    .Join(_textCanvasGroup.DOFade(1f, 0.32f).SetEase(Ease.OutCubic))
                    .Join(_textBoxRect.DOAnchorPosX(54f, 0.42f).SetEase(Ease.OutQuart))
                    .SetLink(gameObject);
            }
            else
            {
                _textTween = DOTween.Sequence()
                    .Join(_textCanvasGroup.DOFade(0f, 0.18f).SetEase(Ease.OutCubic))
                    .Join(_textBoxRect.DOAnchorPosX(46f, 0.25f).SetEase(Ease.OutCubic))
                    .SetLink(gameObject);
            }

            UpdateAuraForExpandedState(expanded, duration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_state == null || _state.Closing) return;
            _onClickCallback?.Invoke(_state);
        }

        private void BuildVisualTree(TMP_FontAsset font)
        {
            _rootRect = GetComponent<RectTransform>();
            if (_rootRect == null) _rootRect = gameObject.AddComponent<RectTransform>();
            _rootRect.anchorMin = new Vector2(0f, 1f);
            _rootRect.anchorMax = new Vector2(0f, 1f);
            _rootRect.pivot = new Vector2(0f, 1f);
            _rootRect.sizeDelta = new Vector2(CollapsedWidth, PillHeight);
            _rootRect.anchoredPosition = Vector2.zero;

            _rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 1. Aura Glow (behind pill)
            var glowGo = CreateChild("AuraGlow", _rootRect);
            _auraGlowRect = glowGo.GetComponent<RectTransform>();
            _auraGlowRect.anchorMin = new Vector2(0f, 0.5f);
            _auraGlowRect.anchorMax = new Vector2(0f, 0.5f);
            _auraGlowRect.pivot = new Vector2(0.5f, 0.5f);
            _auraGlowRect.anchoredPosition = new Vector2(27f, 0f);
            _auraGlowRect.sizeDelta = new Vector2(76f, 76f);
            _auraGlowImage = glowGo.AddComponent<Image>();
            _auraGlowImage.sprite = ToastProceduralSprites.GetAuraGlow();
            _auraGlowImage.raycastTarget = false;

            // 2. Aura Ring (ripple)
            var ringGo = CreateChild("AuraRing", _rootRect);
            _auraRingRect = ringGo.GetComponent<RectTransform>();
            _auraRingRect.anchorMin = new Vector2(0f, 0.5f);
            _auraRingRect.anchorMax = new Vector2(0f, 0.5f);
            _auraRingRect.pivot = new Vector2(0.5f, 0.5f);
            _auraRingRect.anchoredPosition = new Vector2(27f, 0f);
            _auraRingRect.sizeDelta = new Vector2(54f, 54f);
            _auraRingImage = ringGo.AddComponent<Image>();
            _auraRingImage.sprite = ToastProceduralSprites.GetCircleRing();
            _auraRingImage.raycastTarget = false;

            // 3. Pill Body with RectMask2D
            var pillGo = CreateChild("PillBody", _rootRect);
            _pillBodyRect = pillGo.GetComponent<RectTransform>();
            _pillBodyRect.anchorMin = new Vector2(0f, 0.5f);
            _pillBodyRect.anchorMax = new Vector2(0f, 0.5f);
            _pillBodyRect.pivot = new Vector2(0f, 0.5f);
            _pillBodyRect.anchoredPosition = Vector2.zero;
            _pillBodyRect.sizeDelta = new Vector2(CollapsedWidth, PillHeight);

            _pillBgImage = pillGo.AddComponent<Image>();
            _pillBgImage.sprite = ToastProceduralSprites.GetCapsulePill();
            _pillBgImage.type = Image.Type.Sliced;
            _pillBgImage.raycastTarget = true;
            pillGo.AddComponent<RectMask2D>();

            // Glint sweep inside mask
            var glintGo = CreateChild("Glint", _pillBodyRect);
            _glintRect = glintGo.GetComponent<RectTransform>();
            _glintRect.anchorMin = new Vector2(0f, 0.5f);
            _glintRect.anchorMax = new Vector2(0f, 0.5f);
            _glintRect.pivot = new Vector2(0.5f, 0.5f);
            _glintRect.sizeDelta = new Vector2(22f, 90f);
            _glintRect.localRotation = Quaternion.Euler(0f, 0f, -25f);
            _glintRect.anchoredPosition = new Vector2(-40f, 0f);
            _glintImage = glintGo.AddComponent<Image>();
            _glintImage.sprite = ToastProceduralSprites.GetGlintStrip();
            _glintImage.color = new Color(1f, 1f, 1f, 0f);
            _glintImage.raycastTarget = false;

            // Icon Slot (left 54px area)
            var iconGo = CreateChild("Icon", _pillBodyRect);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(27f, 0f);
            iconRect.sizeDelta = new Vector2(26f, 26f);
            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.raycastTarget = false;

            // Text Box
            var textBoxGo = CreateChild("TextBox", _pillBodyRect);
            _textBoxRect = textBoxGo.GetComponent<RectTransform>();
            _textBoxRect.anchorMin = new Vector2(0f, 0.5f);
            _textBoxRect.anchorMax = new Vector2(0f, 0.5f);
            _textBoxRect.pivot = new Vector2(0f, 0.5f);
            _textBoxRect.anchoredPosition = new Vector2(54f, 0f);
            _textBoxRect.sizeDelta = new Vector2(244f, 44f);
            _textCanvasGroup = textBoxGo.AddComponent<CanvasGroup>();
            _textCanvasGroup.alpha = 0f;

            _titleText = CreateTmpText("Title", _textBoxRect, font, 14f, FontStyles.Bold, Color.white, new Vector2(0f, 8f), new Vector2(244f, 22f));
            _subText = CreateTmpText("Sub", _textBoxRect, font, 11f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.88f), new Vector2(0f, -10f), new Vector2(244f, 18f));

            // Mini Close Button (right side of expanded pill)
            var closeGo = CreateChild("CloseMiniBtn", _pillBodyRect);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-12f, 0f);
            closeRect.sizeDelta = new Vector2(22f, 22f);

            var closeBg = closeGo.AddComponent<Image>();
            closeBg.sprite = ToastProceduralSprites.GetCircle();
            closeBg.color = new Color(0f, 0f, 0f, 0.28f);

            _closeMiniBtn = closeGo.AddComponent<Button>();
            _closeMiniBtn.targetGraphic = closeBg;
            _closeMiniBtn.onClick.AddListener(() =>
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
            closeIconImg.color = Color.white;
            closeIconImg.raycastTarget = false;

            // Pill White Outline (drawn on top of pill contents)
            var outlineGo = CreateChild("PillOutline", _pillBodyRect);
            var outlineRect = outlineGo.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            var outlineImg = outlineGo.AddComponent<Image>();
            outlineImg.sprite = ToastProceduralSprites.GetCapsuleOutline();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = ToastThemePalette.Hex("#FFFDF8");
            outlineImg.raycastTarget = false;

            // 4. Three Twinkling 4-Point Sparkles around badge
            Vector2[] spPos =
            {
                new Vector2(46f, -3f),
                new Vector2(8f, -49f),
                new Vector2(2f, -15f)
            };
            float[] spSizes = { 13f, 11f, 10f };
            _sparkles = new Image[3];
            _sparkleTweens = new Tween[3];

            for (int i = 0; i < 3; i++)
            {
                var spGo = CreateChild("Sparkle_" + i, _rootRect);
                var spRect = spGo.GetComponent<RectTransform>();
                spRect.anchorMin = new Vector2(0f, 1f);
                spRect.anchorMax = new Vector2(0f, 1f);
                spRect.pivot = new Vector2(0.5f, 0.5f);
                spRect.anchoredPosition = spPos[i];
                spRect.sizeDelta = new Vector2(spSizes[i], spSizes[i]);

                var spImg = spGo.AddComponent<Image>();
                spImg.sprite = ToastProceduralSprites.GetSparkle4Point();
                spImg.color = ToastThemePalette.Hex("#FEF08A");
                spImg.raycastTarget = false;
                _sparkles[i] = spImg;
            }
        }

        private void ApplyThemeAndContent()
        {
            if (_state == null || _state.Preset == null) return;
            ToastPresetData preset = _state.Preset;
            ToastThemePalette palette = ToastThemePalette.Get(preset.Tone);

            _pillBgImage.color = palette.PillBackground;
            _auraGlowImage.color = palette.AuraGlowColor;
            _auraRingImage.color = palette.AuraRingColor;
            _iconImage.sprite = ToastProceduralSprites.GetIcon(preset.IconType);
            _iconImage.color = palette.IconTint;
            _titleText.text = preset.Title;
            _subText.text = preset.Sub;
        }

        private void PlayEnterAnimation()
        {
            _enterTween?.Kill();
            _rootCanvasGroup.alpha = 0f;
            _rootRect.anchoredPosition = new Vector2(-14f, 0f);
            _enterTween = DOTween.Sequence()
                .Join(_rootCanvasGroup.DOFade(1f, 0.34f).SetEase(Ease.OutQuart))
                .Join(_rootRect.DOAnchorPosX(0f, 0.34f).SetEase(Ease.OutQuart))
                .SetLink(gameObject);
        }

        private void StartAmbientTweens()
        {
            RestartAuraTweens();

            // Sparkle twinkles
            float[] delays = { 0f, 0.7f, 1.35f };
            for (int i = 0; i < _sparkles.Length; i++)
            {
                _sparkleTweens[i]?.Kill();
                Color c = _sparkles[i].color;
                c.a = 0.15f;
                _sparkles[i].color = c;
                _sparkleTweens[i] = _sparkles[i]
                    .DOFade(1f, 1.0f)
                    .SetDelay(delays[i])
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            }

            // Diagonal Glint Sweep
            _glintSequence?.Kill();
            _glintSequence = DOTween.Sequence()
                .AppendCallback(() =>
                {
                    _glintRect.anchoredPosition = new Vector2(-35f, 0f);
                    _glintImage.color = new Color(1f, 1f, 1f, 0f);
                })
                .AppendInterval(1.6f)
                .Append(_glintImage.DOFade(0.75f, 0.25f).SetEase(Ease.OutQuad))
                .Join(_glintRect.DOAnchorPosX(ExpandedWidth + 30f, 1.0f).SetEase(Ease.InOutSine))
                .Insert(2.15f, _glintImage.DOFade(0f, 0.35f).SetEase(Ease.InQuad))
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        private void RestartAuraTweens()
        {
            if (_auraGlowImage == null || _auraRingImage == null || _state == null) return;
            ToastThemePalette palette = ToastThemePalette.Get(_state.Preset.Tone);

            _auraGlowTween?.Kill();
            _auraGlowRect.localScale = new Vector3(0.92f, 0.92f, 1f);
            Color gCol = palette.AuraGlowColor;
            gCol.a = 0.45f;
            _auraGlowImage.color = gCol;

            _auraGlowTween = DOTween.Sequence()
                .Join(_auraGlowRect.DOScale(1.28f, _pulseDuration * 0.5f).SetEase(Ease.InOutSine))
                .Join(_auraGlowImage.DOFade(0.14f, _pulseDuration * 0.5f).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);

            _auraRingTween?.Kill();
            _auraRingRect.localScale = new Vector3(0.96f, 0.96f, 1f);
            Color rCol = palette.AuraRingColor;
            rCol.a = 0.50f;
            _auraRingImage.color = rCol;

            _auraRingTween = DOTween.Sequence()
                .Join(_auraRingRect.DOScale(1.70f, _pulseDuration).SetEase(Ease.OutCubic))
                .Join(_auraRingImage.DOFade(0f, _pulseDuration).SetEase(Ease.OutCubic))
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        private void UpdateAuraForExpandedState(bool expanded, float duration)
        {
            if (_auraGlowRect == null) return;
            _auraExpandTween?.Kill();

            Vector2 targetSize = expanded ? new Vector2(ExpandedWidth + 16f, 76f) : new Vector2(76f, 76f);
            Vector2 targetPos = expanded ? new Vector2(ExpandedWidth * 0.5f, 0f) : new Vector2(27f, 0f);

            if (duration <= 0f)
            {
                _auraGlowRect.sizeDelta = targetSize;
                _auraGlowRect.anchoredPosition = targetPos;
            }
            else
            {
                _auraExpandTween = DOTween.Sequence()
                    .Join(_auraGlowRect.DOSizeDelta(targetSize, duration).SetEase(Ease.OutQuart))
                    .Join(_auraGlowRect.DOAnchorPos(targetPos, duration).SetEase(Ease.OutQuart))
                    .SetLink(gameObject);
            }
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
            _enterTween?.Kill();
            _morphTween?.Kill();
            _textTween?.Kill();
            _auraGlowTween?.Kill();
            _auraExpandTween?.Kill();
            _auraRingTween?.Kill();
            _glintSequence?.Kill();
            if (_sparkleTweens != null)
            {
                for (int i = 0; i < _sparkleTweens.Length; i++)
                {
                    _sparkleTweens[i]?.Kill();
                }
            }
        }
    }
}
